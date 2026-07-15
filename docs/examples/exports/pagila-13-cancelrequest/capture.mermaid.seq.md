
```mermaid
sequenceDiagram
    participant C as Client (::1:65235)
    participant S@{ "type" : "database" } as Server (::1:5434)
    C->>S: Parse / Bind / Describe / Execute / Sync
    C->>S: SSLRequest
    S->>C: SSLResponse
    C->>S: CancelRequest
    S->>C: ParseComplete / BindComplete / RowDescription / ErrorResponse
    S->>C: ReadyForQuery
```
