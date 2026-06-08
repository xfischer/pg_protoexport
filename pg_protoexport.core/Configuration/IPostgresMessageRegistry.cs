namespace pg_protoexport;

public interface IPostgresMessageRegistry
{
    PostgresMessageDescriptor? GetMessage(char messageCode, bool? frontEnd);

    void AddOrReplaceBackendMessage(PostgresMessageDescriptor pgMessage);

    void AddOrReplaceFrontendMessage(PostgresMessageDescriptor pgMessage);

}
