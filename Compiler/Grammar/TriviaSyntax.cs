// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class TriviaSyntax : Syntax, Compiler.IParsable<TriviaSyntax>
{
    public static TriviaSyntax Parse(ref Parser context)
    {
        Parser parser = context;
        if (parser.FailsToConsume<Trivium>()) return null;
        return new TriviaSyntax { Source = parser.Commit(ref context) };
    }
}
