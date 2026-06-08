
# Packet 1 (5 messages, FrontEnd --> BackEnd)

```plantuml
@startjson
{
  "Parse": {
    "Code": "P (1 byte)",
    "Length": "60 (4 bytes)",
    "Stmt": "\"\" (1 byte)",
    "Query": "\"DO $$ BEGIN RAISE NOTICE 'hello from pagila'; E...\" (53 bytes)",
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
  "NoticeResponse": {
    "Code": "N (1 byte)",
    "Length": "134 (4 bytes)",
    "FieldList": [
      {
        "Type": "S (1 byte)",
        "Message": "\"NOTICE\" (7 bytes)"
      },
      {
        "Type": "V (1 byte)",
        "Message": "\"NOTICE\" (7 bytes)"
      },
      {
        "Type": "C (1 byte)",
        "Message": "\"00000\" (6 bytes)"
      },
      {
        "Type": "M (1 byte)",
        "Message": "\"hello from pagila\" (18 bytes)"
      },
      {
        "Type": "W (1 byte)",
        "Message": "\"PL/pgSQL function inline_code_block line 1 at R...\" (52 bytes)"
      },
      {
        "Type": "F (1 byte)",
        "Message": "\"pl_exec.c\" (10 bytes)"
      },
      {
        "Type": "L (1 byte)",
        "Message": "\"3923\" (5 bytes)"
      },
      {
        "Type": "R (1 byte)",
        "Message": "\"exec_stmt_raise\" (16 bytes)"
      }
    ]
  }
}
@endjson
```


# Packet 3 (2 messages, FrontEnd <-- BackEnd)

```plantuml
@startjson
{
  "CommandComplete": {
    "Code": "C (1 byte)",
    "Length": "7 (4 bytes)",
    "Tag": "\"DO\" (3 bytes)"
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

