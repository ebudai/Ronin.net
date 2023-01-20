// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Errors;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

/// <summary>
///     Parent class of all compiler errors (<see cref="ExpectedSyntaxError{TSeparator, TClose}"/>, <see cref="UnexpectedSyntaxError"/>, and <see cref="UnspecifiedDatatypeError"/>)
/// </summary>
internal abstract class Error : Exception
{
    public int Cursor { get; init; }

    public Error(ref Parser parser)
    {
        do
        {
            parser.Advance();
        }
        while (parser.Current is not Sentinel and not Terminal and not Separator and not Close);

        Cursor = parser.Index;
    }
}
