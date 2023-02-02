// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin;

/// <summary>
///     Parent class of all compiler errors (<see cref="ExpectedSyntaxError{TSeparator, TClose}"/>, <see cref="UnexpectedSyntaxError"/>, and <see cref="UnspecifiedDatatypeError"/>)
/// </summary>
internal class Error : Exception
{
    public int Cursor { get; init; }

    public Error(ref Parser parser)
    {
        do
        {
            parser.Advance();
        }
        while (parser.CurrentToken is not Sentinel and not Terminal and not Separator and not Close);

        Cursor = parser.Index;
    }
}
