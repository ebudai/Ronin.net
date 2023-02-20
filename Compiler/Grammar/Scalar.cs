// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Scalar : Syntax, Compiler.IParsable<Scalar>
{
    public static Scalar Parse(ref Parser context)
    {
        Parser parser = context;
        List<Literal> values = new();

        while (parser.IsNotFinished)
        {            
            if (parser.CurrentToken is not Literal literal) break;
            parser.Advance();
            values.Add(literal);   
        }

        if (parser.CurrentToken == context.CurrentToken) return null;

        return new Scalar { Source = parser.Commit(ref context) };
    }
}