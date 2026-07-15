
```plantuml
@startuml
participant "Client (::1:51885)" as C
participant "Server (::1:5434)" as S
C -> S : Query
S -> C : RowDescription / DataRow / CommandComplete
S -> C : ReadyForQuery
@enduml
```
