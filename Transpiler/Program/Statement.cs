namespace Ronin.Transpiler.Program;

internal abstract class Statement 
{
    public abstract int Construct(ReadOnlySpan<Token> tokens, Block block, Parser parser);

    public override string ToString() => string.Empty;

    protected static string GetIdentifier(ReadOnlySpan<Token> tokens, out int index)
    {
        index = 0;
        string name = string.Empty;
        if (tokens.IsEmpty) return name;
        while (index < tokens.Length && tokens[index].Kind is Token.Type.Identifier or Token.Type.Keyword) name += tokens[index++].Value + " ";
        return name.Trim();
    }

    protected static int GetIdentifierLength(ReadOnlySpan<Token> tokens)
    {
        int length = 0;
        while (length < tokens.Length && tokens[length].Kind is Token.Type.Identifier) ++length;
        return length;
    }

    protected static string[] GetTupleIdentifiers(ReadOnlySpan<Token> tokens, out int index)
    {
        index = 1;
        var name = string.Empty;
        List<string> names = new(16);
        while (tokens[index].Value is not Syntax.TupleEnd)
        {
            if (tokens[index].Value is not Syntax.Separator)
            {
                name += tokens[index].Value + " ";
            }
            else
            {
                names.Add(name.Trim());
                name = string.Empty;
            }
            ++index;
        }
        if (name is not "")
        {
            names.Add(name.Trim());
            ++index;
        }
        return names.ToArray();
    }
}

internal class DeclareDeconstructVar : Statement
{
    public override int Construct(ReadOnlySpan<Token> tokens, Block block, Parser parser)
    {
        throw new NotImplementedException();
    }
}

internal class DeclareType : Statement
{
    public override int Construct(ReadOnlySpan<Token> tokens, Block block, Parser parser)
    {
        throw new NotImplementedException();
    }
}

internal class DeclareScope : Statement
{
    public override int Construct(ReadOnlySpan<Token> tokens, Block block, Parser parser)
    {
        throw new NotImplementedException();
    }
}

internal class Variable : Statement
{
    public override int Construct(ReadOnlySpan<Token> tokens, Block block, Parser parser)
    {
        throw new NotImplementedException();
    }
}

internal class Literal : Statement
{
    public override int Construct(ReadOnlySpan<Token> tokens, Block block, Parser parser)
    {
        throw new NotImplementedException();
    }
}

internal class FunctionCall : Statement
{
    public override int Construct(ReadOnlySpan<Token> tokens, Block block, Parser parser)
    {
        throw new NotImplementedException();
    }
}

