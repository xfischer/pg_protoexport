
```mermaid
sequenceDiagram
    participant C as Client (::1:49331)
    participant S@{ "type" : "database" } as Server (::1:5432)
    C->>S: Parse / Bind / Describe / Execute / Sync
    S->>C: ParseComplete / BindComplete / RowDescription
    S->>C: DataRow (x10)
    S->>C: CommandComplete
    S->>C: ReadyForQuery
```

```mermaid
sequenceDiagram
    participant C as Client (::1:49331)
    participant S@{ "type" : "database" } as Server (::1:5432)
    C->>S: Terminate
```
