using Ronin.Compiler;
using Ronin.Lexicon.Reserved;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Declarator : Syntax, IParsable
{
    internal bool IsVariable { get; private init; }
    internal bool IsConstant { get; private init; }
    internal bool IsFunction { get; private init; }
    internal bool IsDatatype { get; private init; }
    internal bool IsReactive { get; private init; }

    public static Syntax Parse(ref Parser context)
    {
        bool variable = false;
        bool constant = false;
        bool function = false;
        bool datatype = false;
        bool reactive = false;

        Parser parser = context;

        while (parser.IsNotEmpty)
        {
            ref readonly var modifier = ref parser[0];
            ++parser.Cursor;

            if (modifier is Trivium) continue;

            // the point of these is to break if you encounter a keyword twice
            // the 2nd time it's part of the name, parsed somewhere else
            if (modifier is Variable && (variable = true)) break;
            if (modifier is Function && (function = true)) break;
            if (modifier is Constant && (constant = true)) break;
            if (modifier is Datatype && (datatype = true)) break;
            if (modifier is Reactive && (reactive = true)) break;

            return null;
        }

        return new Declarator
        {
            IsVariable = variable,
            IsConstant = constant,
            IsFunction = function,
            IsDatatype = datatype,
            IsReactive = reactive,
            Tokens = parser.GetTokens(ref context),
        };
    }
}
