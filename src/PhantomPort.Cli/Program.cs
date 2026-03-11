using System.Text;
using PhantomPort;

/// <summary>
/// Demo console application that creates a virtual serial port and listens for incoming data.
/// </summary>
/// <remarks>
/// For the virtual port to appear in <c>SerialPort.GetPortNames()</c> (so that other applications
/// can discover it), this process must run with elevated permissions (sudo on macOS/Linux, admin
/// on Windows). Without elevation, the port still works — connect to it directly via the printed
/// device path.
/// </remarks>
internal class Program
{
    public static void Main(string[] args)
    {
        using var port = VirtualSerialPortFactory.Create();

        Console.WriteLine($"Virtual serial port created: {port.DevicePath}");
        Console.WriteLine();
        Console.WriteLine("Other applications can now connect to this device path.");
        Console.WriteLine($"  Example: screen {port.DevicePath}");
        Console.WriteLine();
        Console.WriteLine("Press Ctrl+C to close the port and exit.");
        Console.WriteLine();

        using var done = new ManualResetEventSlim();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            done.Set();
        };

// Read from the master side in a background thread.
// This drains incoming data (preventing slave-side writes from blocking)
// and displays it for debugging.
        var readerThread = new Thread(() =>
        {
            var buffer = new byte[4096];
            try
            {
                while (true)
                {
                    var bytesRead = port.Stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0) break;

                    var text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.Write($"[RX] {text}");
                }
            }
            catch (IOException)
            {
                // Expected when the port is closed during shutdown.
            }
        })
        {
            IsBackground = true,
            Name = "PTY Reader"
        };
        readerThread.Start();

        done.Wait();
        Console.WriteLine("Port closed.");
    }
}
