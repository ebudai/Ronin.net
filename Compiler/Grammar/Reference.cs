using OneOf;
using Ronin.Compiler;
using Ronin.Token;

namespace Ronin.Grammar;

internal class Reference : Syntax, IParsable//<Reference>
{
    internal List<Entity> Name { get; init; } = new();

    internal Reference(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(ref Parser parser)
    {
        List<Entity> entities = new();
        int length = 0;
        while (length != parser.Length)
        {
            var lexeme = parser[length];
            if (lexeme is Name name)
            {
                entities.Add(name.ToString());
            }
            else if (lexeme is Keyword keyword)
            {
                entities.Add(keyword.ToString());
            }
            else if (lexeme is Literal)
            {
                entities.Add(new Value(parser, 1));
            }
            else if (lexeme is Symbol symbol)
            {
                if (symbol.IsOpenParenthesis)
                {
                    Parser attempt = new(parser, 0);
                    var syntax = Object.Parse(ref attempt);
                    if (syntax is Object @object)
                    {
                        length = attempt.Cursor;
                        entities.Add(@object);
                    }
                    continue;
                }
                else if (symbol.IsOpenBrace)
                {
                    // start of scope
                }
                else if (symbol.IsOpenSquareBracket)
                {
                    // list or lookup
                }
                else if (symbol.IsTerminal || symbol.IsSeparator || symbol.IsAssign)
                {
                    break;
                }
            }
            ++length;
        }
        return new Reference(parser, length) { Name = entities };
    }

    public string Transpile() => string.Join(' ', Name);
}

internal partial class Entity : OneOfBase<string, Value, Object>
{
    protected Entity(OneOf<string, Value, Object> input) : base(input) { }

    public static explicit operator string(Entity entity) => entity.AsT0;
    public static explicit operator Value(Entity entity) => entity.AsT1;
    public static explicit operator Object(Entity entity) => entity.AsT2;

    public static implicit operator Entity(string name) => new(name);
    public static implicit operator Entity(Value value) => new(value);
    public static implicit operator Entity(Object @object) => new(@object);
}