using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

using Children = Dictionary<Token, Module>;

internal partial class Module : Scope
{
    public List<Scope> Scopes { get; } = new();
    public Children Children { get; } = new();

    public static new Module Parse(ref Parser current)
    {
        Parser parser = current;

        Module values = new();

        while (parser.IsNotFinished)
        {
            var syntax = Statement.Parse(ref parser);
            if (syntax is null) break;
            values.Add(syntax);
            parser.TryAdvance<Terminal>();
        }

        current = parser;
        return values;
    }

    [ExcludeFromCodeCoverage]
    public Module Get(Name name)
    {
        Module found = this;

        for (int i = 0; i != name.Tokens.Length; ++i)
        {
            if (found.Children.TryGetValue(name.Tokens.Span[i], out var child) is false)
            {
                child = new() { Parent = found };
                found.Children.Add(name.Tokens.Span[i], child);
            }
            found = child;
        }

        return found;
    }

    [ExcludeFromCodeCoverage]
    public class Unresolved : Module
    {
        public Name Name { get; init; }
    }
}
