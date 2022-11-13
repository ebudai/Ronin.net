using Ronin.Compiler;
using Ronin.Lexicon.Reserved;

namespace Ronin.Grammar;

internal partial class Declaration
{
    internal class Datum : Parameter, IParsable
    {
        internal Declarator Mutability { get; private init; }

        public static new Syntax Parse(ref Parser context)
        {
            Declarator? declarator = context.Current switch
            {
                Variable => Declarator.Variable,
                Constant => Declarator.Constant,
                Reactive => Declarator.Reactive,
                _ => null
            };
            if (declarator is null) return null;

            Parser parser = context;
            parser.Advance();

            var syntax = Parameter.Parse(ref parser);
            if (syntax is Error or null) return syntax;
            var parameter = syntax as Parameter;

            return new Datum
            {
                Mutability = declarator.Value,
                Name = parameter.Name,
                Is = parameter.Is,
                Datatype = parameter.Datatype,
                Initializer = parameter.Initializer,
                Source = parser.Commit(ref context)
            };
        }

        internal enum Declarator { Variable, Constant, Reactive };
    }
}
