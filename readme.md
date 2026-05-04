![Icon](assets/img/icon.png) Json5
============

[![Version](https://img.shields.io/nuget/vpre/Json5.svg?color=royalblue)](https://www.nuget.org/packages/Json5)
[![Downloads](https://img.shields.io/nuget/dt/Json5.svg?color=darkmagenta)](https://www.nuget.org/packages/Json5)
[![EULA](https://img.shields.io/badge/EULA-OSMF-blue?labelColor=black&color=C9FF30)](https://github.com/devlooped/oss/blob/main/osmfeula.txt)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/devlooped/oss/blob/main/license.txt)

<!-- include https://github.com/devlooped/.github/raw/main/osmf.md -->
## Open Source Maintenance Fee

To ensure the long-term sustainability of this project, users of this package who generate 
revenue must pay an [Open Source Maintenance Fee](https://opensourcemaintenancefee.org). 
While the source code is freely available under the terms of the [License](license.txt), 
this package and other aspects of the project require [adherence to the Maintenance Fee](osmfeula.txt).

To pay the Maintenance Fee, [become a Sponsor](https://github.com/sponsors/devlooped) at the proper 
OSMF tier. A single fee covers all of [Devlooped packages](https://www.nuget.org/profiles/Devlooped).

<!-- https://github.com/devlooped/.github/raw/main/osmf.md -->
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

## Auto-Dedent Multi-line Strings

When parsing multi-line string values, you can automatically remove common leading 
whitespace via `Json5ReaderOptions.AutoDedent`. This makes it easier to write readable 
indented code without the indentation appearing in the final string value.

```csharp
var options = new Json5ReaderOptions { AutoDedent = true };
var node = Json5.Parse("""
    {
        description: '
            This is a multi-line string
            with consistent indentation
            that will be removed.
        '
    }
    """, options);
// description will be: "This is a multi-line string\nwith consistent indentation\nthat will be removed."
```

The algorithm:
1. Strips the first line if blank
2. Strips the last line if blank  
3. Finds the minimum common leading whitespace across all remaining non-blank lines
4. Removes that minimum indent from every line

Only string values are affected; property names are never dedented. Blank lines 
within the string are preserved.

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

![Icon](assets/img/icon.png) Json5.Configuration
============

[![Version](https://img.shields.io/nuget/vpre/Json5.Configuration.svg?color=royalblue)](https://www.nuget.org/packages/Json5.Configuration)
[![Downloads](https://img.shields.io/nuget/dt/Json5.Configuration.svg?color=darkmagenta)](https://www.nuget.org/packages/Json5.Configuration)

<!-- #configuration -->
The `Json5.Configuration` package integrates [JSON5](https://json5.org/) files with the 
[Microsoft.Extensions.Configuration](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) 
infrastructure, so you can use JSON5 anywhere standard JSON configuration is used.

### AddJson5File

```csharp
using Json5;

var config = new ConfigurationBuilder()
    .AddJson5File("appsettings.json5")
    .AddJson5File("appsettings.Development.json5", optional: true, reloadOnChange: true)
    .Build();
```

The file supports all [JSON5 extensions](https://spec.json5.org/) — comments, trailing commas, unquoted keys, etc.:

```json5
// appsettings.json5
{
    Logging: {
        LogLevel: {
            Default: "Information",
            // Silence noisy namespaces
            "Microsoft.AspNetCore": "Warning",
        },
    },
    ConnectionStrings: {
        Default: "Server=localhost;Database=MyApp",
    },
}
```

### AddJson5Stream

```csharp
using Json5;

using var stream = File.OpenRead("config.json5");

var config = new ConfigurationBuilder()
    .AddJson5Stream(stream)
    .Build();
```

### Json5ReaderOptions

Pass `Json5ReaderOptions` via the source action to control special number handling, max depth, and auto-dedent:

```csharp
var config = new ConfigurationBuilder()
    .AddJson5File(source =>
    {
        source.Path = "appsettings.json5";
        source.Optional = true;
        source.ReloadOnChange = true;
        source.Json5ReaderOptions = new Json5ReaderOptions
        {
            AutoDedent = true,
            SpecialNumbers = SpecialNumberHandling.AsNull
        };
    })
    .Build();
```

<!-- #configuration -->
---
<!-- include https://github.com/devlooped/sponsors/raw/main/footer.md -->
# Sponsors 

<!-- sponsors.md -->
[![Clarius Org](https://avatars.githubusercontent.com/u/71888636?v=4&s=39 "Clarius Org")](https://github.com/clarius)
[![MFB Technologies, Inc.](https://avatars.githubusercontent.com/u/87181630?v=4&s=39 "MFB Technologies, Inc.")](https://github.com/MFB-Technologies-Inc)
[![Khamza Davletov](https://avatars.githubusercontent.com/u/13615108?u=11b0038e255cdf9d1940fbb9ae9d1d57115697ab&v=4&s=39 "Khamza Davletov")](https://github.com/khamza85)
[![SandRock](https://avatars.githubusercontent.com/u/321868?u=99e50a714276c43ae820632f1da88cb71632ec97&v=4&s=39 "SandRock")](https://github.com/sandrock)
[![DRIVE.NET, Inc.](https://avatars.githubusercontent.com/u/15047123?v=4&s=39 "DRIVE.NET, Inc.")](https://github.com/drivenet)
[![Keith Pickford](https://avatars.githubusercontent.com/u/16598898?u=64416b80caf7092a885f60bb31612270bffc9598&v=4&s=39 "Keith Pickford")](https://github.com/Keflon)
[![Thomas Bolon](https://avatars.githubusercontent.com/u/127185?u=7f50babfc888675e37feb80851a4e9708f573386&v=4&s=39 "Thomas Bolon")](https://github.com/tbolon)
[![Kori Francis](https://avatars.githubusercontent.com/u/67574?u=3991fb983e1c399edf39aebc00a9f9cd425703bd&v=4&s=39 "Kori Francis")](https://github.com/kfrancis)
[![Reuben Swartz](https://avatars.githubusercontent.com/u/724704?u=2076fe336f9f6ad678009f1595cbea434b0c5a41&v=4&s=39 "Reuben Swartz")](https://github.com/rbnswartz)
[![Jacob Foshee](https://avatars.githubusercontent.com/u/480334?v=4&s=39 "Jacob Foshee")](https://github.com/jfoshee)
[![](https://avatars.githubusercontent.com/u/33566379?u=bf62e2b46435a267fa246a64537870fd2449410f&v=4&s=39 "")](https://github.com/Mrxx99)
[![Eric Johnson](https://avatars.githubusercontent.com/u/26369281?u=41b560c2bc493149b32d384b960e0948c78767ab&v=4&s=39 "Eric Johnson")](https://github.com/eajhnsn1)
[![Jonathan ](https://avatars.githubusercontent.com/u/5510103?u=98dcfbef3f32de629d30f1f418a095bf09e14891&v=4&s=39 "Jonathan ")](https://github.com/Jonathan-Hickey)
[![Ken Bonny](https://avatars.githubusercontent.com/u/6417376?u=569af445b6f387917029ffb5129e9cf9f6f68421&v=4&s=39 "Ken Bonny")](https://github.com/KenBonny)
[![Simon Cropp](https://avatars.githubusercontent.com/u/122666?v=4&s=39 "Simon Cropp")](https://github.com/SimonCropp)
[![agileworks-eu](https://avatars.githubusercontent.com/u/5989304?v=4&s=39 "agileworks-eu")](https://github.com/agileworks-eu)
[![Zheyu Shen](https://avatars.githubusercontent.com/u/4067473?v=4&s=39 "Zheyu Shen")](https://github.com/arsdragonfly)
[![Vezel](https://avatars.githubusercontent.com/u/87844133?v=4&s=39 "Vezel")](https://github.com/vezel-dev)
[![ChilliCream](https://avatars.githubusercontent.com/u/16239022?v=4&s=39 "ChilliCream")](https://github.com/ChilliCream)
[![4OTC](https://avatars.githubusercontent.com/u/68428092?v=4&s=39 "4OTC")](https://github.com/4OTC)
[![domischell](https://avatars.githubusercontent.com/u/66068846?u=0a5c5e2e7d90f15ea657bc660f175605935c5bea&v=4&s=39 "domischell")](https://github.com/DominicSchell)
[![Adrian Alonso](https://avatars.githubusercontent.com/u/2027083?u=129cf516d99f5cb2fd0f4a0787a069f3446b7522&v=4&s=39 "Adrian Alonso")](https://github.com/adalon)
[![torutek](https://avatars.githubusercontent.com/u/33917059?v=4&s=39 "torutek")](https://github.com/torutek)
[![Ryan McCaffery](https://avatars.githubusercontent.com/u/16667079?u=c0daa64bb5c1b572130e05ae2b6f609ecc912d4d&v=4&s=39 "Ryan McCaffery")](https://github.com/mccaffers)
[![Seika Logiciel](https://avatars.githubusercontent.com/u/2564602?v=4&s=39 "Seika Logiciel")](https://github.com/SeikaLogiciel)
[![Andrew Grant](https://avatars.githubusercontent.com/devlooped-user?s=39 "Andrew Grant")](https://github.com/wizardness)
[![eska-gmbh](https://avatars.githubusercontent.com/devlooped-team?s=39 "eska-gmbh")](https://github.com/eska-gmbh)


<!-- sponsors.md -->
[![Sponsor this project](https://avatars.githubusercontent.com/devlooped-sponsor?s=118 "Sponsor this project")](https://github.com/sponsors/devlooped)

[Learn more about GitHub Sponsors](https://github.com/sponsors)

<!-- https://github.com/devlooped/sponsors/raw/main/footer.md -->
