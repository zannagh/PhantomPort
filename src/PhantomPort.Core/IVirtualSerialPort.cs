using System.IO.Ports;

namespace PhantomPort;

/// <summary>
/// Represents a virtual serial port that appears as a real device to the operating system.
/// </summary>
public interface IVirtualSerialPort : IDisposable
{
    /// <summary>
    /// The device path that external applications can use to connect to this virtual port.
    /// On Linux: e.g., /dev/pts/3
    /// On macOS: e.g., /dev/ttys005
    /// </summary>
    public abstract string DevicePath { get; }

    /// <summary>
    /// A stream for reading from and writing to the internal (master) side of the virtual port.
    /// Data written here becomes readable on the device side, and vice versa.
    /// </summary>
    public abstract Stream Stream { get; }

    public abstract void Dispose();
}