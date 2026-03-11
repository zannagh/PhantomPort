using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;

namespace PhantomPort;

/// <summary>
/// Abstract base class for virtual serial ports. Extends <see cref="SerialPort"/> and hides all
/// of its members with <c>new</c>, providing placeholder implementations that don't throw
/// <see cref="PlatformNotSupportedException"/>.
/// </summary>
/// <remarks>
/// Instances must be created via <see cref="CreateUninitializedInstance{T}"/> followed by
/// <see cref="InitializePort"/> to bypass the <see cref="SerialPort"/> constructor, which throws
/// <see cref="PlatformNotSupportedException"/> on macOS.
/// </remarks>
public abstract class VirtualSerialPort : SerialPort, IVirtualSerialPort
{
    // Backing fields for serial port properties.
    // These are NOT initialized via field initializers because instances are created with
    // RuntimeHelpers.GetUninitializedObject (which skips all constructors and initializers).
    // Instead, they are set in InitializePort().
    private int _baudRate;
    private bool _breakState;
    private int _dataBits;
    private bool _discardNull;
    private bool _dtrEnable;
    private Encoding _encoding = null!;
    private Handshake _handshake;
    private bool _isOpen;
    private string _newLine = null!;
    private Parity _parity;
    private byte _parityReplace;
    private string _portName = null!;
    private int _readBufferSize;
    private int _readTimeout;
    private int _receivedBytesThreshold;
    private bool _rtsEnable;
    private StopBits _stopBits;
    private int _writeBufferSize;
    private int _writeTimeout;

    private string? _nextLine;

    // --- IVirtualSerialPort implementation ---

    /// <inheritdoc />
    public string DevicePath { get; private set; } = null!;

    /// <inheritdoc />
    public abstract Stream Stream { get; }

    // --- Instance creation ---

    /// <summary>
    /// Creates an instance of a <see cref="VirtualSerialPort"/>-derived type using
    /// <see cref="RuntimeHelpers.GetUninitializedObject"/> to bypass the <see cref="SerialPort"/>
    /// constructor chain. The returned instance must be initialized via <see cref="InitializePort"/>.
    /// </summary>
    protected static T CreateUninitializedInstance<T>() where T : VirtualSerialPort
    {
        return (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    }

    /// <summary>
    /// Sets default property values. Must be called after <see cref="CreateUninitializedInstance{T}"/>
    /// since field initializers and constructors are skipped.
    /// </summary>
    protected void InitializePort(string devicePath)
    {
        DevicePath = devicePath;
        _baudRate = 9600;
        _dataBits = 8;
        _encoding = Encoding.ASCII;
        _newLine = "\n";
        _parity = Parity.None;
        _parityReplace = (byte)'?';
        _portName = devicePath;
        _readBufferSize = 4096;
        _readTimeout = InfiniteTimeout;
        _receivedBytesThreshold = 1;
        _stopBits = StopBits.One;
        _writeBufferSize = 2048;
        _writeTimeout = InfiniteTimeout;
    }

    // --- Hiding SerialPort properties ---

    public new Stream BaseStream => Stream;

    public new int BaudRate
    {
        get => _baudRate;
        set => _baudRate = value;
    }

    public new bool BreakState
    {
        get => _breakState;
        set => _breakState = value;
    }

    public new int BytesToRead => 0; // TODO

    public new int BytesToWrite => 0; // TODO

    public new bool CDHolding => false; // TODO

    public new bool CtsHolding => false; // TODO

    public new int DataBits
    {
        get => _dataBits;
        set => _dataBits = value;
    }

    public new bool DiscardNull
    {
        get => _discardNull;
        set => _discardNull = value;
    }

    public new bool DsrHolding => false; // TODO

    public new bool DtrEnable
    {
        get => _dtrEnable;
        set => _dtrEnable = value;
    }

    public new Encoding Encoding
    {
        get => _encoding;
        set => _encoding = value;
    }

    public new Handshake Handshake
    {
        get => _handshake;
        set => _handshake = value;
    }

    public new bool IsOpen => _isOpen;

    public new string NewLine
    {
        get => _newLine;
        set => _newLine = value;
    }

    public new Parity Parity
    {
        get => _parity;
        set => _parity = value;
    }

    public new byte ParityReplace
    {
        get => _parityReplace;
        set => _parityReplace = value;
    }

    public new string PortName
    {
        get => _portName;
        set => _portName = value;
    }

    public new int ReadBufferSize
    {
        get => _readBufferSize;
        set => _readBufferSize = value;
    }

    public new int ReadTimeout
    {
        get => _readTimeout;
        set => _readTimeout = value;
    }

    public new int ReceivedBytesThreshold
    {
        get => _receivedBytesThreshold;
        set => _receivedBytesThreshold = value;
    }

    public new bool RtsEnable
    {
        get => _rtsEnable;
        set => _rtsEnable = value;
    }

    public new StopBits StopBits
    {
        get => _stopBits;
        set => _stopBits = value;
    }

    public new int WriteBufferSize
    {
        get => _writeBufferSize;
        set => _writeBufferSize = value;
    }

    public new int WriteTimeout
    {
        get => _writeTimeout;
        set => _writeTimeout = value;
    }

    // --- Hiding SerialPort events ---

#pragma warning disable CS0067 // Event is never used — placeholders for future mock behavior
    public new event SerialDataReceivedEventHandler? DataReceived;
    public new event SerialErrorReceivedEventHandler? ErrorReceived;
    public new event SerialPinChangedEventHandler? PinChanged;
#pragma warning restore CS0067

    // --- Hiding SerialPort methods ---

    public new void Open() => _isOpen = true; // TODO

    public new void Close() => _isOpen = false; // TODO

    public new void DiscardInBuffer() { } // TODO

    public new void DiscardOutBuffer() { } // TODO

    public new int Read(byte[] buffer, int offset, int count) =>
        Stream.Read(buffer, offset, count);

    public new int Read(char[] buffer, int offset, int count) =>
        throw new NotImplementedException(); // TODO

    public new int ReadByte() =>
        throw new NotImplementedException(); // TODO

    public new int ReadChar() =>
        throw new NotImplementedException(); // TODO

    public new string ReadExisting() =>
        throw new NotImplementedException(); // TODO

    public new string ReadLine() => _nextLine ?? string.Empty;

    public new string ReadTo(string value) =>
        throw new NotImplementedException(); // TODO

    public new void Write(byte[] buffer, int offset, int count) =>
        Stream.Write(buffer, offset, count);

    public new void Write(char[] buffer, int offset, int count) =>
        throw new NotImplementedException(); // TODO

    public new void Write(string text) =>
        throw new NotImplementedException(); // TODO

    public new void WriteLine(string text) =>
        throw new NotImplementedException(); // TODO

    public new static string[] GetPortNames() => []; // TODO

    // --- Mocking helpers ---

    public void SetNextLine(string nextLine) => _nextLine = nextLine;

    // --- Dispose pattern ---

    protected override void Dispose(bool disposing)
    {
        // Derived classes override this for cleanup.
        // base.Dispose(disposing) is safe even with GetUninitializedObject —
        // Component.Dispose(bool) handles null fields gracefully.
        base.Dispose(disposing);
    }
}
