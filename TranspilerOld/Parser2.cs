using Ronin.Transpiler.Program;

namespace Ronin.Transpiler;

// all declarations have the form keyword* id (parameter pack) id2 (parameter pack 2) id3 etc.. { body }
// everything else is a literal, function call, package/using declaration, 
// package first level secondlevel third.
// using first secondlevel.
//
internal class Parser2
{

    public Block Parse(ReadOnlySpan<Token> tokens, Block parent = null)
    {
        Block block = new() { Parent = parent };



        return block;
    }

    

    private ReadOnlySpan<Token> ParseBrackets(ReadOnlySpan<Token> tokens)
    {
        int innerMatchCount = 0;
        var left = tokens[0].Value;

        if (!Syntax.Brackets.TryGetValue(left, out var right)) throw new Parser.Exception("expected bracket");        

        for (int i = 0, max = tokens.Length; i != max; ++i)
        {
            if (tokens[i].Kind is not Token.Type.Symbol) continue;

            if (tokens[i].Value == left)
            {
                ++innerMatchCount;
            }
            else if (tokens[i].Value == right)
            {
                if (innerMatchCount is 0) return tokens[..i];
                --innerMatchCount;
            }
        }

        return ReadOnlySpan<Token>.Empty;
    }

    private Statement ParseParameterBlock(ReadOnlySpan<Token> tokens)
    {
        throw new NotImplementedException();
    }

    private Statement ParseGenericParameterBlock(ReadOnlySpan<Token> tokens)
    {

    }

    private Statement ParseBlock(ReadOnlySpan<Token> tokens)
    {
        throw new NotImplementedException();
    }

    private string[] ParseKeywords(ReadOnlySpan<Token> tokens)
    {
        throw new NotImplementedException();
    }

    private string[] ParseIdentifier(ReadOnlySpan<Token> tokens)
    {
        throw new NotImplementedException();
    }

    private string ParseScalarLiteral(ReadOnlySpan<Token> tokens)
    {
        throw new NotImplementedException();
    }

    private string ParseListLiteral(ReadOnlySpan<Token> tokens, out int index)
    {
        throw new NotImplementedException();
    }

    private string ParseSetLiteral(ReadOnlySpan<Token> tokens, out int index)
    {
        throw new NotImplementedException();
    }

    private string ParseMapLiteral(ReadOnlySpan<Token> tokens, out int index)
    {
        throw new NotImplementedException();
    }
}
