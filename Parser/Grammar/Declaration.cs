using System.Text.RegularExpressions;

namespace Ronin.Parser.Grammar;

internal class Declaration : Identifier
{
    internal Declaration(string name) : base(name) { }

    internal new static Declaration Parse(Context context)
    {
        var lexed = context.Lex(declaration);
        return lexed is null ? null : new(replacewhitespace.Replace(lexed, " "));
    }

    private static readonly Regex replacewhitespace = new(@"\s+", RegexOptions.Compiled);
}
