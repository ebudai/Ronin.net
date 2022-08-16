using Ronin.Grammar;
using System.Text.RegularExpressions;

namespace Ronin.Parser;

internal static class DeclarationParser
{
    internal static Declaration Parse(Context context)
    {
        var lexed = context.Lex(Form.declaration);
        return lexed is null ? null : new(replacewhitespace.Replace(lexed, " "));
    }

    private static readonly Regex replacewhitespace = new(@"\s+", RegexOptions.Compiled);
}
