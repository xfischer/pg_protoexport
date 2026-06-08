
namespace pg_protoexport;

public sealed class PcapPostgresOptions
{

    /// <summary>
    /// Access to the message registry configuration, where you can declare Front End or Back End messages.
    /// Any message added here will be detected and a processor should be also configured using <see cref="CustomMessageProcessor"/>
    /// </summary>
    public IPostgresMessageRegistry MessageCatalog { get; set; } = new PostgresMessageRegistry();

    /// <summary>
    /// Delegate called to when no conversion was found for a given <see cref="PostgresMessageDescriptor"/>.
    /// Implementers should return an <see cref="PostgresMessageBase"/> instance or <c>null</c> when message can't be parsed
    /// </summary>
    public Func<PostgresMessageDescriptor, ParserInfo, PostgresMessageBase?>? CustomMessageProcessor { get; set; }

    /// <summary>
    /// When enabled, the parser records (name, offset, length) metadata for each field it reads,
    /// exposed on <see cref="PostgresMessageBase.ParsedFields"/>. Off by default to keep existing
    /// exporters byte-identical and avoid per-field allocations.
    /// </summary>
    public bool RecordFieldMetadata { get; set; } = false;
}

public static class PcapPostgresOptionsExtensions
{
    public static PcapPostgresOptions AddDefaultPostgresMessages(this PcapPostgresOptions options)
    {
        options.MessageCatalog.AddOrReplaceBackendMessage(new('R', "AuthenticationRequest", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('K', "BackendKeyData", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('2', "BindComplete", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('3', "CloseComplete", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('C', "CommandComplete", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('d', "CopyData", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('c', "CopyDone", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('W', "CopyBothResponse", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('G', "CopyInResponse", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('H', "CopyOutResponse", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('D', "DataRow", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('I', "EmptyQueryResponse", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('E', "ErrorResponse", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('F', "FunctionCall", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('V', "FunctionCallResponse", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('n', "NoData", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('N', "NoticeResponse", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('A', "NotificationResponse", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('t', "ParameterDescription", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('S', "ParameterStatus", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('1', "ParseComplete", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new(' ', "PasswordPacket", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('s', "PortalSuspended", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('Z', "ReadyForQuery", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('T', "RowDescription", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceBackendMessage(new('v', "NegotiateProtocolVersion", IsFrontEnd: false));

        options.MessageCatalog.AddOrReplaceBackendMessage(new('?', "StartupMessage", IsFrontEnd: false));
        options.MessageCatalog.AddOrReplaceFrontendMessage(new('?', "StartupMessage", IsFrontEnd: true));

        options.MessageCatalog.AddOrReplaceFrontendMessage(new('D', "Describe", IsFrontEnd: true));
        options.MessageCatalog.AddOrReplaceFrontendMessage(new('S', "Sync", IsFrontEnd: true));
        options.MessageCatalog.AddOrReplaceFrontendMessage(new('E', "Execute", IsFrontEnd: true));
        options.MessageCatalog.AddOrReplaceFrontendMessage(new('P', "Parse", IsFrontEnd: true));
        options.MessageCatalog.AddOrReplaceFrontendMessage(new('B', "Bind", IsFrontEnd: true));
        options.MessageCatalog.AddOrReplaceFrontendMessage(new('C', "Close", IsFrontEnd: true));
        options.MessageCatalog.AddOrReplaceFrontendMessage(new('Q', "Query", IsFrontEnd: true));
        options.MessageCatalog.AddOrReplaceFrontendMessage(new('d', "CopyData", IsFrontEnd: true));
        options.MessageCatalog.AddOrReplaceFrontendMessage(new('c', "CopyDone", IsFrontEnd: true));
        options.MessageCatalog.AddOrReplaceFrontendMessage(new('f', "CopyFail", IsFrontEnd: true));
        options.MessageCatalog.AddOrReplaceFrontendMessage(new('X', "Terminate", IsFrontEnd: true));
        options.MessageCatalog.AddOrReplaceFrontendMessage(new('p', "Password", IsFrontEnd: true));

        return options;
    }

}