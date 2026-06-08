namespace pg_protoexport;

class PostgresMessageRegistry() : IPostgresMessageRegistry
{
    private readonly Dictionary<char, PostgresMessageDescriptor> backendMessages = [];
    private readonly Dictionary<char, PostgresMessageDescriptor> frontendMessages = [];

    public void AddOrReplaceBackendMessage(PostgresMessageDescriptor pgMessage)
    {
        backendMessages[pgMessage.Code] = pgMessage;
    }

    public void AddOrReplaceFrontendMessage(PostgresMessageDescriptor pgMessage)
    {
        frontendMessages[pgMessage.Code] = pgMessage;
    }

    public PostgresMessageDescriptor? GetMessage(char messageCode, bool? frontEnd)
    {
        if (frontEnd ?? false)
        {
            if (frontendMessages.TryGetValue(messageCode, out var message))
                return message;
        }
        else
        {
            if (backendMessages.TryGetValue(messageCode, out var message))
                return message;
        }
        return null;
    }
}
