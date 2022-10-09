using Ronin.Compiler;
using Ronin.Token;
using static Ronin.Token.Keyword.Word;

namespace Ronin.Grammar.Declaration;

internal class Datum : Syntax, IParsable//<Datum>
{
    internal bool IsReactive { get; set; }
    internal bool IsCompiled { get; set; }
    internal bool IsPersistent { get; set; }
    internal bool IsShared { get; set; }
    internal bool IsOptional { get; set; }
    internal bool IsReadonly { get; set; }

    internal string Identifier { get; set; }
    internal Reference Datatype { get; set; }
    internal Reference Initializer { get; set; }

    internal Datum(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(ref Parser parser)
    {
        int length = 0;
        int max = parser.Length;
        bool isReactive = false;
        bool isCompiled = false;
        bool isPersistent = false;
        bool isShared = false;
        bool isOptional = false;
        bool isReadonly = false;
        string identifier = null;
        Reference datatype = null;
        Reference initializer = null;

        // ingest keywords
        for (; length != max && identifier is null; ++length)
        {
            if (parser[length] is Whitespace) continue;
            if (parser[length] is not Keyword keyword)
            {
                identifier ??= string.Empty;
                break;
            }

            static string handleModifier(Keyword.Word keyword, ref bool modifier)
            {
                if (modifier) return Enum.GetName(keyword);
                modifier = true;
                return null;
            }

            identifier = keyword.Type switch
            {
                reactive => handleModifier(keyword.Type, ref isReactive),
                compiled => handleModifier(keyword.Type, ref isCompiled),
                persistent => handleModifier(keyword.Type, ref isPersistent),
                shared => handleModifier(keyword.Type, ref isShared),
                optional => handleModifier(keyword.Type, ref isOptional),
                constant => handleModifier(keyword.Type, ref isReadonly) ?? string.Empty,
                var => string.Empty,
                _ => Enum.GetName(keyword.Type)
            };
        }

        // form the identifier, type, and/or initializer
        for (; length != max && initializer is null; ++length)
        {
            if (parser[length] is Symbol symbol)
            {
                if (symbol.IsTerminal) break;
                if (!symbol.IsReturns && !symbol.IsAssign)
                {
                    return new Expected<Name, Keyword>(parser, Symbol.terminal.ToString(), Symbol.assign.ToString(), Symbol.returns);
                }
                Parser attempt = new(parser, length + 1);
                var syntax = Reference.Parse(ref attempt);
                if (syntax is Reference reference)
                {
                    if (symbol.IsReturns) datatype = reference;
                    else if (symbol.IsAssign) initializer = reference;
                    length = attempt.Cursor - 1;
                }
                else
                {
                    return new Expected<Name, Keyword>(attempt);
                }                
            }
            else if (parser[length] is Name name)
            {
                if (identifier.Length is not 0) identifier += ' ';
                identifier += name.ToString();
            }
            else if (parser[length] is Keyword keyword)
            {
                if (identifier.Length is not 0) identifier += ' ';
                identifier += keyword.ToString();
            }
            else if (parser[length] is Literal)
            {
                return new Expected<Name, Keyword>(parser, Symbol.terminal.ToString(), Symbol.assign.ToString(), Symbol.returns);
            }
        }

        return new Datum(parser, length)
        {
            Datatype = datatype, 
            Identifier = identifier,
            Initializer = initializer, 
            IsCompiled = isCompiled, 
            IsOptional = isOptional,
            IsPersistent = isPersistent,
            IsReactive = isReactive,
            IsReadonly = isReadonly,
            IsShared = isShared,
        };
    }

    public string Transpile()
    {
        throw new NotImplementedException();
    }

}