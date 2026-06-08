namespace pg_protoexport;

/// <summary>
/// Parsed PostgreSQL binary-COPY file header (see <see href="https://www.postgresql.org/docs/18/sql-copy.html"/>,
/// "Binary Format"). Populated on the FIRST <see cref="CopyDataMessage"/> of a
/// <c>COPY ... WITH (FORMAT BINARY)</c> stream (CopyIn or CopyOut). Replication streams
/// (<see cref="CopyBothResponseMessage"/>) carry the streaming-replication sub-protocol, not a
/// binary-COPY file stream, so no header is parsed for them.
/// </summary>
/// <param name="Signature">11-byte magic. Should equal <c>50 47 43 4F 50 59 0A FF 0D 0A 00</c>
/// (<c>"PGCOPY\n\377\r\n\0"</c>).</param>
/// <param name="SignatureValid">True when <paramref name="Signature"/> matches the expected bytes.</param>
/// <param name="Flags">32-bit critical-flags field. PG 12+ reserves all bits — value is zero on the wire.</param>
/// <param name="HeaderExtensionLength">Number of extension bytes that follow the flags field on the wire.</param>
/// <param name="HeaderExtensionPreview">First few bytes of the extension area, captured up to the
/// preview budget. Null when <paramref name="HeaderExtensionLength"/> is zero.</param>
public sealed record CopyBinaryHeader(
    byte[] Signature,
    bool SignatureValid,
    uint Flags,
    int HeaderExtensionLength,
    byte[]? HeaderExtensionPreview)
{
    /// <summary>Canonical magic bytes (<c>"PGCOPY\n\377\r\n\0"</c>).</summary>
    public static ReadOnlySpan<byte> ExpectedSignature => [0x50, 0x47, 0x43, 0x4F, 0x50, 0x59, 0x0A, 0xFF, 0x0D, 0x0A, 0x00];
}
