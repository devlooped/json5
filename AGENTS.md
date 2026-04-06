# Json5 — Agent Instructions

## Project Overview

A JSON5 parser for .NET 10+ built on top of the public `System.Text.Json` API surface.
Parses [JSON5](https://spec.json5.org/) input and produces standard `System.Text.Json` types 
(`JsonNode`, `JsonDocument`, deserialized objects, or `Utf8JsonWriter` output).

## Architecture

```
                                ┌─ Json5NodeBuilder ──► JsonNode
JSON5 input ──► Json5Tokenizer ─┤
                                └─ Json5Writer ──► Utf8JsonWriter
```

### Key Components

| File | Role |
|------|------|
| `src/Json5/Json5.cs` | Public static API — all user-facing methods |
| `src/Json5/Json5Tokenizer.cs` | Core JSON5 tokenizer (`ref struct`, UTF-8 input) |
| `src/Json5/Json5NodeBuilder.cs` | Builds `JsonNode` tree from tokenizer output |
| `src/Json5/Json5Writer.cs` | Writes to `Utf8JsonWriter` from tokenizer output |
| `src/Json5/Json5ReaderOptions.cs` | Options + `SpecialNumberHandling` enum |
| `src/Json5/Json5Exception.cs` | Exception with line/column/position |
| `src/Json5/Json5TokenType.cs` | Internal token type enum |

### Dual-Path Design

- **Parse()** → `Json5NodeBuilder` path (direct `JsonNode` construction, no intermediate bytes)
- **Deserialize\<T\>()** → `Json5Writer` path (buffer → `JsonSerializer.Deserialize<T>()`)
- **ParseDocument()** → `Json5Writer` path (`JsonDocument` requires JSON bytes)
- **WriteTo()** → `Json5Writer` path (streaming to caller's writer)
- **ToJson() / ToUtf8Json()** → `Json5Writer` path (direct serialization)

### Infinity/NaN Handling

Configurable via `SpecialNumberHandling`: `AsString` (default), `AsNull`, or `Throw`.

## Build & Test

```
dotnet build
dotnet test
```

Target framework: `net10.0`. Tests use xUnit.

## Key Design Decisions

- `Json5Tokenizer` is a `ref struct` operating on `ReadOnlySpan<byte>` (UTF-8)
- Two read modes: `Read()` for values, `ReadPropertyName()` for object keys
- Commas/colons are consumed transparently by the tokenizer
- Identifier classification uses `CharUnicodeInfo.GetUnicodeCategory` for ES5.1 compliance
- Manual UTF-8 decoding for multi-byte Unicode characters
