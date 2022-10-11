using Ronin.Compiler;
using Ronin.Token;
using Ronin.Token.Delimiter;

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
            if (parser[length] is Whitespace or Comment) continue;
            if (parser[length] is not Keyword keyword)
            {
                identifier ??= string.Empty;
                break;
            }

            static string handleModifier(string keyword, ref bool modifier)
            {
                if (modifier) return keyword;
                modifier = true;
                return null;
            }

            identifier = keyword.ToString() switch
            {
                Keyword.reactive => handleModifier(keyword.ToString(), ref isReactive),
                Keyword.compiled => handleModifier(keyword.ToString(), ref isCompiled),
                Keyword.persistent => handleModifier(keyword.ToString(), ref isPersistent),
                Keyword.shared => handleModifier(keyword.ToString(), ref isShared),
                Keyword.optional => handleModifier(keyword.ToString(), ref isOptional),
                Keyword.constant => handleModifier(keyword.ToString(), ref isReadonly) ?? string.Empty,
                Keyword.var => string.Empty,
                _ => keyword.ToString()
            };
        }

        // form the identifier, type, and/or initializer
        for (; length != max && initializer is null; ++length)
        {
            if (parser[length] is Symbol symbol)
            {
                if (symbol is Terminal) break;
                if (symbol is not Returns and not Assign)
                {
                    return new Expected<Name, Keyword>(parser, Terminal.character.ToString(), Assign.character.ToString(), Returns.character);
                }
                Parser attempt = new(parser, length + 1);
                var syntax = Reference.Parse(ref attempt);
                if (syntax is Reference reference)
                {
                    if (symbol is Returns) datatype = reference;
                    else if (symbol is Assign) initializer = reference;
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
                return new Expected<Name, Keyword>(parser, Terminal.character.ToString(), Assign.character.ToString(), Returns.character);
            }
        }

        return datatype is null && initializer is null ? null : new Datum(parser, length)
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