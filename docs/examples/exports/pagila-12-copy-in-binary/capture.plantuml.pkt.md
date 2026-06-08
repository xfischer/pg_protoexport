
# Packet 1 (1 messages, FrontEnd --> BackEnd)

```plantuml
@startjson
{
  "Query": {
    "Code": "Q (1 byte)",
    "Length": "56 (4 bytes)",
    "Query": "\"COPY tmp_demo (id, name) FROM STDIN (FORMAT BIN...\" (52 bytes)"
  }
}
@endjson
```


# Packet 2 (1 messages, FrontEnd <-- BackEnd)

```plantuml
@startjson
{
  "CopyInResponse": {
    "Code": "G (1 byte)",
    "Length": "11 (4 bytes)",
    "OverallFormat": "binary (1 byte)",
    "ColumnCount": "2 (2 bytes)",
    "ColumnFormats": [
      "binary (2 bytes)",
      "binary (2 bytes)"
    ]
  }
}
@endjson
```


# Packet 3 (1 messages, FrontEnd --> BackEnd)

```plantuml
@startjson
{
  "CopyData": {
    "Code": "d (1 byte)",
    "Length": "85 (4 bytes)",
    "DataLength": "81 bytes [binary header]",
    "Signature": "\"5047434F50590AFF0D0A00\" (PGCOPY OK, 11 bytes)",
    "Flags": "0x00000000 (4 bytes)",
    "HeaderExtensionLength": "0 (4 bytes)",
    "TupleData": "(62 bytes)"
  }
}
@endjson
```


# Packet 4 (1 messages, FrontEnd --> BackEnd)

```plantuml
@startjson
{
  "CopyDone": {
    "Code": "c (1 byte)",
    "Length": "4 (4 bytes)"
  }
}
@endjson
```


# Packet 5 (2 messages, FrontEnd <-- BackEnd)

```plantuml
@startjson
{
  "CommandComplete": {
    "Code": "C (1 byte)",
    "Length": "11 (4 bytes)",
    "Tag": "\"COPY 3\" (7 bytes)"
  }
}
@endjson
```

```plantuml
@startjson
{
  "ReadyForQuery": {
    "Code": "Z (1 byte)",
    "Length": "5 (4 bytes)",
    "Status": "Idle (1 byte)"
  }
}
@endjson
```

