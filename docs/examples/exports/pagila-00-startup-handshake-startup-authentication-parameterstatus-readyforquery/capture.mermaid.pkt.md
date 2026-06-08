
# Packet 1 (1 messages, FrontEnd --> BackEnd)

```mermaid
---
title: "GSSENCRequest"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "?"
    +4: "Length: 8"
```


# Packet 2 (1 messages, FrontEnd <-- BackEnd)

```mermaid
---
title: "GSSENCResponse"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "?"
    +4: "Length: 1"
```


# Packet 3 (1 messages, FrontEnd --> BackEnd)

```mermaid
---
title: "StartupMessage"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "?"
    +4: "Length: 60"
    +4: "Protocol: 3.0"
    +5: "user"
    +9: "postgres"
    +16: "client_encoding"
    +5: "UTF8"
    +9: "database"
    +7: "pagila"
```


# Packet 4 (1 messages, FrontEnd <-- BackEnd)

```mermaid
---
title: "AuthenticationRequest"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "R"
    +4: "Length: 23"
```


# Packet 5 (1 messages, FrontEnd --> BackEnd)

```mermaid
---
title: "Password"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "p"
    +4: "Length: 55"
```


# Packet 6 (1 messages, FrontEnd <-- BackEnd)

```mermaid
---
title: "AuthenticationRequest"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "R"
    +4: "Length: 92"
```


# Packet 7 (1 messages, FrontEnd --> BackEnd)

```mermaid
---
title: "Password"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "p"
    +4: "Length: 108"
```


# Packet 8 (19 messages, FrontEnd <-- BackEnd)

```mermaid
---
title: "AuthenticationRequest (x2)"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "R"
    +4: "Length: 54"
```

```mermaid
---
title: "ParameterStatus (x15)"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "S"
    +4: "Length: 23"
    +15: "Name: in_hot_standby"
    +4: "Value: off"
```

```mermaid
---
title: "BackendKeyData"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "K"
    +4: "Length: 12"
    +4: "PID: 49040"
    +4: "Key: 1731653302"
```

```mermaid
---
title: "NoticeResponse"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "N"
    +4: "Length: 178"
    +1: "S"
    +7: "NOTICE"
    +1: "V"
    +7: "NOTICE"
    +1: "C"
    +6: "00000"
    +1: "M"
    +61: "Welcome to Pagila, the time is 2026-05-21 09:41..."
    +1: "W"
    +53: "PL/pgSQL function _welcome_message() line 3 at ..."
    +1: "F"
    +10: "pl_exec.c"
    +1: "L"
    +5: "3923"
    +1: "R"
    +16: "exec_stmt_raise"
```


# Packet 9 (1 messages, FrontEnd <-- BackEnd)

```mermaid
---
title: "ReadyForQuery"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "Z"
    +4: "Length: 5"
    +1: "Idle"
```


# Packet 10 (1 messages, FrontEnd --> BackEnd)

```mermaid
---
title: "Query"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "Q"
    +4: "Length: 3751"
    +3747: "Query: SELECT version();

SELECT ns.nspname, t.oid, t..."
```


# Packet 11 (145 messages, FrontEnd <-- BackEnd)

```mermaid
---
title: "RowDescription"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "T"
    +4: "Length: 32"
    +2: "Fields: 1"
    +8: "Name: version"
    +4: "TableOid: 0"
    +2: "ColIdx: 0"
    +4: "TypeOid: 25"
    +2: "ColLen: -1"
    +4: "TypeMod: -1"
    +2: "Text"
```

```mermaid
---
title: "DataRow"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "D"
    +4: "Length: 81"
    +2: "Fields: 1"
    +4: "Len: 71"
    +71: "version: PostgreSQL 18.3 on x86_64-windows, co..."
```

```mermaid
---
title: "CommandComplete"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "C"
    +4: "Length: 13"
    +9: "Tag: SELECT 1"
```

```mermaid
---
title: "RowDescription"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "T"
    +4: "Length: 164"
    +2: "Fields: 6"
    +8: "Name: nspname"
    +4: "TableOid: 2615"
    +2: "ColIdx: 2"
    +4: "TypeOid: 19"
    +2: "ColLen: 64"
    +4: "TypeMod: -1"
    +2: "Text"
    +4: "Name: oid"
    +4: "TableOid: 1247"
    +2: "ColIdx: 1"
    +4: "TypeOid: 26"
    +2: "ColLen: 4"
    +4: "TypeMod: -1"
    +2: "Text"
    +8: "Name: typname"
    +4: "TableOid: 1247"
    +2: "ColIdx: 2"
    +4: "TypeOid: 19"
    +2: "ColLen: 64"
    +4: "TypeMod: -1"
    +2: "Text"
    +8: "Name: typtype"
    +4: "TableOid: 0"
    +2: "ColIdx: 0"
    +4: "TypeOid: 18"
    +2: "ColLen: 1"
    +4: "TypeMod: -1"
    +2: "Text"
    +11: "Name: typnotnull"
    +4: "TableOid: 1247"
    +2: "ColIdx: 25"
    +4: "TypeOid: 16"
    +2: "ColLen: 1"
    +4: "TypeMod: -1"
    +2: "Text"
    +11: "Name: elemtypoid"
    +4: "TableOid: 1247"
    +2: "ColIdx: 1"
    +4: "TypeOid: 26"
    +2: "ColLen: 4"
    +4: "TypeMod: -1"
    +2: "Text"
```

```mermaid
---
title: "DataRow (x141)"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "D"
    +4: "Length: 48"
    +2: "Fields: 6"
    +4: "Len: 10"
    +10: "nspname: pg_catalog"
    +4: "Len: 2"
    +2: "oid: 23"
    +4: "Len: 4"
    +4: "typname: int4"
    +4: "Len: 1"
    +1: "typtype: b"
    +4: "Len: 1"
    +1: "typnotnull: f"
    +4: "Len: -1"
```


# Packet 12 (46 messages, FrontEnd <-- BackEnd)

```mermaid
---
title: "DataRow (x35)"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "D"
    +4: "Length: 62"
    +2: "Fields: 6"
    +4: "Len: 10"
    +10: "nspname: pg_catalog"
    +4: "Len: 4"
    +4: "oid: 2209"
    +4: "Len: 12"
    +12: "typname: _regoperator"
    +4: "Len: 1"
    +1: "typtype: a"
    +4: "Len: 1"
    +1: "typnotnull: f"
    +4: "Len: 4"
    +4: "elemtypoid: 2204"
```

```mermaid
---
title: "CommandComplete"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "C"
    +4: "Length: 15"
    +11: "Tag: SELECT 176"
```

```mermaid
---
title: "RowDescription"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "T"
    +4: "Length: 81"
    +2: "Fields: 3"
    +4: "Name: oid"
    +4: "TableOid: 1247"
    +2: "ColIdx: 1"
    +4: "TypeOid: 26"
    +2: "ColLen: 4"
    +4: "TypeMod: -1"
    +2: "Text"
    +8: "Name: attname"
    +4: "TableOid: 1249"
    +2: "ColIdx: 2"
    +4: "TypeOid: 19"
    +2: "ColLen: 64"
    +4: "TypeMod: -1"
    +2: "Text"
    +9: "Name: atttypid"
    +4: "TableOid: 1249"
    +2: "ColIdx: 3"
    +4: "TypeOid: 26"
    +2: "ColLen: 4"
    +4: "TypeMod: -1"
    +2: "Text"
```

```mermaid
---
title: "CommandComplete"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "C"
    +4: "Length: 13"
    +9: "Tag: SELECT 0"
```

```mermaid
---
title: "RowDescription"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "T"
    +4: "Length: 56"
    +2: "Fields: 2"
    +4: "Name: oid"
    +4: "TableOid: 1247"
    +2: "ColIdx: 1"
    +4: "TypeOid: 26"
    +2: "ColLen: 4"
    +4: "TypeMod: -1"
    +2: "Text"
    +10: "Name: enumlabel"
    +4: "TableOid: 3501"
    +2: "ColIdx: 4"
    +4: "TypeOid: 19"
    +2: "ColLen: 64"
    +4: "TypeMod: -1"
    +2: "Text"
```

```mermaid
---
title: "DataRow (x5)"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "D"
    +4: "Length: 22"
    +2: "Fields: 2"
    +4: "Len: 7"
    +7: "oid: 1469004"
    +4: "Len: 1"
    +1: "enumlabel: G"
```

```mermaid
---
title: "CommandComplete"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "C"
    +4: "Length: 13"
    +9: "Tag: SELECT 5"
```

```mermaid
---
title: "ReadyForQuery"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "Z"
    +4: "Length: 5"
    +1: "Idle"
```

