using Ronin.Grammar;

using static Ronin.Parser.Form;

namespace Ronin.Parser;

internal static class SymbolParser
{
    internal static Symbol Parse(Context context) => context.Lex(symbol) switch
    {
        terminal => new Terminal { Value = terminal },
        separator => new Separator { Value = separator },
        groupingopen => new OpeningParenthesis { Value = groupingopen },
        listopen => new OpeningSquareBracket { Value = listopen },
        scopeopen => new OpeningBrace { Value = scopeopen },
        groupingclose => new ClosingParenthesis { Value = groupingclose },
        listclose => new ClosingSquareBracket { Value = listclose },
        scopeclose => new ClosingBrace { Value = scopeclose },
        _ => null
    };
}
