using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Identifier : Syntax, IParsable
{
    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        List<Component> components = new();
        
        while (parser.IsNotFinished)
        {
            var syntax = Component.Parse(ref parser);
            if (syntax is Error) return syntax;
            if (syntax is null) break;
            Component component = syntax switch
            {
                Name name => name,
                Parameters parameters => parameters,
                _ => null
            };
            components.Add(component);
        }

        if (components.Count is 0) return null;

        return new Identifier { Components = components.ToArray(), Source = parser.Commit(ref context) };
    }

    internal Component[] Components;

    internal class Component : Syntax, IParsable
    {
        public static Syntax Parse(ref Parser context)
            => Name.Parse(ref context) 
            ?? Parameters.Parse(ref context);

        public static implicit operator Component(Name name) => new() { _storage = name };
        public static implicit operator Component(Parameters parameters) => new() { _storage = parameters };

        public static implicit operator Name(Component component) => component._storage as Name;
        public static implicit operator Parameters(Component component) => component._storage as Parameters;

        private object _storage;
    }
}
