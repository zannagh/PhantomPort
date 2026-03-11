namespace PhantomPort;

public static class VirtualSerialPortFactory
{
    /// <summary>
    /// Creates a new virtual serial port that appears as a real device to the operating system.
    /// The port remains active until the returned object is disposed.
    /// </summary>
    /// <remarks>
    /// On Linux/macOS: Uses POSIX pseudo-terminals (PTY). The slave device appears as a real
    /// terminal device file (e.g., /dev/pts/3 or /dev/ttys005).
    ///
    /// On Windows: Uses a named pipe, optionally mapped to a COM port name via DefineDosDevice.
    /// Note that serial-specific IOCTLs (baud rate, flow control) are not supported — only
    /// raw data transfer works. The pipe blocks until a client connects.
    /// </remarks>
    public static VirtualSerialPort Create()
    {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return PosixVirtualSerialPort.Create();
        }

        if (OperatingSystem.IsWindows())
        {
            return WindowsVirtualSerialPort.Create();
        }

        throw new PlatformNotSupportedException(
            "Virtual serial port creation is supported on Linux, macOS, and Windows only.");
    }
}
