using Ronin.Compiler;
using Ronin.Lexicon.Symbols;
using Ronin.Lexicon.Reserved;

namespace Ronin.Grammar.Declaration;

internal class Datum : Syntax, IParsable
{
    internal Modifiers Is { get; private set; }
    internal string Identifier { get; private set; }
    internal Reference Datatype { get; private set; }
    internal Reference Initializer { get; private set; }

    public static Syntax Parse(Parser parser)
    {
        Parser attempt = new(parser);

        //var modifiers = Modifiers.Parse(attempt) as Modifiers;

        bool constant = false;
        if (attempt[0] is Variable or Constant)
        {
            constant = attempt[0] is Constant;
            ++attempt.Cursor;
        }

        var name = Name.Parse(attempt);
        if (name is null) return name;
        if (name is Error)
        {
            parser.Cursor = attempt.Cursor;
            return name;
        }

        Modifiers modifiers = null;
        Syntax datatype = null;
        if (attempt[0] is Returns)
        {
            ++attempt.Cursor;
            modifiers = Modifiers.Parse(attempt) as Modifiers;
            datatype = Declaration.Datatype.Parse(attempt);
            if (datatype is null) return datatype;
            if (datatype is Error)
            {
                parser.Cursor = attempt.Cursor;
                return datatype;
            }
        }

        Syntax initializer = null;
        if (attempt[0] is Assign)
        {
            ++attempt.Cursor;
            //initializer = 
        }

        return datatype is null && initializer is null ? null : new Datum
        {
            Is = modifiers,
            Datatype = datatype as Reference,
            Identifier = string.Join(' ', name),
            Initializer = initializer as Reference,
        };
        /*int length = 0;
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
                Reactive.keyword => handleModifier(keyword.ToString(), ref isReactive),
                Compiled.keyword => handleModifier(keyword.ToString(), ref isCompiled),
                Persistent.keyword => handleModifier(keyword.ToString(), ref isPersistent),
                Shared.keyword => handleModifier(keyword.ToString(), ref isShared),
                Optional.keyword => handleModifier(keyword.ToString(), ref isOptional),
                Constant.keyword => handleModifier(keyword.ToString(), ref isReadonly) ?? string.Empty,
                Variable.keyword => string.Empty,
                _ => keyword.ToString()
            };

            ++length;
        }

        // form the identifier, type, and/or initializer
        while (length != max)
        {
            var syntax = parser[length];
            if (syntax is Whitespace or Comment)
            {
                ++length;
                continue;
            }
            if (syntax is Terminal)
            {
                ++length;
                break;
            }
            if (syntax is Returns or Assign)
            {
                if (identifier.Length is 0) return Error.Parse(parser);
                Parser attempt = new(parser, length + 1);
                var parsed = Reference.Parse(attempt);
                if (parsed is Reference reference)
                {
                    if (syntax is Returns) datatype = reference;
                    else if (syntax is Assign) initializer = reference;
                    length = attempt.Cursor;
                    continue;
                }
                else
                {
                    parser.Cursor = attempt.Cursor + length - 1;
                    return parsed as Error;
                }
            }
            else if (syntax is Symbol symbol)
            {
                if (symbol.CanBeUsedInNames)
                {
                    identifier += symbol.ToString();
                }
                else
                {
                    return null;
                }
            }
            else if (syntax is Keyword keyword)
            {
                if (identifier.Length is not 0) identifier += ' ';
                identifier += keyword.ToString();
            }
            else if (syntax is Word name)
            {
                if (identifier.Length is not 0) identifier += ' ';
                identifier += name.ToString();
            }
            else if (syntax is Literal)
            {
                return null;
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
        };*/
    }
}