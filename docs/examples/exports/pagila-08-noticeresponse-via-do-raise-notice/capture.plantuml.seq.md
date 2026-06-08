
```plantuml
@startuml
participant "Client (::1:57479)" as C
participant "Server (::1:5434)" as S
C -> S : Parse / Bind / Describe / Execute / Sync
S -> C : ParseComplete / BindComplete / NoData / NoticeResponse
S -> C : CommandComplete
S -> C : ReadyForQuery
@enduml
```
