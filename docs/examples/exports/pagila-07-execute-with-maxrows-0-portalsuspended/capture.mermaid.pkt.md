
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
    +4: "Length: 56"
    +1: "Stmt: "
    +49: "Query: SELECT film_id, title FROM film ORDER BY film_id"
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


# Packet 2 (242 messages, FrontEnd <-- BackEnd)

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
    +4: "Length: 56"
    +2: "Fields: 2"
    +8: "Name: film_id"
    +4: "TableOid: 1469070"
    +2: "ColIdx: 1"
    +4: "TypeOid: 23"
    +2: "ColLen: 4"
    +4: "TypeMod: -1"
    +2: "Binary"
    +6: "Name: title"
    +4: "TableOid: 1469070"
    +2: "ColIdx: 2"
    +4: "TypeOid: 1043"
    +2: "ColLen: -1"
    +4: "TypeMod: 259"
    +2: "Binary"
```

```mermaid
---
title: "DataRow (x239)"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "D"
    +4: "Length: 34"
    +2: "Fields: 2"
    +4: "Len: 4"
    +4: "film_id: 00000001"
    +4: "Len: 16"
    +16: "title: 41434144454d592044494e4f53415552"
```


# Packet 3 (248 messages, FrontEnd <-- BackEnd)

```mermaid
---
title: "DataRow (x248)"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "D"
    +4: "Length: 28"
    +2: "Fields: 2"
    +4: "Len: 4"
    +4: "film_id: 000000f0"
    +4: "Len: 10"
    +10: "title: 444f4c4c532052414745"
```


# Packet 4 (249 messages, FrontEnd <-- BackEnd)

```mermaid
---
title: "DataRow (x249)"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "D"
    +4: "Length: 32"
    +2: "Fields: 2"
    +4: "Len: 4"
    +4: "film_id: 000001e8"
    +4: "Len: 14"
    +14: "title: 4a4f4f4e204e4f52544857455354"
```


# Packet 5 (247 messages, FrontEnd <-- BackEnd)

```mermaid
---
title: "DataRow (x247)"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "D"
    +4: "Length: 31"
    +2: "Fields: 2"
    +4: "Len: 4"
    +4: "film_id: 000002e1"
    +4: "Len: 13"
    +13: "title: 524f434b20494e5354494e4354"
```


# Packet 6 (19 messages, FrontEnd <-- BackEnd)

```mermaid
---
title: "DataRow (x17)"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "D"
    +4: "Length: 32"
    +2: "Fields: 2"
    +4: "Len: 4"
    +4: "film_id: 000003d8"
    +4: "Len: 14"
    +14: "title: 574f4e44455246554c2044524f50"
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
    +4: "Length: 16"
    +12: "Tag: SELECT 1000"
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

