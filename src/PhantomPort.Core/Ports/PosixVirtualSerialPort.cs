using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using PhantomPort.Interop;

namespace PhantomPort;

/// <summary>
/// Creates a virtual serial port using the POSIX pseudo-terminal (PTY) API.
/// The slave side of the PTY appears as a real device file that external applications can open.
/// </summary>
internal sealed class PosixVirtualSerialPort : VirtualSerialPort
{
    private int _masterFd;
    private int _slaveFd;
    private PosixDeviceStream _stream = null!;
    private bool _disposed;

    public override Stream Stream => _stream;

    private void Initialize(int masterFd, int slaveFd, string devicePath)
    {
        InitializePort(devicePath);
        _masterFd = masterFd;
        _slaveFd = slaveFd;
        _stream = new PosixDeviceStream(masterFd);
    }

    internal static PosixVirtualSerialPort Create()
    {
        var flags = Posix.O_RDWR | Posix.NocttyFlag;

        var masterFd = Posix.posix_openpt(flags);
        if (masterFd < 0)
        {
            ThrowPosixError("posix_openpt");
        }

        try
        {
            if (Posix.grantpt(masterFd) != 0)
            {
                ThrowPosixError("grantpt");
            }

            if (Posix.unlockpt(masterFd) != 0)
            {
                ThrowPosixError("unlockpt");
            }

            var namePtr = Posix.ptsname(masterFd);
            if (namePtr == 0)
            {
                ThrowPosixError("ptsname");
            }

            var devicePath = Marshal.PtrToStringAnsi(namePtr) ?? throw new IOException("ptsname returned null");

            // Open the slave and keep it open for the lifetime of the port.
            // This prevents the master from receiving EIO when no external application
            // has the slave open, keeping the PTY fully operational at all times.
            var slaveFd = Posix.open(devicePath, flags);
            if (slaveFd < 0)
            {
                ThrowPosixError("open(slave)");
            }

            try
            {
                ConfigureRawMode(slaveFd);
                var port = CreateUninitializedInstance<PosixVirtualSerialPort>();
                port.Initialize(masterFd, slaveFd, devicePath);
                return port;
            }
            catch
            {
                Posix.close(slaveFd);
                throw;
            }
        }
        catch
        {
            Posix.close(masterFd);
            throw;
        }
    }

    /// <summary>
    /// Configures the slave PTY in raw mode (no echo, no line buffering, no signal processing)
    /// so it behaves like a serial port rather than an interactive terminal.
    /// </summary>
    /// <remarks>
    /// On macOS (BSD), tcgetattr only works on the slave side of a PTY, not the master.
    /// Configuring the slave is correct on both Linux and macOS.
    /// Uses an opaque byte buffer for the termios struct to avoid needing platform-specific
    /// struct definitions — the native tcgetattr/cfmakeraw/tcsetattr functions handle the
    /// layout internally. 256 bytes is well above the largest known termios struct (~72 bytes on macOS).
    /// </remarks>
    private static void ConfigureRawMode(int fd)
    {
        var termiosPtr = Marshal.AllocHGlobal(256);
        try
        {
            if (Posix.tcgetattr(fd, termiosPtr) != 0)
                ThrowPosixError("tcgetattr");

            Posix.cfmakeraw(termiosPtr);

            if (Posix.tcsetattr(fd, Posix.TCSANOW, termiosPtr) != 0)
                ThrowPosixError("tcsetattr");
        }
        finally
        {
            Marshal.FreeHGlobal(termiosPtr);
        }
    }

    [DoesNotReturn]
    private static void ThrowPosixError(string function)
    {
        var errno = Marshal.GetLastPInvokeError();
        throw new IOException($"{function} failed (errno: {errno})");
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _stream.MarkDisposed();
        Posix.close(_slaveFd);
        Posix.close(_masterFd);
        base.Dispose(disposing);
    }
}
