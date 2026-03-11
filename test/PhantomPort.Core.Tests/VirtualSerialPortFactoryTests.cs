using Xunit;

namespace PhantomPort.Core.Tests;

public class VirtualSerialPortFactoryTests
{
    [Fact]
    public void Create_ReturnsFunctionalPort()
    {
        using var port = VirtualSerialPortFactory.Create();

        Assert.NotNull(port);
        Assert.NotNull(port.DevicePath);
        Assert.NotEmpty(port.DevicePath);
        Assert.NotNull(port.Stream);
    }

    [Fact]
    public void Create_MultiplePortsHaveDifferentDevicePaths()
    {
        using var port1 = VirtualSerialPortFactory.Create();
        using var port2 = VirtualSerialPortFactory.Create();

        Assert.NotEqual(port1.DevicePath, port2.DevicePath);
    }

    [Fact]
    public void Create_PortStreamCanReadAndWrite()
    {
        using var port = VirtualSerialPortFactory.Create();

        Assert.True(port.Stream.CanRead);
        Assert.True(port.Stream.CanWrite);
        Assert.False(port.Stream.CanSeek);
    }

    [Fact]
    public void Create_PortStreamThrowsOnSeek()
    {
        using var port = VirtualSerialPortFactory.Create();

        Assert.Throws<NotSupportedException>(() => port.Stream.Seek(0, SeekOrigin.Begin));
        Assert.Throws<NotSupportedException>(() => port.Stream.SetLength(0));
        Assert.Throws<NotSupportedException>(() => port.Stream.Length);
        Assert.Throws<NotSupportedException>(() => port.Stream.Position);
        Assert.Throws<NotSupportedException>(() => port.Stream.Position = 0);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var port = VirtualSerialPortFactory.Create();

        port.Dispose();
        port.Dispose(); // Should not throw
    }
}
