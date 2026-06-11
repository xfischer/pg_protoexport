using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace pg_protoexport;

public sealed class PcapBinaryReader(BinaryReader reader, Encoding encoding) : IDisposable
{
    private List<ParsedField>? _recordedFields;
    private long _messageStartPosition;
    private FieldScope? _activeField;

    public uint ReadUInt32()
    {
        var v = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(reader.ReadUInt32()) : reader.ReadUInt32();
        _activeField?.SetValue(v.ToString(CultureInfo.InvariantCulture));
        return v;
    }
    public int ReadInt32()
    {
        var v = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(reader.ReadInt32()) : reader.ReadInt32();
        _activeField?.SetValue(v.ToString(CultureInfo.InvariantCulture));
        return v;
    }
    public ushort ReadUInt16()
    {
        var v = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(reader.ReadUInt16()) : reader.ReadUInt16();
        _activeField?.SetValue(v.ToString(CultureInfo.InvariantCulture));
        return v;
    }
    public short ReadInt16()
    {
        var v = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(reader.ReadInt16()) : reader.ReadInt16();
        _activeField?.SetValue(v.ToString(CultureInfo.InvariantCulture));
        return v;
    }

    public string ReadNullTerminatedString(int maxLength)
    {
        var array = ArrayPool<byte>.Shared.Rent(maxLength);
        try
        {
            int index = 0;
            byte currentByte = 0;
            while ((currentByte = reader.ReadByte()) != 0)
            {
                array[index++] = currentByte;
            }

            var result = encoding.GetString(array, 0, index);
            _activeField?.SetValue(result);
            return result;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    public void ThrowIfEndOfStream(int len)
    {
        if (reader.BaseStream.Position + len > reader.BaseStream.Length)
            throw new EndOfStreamException($"Not sufficient data to read {len} bytes.");
    }

    public bool HasSufficientData(int len) => reader.BaseStream.Position + len <= reader.BaseStream.Length;

    public byte[] ReadBytes(int count)
    {
        var bytes = reader.ReadBytes(count);
        _activeField?.SetValue(Convert.ToHexString(bytes));
        return bytes;
    }

    public char ReadChar()
    {
        var c = reader.ReadChar();
        _activeField?.SetValue(c.ToString());
        return c;
    }

    public byte ReadByte()
    {
        var b = reader.ReadByte();
        _activeField?.SetValue(b.ToString(CultureInfo.InvariantCulture));
        return b;
    }

    public long Seek(long offset, SeekOrigin origin) => reader.BaseStream.Seek(offset, origin);

    public void Dispose()
    {
        ((IDisposable)reader).Dispose();
    }

    internal void BeginMessage()
    {
        _messageStartPosition = reader.BaseStream.Position;
        _recordedFields = new List<ParsedField>();
    }

    internal IReadOnlyList<ParsedField> EndMessage()
    {
        var list = (IReadOnlyList<ParsedField>?)_recordedFields ?? Array.Empty<ParsedField>();
        _recordedFields = null;
        return list;
    }

    internal int MessageStartOffset => (int)_messageStartPosition;

    internal int CurrentStreamOffset => (int)reader.BaseStream.Position;

    public FieldScope BeginField(string name)
    {
        if (_recordedFields is null)
            return FieldScope.NoOp;
        var scope = new FieldScope(this, name, reader.BaseStream.Position - _messageStartPosition, _activeField);
        _activeField = scope;
        return scope;
    }

    private long CurrentMessageOffset => reader.BaseStream.Position - _messageStartPosition;

    private void AddField(ParsedField field) => _recordedFields?.Add(field);

    /// <summary>
    /// Scope returned by <see cref="BeginField"/>. The <c>Read*</c> methods auto-populate
    /// the display value; callers can override it via <see cref="FieldScope.SetValue"/> before disposal
    /// when the bytes should be displayed differently (e.g. text instead of hex).
    /// </summary>
    public sealed class FieldScope : IDisposable
    {
        internal static readonly FieldScope NoOp = new();

        private readonly PcapBinaryReader? _owner;
        private readonly string? _name;
        private readonly long _startOffset;
        private readonly FieldScope? _previous;
        private string? _display;

        internal FieldScope(PcapBinaryReader owner, string name, long startOffset, FieldScope? previous)
        {
            _owner = owner;
            _name = name;
            _startOffset = startOffset;
            _previous = previous;
        }

        private FieldScope() { }

        /// <summary>Set or replace the display value. Last call wins.</summary>
        public void SetValue(string value)
        {
            if (_owner is null) return;
            _display = value;
        }

        public void Dispose()
        {
            if (_owner is null) return;
            _owner._activeField = _previous;
            if (_owner._recordedFields is null) return;
            var endOffset = _owner.CurrentMessageOffset;
            _owner.AddField(new ParsedField(_name!, (int)_startOffset, (int)(endOffset - _startOffset), _display));
        }
    }
}