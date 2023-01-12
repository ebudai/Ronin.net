using Ronin.Compiler;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Datatype
{
    internal class Declaration : Syntax, IParsable
    {
        public Modifiers Is { get; init; }
        public Identifier Identifier { get; init; }
        public Reference Algebra { get; init; }
        public Scope Body { get; init; }

        public static Syntax Parse(ref Parser context)
        {
            Parser parser = context;

            var modifiers = Modifiers.Parse(ref parser) as Modifiers;

            if (parser.Current is not Lexicon.Reserved.Datatype) return null;
            parser.Advance();

            var identifier = Identifier.Parse(ref parser);
            if (identifier is Error or null) return identifier;

            Syntax algebra = null;
            if (parser.Current is Assign)
            {
                parser.Advance();
                algebra = Reference.Parse(ref parser);
                if (algebra is Error or null) return algebra;
            }

            var body = Scope.Parse(ref parser);
            if (body is Error or null) return body;

            return new Declaration
            {
                Is = modifiers,
                Identifier = identifier as Identifier,
                Algebra = algebra as Reference,
                Body = body as Scope,
                Source = parser.Commit(ref context)
            };
        }
    }

    /*internal static Datatype Of(Scalar scalar)
    {
        if (scalar.Literals.Length is 0) return null; // this should be Scope.Global.stuff

        if (scalar.Literals[0] is Binary binary)
        {
            var number = binary.Sourcecode[2..].Span
        }


        return default;
    }*/
}
