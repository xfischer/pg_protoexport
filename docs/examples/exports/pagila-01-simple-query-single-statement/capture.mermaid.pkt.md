
# Packet 1 (1 messages, FrontEnd --> BackEnd)

```mermaid
---
title: "Query"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "Q"
    +4: "Length: 33"
    +29: "Query: SELECT * FROM actor LIMIT 1;"
```


# Packet 2 (4 messages, FrontEnd <-- BackEnd)

```mermaid
---
title: "RowDescription"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "T"
    +4: "Length: 120"
    +2: "Fields: 4"
    +9: "Name: actor_id"
    +4: "TableOid: 1469051"
    +2: "ColIdx: 1"
    +4: "TypeOid: 23"
    +2: "ColLen: 4"
    +4: "TypeMod: -1"
    +2: "Text"
    +11: "Name: first_name"
    +4: "TableOid: 1469051"
    +2: "ColIdx: 2"
    +4: "TypeOid: 1043"
    +2: "ColLen: -1"
    +4: "TypeMod: 49"
    +2: "Text"
    +10: "Name: last_name"
    +4: "TableOid: 1469051"
    +2: "ColIdx: 3"
    +4: "TypeOid: 1043"
    +2: "ColLen: -1"
    +4: "TypeMod: 49"
    +2: "Text"
    +12: "Name: last_update"
    +4: "TableOid: 1469051"
    +2: "ColIdx: 4"
    +4: "TypeOid: 1114"
    +2: "ColLen: 8"
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
    +4: "Length: 54"
    +2: "Fields: 4"
    +4: "Len: 1"
    +1: "actor_id: 2"
    +4: "Len: 4"
    +4: "first_name: NICK"
    +4: "Len: 8"
    +8: "last_name: WAHLBERG"
    +4: "Len: 19"
    +19: "last_update: 2006-02-15 09:34:33"
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

