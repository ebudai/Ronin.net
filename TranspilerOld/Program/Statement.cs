namespace Ronin.Transpiler.Program;

internal abstract class Statement
{
    protected Statement(int length) => Length = length;

    public override string ToString() => string.Empty;

    protected static string GetIdentifier(ReadOnlySpan<Token> tokens, out int index)
    {
        index = 0;
        string name = string.Empty;
        if (tokens.IsEmpty) return name;
        while (index < tokens.Length && tokens[index].Kind is Token.Type.Identifier or Token.Type.Keyword)
        {
            if (index is 0 && tokens[index].Kind is Token.Type.Keyword) throw new Parser.Exception($"identifiers cannot begin with keywords ({tokens[index].Value} in this case)");
            name += tokens[index++].Value + " ";
        }
        return name.Trim();
    }

    protected static int GetIdentifierLength(ReadOnlySpan<Token> tokens)
    {
        int length = 0;
        while (length < tokens.Length && tokens[length].Kind is Token.Type.Identifier or Token.Type.Keyword)
        {
            if (length is 0 && tokens[length].Kind is Token.Type.Keyword) throw new Parser.Exception($"identifiers cannot begin with keywords ({tokens[length].Value} in this case)");
            ++length;
        }

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

    public class Declare
    {
        public static Statement Variable(ReadOnlySpan<Token> tokens, Block block, Parser parser)
        {
            if (tokens.Length is < 5) throw new Parser.Exception("unexpected end of statement");
            tokens = tokens[1..];

            int length = 1;
            string type = null;
            Statement initializer = null;

            var name = GetIdentifier(tokens, out var index);

            length += index;
            tokens = tokens[index..];

            // explicit
            if (tokens[0].Value is Syntax.TypeStart)
            {
                tokens = tokens[1..];
                ++length;
                type = GetIdentifier(tokens, out index);
                length += index;
                tokens = tokens[index..];
            }

            // with initializer
            if (tokens[0].Value is Syntax.Assign)
            {
                tokens = tokens[1..];
                ++length;
                initializer = parser.Parse(tokens, block, out index);
                length += index;
            }

            if (tokens[0].Value is Syntax.Terminal) length += 1;

            if (type is null && initializer is null) throw new Parser.Exception($"expecting {Syntax.TypeStart} or {Syntax.Assign}");

            block.Data.Add(name, new() { Type = new Block { Name = type }, Initializer = initializer });

            return new DeclareVariableStatement(length);
        }

        public static Statement Tuple(ReadOnlySpan<Token> tokens, Block block, Parser parser)
        {
            if (tokens.Length is < 5) throw new Parser.Exception("unexpected end of statement");
            tokens = tokens[1..];

            int length = 1;
            string type = null;
            Statement initializer = null;

            var names = tokens[0].Value is Syntax.TupleStart
                ? GetTupleIdentifiers(tokens, out var index)
                : new[] { GetIdentifier(tokens, out index) };

            length += index;
            tokens = tokens[index..];

            // explicit
            if (tokens[0].Value is Syntax.TypeStart)
            {
                tokens = tokens[1..];
                ++length;
                if (tokens[0].Value is Syntax.TupleStart)
                {
                    type = Syntax.TupleStart + string.Join(",", GetTupleIdentifiers(tokens, out index)) + Syntax.TupleEnd;
                }
                else
                {
                    type = GetIdentifier(tokens, out index);
                }

                length += index;
                tokens = tokens[index..];
            }

            // with initializer
            if (tokens[0].Value is Syntax.Assign)
            {
                tokens = tokens[1..];
                ++length;
                initializer = parser.Parse(tokens, block, out index);
                length += index;
            }

            if (tokens[0].Value is Syntax.Terminal) length += 1;

            if (type is null && initializer is null) throw new Parser.Exception($"expecting {Syntax.TypeStart} or {Syntax.Assign}");

            foreach (var name in names)
            {
                block.Data.Add(name, new() { Type = new Block { Name = type }, Initializer = initializer });
            }

            return new DeclareTupleStatement(length);
        }

        public static Statement Function(ReadOnlySpan<Token> tokens, Block block, Parser parser)
        {
            if (tokens.Length is < 5) throw new Parser.Exception("unexpected end of statement");
            tokens = tokens[1..];

            int length = 1;
            Block function = new() { Parent = block };
            
            while (tokens[0].Value is not Syntax.BlockStart and not Syntax.Terminal)
            {
                // parameter(s)
                if (tokens[0].Value is Syntax.TupleStart)
                {
                    var end = tokens.IndexOfMatching(1, Syntax.TupleEnd);
                    var parameters = tokens[1..end];
                    while (!parameters.IsEmpty)
                    {
                        var parametername = GetIdentifier(parameters, out length);

                        parameters = parameters[length..];
                        if (parameters[0].Value is not Syntax.TypeStart) throw new Parser.Exception($"expected {Syntax.TypeStart}");
                        parameters = parameters[1..];

                        var parametertype = GetIdentifier(parameters, out length);

                        parameters = parameters[length..];
                        if (parameters[0].Value is not Syntax.Separator and not Syntax.TupleEnd) throw new Parser.Exception($"expected {Syntax.Separator} or {Syntax.TupleEnd}");
                        parameters = parameters[1..];

                        function.Name += $"({parametertype})";
                        function.Data.Add(parametername, new Datum { Type = new() { Name = parametertype } });
                    }
                }

                else if (tokens[0].Kind is Token.Type.Identifier)
                {
                    function.Name += GetIdentifier(tokens, out length);
                    tokens = tokens[length..];
                }
            }

            block.Functions.Add(function.Name, function);

            if (tokens[0].Value is Syntax.BlockStart)
            {
                var index = tokens.IndexOfMatching(0, Syntax.BlockEnd);
                //var body = parser.Parse(tokens[..index], function);
            }
            return new DeclareFunctionStatement(length);
        }

        public static Statement Type(ReadOnlySpan<Token> tokens, Block block, Parser parser)
        {
            throw new NotImplementedException();
        }

        public static Statement Scope(ReadOnlySpan<Token> tokens, Block block, Parser parser)
        {
            throw new NotImplementedException();
        }
    }

    public static Statement Package(ReadOnlySpan<Token> tokens, Block block, Parser _)
    {
        if (tokens.Length is < 2) return new PackageStatement(tokens.Length);
        tokens = tokens[1..];
        block.Name = GetIdentifier(tokens, out var index);
        if (tokens[index].Value is not Syntax.Terminal) throw new Parser.Exception($"expecting {Syntax.Terminal}");
        return new PackageStatement(index + 2);
    }

    public static Statement Tuple(ReadOnlySpan<Token> tokens, Block _, Parser __)
    {
        if (tokens.Length is < 5) throw new Parser.Exception("unexpected end of statement");
        int length = 1;
        while (tokens[length].Value is not Syntax.TupleEnd)
        {
            if (tokens[length].Kind is Token.Type.Identifier)
            {
                length += GetIdentifierLength(tokens[length..]);
            }
            if (tokens[length].Kind is Token.Type.Literal) ++length;
            if (tokens[length].Value is Syntax.Separator) ++length;
        }
        return new TupleStatement(length + 2);
    }

    public static Statement Literal(ReadOnlySpan<Token> tokens, Block _, Parser __)
    {
        if (tokens.Length is < 2) throw new Parser.Exception("unexpected end of statement");
        return new LiteralStatement();
    }
    
    public static Statement Identifier(ReadOnlySpan<Token> tokens, Block _, Parser __)
    {
        if (tokens.Length is < 2) throw new Parser.Exception("unexpected end of statement");
        return new IdentifierStatement(GetIdentifierLength(tokens) + 1);
    }

    public static Statement FunctionCall(ReadOnlySpan<Token> tokens, Block block, Parser parser)
    {
        throw new NotImplementedException();
    }

    public int Length { get; protected set; }
}

internal class LiteralStatement : Statement
{
    public LiteralStatement() : base(2) { }
}

internal class IdentifierStatement : Statement
{
    public IdentifierStatement(int length) : base(length) { }
}

internal class PackageStatement : Statement
{
    public PackageStatement(int length) : base(length) { }
}

internal class TupleStatement : Statement
{
    public TupleStatement(int length) : base(length) { }
}

internal class DeclareVariableStatement : Statement
{
    public DeclareVariableStatement(int length) : base(length) { }
}

internal class DeclareTupleStatement : Statement
{
    public DeclareTupleStatement(int length) : base(length) { }
}

internal class DeclareFunctionStatement : Statement
{ 
    public DeclareFunctionStatement(int length) : base(length) { }
}

internal class DeclareTypeStatement : Statement
{
    public DeclareTypeStatement(int length) : base(length) { }
}

internal class DeclareScopeStatement : Statement
{
    public DeclareScopeStatement(int length) : base(length) { }
}
