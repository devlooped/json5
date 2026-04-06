# Json5

[![Version](https://img.shields.io/nuget/vpre/Json5.svg?color=royalblue)](https://www.nuget.org/packages/Json5)
[![Downloads](https://img.shields.io/nuget/dt/Json5.svg?color=darkmagenta)](https://www.nuget.org/packages/Json5)
[![EULA](https://img.shields.io/badge/EULA-OSMF-blue?labelColor=black&color=C9FF30)](https://github.com/devlooped/oss/blob/main/osmfeula.txt)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/devlooped/oss/blob/main/license.txt)

<!-- include https://github.com/devlooped/.github/raw/main/osmf.md -->
<!-- #content -->

A [JSON5](https://json5.org/) parser for .NET built entirely on top of the public 
`System.Text.Json` API surface. Requires .NET 10+.

## Usage

### Parse to JsonNode

```csharp
using Json5;

JsonNode? node = Json5.Parse("""
    {
        // comments are allowed
        unquoted: 'single-quoted string',
        hex: 0xDECAF,
        leadingDot: .5,
        trailing: 'comma',
    }
    """);
```

### Deserialize to a typed object

```csharp
var config = Json5.Deserialize<AppConfig>(json5String);
```

### Parse to JsonDocument

```csharp
using var doc = Json5.ParseDocument(json5String);
JsonElement root = doc.RootElement;
```

### Convert to standard JSON

```csharp
string json = Json5.ToJson(json5String);
byte[] utf8 = Json5.ToUtf8Json(json5Bytes);
```

### Write to Utf8JsonWriter

```csharp
Json5.WriteTo(json5String, writer);
```

## JSON5 Features

All [JSON5 extensions](https://spec.json5.org/) beyond standard JSON are supported:

| Feature | Example |
|---------|---------|
| Unquoted object keys | `{ foo: 1 }` |
| Single-quoted strings | `'hello'` |
| Multi-line strings (escaped newlines) | `'line1\↵line2'` |
| Hexadecimal numbers | `0xFF` |
| Leading/trailing decimal points | `.5`, `2.` |
| Explicit positive sign | `+1` |
| Infinity, -Infinity, NaN | `Infinity` |
| Single and multi-line comments | `// …` and `/* … */` |
| Trailing commas | `[1, 2,]` |
| Additional escape sequences | `\v`, `\0`, `\xHH` |
| Extended whitespace | Unicode Zs category, BOM |

## Infinity / NaN Handling

Since standard JSON has no representation for `Infinity` and `NaN`, the behavior 
is configurable via `Json5ReaderOptions.SpecialNumbers`:

| Mode | Behavior |
|------|----------|
| `AsString` (default) | Emits as `"Infinity"`, `"-Infinity"`, or `"NaN"` |
| `AsNull` | Emits as `null` |
| `Throw` | Throws `Json5Exception` |

```csharp
var options = new Json5ReaderOptions 
{ 
    SpecialNumbers = SpecialNumberHandling.AsNull 
};
var node = Json5.Parse("Infinity", options); // returns null
```

<!-- #content -->
---
<!-- include https://github.com/devlooped/sponsors/raw/main/footer.md -->