# Sequence diagram

Recorded : 2025-03-13T06:36:30.1436940Z

## Compact diagram

```mermaid
sequenceDiagram
    participant C as Client<br/>::1:49331
    participant S as Server<br/>::1:5432
    C->>S: Parse / Bind / Describe / Execute / Sync

    S->>C: ParseComplete / BindComplete / RowDescription / DataRow (x10) / CommandComplete / ReadyForQuery

    C->>S: Terminate

```

## Detailed diagram

```mermaid
sequenceDiagram
    participant C as Client<br/>::1:49331
    participant S as Server<br/>::1:5432
    rect rgb(240, 240, 240)
    Note over C,S: packet 1
    C->>S: Parse
    C->>S: Bind
    C->>S: Describe
    C->>S: Execute
    C->>S: Sync

    end
    rect rgb(240, 240, 240)
    Note over C,S: packet 2 | 0.7 ms TOTAL | +0.7 ms SINCE last
    S->>C: ParseComplete
    S->>C: BindComplete
    S->>C: RowDescription
    S->>C: DataRow (x10)
    S->>C: CommandComplete
    S->>C: ReadyForQuery

    end
    rect rgb(240, 240, 240)
    Note over C,S: packet 3 | 17.1 ms TOTAL | +16.4 ms SINCE last
    C->>S: Terminate

    end
```

## Packet detail

### Packet 1 (5 messages, FrontEnd --> BackEnd)

```mermaid
packet
    0-0: "P"
    1-4: "length: 83"
    5-10: "Stmt: "
    11-29: "Query: select oid, typname, typtyp..."
    30-31: "params: 1"
    32-35: "oid: 25"
    36-63: " "
    64-64: "B"
    65-68: "length: 19"
    69-81: "portal: "
    82-95: "statement: "
    96-97: "fmt cnt: 0"
    98-127: " "
    128-129: "val cnt: 1"
    130-133: "len: 1"
    134-159: " "
    160-161: "res cnt: 1"
    162-163: "Binary"
    164-191: " "
    192-192: "D"
    193-196: "length: 6"
    197-197: "P"
    198-223: "portal: "
    224-224: "E"
    225-228: "length: 9"
    229-251: "portal: "
    252-255: "maxrows: 0"
    256-256: "S"
    257-260: "length: 4"
    261-287: " "
```

### Packet 2 (15 messages, FrontEnd <-- BackEnd)

```mermaid
packet
    0-0: "1"
    1-4: "length: 4"
    5-31: " "
    32-32: "2"
    33-36: "length: 4"
    37-63: " "
    64-64: "T"
    65-68: "length: 80"
    69-70: "fields: 3"
    71-95: " "
    96-109: "name: oid"
    110-113: "tbl oid: 1247"
    114-115: "idx: 1"
    116-119: "type: 26"
    120-121: "len: 4"
    122-125: "mod: -1"
    126-127: "Binary"
    128-141: "name: typname"
    142-145: "tbl oid: 1247"
    146-147: "idx: 2"
    148-151: "type: 19"
    152-153: "len: 64"
    154-157: "mod: -1"
    158-159: "Binary"
    160-173: "name: typtype"
    174-177: "tbl oid: 1247"
    178-179: "idx: 7"
    180-183: "type: 18"
    184-185: "len: 1"
    186-189: "mod: -1"
    190-191: "Binary"
    192-192: "D"
    193-196: "length: 30"
    197-198: "fields: 3"
    199-223: " "
    224-227: "len: 4"
    228-255: "oid: 00000047"
    256-259: "len: 7"
    260-287: "typname: pg_type"
    288-291: "len: 1"
    292-319: "typtype: c"
    320-351: "DataRow x9 skipped"
    352-352: "C"
    353-356: "length: 14"
    357-383: "SELECT 10"
    384-384: "Z"
    385-388: "length: 5"
    389-415: "Idle"
```

### Packet 3 (1 messages, FrontEnd --> BackEnd)

```mermaid
packet
    0-0: "X"
    1-4: "length: 4"
    5-31: " "
```

