
# Packet 1 (5 messages, FrontEnd --> BackEnd)

```plantuml
@startjson
{
  "Parse": {
    "Code": "P (1 byte)",
    "Length": "56 (4 bytes)",
    "Stmt": "\"\" (1 byte)",
    "Query": "\"SELECT film_id, title FROM film ORDER BY film_id\" (49 bytes)",
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


# Packet 2 (242 messages, FrontEnd <-- BackEnd)

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
    "Length": "56 (4 bytes)",
    "Fields": "2 (2 bytes)",
    "FieldDescriptions": [
      {
        "Name": "\"film_id\" (8 bytes)",
        "TableOid": "1469070 (4 bytes)",
        "ColIdx": "1 (2 bytes)",
        "TypeOid": "23 (4 bytes)",
        "ColLen": "4 (2 bytes)",
        "TypeMod": "-1 (4 bytes)",
        "Format": "Binary (2 bytes)"
      },
      {
        "Name": "\"title\" (6 bytes)",
        "TableOid": "1469070 (4 bytes)",
        "ColIdx": "2 (2 bytes)",
        "TypeOid": "1043 (4 bytes)",
        "ColLen": "-1 (2 bytes)",
        "TypeMod": "259 (4 bytes)",
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
  "DataRow (x239)": {
    "Code": "D (1 byte)",
    "Length": "34 (4 bytes)",
    "Fields": "2 (2 bytes)",
    "Columns": [
      {
        "Len": "4 (4 bytes)",
        "Value": "film_id: \"00000001\" (4 bytes)"
      },
      {
        "Len": "16 (4 bytes)",
        "Value": "title: \"41434144454d592044494e4f53415552\" (16 bytes)"
      }
    ]
  }
}
@endjson
```


# Packet 3 (248 messages, FrontEnd <-- BackEnd)

```plantuml
@startjson
{
  "DataRow (x248)": {
    "Code": "D (1 byte)",
    "Length": "28 (4 bytes)",
    "Fields": "2 (2 bytes)",
    "Columns": [
      {
        "Len": "4 (4 bytes)",
        "Value": "film_id: \"000000f0\" (4 bytes)"
      },
      {
        "Len": "10 (4 bytes)",
        "Value": "title: \"444f4c4c532052414745\" (10 bytes)"
      }
    ]
  }
}
@endjson
```


# Packet 4 (249 messages, FrontEnd <-- BackEnd)

```plantuml
@startjson
{
  "DataRow (x249)": {
    "Code": "D (1 byte)",
    "Length": "32 (4 bytes)",
    "Fields": "2 (2 bytes)",
    "Columns": [
      {
        "Len": "4 (4 bytes)",
        "Value": "film_id: \"000001e8\" (4 bytes)"
      },
      {
        "Len": "14 (4 bytes)",
        "Value": "title: \"4a4f4f4e204e4f52544857455354\" (14 bytes)"
      }
    ]
  }
}
@endjson
```


# Packet 5 (247 messages, FrontEnd <-- BackEnd)

```plantuml
@startjson
{
  "DataRow (x247)": {
    "Code": "D (1 byte)",
    "Length": "31 (4 bytes)",
    "Fields": "2 (2 bytes)",
    "Columns": [
      {
        "Len": "4 (4 bytes)",
        "Value": "film_id: \"000002e1\" (4 bytes)"
      },
      {
        "Len": "13 (4 bytes)",
        "Value": "title: \"524f434b20494e5354494e4354\" (13 bytes)"
      }
    ]
  }
}
@endjson
```


# Packet 6 (19 messages, FrontEnd <-- BackEnd)

```plantuml
@startjson
{
  "DataRow (x17)": {
    "Code": "D (1 byte)",
    "Length": "32 (4 bytes)",
    "Fields": "2 (2 bytes)",
    "Columns": [
      {
        "Len": "4 (4 bytes)",
        "Value": "film_id: \"000003d8\" (4 bytes)"
      },
      {
        "Len": "14 (4 bytes)",
        "Value": "title: \"574f4e44455246554c2044524f50\" (14 bytes)"
      }
    ]
  }
}
@endjson
```

```plantuml
@startjson
{
  "CommandComplete": {
    "Code": "C (1 byte)",
    "Length": "16 (4 bytes)",
    "Tag": "\"SELECT 1000\" (12 bytes)"
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

