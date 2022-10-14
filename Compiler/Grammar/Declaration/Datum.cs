using Ronin.Compiler;
using Ronin.Token;
using Ronin.Token.Delimiter;

namespace Ronin.Grammar.Declaration;

internal class Datum : Syntax, IParsable
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
        while (length != max && identifier is null)
        {
            if (parser[length] is Whitespace or Comment)
            {
                ++length;
                continue;
            }
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

            ++length;
        }

        // form the identifier, type, and/or initializer
        while (length != max && initializer is null)
        {
            var syntax = parser[length];
            if (syntax is Whitespace or Comment)
            {
                ++length;
                continue;
            }
            if (syntax is Terminal) break;
            if (syntax is Returns or Assign)
            {
                if (identifier.Length is 0) return new Expected<Name>(parser);
                Parser attempt = new(parser, length + 1);
                var parsed = Reference.Parse(ref attempt);
                if (parsed is Reference reference)
                {
                    if (syntax is Returns) datatype = reference;
                    else if (syntax is Assign) initializer = reference;
                    length = attempt.Cursor;
                    continue;
                }
                else
                {
                    return parsed as Expected;
                }
            }
            else if (syntax is Symbol)
            {
                return new Expected<Name, Terminal, Returns, Assign>(parser);
            }
            else if (syntax is Name name)
            {
                if (identifier.Length is not 0) identifier += ' ';
                identifier += name.ToString();
            }
            else if (syntax is Keyword keyword)
            {
                if (identifier.Length is not 0) identifier += ' ';
                identifier += keyword.ToString();
            }
            else if (syntax is Literal)
            {
                return datatype is null ? new Expected<Name>(parser) : new Expected<Name, Literal, OpenParenthesis>(parser);
            }
            ++length;
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
}