using Ronin.Compiler;
using Ronin.Token;
using static Ronin.Token.Keyword.Word;

namespace Ronin.Grammar.Declaration;

internal class Datum : Syntax, IParsable<Datum>
{
    internal bool IsReactive { get; set; }
    internal bool IsCompiled { get; set; }
    internal bool IsPersistent { get; set; }
    internal bool IsShared { get; set; }
    internal bool IsOptional { get; set; }
    internal bool IsReadonly { get; set; }

    internal string Name { get; set; }
    internal Reference Datatype { get; set; }
    internal Reference Initializer { get; set; }

    internal Datum(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(Parser parser)
    {
        if (parser.IsEmpty) return null;
        if (parser[0] is not Keyword) return null;

        int length = 0;
        bool isReactive = false;
        bool isCompiled = false;
        bool isPersistent = false;
        bool isShared = false;
        bool isOptional = false;
        bool isReadonly = false;
        bool isVariable = false;
        string name = null;

        while (length < parser.Length && name is null)
        {
            if (parser[length] is Whitespace) continue;
            if (parser[length] is not Keyword keyword) break;

            if (keyword.Type is reactive)
            {
                if (isReactive) name = string.Empty;
                else isReactive = true;
            }
            else if (keyword.Type is compiled)
            {
                if (isCompiled) name = string.Empty;
                else isCompiled = true;
            }
            else if (keyword.Type is persistent)
            {
                if (isPersistent) name = string.Empty;
                else isPersistent = true;
            }
            else if (keyword.Type is shared)
            {
                if (isShared) name = string.Empty;
                else isShared = true;
            }
            else if (keyword.Type is optional)
            {
                if (isOptional) name = string.Empty;
                else isOptional = true;
            }
            else if (keyword.Type is constant)
            {
                name ??= string.Empty;
                if (isReadonly)
                {                    
                    if (name.Length is not 0) name += ' ';
                    name += nameof(constant);
                }
                else isReadonly = true;
            }
            else if (keyword.Type is var)
            {
                name ??= string.Empty;
                if (isVariable)
                {
                    if (name.Length is not 0) name += ' ';
                    name += nameof(var);
                }
                else isVariable = true;
            }
            else
            {
                throw new NotImplementedException(nameof(keyword) + '.' + nameof(keyword.Type));
            }
        }

        return null;
    }

    public string Transpile()
    {
        throw new NotImplementedException();
    }

    private bool IsTyped => Datatype is not null || Initializer is not null;
}
