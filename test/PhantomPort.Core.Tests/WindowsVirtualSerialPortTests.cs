using System.IO.Pipes;
using System.IO.Ports;
using System.Text;
using Xunit;

namespace PhantomPort.Core.Tests;

public class WindowsVirtualSerialPortTests
{
    [SkippableFact]
    public void Create_ReturnsCorrectImplementation()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        using var port = VirtualSerialPortFactory.Create();

        Assert.IsType<WindowsVirtualSerialPort>(port);
    }

    [SkippableFact]
    public void DevicePath_IsComOrPipePath()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        using var port = VirtualSerialPortFactory.Create();

        Assert.True(
            port.DevicePath.StartsWith("COM") || port.DevicePath.StartsWith(@"\\.\pipe\"),
            $"Unexpected DevicePath format: {port.DevicePath}");
    }

    [SkippableFact]
    public void VirtualPort_AppearsInGetPortNames()
    {
        Skip.IfNot(OperatingSystem.IsWindows());
        Skip.IfNot(TestEnvironment.IsElevated,
            "Requires Administrator — run as admin for system-level serial port discovery");

        using var port = VirtualSerialPortFactory.Create();

        var portNames = SerialPort.GetPortNames();

        Assert.Contains(port.DevicePath, portNames);
    }

    [SkippableFact]
    public async Task DataRoundtrip_ViaNamedPipe()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        using var port = VirtualSerialPortFactory.Create();

        var windowsPort = Assert.IsType<WindowsVirtualSerialPort>(port);
        var testData = Encoding.UTF8.GetBytes("Windows roundtrip");

        using var client = new NamedPipeClientStream(".", windowsPort.PipeName, PipeDirection.InOut);

        var readTask = Task.Run(() =>
        {
            var buffer = new byte[testData.Length];
            var totalRead = 0;
            while (totalRead < testData.Length)
            {
                var read = port.Stream.Read(buffer, totalRead, buffer.Length - totalRead);
                if (read == 0) break;
                totalRead += read;
            }
            return buffer;
        });

        await client.ConnectAsync(5000);
        await client.WriteAsync(testData);
        await client.FlushAsync();

        var buffer = await readTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(testData, buffer);
    }
}
