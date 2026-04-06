namespace Json5;

/// <summary>
/// Token types produced by <see cref="Json5Tokenizer"/>.
/// </summary>
enum Json5TokenType
{
    None,
    StartObject,
    EndObject,
    StartArray,
    EndArray,
    PropertyName,
    String,
    Number,
    True,
    False,
    Null,
    Infinity,
    NegativeInfinity,
    PositiveInfinity,
    NaN,
    Comment,
}
