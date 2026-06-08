
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
    +4: "Length: 102"
    +98: "Query: COPY (SELECT actor_id, first_name FROM actor OR..."
```


# Packet 2 (10 messages, FrontEnd <-- BackEnd)

```mermaid
---
title: "CopyOutResponse"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "H"
    +4: "Length: 11"
    +1: "Binary"
    +2: "Cols: 2"
    +2: "Binary"
    +2: "Binary"
```

```mermaid
---
title: "CopyData (x6)"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "d"
    +4: "Length: 45"
    +11: "sig: PGCOPY OK"
    +4: "flags: 0x00000000"
    +4: "ext-len: 0"
    +22: "tuple data (22 bytes)"
```

```mermaid
---
title: "CopyDone"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "c"
    +4: "Length: 4"
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
    +4: "Length: 11"
    +7: "Tag: COPY 5"
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

