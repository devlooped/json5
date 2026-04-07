using System.Buffers;
using System.Globalization;
using System.Text;

namespace Json5;

/// <summary>
/// A forward-only tokenizer for JSON5 UTF-8 input.
/// </summary>
ref struct Json5Tokenizer
{
    readonly ReadOnlySpan<byte> buffer;
    readonly int maxDepth;
    readonly bool autoDedent;
    int position;
    int line;
    int column;
    int depth;

    public Json5TokenType TokenType { get; private set; }

    // For string/identifier/number tokens, the decoded value.
    public string? StringValue { get; private set; }
    public double NumberValue { get; private set; }
    public bool NumberIsInteger { get; private set; }
    public long IntegerValue { get; private set; }

    public int Line => line;
    public int Column => column;
    public int Position => position;

    public Json5Tokenizer(ReadOnlySpan<byte> utf8, Json5ReaderOptions options)
    {
        buffer = utf8;
        maxDepth = options.MaxDepth > 0 ? options.MaxDepth : 64;
        autoDedent = options.AutoDedent;
        position = 0;
        line = 1;
        column = 1;
        depth = 0;
        TokenType = Json5TokenType.None;
        StringValue = null;
        NumberValue = 0;
        NumberIsInteger = false;
        IntegerValue = 0;
    }

    /// <summary>
    /// Reads the next token. Returns false at end of input.
    /// </summary>
    public bool Read()
    {
        SkipWhitespaceAndComments();

        if (position >= buffer.Length)
        {
            TokenType = Json5TokenType.None;
            return false;
        }

        var b = buffer[position];

        switch (b)
        {
            case (byte)'{':
                Advance();
                depth++;
                if (depth > maxDepth)
                    throw Error($"Maximum depth of {maxDepth} exceeded.");
                TokenType = Json5TokenType.StartObject;
                return true;

            case (byte)'}':
                Advance();
                depth--;
                TokenType = Json5TokenType.EndObject;
                return true;

            case (byte)'[':
                Advance();
                depth++;
                if (depth > maxDepth)
                    throw Error($"Maximum depth of {maxDepth} exceeded.");
                TokenType = Json5TokenType.StartArray;
                return true;

            case (byte)']':
                Advance();
                depth--;
                TokenType = Json5TokenType.EndArray;
                return true;

            case (byte)',':
                Advance();
                // Comma is a separator — read the next actual token
                return Read();

            case (byte)':':
                Advance();
                return Read();

            case (byte)'"':
            case (byte)'\'':
                ReadString(b);
                if (autoDedent) DedentStringValue();
                return true;

            default:
                if (b == (byte)'-' || b == (byte)'+' || b == (byte)'.' || IsDigit(b))
                {
                    ReadNumber();
                    return true;
                }

                if (IsIdentifierStart(b))
                {
                    ReadIdentifierOrKeyword();
                    return true;
                }

                throw Error($"Unexpected character '{(char)b}' (0x{b:X2}).");
        }
    }

    /// <summary>
    /// Reads a property name (identifier or string) in object context.
    /// Returns true if a property name was read, false if the object is ending.
    /// </summary>
    public bool ReadPropertyName()
    {
        SkipWhitespaceAndComments();

        if (position >= buffer.Length)
            throw Error("Unexpected end of input while reading property name.");

        var b = buffer[position];

        if (b == (byte)'}')
            return false;

        if (b == (byte)',')
        {
            Advance();
            SkipWhitespaceAndComments();
            if (position >= buffer.Length)
                throw Error("Unexpected end of input after comma.");
            b = buffer[position];
            if (b == (byte)'}')
                return false;
        }

        if (b == (byte)'"' || b == (byte)'\'')
        {
            ReadString(b);
            TokenType = Json5TokenType.PropertyName;
        }
        else if (IsIdentifierStart(b))
        {
            ReadIdentifier();
            TokenType = Json5TokenType.PropertyName;
        }
        else
        {
            throw Error($"Expected property name, got '{(char)b}' (0x{b:X2}).");
        }

        // Expect colon
        SkipWhitespaceAndComments();
        if (position >= buffer.Length || buffer[position] != (byte)':')
            throw Error("Expected ':' after property name.");
        Advance();

        return true;
    }

    // ── Whitespace & Comments ────────────────────────────────────────

    void SkipWhitespaceAndComments()
    {
        while (position < buffer.Length)
        {
            var b = buffer[position];

            // Standard ASCII whitespace
            if (b == ' ' || b == '\t' || b == '\r' || b == '\n' ||
                b == 0x0B /* vertical tab */ || b == 0x0C /* form feed */)
            {
                if (b == '\n')
                {
                    line++;
                    column = 0;
                }
                else if (b == '\r')
                {
                    line++;
                    column = 0;
                    if (position + 1 < buffer.Length && buffer[position + 1] == '\n')
                    {
                        position++;
                    }
                }
                Advance();
                continue;
            }

            // BOM (U+FEFF in UTF-8: EF BB BF)
            if (b == 0xEF && position + 2 < buffer.Length &&
                buffer[position + 1] == 0xBB && buffer[position + 2] == 0xBF)
            {
                position += 2;
                Advance();
                continue;
            }

            // Non-ASCII whitespace (UTF-8 multi-byte)
            if (b >= 0xC0)
            {
                if (TrySkipUnicodeWhitespace())
                    continue;
            }

            // Comments
            if (b == (byte)'/' && position + 1 < buffer.Length)
            {
                var next = buffer[position + 1];
                if (next == (byte)'/')
                {
                    SkipSingleLineComment();
                    continue;
                }
                if (next == (byte)'*')
                {
                    SkipMultiLineComment();
                    continue;
                }
            }

            break;
        }
    }

    bool TrySkipUnicodeWhitespace()
    {
        // Decode the UTF-8 code point to check for Unicode whitespace
        if (!TryDecodeUtf8(buffer[position..], out var cp, out var bytesConsumed))
            return false;

        if (IsUnicodeWhitespace(cp))
        {
            if (cp == 0x2028 || cp == 0x2029) // Line/Paragraph separator
            {
                line++;
                column = 0;
            }
            position += bytesConsumed;
            column++;
            return true;
        }

        return false;
    }

    static bool IsUnicodeWhitespace(int codePoint) =>
        codePoint == 0x00A0 || // Non-breaking space
        codePoint == 0x1680 || // Ogham space mark
        codePoint == 0x2028 || // Line separator
        codePoint == 0x2029 || // Paragraph separator
        codePoint == 0xFEFF || // BOM
        (codePoint >= 0x2000 && codePoint <= 0x200A) || // En/Em/various spaces
        codePoint == 0x202F || // Narrow no-break space
        codePoint == 0x205F || // Medium mathematical space
        codePoint == 0x3000;   // Ideographic space

    void SkipSingleLineComment()
    {
        position += 2; // skip //
        column += 2;
        while (position < buffer.Length)
        {
            var b = buffer[position];
            if (b == '\n')
            {
                Advance();
                line++;
                column = 1;
                return;
            }
            if (b == '\r')
            {
                Advance();
                line++;
                column = 1;
                if (position < buffer.Length && buffer[position] == '\n')
                    position++;
                return;
            }
            // Check for Unicode line terminators (U+2028, U+2029)
            if (b >= 0xE0 && TryDecodeUtf8(buffer[position..], out var cp, out var consumed))
            {
                if (cp == 0x2028 || cp == 0x2029)
                {
                    position += consumed;
                    line++;
                    column = 1;
                    return;
                }
            }
            Advance();
        }
    }

    void SkipMultiLineComment()
    {
        position += 2; // skip /*
        column += 2;
        while (position < buffer.Length)
        {
            var b = buffer[position];
            if (b == '*' && position + 1 < buffer.Length && buffer[position + 1] == '/')
            {
                position += 2;
                column += 2;
                return;
            }
            if (b == '\n')
            {
                line++;
                column = 0;
            }
            else if (b == '\r')
            {
                line++;
                column = 0;
                if (position + 1 < buffer.Length && buffer[position + 1] == '\n')
                    position++;
            }
            Advance();
        }
        throw Error("Unterminated multi-line comment.");
    }

    // ── String parsing ───────────────────────────────────────────────

    void ReadString(byte quote)
    {
        Advance(); // skip opening quote
        var sb = new StringBuilder();

        while (position < buffer.Length)
        {
            var b = buffer[position];

            if (b == quote)
            {
                Advance(); // skip closing quote
                StringValue = sb.ToString();
                TokenType = Json5TokenType.String;
                return;
            }

            if (b == (byte)'\\')
            {
                ReadEscapeSequence(sb);
                continue;
            }

            // Check for unescaped line terminators (not allowed in strings, except U+2028/U+2029)
            if (b == '\n' || b == '\r')
                throw Error("Unescaped line terminator in string.");

            // Multi-byte UTF-8
            if (b >= 0x80)
            {
                if (TryDecodeUtf8(buffer[position..], out var cp, out var consumed))
                {
                    // U+2028 and U+2029 are allowed unescaped per JSON5 spec
                    sb.Append(char.ConvertFromUtf32(cp));
                    position += consumed;
                    column++;
                    continue;
                }
                throw Error("Invalid UTF-8 sequence in string.");
            }

            sb.Append((char)b);
            Advance();
        }

        throw Error("Unterminated string.");
    }

    void ReadEscapeSequence(StringBuilder sb)
    {
        Advance(); // skip backslash

        if (position >= buffer.Length)
            throw Error("Unexpected end of input in escape sequence.");

        var b = buffer[position];

        switch (b)
        {
            case (byte)'\'': sb.Append('\''); Advance(); break;
            case (byte)'"': sb.Append('"'); Advance(); break;
            case (byte)'\\': sb.Append('\\'); Advance(); break;
            case (byte)'/': sb.Append('/'); Advance(); break;
            case (byte)'b': sb.Append('\b'); Advance(); break;
            case (byte)'f': sb.Append('\f'); Advance(); break;
            case (byte)'n': sb.Append('\n'); Advance(); break;
            case (byte)'r': sb.Append('\r'); Advance(); break;
            case (byte)'t': sb.Append('\t'); Advance(); break;
            case (byte)'v': sb.Append('\v'); Advance(); break;
            case (byte)'0':
                // \0 is null character, but \0 followed by a digit is an error
                Advance();
                if (position < buffer.Length && IsDigit(buffer[position]))
                    throw Error("Decimal digit after \\0 is not allowed.");
                sb.Append('\0');
                break;

            case (byte)'x':
                Advance();
                sb.Append(ReadHexEscape(2));
                break;

            case (byte)'u':
                Advance();
                sb.Append(ReadHexEscape(4));
                break;

            // Line continuation: backslash followed by line terminator
            case (byte)'\n':
                Advance();
                line++;
                column = 1;
                break;
            case (byte)'\r':
                Advance();
                line++;
                column = 1;
                if (position < buffer.Length && buffer[position] == '\n')
                    position++;
                break;

            default:
                // Check for Unicode line terminators (line continuation)
                if (b >= 0xE0 && TryDecodeUtf8(buffer[position..], out var cp, out var consumed))
                {
                    if (cp == 0x2028 || cp == 0x2029)
                    {
                        position += consumed;
                        line++;
                        column = 1;
                        break;
                    }
                }

                // Identity escape: digits 1-9 are not allowed
                if (b >= (byte)'1' && b <= (byte)'9')
                    throw Error($"Invalid escape sequence '\\{(char)b}'.");

                // Any other character: identity escape (the character itself, without backslash)
                if (b >= 0x80)
                {
                    if (TryDecodeUtf8(buffer[position..], out cp, out consumed))
                    {
                        sb.Append(char.ConvertFromUtf32(cp));
                        position += consumed;
                        column++;
                        break;
                    }
                }
                sb.Append((char)b);
                Advance();
                break;
        }
    }

    char ReadHexEscape(int digits)
    {
        if (position + digits > buffer.Length)
            throw Error($"Expected {digits} hex digits in escape sequence.");

        var value = 0;
        for (var i = 0; i < digits; i++)
        {
            var h = buffer[position];
            if (!IsHexDigit(h))
                throw Error($"Expected hex digit, got '{(char)h}'.");
            value = (value << 4) | HexValue(h);
            Advance();
        }
        return (char)value;
    }

    // ── Auto-dedent ──────────────────────────────────────────────────

    void DedentStringValue()
    {
        var s = StringValue;
        if (s is null) return;

        // Fast path: no newline at all — nothing to dedent
        if (s.IndexOf('\n') < 0) return;

        var span = s.AsSpan();

        // ── Pass 1: count lines; detect first/last blank lines ───────
        var totalLines = 0;
        var firstLineBlank = false;
        var lastLineBlank = false;

        var pos = 0;
        while (pos <= span.Length)
        {
            var lineStart = pos;
            var lineEnd = pos;
            while (lineEnd < span.Length && span[lineEnd] != '\n' && span[lineEnd] != '\r')
                lineEnd++;

            var isBlank = IsLineAllWhitespace(span, lineStart, lineEnd - lineStart);

            if (totalLines == 0) firstLineBlank = isBlank;
            lastLineBlank = isBlank;
            totalLines++;

            if (lineEnd >= span.Length) break;
            pos = AdvancePastNewline(span, lineEnd);
        }

        var effectiveFirst = firstLineBlank ? 1 : 0;
        var effectiveLast = lastLineBlank && totalLines - 1 > effectiveFirst
            ? totalLines - 2
            : totalLines - 1;

        if (effectiveFirst > effectiveLast)
        {
            StringValue = string.Empty;
            return;
        }

        // ── Pass 2: find minimum indent across non-blank lines ───────
        var minIndent = int.MaxValue;
        pos = 0;
        var lineIdx = 0;
        while (pos <= span.Length)
        {
            var lineStart = pos;
            var lineEnd = pos;
            while (lineEnd < span.Length && span[lineEnd] != '\n' && span[lineEnd] != '\r')
                lineEnd++;

            if (lineIdx >= effectiveFirst && lineIdx <= effectiveLast)
            {
                var lineLen = lineEnd - lineStart;
                if (!IsLineAllWhitespace(span, lineStart, lineLen))
                {
                    var indent = 0;
                    while (indent < lineLen && (span[lineStart + indent] == ' ' || span[lineStart + indent] == '\t'))
                        indent++;
                    if (indent < minIndent) minIndent = indent;
                }
            }

            lineIdx++;
            if (lineEnd >= span.Length) break;
            pos = AdvancePastNewline(span, lineEnd);
        }

        if (minIndent == int.MaxValue) minIndent = 0;

        // Fast path: nothing to strip
        if (minIndent == 0 && effectiveFirst == 0 && effectiveLast == totalLines - 1)
            return;

        // ── Pass 3: build dedented result ────────────────────────────
        var sb = new StringBuilder(s.Length);
        pos = 0;
        lineIdx = 0;
        var needSeparator = false;
        while (pos <= span.Length)
        {
            var lineStart = pos;
            var lineEnd = pos;
            while (lineEnd < span.Length && span[lineEnd] != '\n' && span[lineEnd] != '\r')
                lineEnd++;

            if (lineIdx >= effectiveFirst && lineIdx <= effectiveLast)
            {
                if (needSeparator) sb.Append('\n');
                needSeparator = true;

                var lineLen = lineEnd - lineStart;
                var skip = IsLineAllWhitespace(span, lineStart, lineLen)
                    ? lineLen
                    : Math.Min(minIndent, lineLen);
                var contentLen = lineLen - skip;
                if (contentLen > 0)
                    sb.Append(span.Slice(lineStart + skip, contentLen));
            }

            lineIdx++;
            if (lineEnd >= span.Length) break;
            pos = AdvancePastNewline(span, lineEnd);
        }

        StringValue = sb.ToString();
    }

    static bool IsLineAllWhitespace(ReadOnlySpan<char> span, int start, int len)
    {
        for (var i = start; i < start + len; i++)
            if (span[i] != ' ' && span[i] != '\t') return false;
        return true;
    }

    static int AdvancePastNewline(ReadOnlySpan<char> span, int pos)
    {
        if (pos >= span.Length) return pos;
        if (span[pos] == '\r')
        {
            pos++;
            if (pos < span.Length && span[pos] == '\n') pos++;
        }
        else pos++; // '\n'
        return pos;
    }


    // ── Number parsing ───────────────────────────────────────────────

    void ReadNumber()
    {
        var start = position;
        var negative = false;
        var hasPlus = false;

        // Handle sign
        if (buffer[position] == (byte)'-')
        {
            negative = true;
            Advance();
        }
        else if (buffer[position] == (byte)'+')
        {
            hasPlus = true;
            Advance();
        }

        if (position >= buffer.Length)
            throw Error("Unexpected end of input in number.");

        var b = buffer[position];

        // Check for Infinity
        if (b == (byte)'I')
        {
            ExpectLiteral("Infinity"u8);
            if (negative)
                TokenType = Json5TokenType.NegativeInfinity;
            else if (hasPlus)
                TokenType = Json5TokenType.PositiveInfinity;
            else
                TokenType = Json5TokenType.Infinity;
            NumberValue = negative ? double.NegativeInfinity : double.PositiveInfinity;
            return;
        }

        // Check for NaN (with optional sign)
        if (b == (byte)'N')
        {
            ExpectLiteral("NaN"u8);
            TokenType = Json5TokenType.NaN;
            NumberValue = double.NaN;
            return;
        }

        // Hexadecimal: 0x or 0X
        if (b == (byte)'0' && position + 1 < buffer.Length &&
            (buffer[position + 1] == (byte)'x' || buffer[position + 1] == (byte)'X'))
        {
            Advance(); // '0'
            Advance(); // 'x'
            ReadHexNumber(negative);
            return;
        }

        // Decimal number
        ReadDecimalNumber(negative);
    }

    void ReadHexNumber(bool negative)
    {
        if (position >= buffer.Length || !IsHexDigit(buffer[position]))
            throw Error("Expected hex digit after '0x'.");

        long value = 0;
        while (position < buffer.Length && IsHexDigit(buffer[position]))
        {
            value = (value << 4) | (long)(uint)HexValue(buffer[position]);
            Advance();
        }

        if (negative) value = -value;
        IntegerValue = value;
        NumberValue = value;
        NumberIsInteger = true;
        TokenType = Json5TokenType.Number;
    }

    void ReadDecimalNumber(bool negative)
    {
        var sb = new StringBuilder();
        if (negative) sb.Append('-');

        var hasInteger = false;
        var hasFraction = false;
        var hasExponent = false;

        // Integer part (may be absent if starts with '.')
        if (position < buffer.Length && IsDigit(buffer[position]))
        {
            hasInteger = true;
            while (position < buffer.Length && IsDigit(buffer[position]))
            {
                sb.Append((char)buffer[position]);
                Advance();
            }
        }

        // Fraction part
        if (position < buffer.Length && buffer[position] == (byte)'.')
        {
            hasFraction = true;
            sb.Append('.');
            Advance();

            if (position < buffer.Length && IsDigit(buffer[position]))
            {
                while (position < buffer.Length && IsDigit(buffer[position]))
                {
                    sb.Append((char)buffer[position]);
                    Advance();
                }
            }
            else if (!hasInteger)
            {
                throw Error("Invalid number: no digits.");
            }
        }

        if (!hasInteger && !hasFraction)
            throw Error("Invalid number.");

        // Exponent part
        if (position < buffer.Length && (buffer[position] == (byte)'e' || buffer[position] == (byte)'E'))
        {
            hasExponent = true;
            sb.Append((char)buffer[position]);
            Advance();

            if (position < buffer.Length && (buffer[position] == (byte)'+' || buffer[position] == (byte)'-'))
            {
                sb.Append((char)buffer[position]);
                Advance();
            }

            if (position >= buffer.Length || !IsDigit(buffer[position]))
                throw Error("Expected digit in exponent.");

            while (position < buffer.Length && IsDigit(buffer[position]))
            {
                sb.Append((char)buffer[position]);
                Advance();
            }
        }

        var numStr = sb.ToString();

        // Try to parse as integer if no fraction/exponent
        if (!hasFraction && !hasExponent &&
            long.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longVal))
        {
            IntegerValue = longVal;
            NumberValue = longVal;
            NumberIsInteger = true;
        }
        else
        {
            NumberValue = double.Parse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture);
            NumberIsInteger = false;
        }

        StringValue = numStr;
        TokenType = Json5TokenType.Number;
    }

    // ── Identifier / keyword parsing ─────────────────────────────────

    void ReadIdentifierOrKeyword()
    {
        ReadIdentifier();

        // Check for keywords
        switch (StringValue)
        {
            case "true":
                TokenType = Json5TokenType.True;
                break;
            case "false":
                TokenType = Json5TokenType.False;
                break;
            case "null":
                TokenType = Json5TokenType.Null;
                break;
            case "Infinity":
                TokenType = Json5TokenType.Infinity;
                NumberValue = double.PositiveInfinity;
                break;
            case "NaN":
                TokenType = Json5TokenType.NaN;
                NumberValue = double.NaN;
                break;
            default:
                // It's an identifier in value context — this is an error
                throw Error($"Unexpected identifier '{StringValue}' in value context.");
        }
    }

    void ReadIdentifier()
    {
        var sb = new StringBuilder();
        var b = buffer[position];

        // First character: may be a Unicode escape
        if (b == (byte)'\\')
        {
            Advance();
            if (position >= buffer.Length || buffer[position] != (byte)'u')
                throw Error("Expected 'u' after '\\' in identifier.");
            Advance();
            var ch = ReadHexEscape(4);
            if (!IsIdentifierStartChar(ch))
                throw Error($"Invalid identifier start character '\\u{(int)ch:X4}'.");
            sb.Append(ch);
        }
        else if (b == (byte)'$' || b == (byte)'_')
        {
            sb.Append((char)b);
            Advance();
        }
        else if (b >= 0x80)
        {
            // Multi-byte UTF-8 identifier start
            if (TryDecodeUtf8(buffer[position..], out var cp, out var consumed))
            {
                if (!IsIdentifierStartCodePoint(cp))
                    throw Error($"Invalid identifier start character U+{cp:X4}.");
                sb.Append(char.ConvertFromUtf32(cp));
                position += consumed;
                column++;
            }
            else
            {
                throw Error("Invalid UTF-8 in identifier.");
            }
        }
        else
        {
            sb.Append((char)b);
            Advance();
        }

        // Subsequent characters
        while (position < buffer.Length)
        {
            b = buffer[position];

            if (b == (byte)'\\')
            {
                Advance();
                if (position >= buffer.Length || buffer[position] != (byte)'u')
                    throw Error("Expected 'u' after '\\' in identifier.");
                Advance();
                var ch = ReadHexEscape(4);
                if (!IsIdentifierPartChar(ch))
                    throw Error($"Invalid identifier character '\\u{(int)ch:X4}'.");
                sb.Append(ch);
                continue;
            }

            if (b == (byte)'$' || b == (byte)'_' || IsAsciiLetterOrDigit(b))
            {
                sb.Append((char)b);
                Advance();
                continue;
            }

            if (b >= 0x80)
            {
                if (TryDecodeUtf8(buffer[position..], out var cp, out var consumed))
                {
                    if (IsIdentifierPartCodePoint(cp))
                    {
                        sb.Append(char.ConvertFromUtf32(cp));
                        position += consumed;
                        column++;
                        continue;
                    }
                }
                break;
            }

            break;
        }

        StringValue = sb.ToString();
        TokenType = Json5TokenType.String; // Will be re-set by caller if used as property name
    }

    // ── Public helpers for backends ─────────────────────────────────

    /// <summary>
    /// Consumes the closing bracket ('}' or ']') that ReadPropertyName/Read
    /// has already detected but not advanced past.
    /// </summary>
    public void ConsumeEndToken()
    {
        SkipWhitespaceAndComments();
        if (position < buffer.Length)
        {
            var b = buffer[position];
            if (b == (byte)'}' || b == (byte)']')
            {
                Advance();
                depth--;
            }
        }
    }

    /// <summary>
    /// After parsing the root value, verifies there's no trailing non-whitespace/comment content.
    /// </summary>
    public void SkipTrailingContent()
    {
        SkipWhitespaceAndComments();
        if (position < buffer.Length)
        {
            throw Error($"Unexpected content after JSON5 value: '{(char)buffer[position]}'.");
        }
    }

    // ── Utility methods ──────────────────────────────────────────────

    void Advance()
    {
        position++;
        column++;
    }

    void ExpectLiteral(ReadOnlySpan<byte> expected)
    {
        if (position + expected.Length > buffer.Length)
            throw Error($"Unexpected end of input.");

        for (var i = 0; i < expected.Length; i++)
        {
            if (buffer[position + i] != expected[i])
                throw Error($"Expected '{Encoding.UTF8.GetString(expected)}'.");
        }

        position += expected.Length;
        column += expected.Length;
    }

    Json5Exception Error(string message) =>
        new($"{message} (line {line}, col {column})", position, line, column);

    static bool IsDigit(byte b) => b >= (byte)'0' && b <= (byte)'9';
    static bool IsHexDigit(byte b) =>
        (b >= (byte)'0' && b <= (byte)'9') ||
        (b >= (byte)'a' && b <= (byte)'f') ||
        (b >= (byte)'A' && b <= (byte)'F');

    static int HexValue(byte b) => b switch
    {
        >= (byte)'0' and <= (byte)'9' => b - '0',
        >= (byte)'a' and <= (byte)'f' => b - 'a' + 10,
        >= (byte)'A' and <= (byte)'F' => b - 'A' + 10,
        _ => throw new InvalidOperationException()
    };

    static bool IsAsciiLetterOrDigit(byte b) =>
        (b >= (byte)'a' && b <= (byte)'z') ||
        (b >= (byte)'A' && b <= (byte)'Z') ||
        (b >= (byte)'0' && b <= (byte)'9');

    static bool IsIdentifierStart(byte b) =>
        (b >= (byte)'a' && b <= (byte)'z') ||
        (b >= (byte)'A' && b <= (byte)'Z') ||
        b == (byte)'$' || b == (byte)'_' || b == (byte)'\\' ||
        b >= 0x80; // potential multi-byte Unicode

    static bool IsIdentifierStartChar(char ch) =>
        char.IsLetter(ch) || ch == '$' || ch == '_';

    static bool IsIdentifierPartChar(char ch) =>
        char.IsLetterOrDigit(ch) || ch == '$' || ch == '_' ||
        CharUnicodeInfo.GetUnicodeCategory(ch) is
            UnicodeCategory.NonSpacingMark or
            UnicodeCategory.SpacingCombiningMark or
            UnicodeCategory.ConnectorPunctuation;

    static bool IsIdentifierStartCodePoint(int cp)
    {
        var cat = CharUnicodeInfo.GetUnicodeCategory(char.ConvertFromUtf32(cp), 0);
        return cat is UnicodeCategory.UppercaseLetter or
            UnicodeCategory.LowercaseLetter or
            UnicodeCategory.TitlecaseLetter or
            UnicodeCategory.ModifierLetter or
            UnicodeCategory.OtherLetter or
            UnicodeCategory.LetterNumber;
    }

    static bool IsIdentifierPartCodePoint(int cp)
    {
        var cat = CharUnicodeInfo.GetUnicodeCategory(char.ConvertFromUtf32(cp), 0);
        return cat is UnicodeCategory.UppercaseLetter or
            UnicodeCategory.LowercaseLetter or
            UnicodeCategory.TitlecaseLetter or
            UnicodeCategory.ModifierLetter or
            UnicodeCategory.OtherLetter or
            UnicodeCategory.LetterNumber or
            UnicodeCategory.NonSpacingMark or
            UnicodeCategory.SpacingCombiningMark or
            UnicodeCategory.DecimalDigitNumber or
            UnicodeCategory.ConnectorPunctuation;
    }

    static bool TryDecodeUtf8(ReadOnlySpan<byte> bytes, out int codePoint, out int bytesConsumed)
    {
        codePoint = 0;
        bytesConsumed = 0;

        if (bytes.IsEmpty) return false;

        var b0 = bytes[0];

        if (b0 < 0x80)
        {
            codePoint = b0;
            bytesConsumed = 1;
            return true;
        }

        if ((b0 & 0xE0) == 0xC0 && bytes.Length >= 2)
        {
            codePoint = ((b0 & 0x1F) << 6) | (bytes[1] & 0x3F);
            bytesConsumed = 2;
            return true;
        }

        if ((b0 & 0xF0) == 0xE0 && bytes.Length >= 3)
        {
            codePoint = ((b0 & 0x0F) << 12) | ((bytes[1] & 0x3F) << 6) | (bytes[2] & 0x3F);
            bytesConsumed = 3;
            return true;
        }

        if ((b0 & 0xF8) == 0xF0 && bytes.Length >= 4)
        {
            codePoint = ((b0 & 0x07) << 18) | ((bytes[1] & 0x3F) << 12) | ((bytes[2] & 0x3F) << 6) | (bytes[3] & 0x3F);
            bytesConsumed = 4;
            return true;
        }

        return false;
    }
}
