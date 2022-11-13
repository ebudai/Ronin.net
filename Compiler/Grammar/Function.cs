using Ronin.Compiler;

namespace Ronin.Grammar;

internal partial class Declaration
{
    internal class Function : Syntax, IParsable
    {
        internal Modifiers Is { get; private init; }
        internal Identifier Identifier { get; private init; }
        internal Scope Body { get; private init; }

        public static Syntax Parse(ref Parser context)
        {
            Parser parser = context;

            var modifiers = Modifiers.Parse(ref parser) as Modifiers;

            if (parser.Current is not Lexicon.Reserved.Function) return null;
            parser.Advance();

            var identifier = Identifier.Parse(ref parser);
            if (identifier is Error or null) return identifier;

            var body = Scope.Parse(ref parser);
            if (body is Error or null) return body;

            return new Function
            {
                Is = modifiers,
                Identifier = identifier as Identifier,
                Body = body as Scope,
                Source = parser.Commit(ref context)
            };
        }
    }
}
