
# Packet 1 (5 messages, FrontEnd --> BackEnd)

```mermaid
---
title: "Parse"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "P"
    +4: "Length: 26"
    +1: "Stmt: "
    +19: "Query: SELECT pg_sleep(2)"
    +2: "Params: 0"
```

```mermaid
---
title: "Bind"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "B"
    +4: "Length: 14"
    +1: "Portal: "
    +1: "Statement: "
    +2: "Fmt count: 0"
    +2: "Val count: 0"
    +2: "Res fmt count: 1"
    +2: "Binary"
```

```mermaid
---
title: "Describe"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "D"
    +4: "Length: 6"
    +1: "P"
    +1: "Portal: "
```

```mermaid
---
title: "Execute"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "E"
    +4: "Length: 9"
    +1: "Portal: "
    +4: "MaxRows: 0"
```

```mermaid
---
title: "Sync"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "S"
    +4: "Length: 4"
```


# Packet 2 (1 messages, FrontEnd --> BackEnd)

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


# Packet 3 (1 messages, FrontEnd <-- BackEnd)

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


# Packet 4 (1 messages, FrontEnd --> BackEnd)

```mermaid
---
title: "CancelRequest"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "?"
    +4: "Length: 16"
```


# Packet 5 (4 messages, FrontEnd <-- BackEnd)

```mermaid
---
title: "ParseComplete"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "1"
    +4: "Length: 4"
```

```mermaid
---
title: "BindComplete"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "2"
    +4: "Length: 4"
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
    +4: "Length: 33"
    +2: "Fields: 1"
    +9: "Name: pg_sleep"
    +4: "TableOid: 0"
    +2: "ColIdx: 0"
    +4: "TypeOid: 2278"
    +2: "ColLen: 4"
    +4: "TypeMod: -1"
    +2: "Binary"
```

```mermaid
---
title: "ErrorResponse"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "E"
    +4: "Length: 104"
    +1: "S"
    +6: "ERROR"
    +1: "V"
    +6: "ERROR"
    +1: "C"
    +6: "57014"
    +1: "M"
    +40: "canceling statement due to user request"
    +1: "F"
    +11: "postgres.c"
    +1: "L"
    +5: "3465"
    +1: "R"
    +18: "ProcessInterrupts"
```


# Packet 6 (1 messages, FrontEnd <-- BackEnd)

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

