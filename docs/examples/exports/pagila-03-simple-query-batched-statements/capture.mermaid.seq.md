
```mermaid
sequenceDiagram
    participant C as Client (::1:57479)
    participant S@{ "type" : "database" } as Server (::1:5434)
    C->>S: Parse / Bind / Describe / Execute / Parse / Bind / Describe / Execute / Parse / Bind / Describe / Execute / Sync
    S->>C: ParseComplete / BindComplete / RowDescription / DataRow / CommandComplete / ParseComplete / BindComplete / RowDescription / DataRow / CommandComplete / ParseComplete / BindComplete / RowDescription / DataRow / CommandComplete
    S->>C: ReadyForQuery
```
