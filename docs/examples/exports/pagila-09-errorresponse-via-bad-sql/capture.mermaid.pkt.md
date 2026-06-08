
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
    +4: "Length: 40"
    +1: "Stmt: "
    +33: "Query: SELECT * FROM not_a_table_at_all"
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


# Packet 2 (1 messages, FrontEnd <-- BackEnd)

```mermaid
---
title: "ErrorResponse"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "E"
    +4: "Length: 117"
    +1: "S"
    +6: "ERROR"
    +1: "V"
    +6: "ERROR"
    +1: "C"
    +6: "42P01"
    +1: "M"
    +45: "relation 'not_a_table_at_all' does not exist"
    +1: "P"
    +3: "15"
    +1: "F"
    +17: "parse_relation.c"
    +1: "L"
    +5: "1469"
    +1: "R"
    +16: "parserOpenTable"
```


# Packet 3 (1 messages, FrontEnd <-- BackEnd)

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

