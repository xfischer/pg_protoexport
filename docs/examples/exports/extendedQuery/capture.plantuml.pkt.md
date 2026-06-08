
# Packet 1 (5 messages, FrontEnd --> BackEnd)

```plantuml
@startjson
{
  "Parse": {
    "Code": "P (1 byte)",
    "Length": "83 (4 bytes)",
    "Stmt": "\"\" (1 byte)",
    "Query": "\"select oid, typname, typtype from pg_type where...\" (72 bytes)",
    "Params": "1 (2 bytes)",
    "OIDs": [
      "25 (4 bytes)"
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
    "Length": "19 (4 bytes)",
    "Portal": "\"\" (1 byte)",
    "Statement": "\"\" (1 byte)",
    "FmtCount": "0 (2 bytes)",
    "ValCount": "1 (2 bytes)",
    "ParameterValues": [
      {
        "Len": "1 (4 bytes)",
        "Data": "(1 bytes)"
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


# Packet 2 (15 messages, FrontEnd <-- BackEnd)

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
    "Length": "80 (4 bytes)",
    "Fields": "3 (2 bytes)",
    "FieldDescriptions": [
      {
        "Name": "\"oid\" (4 bytes)",
        "TableOid": "1247 (4 bytes)",
        "ColIdx": "1 (2 bytes)",
        "TypeOid": "26 (4 bytes)",
        "ColLen": "4 (2 bytes)",
        "TypeMod": "-1 (4 bytes)",
        "Format": "Binary (2 bytes)"
      },
      {
        "Name": "\"typname\" (8 bytes)",
        "TableOid": "1247 (4 bytes)",
        "ColIdx": "2 (2 bytes)",
        "TypeOid": "19 (4 bytes)",
        "ColLen": "64 (2 bytes)",
        "TypeMod": "-1 (4 bytes)",
        "Format": "Binary (2 bytes)"
      },
      {
        "Name": "\"typtype\" (8 bytes)",
        "TableOid": "1247 (4 bytes)",
        "ColIdx": "7 (2 bytes)",
        "TypeOid": "18 (4 bytes)",
        "ColLen": "1 (2 bytes)",
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
  "DataRow (x10)": {
    "Code": "D (1 byte)",
    "Length": "30 (4 bytes)",
    "Fields": "3 (2 bytes)",
    "Columns": [
      {
        "Len": "4 (4 bytes)",
        "Value": "oid: \"00000047\" (4 bytes)"
      },
      {
        "Len": "7 (4 bytes)",
        "Value": "typname: \"pg_type\" (7 bytes)"
      },
      {
        "Len": "1 (4 bytes)",
        "Value": "typtype: \"c\" (1 bytes)"
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
    "Tag": "\"SELECT 10\" (10 bytes)"
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


# Packet 3 (1 messages, FrontEnd --> BackEnd)

```plantuml
@startjson
{
  "Terminate": {
    "Code": "X (1 byte)",
    "Length": "4 (4 bytes)"
  }
}
@endjson
```

