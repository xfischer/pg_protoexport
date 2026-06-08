
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
    +4: "Length: 56"
    +52: "Query: COPY tmp_demo (id, name) FROM STDIN (FORMAT BIN..."
```


# Packet 2 (1 messages, FrontEnd <-- BackEnd)

```mermaid
---
title: "CopyInResponse"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "G"
    +4: "Length: 11"
    +1: "Binary"
    +2: "Cols: 2"
    +2: "Binary"
    +2: "Binary"
```


# Packet 3 (1 messages, FrontEnd --> BackEnd)

```mermaid
---
title: "CopyData"
config:
  packet:
    bitsPerRow: 32
---
packet
    +1: "d"
    +4: "Length: 85"
    +11: "sig: PGCOPY OK"
    +4: "flags: 0x00000000"
    +4: "ext-len: 0"
    +62: "tuple data (62 bytes)"
```


# Packet 4 (1 messages, FrontEnd --> BackEnd)

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


# Packet 5 (2 messages, FrontEnd <-- BackEnd)

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
    +7: "Tag: COPY 3"
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

