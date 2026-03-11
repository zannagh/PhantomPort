using System.IO.Pipes;
using PhantomPort.Interop;

namespace PhantomPort;

/// <summary>
/// Creates a virtual serial port on Windows backed by a named pipe.
/// Optionally maps the pipe to a COM port name via DefineDosDevice for compatibility
/// with applications that expect a COMx path.
/// </summary>
/// <remarks>
/// Limitations: Serial-specific IOCTLs (baud rate, flow control, etc.) are not supported
/// since the underlying transport is a named pipe. Applications that only perform read/write
/// operations will work correctly.
/// </remarks>
internal sealed class WindowsVirtualSerialPort : VirtualSerialPort
{
    private NamedPipeServerStream _pipe = null!;
    internal string PipeName = null!;
    private string? _comPortName;
    private WindowsPipeStream _stream = null!;
    private bool _disposed;

    public override Stream Stream => _stream;

    private void Initialize(NamedPipeServerStream pipe, string pipeName, string? comPortName)
    {
        InitializePort(comPortName ?? $@"\\.\pipe\{pipeName}");
        _pipe = pipe;
        PipeName = pipeName;
        _comPortName = comPortName;
        _stream = new WindowsPipeStream(pipe);
    }

    internal static WindowsVirtualSerialPort Create()
    {
        var pipeName = $"PhantomPort_{Guid.NewGuid():N}";

        var pipe = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

        try
        {
            var comPortName = TryMapComPort(pipeName);
            var port = CreateUninitializedInstance<WindowsVirtualSerialPort>();
            port.Initialize(pipe, pipeName, comPortName);
            return port;
        }
        catch
        {
            pipe.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Attempts to map the named pipe to an unused COM port via DefineDosDevice.
    /// Starts at COM50 to avoid conflicts with physical ports.
    /// Returns the COM port name (e.g., "COM50") on success, null if no mapping could be created.
    /// </summary>
    private static string? TryMapComPort(string pipeName)
    {
        var target = $@"\Device\NamedPipe\{pipeName}";

        for (var i = 50; i <= 255; i++)
        {
            var name = $"COM{i}";

            if (Windows.DosDeviceExists(name))
                continue;

            if (Windows.DefineDosDeviceW(Windows.DDD_RAW_TARGET_PATH, name, target))
                return name;
        }

        return null;
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        if (_comPortName is not null)
        {
            Windows.DefineDosDeviceW(
                Windows.DDD_REMOVE_DEFINITION | Windows.DDD_RAW_TARGET_PATH,
                _comPortName,
                $@"\Device\NamedPipe\{PipeName}");
        }

        _pipe.Dispose();
        base.Dispose(disposing);
    }
}
