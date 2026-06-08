
# Packet 1 (5 messages, FrontEnd --> BackEnd)

```plantuml
@startjson
{
  "Parse": {
    "Code": "P (1 byte)",
    "Length": "26 (4 bytes)",
    "Stmt": "\"\" (1 byte)",
    "Query": "\"SELECT pg_sleep(2)\" (19 bytes)",
    "Params": "0 (2 bytes)"
  }
}
@endjson
```

```plantuml
@startjson
{
  "Bind": {
    "Code": "B (1 byte)",
    "Length": "14 (4 bytes)",
    "Portal": "\"\" (1 byte)",
    "Statement": "\"\" (1 byte)",
    "FmtCount": "0 (2 bytes)",
    "ValCount": "0 (2 bytes)",
    "ResFmtCount": "1 (2 bytes)",
    "ResultFormats": [
      "Binary (2 bytes)"
    ]
  }
}
@endjson
```

```plantuml
@startjson
{
  "Describe": {
    "Code": "D (1 byte)",
    "Length": "6 (4 bytes)",
    "PortalOrStatement": "P (1 byte)",
    "Portal": "\"\" (1 byte)"
  }
}
@endjson
```

```plantuml
@startjson
{
  "Execute": {
    "Code": "E (1 byte)",
    "Length": "9 (4 bytes)",
    "Portal": "\"\" (1 byte)",
    "MaxRows": "0 (4 bytes)"
  }
}
@endjson
```

```plantuml
@startjson
{
  "Sync": {
    "Code": "S (1 byte)",
    "Length": "4 (4 bytes)"
  }
}
@endjson
```


# Packet 2 (1 messages, FrontEnd --> BackEnd)

```plantuml
@startjson
{
  "GSSENCRequest": {
    "Code": "? (1 byte)",
    "Length": "8 (4 bytes)"
  }
}
@endjson
```


# Packet 3 (1 messages, FrontEnd <-- BackEnd)

```plantuml
@startjson
{
  "GSSENCResponse": {
    "Code": "? (1 byte)",
    "Length": "1 (4 bytes)"
  }
}
@endjson
```


# Packet 4 (1 messages, FrontEnd --> BackEnd)

```plantuml
@startjson
{
  "CancelRequest": {
    "Code": "? (1 byte)",
    "Length": "16 (4 bytes)"
  }
}
@endjson
```


# Packet 5 (4 messages, FrontEnd <-- BackEnd)

```plantuml
@startjson
{
  "ParseComplete": {
    "Code": "1 (1 byte)",
    "Length": "4 (4 bytes)"
  }
}
@endjson
```

```plantuml
@startjson
{
  "BindComplete": {
    "Code": "2 (1 byte)",
    "Length": "4 (4 bytes)"
  }
}
@endjson
```

```plantuml
@startjson
{
  "RowDescription": {
    "Code": "T (1 byte)",
    "Length": "33 (4 bytes)",
    "Fields": "1 (2 bytes)",
    "FieldDescriptions": [
      {
        "Name": "\"pg_sleep\" (9 bytes)",
        "TableOid": "0 (4 bytes)",
        "ColIdx": "0 (2 bytes)",
        "TypeOid": "2278 (4 bytes)",
        "ColLen": "4 (2 bytes)",
        "TypeMod": "-1 (4 bytes)",
        "Format": "Binary (2 bytes)"
      }
    ]
  }
}
@endjson
```

```plantuml
@startjson
{
  "ErrorResponse": {
    "Code": "E (1 byte)",
    "Length": "104 (4 bytes)",
    "FieldList": [
      {
        "Type": "S (1 byte)",
        "Message": "\"ERROR\" (6 bytes)"
      },
      {
        "Type": "V (1 byte)",
        "Message": "\"ERROR\" (6 bytes)"
      },
      {
        "Type": "C (1 byte)",
        "Message": "\"57014\" (6 bytes)"
      },
      {
        "Type": "M (1 byte)",
        "Message": "\"canceling statement due to user request\" (40 bytes)"
      },
      {
        "Type": "F (1 byte)",
        "Message": "\"postgres.c\" (11 bytes)"
      },
      {
        "Type": "L (1 byte)",
        "Message": "\"3465\" (5 bytes)"
      },
      {
        "Type": "R (1 byte)",
        "Message": "\"ProcessInterrupts\" (18 bytes)"
      }
    ]
  }
}
@endjson
```


# Packet 6 (1 messages, FrontEnd <-- BackEnd)

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

