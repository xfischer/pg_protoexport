
# Packet 1 (3 messages, FrontEnd --> BackEnd)

```plantuml
@startjson
{
  "Parse": {
    "Code": "P (1 byte)",
    "Length": "77 (4 bytes)",
    "Stmt": "\"_p2\" (4 bytes)",
    "Query": "\"UPDATE actor SET last_update = last_update WHER...\" (63 bytes)",
    "Params": "1 (2 bytes)",
    "OIDs": [
      "23 (4 bytes)"
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
    "Length": "9 (4 bytes)",
    "PortalOrStatement": "S (1 byte)",
    "Statement": "\"_p2\" (4 bytes)"
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


# Packet 2 (4 messages, FrontEnd <-- BackEnd)

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
  "ParameterDescription": {
    "Code": "t (1 byte)",
    "Length": "10 (4 bytes)"
  }
}
@endjson
```

```plantuml
@startjson
{
  "NoData": {
    "Code": "n (1 byte)",
    "Length": "4 (4 bytes)"
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


# Packet 3 (3 messages, FrontEnd --> BackEnd)

```plantuml
@startjson
{
  "Bind": {
    "Code": "B (1 byte)",
    "Length": "27 (4 bytes)",
    "Portal": "\"\" (1 byte)",
    "Statement": "\"_p2\" (4 bytes)",
    "FmtCount": "1 (2 bytes)",
    "ParameterFormats": [
      "Binary (2 bytes)"
    ],
    "ValCount": "1 (2 bytes)",
    "ParameterValues": [
      {
        "Len": "4 (4 bytes)",
        "Data": "(4 bytes)"
      }
    ],
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


# Packet 4 (3 messages, FrontEnd <-- BackEnd)

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
  "CommandComplete": {
    "Code": "C (1 byte)",
    "Length": "13 (4 bytes)",
    "Tag": "\"UPDATE 1\" (9 bytes)"
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

