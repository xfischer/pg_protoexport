
# Packet 1 (5 messages, FrontEnd --> BackEnd)

```plantuml
@startjson
{
  "Parse": {
    "Code": "P (1 byte)",
    "Length": "102 (4 bytes)",
    "Stmt": "\"\" (1 byte)",
    "Query": "\"SELECT film_id, title, length FROM film WHERE r...\" (87 bytes)",
    "Params": "2 (2 bytes)",
    "OIDs": [
      "25 (4 bytes)",
      "23 (4 bytes)"
    ]
  }
}
@endjson
```

```plantuml
@startjson
{
  "Bind": {
    "Code": "B (1 byte)",
    "Length": "32 (4 bytes)",
    "Portal": "\"\" (1 byte)",
    "Statement": "\"\" (1 byte)",
    "FmtCount": "2 (2 bytes)",
    "ParameterFormats": [
      "Text (2 bytes)",
      "Binary (2 bytes)"
    ],
    "ValCount": "2 (2 bytes)",
    "ParameterValues": [
      {
        "Len": "2 (4 bytes)",
        "Data": "(2 bytes)"
      },
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


# Packet 2 (67 messages, FrontEnd <-- BackEnd)

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
    "Length": "81 (4 bytes)",
    "Fields": "3 (2 bytes)",
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
      },
      {
        "Name": "\"length\" (7 bytes)",
        "TableOid": "1469070 (4 bytes)",
        "ColIdx": "9 (2 bytes)",
        "TypeOid": "21 (4 bytes)",
        "ColLen": "2 (2 bytes)",
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
  "DataRow (x62)": {
    "Code": "D (1 byte)",
    "Length": "40 (4 bytes)",
    "Fields": "3 (2 bytes)",
    "Columns": [
      {
        "Len": "4 (4 bytes)",
        "Value": "film_id: \"00000001\" (4 bytes)"
      },
      {
        "Len": "16 (4 bytes)",
        "Value": "title: \"41434144454d592044494e4f53415552\" (16 bytes)"
      },
      {
        "Len": "2 (4 bytes)",
        "Value": "length: \"0056\" (2 bytes)"
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
    "Length": "14 (4 bytes)",
    "Tag": "\"SELECT 62\" (10 bytes)"
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

