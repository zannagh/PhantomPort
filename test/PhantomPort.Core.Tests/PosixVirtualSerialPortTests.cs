using System.IO.Ports;
using System.Text;
using PhantomPort.Interop;
using Xunit;

namespace PhantomPort.Core.Tests;

public class PosixVirtualSerialPortTests
{
    [SkippableFact]
    public void Create_ReturnsCorrectImplementation()
    {
        Skip.IfNot(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS());

        using var port = VirtualSerialPortFactory.Create();

        Assert.IsType<PosixVirtualSerialPort>(port);
    }

    [SkippableFact]
    public void DevicePath_IsValidDeviceFile()
    {
        Skip.IfNot(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS());

        using var port = VirtualSerialPortFactory.Create();

        Assert.StartsWith("/dev/", port.DevicePath);
        Assert.True(File.Exists(port.DevicePath), $"Device file should exist: {port.DevicePath}");
    }

    [SkippableFact]
    public void SlaveToMaster_DataFlowsCorrectly()
    {
        Skip.IfNot(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS());

        using var port = VirtualSerialPortFactory.Create();

        var testData = Encoding.UTF8.GetBytes("Stream roundtrip test");

        var slaveFd = Posix.open(port.DevicePath, Posix.O_RDWR | Posix.NocttyFlag);
        Assert.True(slaveFd >= 0);

        try
        {
            Posix.write(slaveFd, testData, testData.Length);

            var buffer = new byte[testData.Length];
            var totalRead = 0;
            while (totalRead < testData.Length)
            {
                var read = port.Stream.Read(buffer, totalRead, buffer.Length - totalRead);
                if (read == 0) break;
                totalRead += read;
            }

            Assert.Equal(testData, buffer);
        }
        finally
        {
            Posix.close(slaveFd);
        }
    }

    [SkippableFact]
    public void VirtualPort_AppearsInGetPortNames()
    {
        Skip.IfNot(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS());
        Skip.IfNot(TestEnvironment.IsElevated,
            "Requires root — run with sudo for system-level serial port discovery");

        using var port = VirtualSerialPortFactory.Create();

        var portNames = SerialPort.GetPortNames();

        Assert.Contains(port.DevicePath, portNames);
    }

    [SkippableFact]
    public void MasterToSlave_DataFlowsCorrectly()
    {
        Skip.IfNot(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS());

        using var port = VirtualSerialPortFactory.Create();

        var testData = Encoding.UTF8.GetBytes("Master to slave");

        port.Stream.Write(testData, 0, testData.Length);
        port.Stream.Flush();

        var slaveFd = Posix.open(port.DevicePath, Posix.O_RDWR | Posix.NocttyFlag);
        Assert.True(slaveFd >= 0);

        try
        {
            var buffer = new byte[testData.Length];
            var totalRead = 0;
            while (totalRead < testData.Length)
            {
                var span = buffer.AsSpan(totalRead);
                var read = Posix.read(slaveFd, span, span.Length);
                if (read <= 0) break;
                totalRead += (int)read;
            }

            Assert.Equal(testData, buffer);
        }
        finally
        {
            Posix.close(slaveFd);
        }
    }
}
