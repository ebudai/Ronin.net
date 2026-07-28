using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;

namespace Ronin.Grammar;

internal class Module
{
    public List<Scope> Scopes { get; } = [];

    public Module() { }
    public Module(Scope scope) => Scopes.Add(scope);

    public static Module Parse(ref Parser current)
    {
        Parser parser = current;

        Scope scope = new();

        while (Statement.Parse(ref parser) is Statement statement)
        {
            scope.Statements.Add(statement);
            parser.TryAdvance<Terminal>();
        }

        // Stopping where statements stop discarded the rest of the file in
        // silence: an unmatched delimiter or a stray token meant everything after
        // it simply did not exist. What is left has to be the sentinel.
        if (parser.IsNotFinished)
        {
            Parser stopped = parser;
            while (parser.IsNotFinished) parser.Advance();

            var remainder = stopped.AdvanceTo(parser);
            current = parser;

            return new UnexpectedInputError(scope) { Tokens = remainder };
        }

        current = parser;
        return new Module(scope);
    }

    public class Unresolved : Module
    {
        public Name Name { get; init; }
    }

    /// <summary>
    ///     Input that no statement could account for, which is reported rather
    ///     than discarded. The statements parsed before it are kept.
    /// </summary>
    public class UnexpectedInputError(Scope scope) : Module(scope), IError
    {
        public string Reason { get; } = "unexpected input";
        public System.ReadOnlyMemory<Token> Tokens { get; init; }
    }
}
