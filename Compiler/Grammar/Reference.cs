using OneOf;
using Ronin.Compiler;
using Ronin.Token;
using Ronin.Token.Delimiter;
using Object = Ronin.Grammar.Aggregate.Object;

namespace Ronin.Grammar;

internal class Reference : Syntax, IParsable
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
            else if (lexeme is Literal literal)
            {
                entities.Add(literal);
            }
            else if (lexeme is OpenParenthesis)
            {
                Parser attempt = new(parser);
                var syntax = Object.Parse(ref attempt);
                if (syntax is Object @object)
                {
                    length = attempt.Cursor;
                    entities.Add(@object);
                }
            }
            else if (lexeme is OpenBrace)
            {
                // scope or list declaration
            }
            else if (lexeme is OpenSquareBracket)
            {
                // index for list or lookup
            }
            else if (lexeme is Terminal or Separator)
            {
                ++length;
                break;
            }
            else if (lexeme is Assign or Close)
            {
                break;
            }
            ++length;
        }
        return entities.Count is 0 
            ? new Expected<Name, OpenParenthesis, Literal>(parser)
            : new Reference(parser, length) { Name = entities };
    }
}

internal partial class Entity : OneOfBase<string, Literal, Object>
{
    protected Entity(OneOf<string, Literal, Object> input) : base(input) { }

    public static implicit operator string(Entity entity) => entity.AsT0;
    public static implicit operator Literal(Entity entity) => entity.AsT1;
    public static implicit operator Object(Entity entity) => entity.AsT2;

    public static implicit operator Entity(string name) => new(name);
    public static implicit operator Entity(Literal value) => new(value);
    public static implicit operator Entity(Object @object) => new(@object);
}