// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     Catch-all class for any unparsable <see cref="Token"/>s
/// </summary>
internal class Unknown : Statement, IParsableSyntax<Unknown>
{
    public new static Unknown Parse(ref Parser current)
    {
        Parser parser = current;

        if (parser.Token is Sentinel or Terminal or Separator or CloseSquareBracket or CloseBrace or CloseParenthesis) return null;

        do
        {
            parser.Advance();
        }
        while (parser.Token is not Sentinel and not Terminal and not Separator and not CloseSquareBracket and not CloseBrace and not CloseParenthesis);

        return new Unknown { Source = parser.Commit(ref current) };
    }
}