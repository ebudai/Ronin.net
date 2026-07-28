using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Punctuation : Symbol
{
    public new static Punctuation Lex(ref Lexer lexer)
        => Returns.Lex(ref lexer)
        ?? Assignment.Lex(ref lexer)
        ?? Bracket.Lex(ref lexer)
        ?? Separator.Lex(ref lexer)
        ?? Terminal.Lex(ref lexer)
        ?? Question.Lex(ref lexer)
        ?? TextDelimiter.Lex(ref lexer) as Punctuation;

    protected static T Lex<T>(ref Lexer lexer, string symbol) where T : Punctuation, new()
    {
        if (lexer.IsEmpty || lexer.StartsWith(symbol) is false) return null;
        return new T { Memory = lexer.AdvanceBy(symbol.Length) };
    }

    protected static T Lex<T>(ref Lexer lexer, char symbol) where T : Punctuation, new()
    {
        if (lexer.IsEmpty || lexer[0] != symbol) return null;
        return new T { Memory = lexer.AdvanceBy(1) };
    }
}

internal class Returns : Punctuation
{
    internal const string symbol = "=>";

    public new static Returns Lex(ref Lexer lexer) => Lex<Returns>(ref lexer, symbol);
}

/// <summary>
///     The comma between elements, which must be followed by a space.
/// </summary>
///
/// <remarks>
///     The companion to the digit-separator rule in <see cref="Numeric"/>. A
///     comma directly between digits is part of a number, so requiring a space
///     after every separator makes the unspaced form always a number and never a
///     list — «1,234» is one argument and «1, 234» is two, which is how both are
///     already written by hand. Without the rule, inlining a constant into
///     «f(count, 234)» could silently change the call's arity.
/// </remarks>
internal class Separator : Punctuation
{
    internal const char symbol = ',';

    /// <summary>Whether a space followed it, which <c>Aggregate</c> requires.</summary>
    public bool Spaced { get; init; }

    public new static Separator Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not symbol) return null;

        // A comma is still a comma when unspaced — declining to classify it here
        // would leave a bare Symbol that Symbolic absorbs into the reference
        // beside it, turning «f(a,b)» into one argument rather than an error.
        // The rule is the parser's; the token stays honest.
        var spaced = lexer.Length is 1 || char.IsWhiteSpace(lexer[1]);

        return new Separator { Spaced = spaced, Memory = lexer.AdvanceBy(1) };
    }
}

internal class Terminal : Punctuation
{
    internal const char symbol = ';';

    public new static Terminal Lex(ref Lexer lexer) => Lex<Terminal>(ref lexer, symbol);
}

internal class TextDelimiter : Punctuation
{
    internal const char symbol = '"';

    public new static TextDelimiter Lex(ref Lexer lexer) => Lex<TextDelimiter>(ref lexer, symbol);
}

internal class Question : Punctuation
{
    internal const char symbol = '?';

    public new static Question Lex(ref Lexer lexer) => Lex<Question>(ref lexer, symbol);
}
