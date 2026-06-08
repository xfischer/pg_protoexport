
# Packet 1 (1 messages, FrontEnd --> BackEnd)

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


# Packet 2 (1 messages, FrontEnd <-- BackEnd)

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


# Packet 3 (1 messages, FrontEnd --> BackEnd)

```plantuml
@startjson
{
  "StartupMessage": {
    "Code": "? (1 byte)",
    "Length": "60 (4 bytes)",
    "Protocol": "3.0 (4 bytes)",
    "Parameters": [
      {
        "Name": "\"user\" (5 bytes)",
        "Value": "\"postgres\" (9 bytes)"
      },
      {
        "Name": "\"client_encoding\" (16 bytes)",
        "Value": "\"UTF8\" (5 bytes)"
      },
      {
        "Name": "\"database\" (9 bytes)",
        "Value": "\"pagila\" (7 bytes)"
      }
    ]
  }
}
@endjson
```


# Packet 4 (1 messages, FrontEnd <-- BackEnd)

```plantuml
@startjson
{
  "AuthenticationRequest": {
    "Code": "R (1 byte)",
    "Length": "23 (4 bytes)"
  }
}
@endjson
```


# Packet 5 (1 messages, FrontEnd --> BackEnd)

```plantuml
@startjson
{
  "Password": {
    "Code": "p (1 byte)",
    "Length": "55 (4 bytes)"
  }
}
@endjson
```


# Packet 6 (1 messages, FrontEnd <-- BackEnd)

```plantuml
@startjson
{
  "AuthenticationRequest": {
    "Code": "R (1 byte)",
    "Length": "92 (4 bytes)"
  }
}
@endjson
```


# Packet 7 (1 messages, FrontEnd --> BackEnd)

```plantuml
@startjson
{
  "Password": {
    "Code": "p (1 byte)",
    "Length": "108 (4 bytes)"
  }
}
@endjson
```


# Packet 8 (19 messages, FrontEnd <-- BackEnd)

```plantuml
@startjson
{
  "AuthenticationRequest (x2)": {
    "Code": "R (1 byte)",
    "Length": "54 (4 bytes)"
  }
}
@endjson
```

```plantuml
@startjson
{
  "ParameterStatus (x15)": {
    "Code": "S (1 byte)",
    "Length": "23 (4 bytes)",
    "Name": "\"in_hot_standby\" (15 bytes)",
    "Value": "\"off\" (4 bytes)"
  }
}
@endjson
```

```plantuml
@startjson
{
  "BackendKeyData": {
    "Code": "K (1 byte)",
    "Length": "12 (4 bytes)",
    "PID": "49040 (4 bytes)",
    "Key": "1731653302 (4 bytes)"
  }
}
@endjson
```

```plantuml
@startjson
{
  "NoticeResponse": {
    "Code": "N (1 byte)",
    "Length": "178 (4 bytes)",
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
        "Message": "\"Welcome to Pagila, the time is 2026-05-21 09:41...\" (61 bytes)"
      },
      {
        "Type": "W (1 byte)",
        "Message": "\"PL/pgSQL function _welcome_message() line 3 at ...\" (53 bytes)"
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


# Packet 9 (1 messages, FrontEnd <-- BackEnd)

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


# Packet 10 (1 messages, FrontEnd --> BackEnd)

```plantuml
@startjson
{
  "Query": {
    "Code": "Q (1 byte)",
    "Length": "3751 (4 bytes)",
    "Query": "\"SELECT version();\r\n\nSELECT ns.nspname, t.oid, t...\" (3747 bytes)"
  }
}
@endjson
```


# Packet 11 (145 messages, FrontEnd <-- BackEnd)

```plantuml
@startjson
{
  "RowDescription": {
    "Code": "T (1 byte)",
    "Length": "32 (4 bytes)",
    "Fields": "1 (2 bytes)",
    "FieldDescriptions": [
      {
        "Name": "\"version\" (8 bytes)",
        "TableOid": "0 (4 bytes)",
        "ColIdx": "0 (2 bytes)",
        "TypeOid": "25 (4 bytes)",
        "ColLen": "-1 (2 bytes)",
        "TypeMod": "-1 (4 bytes)",
        "Format": "Text (2 bytes)"
      }
    ]
  }
}
@endjson
```

```plantuml
@startjson
{
  "DataRow": {
    "Code": "D (1 byte)",
    "Length": "81 (4 bytes)",
    "Fields": "1 (2 bytes)",
    "Columns": [
      {
        "Len": "71 (4 bytes)",
        "Value": "version: \"PostgreSQL 18.3 on x86_64-windows, co...\" (71 bytes)"
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
    "Length": "13 (4 bytes)",
    "Tag": "\"SELECT 1\" (9 bytes)"
  }
}
@endjson
```

```plantuml
@startjson
{
  "RowDescription": {
    "Code": "T (1 byte)",
    "Length": "164 (4 bytes)",
    "Fields": "6 (2 bytes)",
    "FieldDescriptions": [
      {
        "Name": "\"nspname\" (8 bytes)",
        "TableOid": "2615 (4 bytes)",
        "ColIdx": "2 (2 bytes)",
        "TypeOid": "19 (4 bytes)",
        "ColLen": "64 (2 bytes)",
        "TypeMod": "-1 (4 bytes)",
        "Format": "Text (2 bytes)"
      },
      {
        "Name": "\"oid\" (4 bytes)",
        "TableOid": "1247 (4 bytes)",
        "ColIdx": "1 (2 bytes)",
        "TypeOid": "26 (4 bytes)",
        "ColLen": "4 (2 bytes)",
        "TypeMod": "-1 (4 bytes)",
        "Format": "Text (2 bytes)"
      },
      {
        "Name": "\"typname\" (8 bytes)",
        "TableOid": "1247 (4 bytes)",
        "ColIdx": "2 (2 bytes)",
        "TypeOid": "19 (4 bytes)",
        "ColLen": "64 (2 bytes)",
        "TypeMod": "-1 (4 bytes)",
        "Format": "Text (2 bytes)"
      },
      {
        "Name": "\"typtype\" (8 bytes)",
        "TableOid": "0 (4 bytes)",
        "ColIdx": "0 (2 bytes)",
        "TypeOid": "18 (4 bytes)",
        "ColLen": "1 (2 bytes)",
        "TypeMod": "-1 (4 bytes)",
        "Format": "Text (2 bytes)"
      },
      {
        "Name": "\"typnotnull\" (11 bytes)",
        "TableOid": "1247 (4 bytes)",
        "ColIdx": "25 (2 bytes)",
        "TypeOid": "16 (4 bytes)",
        "ColLen": "1 (2 bytes)",
        "TypeMod": "-1 (4 bytes)",
        "Format": "Text (2 bytes)"
      },
      {
        "Name": "\"elemtypoid\" (11 bytes)",
        "TableOid": "1247 (4 bytes)",
        "ColIdx": "1 (2 bytes)",
        "TypeOid": "26 (4 bytes)",
        "ColLen": "4 (2 bytes)",
        "TypeMod": "-1 (4 bytes)",
        "Format": "Text (2 bytes)"
      }
    ]
  }
}
@endjson
```

```plantuml
@startjson
{
  "DataRow (x141)": {
    "Code": "D (1 byte)",
    "Length": "48 (4 bytes)",
    "Fields": "6 (2 bytes)",
    "Columns": [
      {
        "Len": "10 (4 bytes)",
        "Value": "nspname: \"pg_catalog\" (10 bytes)"
      },
      {
        "Len": "2 (4 bytes)",
        "Value": "oid: \"23\" (2 bytes)"
      },
      {
        "Len": "4 (4 bytes)",
        "Value": "typname: \"int4\" (4 bytes)"
      },
      {
        "Len": "1 (4 bytes)",
        "Value": "typtype: \"b\" (1 bytes)"
      },
      {
        "Len": "1 (4 bytes)",
        "Value": "typnotnull: \"f\" (1 bytes)"
      },
      {
        "Len": "-1 (4 bytes)"
      }
    ]
  }
}
@endjson
```


# Packet 12 (46 messages, FrontEnd <-- BackEnd)

```plantuml
@startjson
{
  "DataRow (x35)": {
    "Code": "D (1 byte)",
    "Length": "62 (4 bytes)",
    "Fields": "6 (2 bytes)",
    "Columns": [
      {
        "Len": "10 (4 bytes)",
        "Value": "nspname: \"pg_catalog\" (10 bytes)"
      },
      {
        "Len": "4 (4 bytes)",
        "Value": "oid: \"2209\" (4 bytes)"
      },
      {
        "Len": "12 (4 bytes)",
        "Value": "typname: \"_regoperator\" (12 bytes)"
      },
      {
        "Len": "1 (4 bytes)",
        "Value": "typtype: \"a\" (1 bytes)"
      },
      {
        "Len": "1 (4 bytes)",
        "Value": "typnotnull: \"f\" (1 bytes)"
      },
      {
        "Len": "4 (4 bytes)",
        "Value": "elemtypoid: \"2204\" (4 bytes)"
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
    "Length": "15 (4 bytes)",
    "Tag": "\"SELECT 176\" (11 bytes)"
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
        "Name": "\"oid\" (4 bytes)",
        "TableOid": "1247 (4 bytes)",
        "ColIdx": "1 (2 bytes)",
        "TypeOid": "26 (4 bytes)",
        "ColLen": "4 (2 bytes)",
        "TypeMod": "-1 (4 bytes)",
        "Format": "Text (2 bytes)"
      },
      {
        "Name": "\"attname\" (8 bytes)",
        "TableOid": "1249 (4 bytes)",
        "ColIdx": "2 (2 bytes)",
        "TypeOid": "19 (4 bytes)",
        "ColLen": "64 (2 bytes)",
        "TypeMod": "-1 (4 bytes)",
        "Format": "Text (2 bytes)"
      },
      {
        "Name": "\"atttypid\" (9 bytes)",
        "TableOid": "1249 (4 bytes)",
        "ColIdx": "3 (2 bytes)",
        "TypeOid": "26 (4 bytes)",
        "ColLen": "4 (2 bytes)",
        "TypeMod": "-1 (4 bytes)",
        "Format": "Text (2 bytes)"
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
    "Length": "13 (4 bytes)",
    "Tag": "\"SELECT 0\" (9 bytes)"
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
        "Name": "\"oid\" (4 bytes)",
        "TableOid": "1247 (4 bytes)",
        "ColIdx": "1 (2 bytes)",
        "TypeOid": "26 (4 bytes)",
        "ColLen": "4 (2 bytes)",
        "TypeMod": "-1 (4 bytes)",
        "Format": "Text (2 bytes)"
      },
      {
        "Name": "\"enumlabel\" (10 bytes)",
        "TableOid": "3501 (4 bytes)",
        "ColIdx": "4 (2 bytes)",
        "TypeOid": "19 (4 bytes)",
        "ColLen": "64 (2 bytes)",
        "TypeMod": "-1 (4 bytes)",
        "Format": "Text (2 bytes)"
      }
    ]
  }
}
@endjson
```

```plantuml
@startjson
{
  "DataRow (x5)": {
    "Code": "D (1 byte)",
    "Length": "22 (4 bytes)",
    "Fields": "2 (2 bytes)",
    "Columns": [
      {
        "Len": "7 (4 bytes)",
        "Value": "oid: \"1469004\" (7 bytes)"
      },
      {
        "Len": "1 (4 bytes)",
        "Value": "enumlabel: \"G\" (1 bytes)"
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
    "Length": "13 (4 bytes)",
    "Tag": "\"SELECT 5\" (9 bytes)"
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

