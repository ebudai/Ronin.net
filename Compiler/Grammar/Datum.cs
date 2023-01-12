using Ronin.Compiler;
using Ronin.Lexicon.Reserved;

namespace Ronin.Grammar;

internal partial class Datum
{
    internal class Declaration : Parameter, IParsable
    {
        internal Mutability Mutability { get; private init; }

        public static new Syntax Parse(ref Parser context)
        {
            Mutability? declarator = context.Current switch
            {
                Variable => Mutability.Variable,
                Constant => Mutability.Constant,
                Reactive => Mutability.Reactive,
                _ => null
            };
            if (declarator is null) return null;

            Parser parser = context;
            parser.Advance();

            var syntax = Parameter.Parse(ref parser);
            if (syntax is Error or null) return syntax;
            var parameter = syntax as Parameter;

            return new Declaration
            {
                Mutability = declarator.Value,
                Name = parameter.Name,
                Is = parameter.Is,
                Datatype = parameter.Datatype,
                Initializer = parameter.Initializer,
                Source = parser.Commit(ref context)
            };
        }
    }
}
