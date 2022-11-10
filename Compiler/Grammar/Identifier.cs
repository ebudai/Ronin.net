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
            var component = Component.Parse(ref parser);
            if (component is Error) return component;
            if (component is null) break;
            components.Add(Component.FromSyntax(component));
        }

        if (components.Count is 0) return null;

        return new Identifier { Components = components.ToArray(), Source = parser.Commit(ref context) };
    }

    internal Component[] Components;

    internal class Component : Syntax, IParsable<Component>
    {
        internal Name Name
        {
            get => _storage as Name;
            set => _storage = value;
        }

        internal Parameters Parameters
        {
            get => _storage as Parameters;
            set => _storage = value;
        }

        public static Component FromSyntax(Syntax syntax) => syntax switch
        {
            Name name => new(name),
            Parameters parameters => new(parameters),
            _ => null,
        };

        public static Syntax Parse(ref Parser context) => Name.Parse(ref context) ?? Parameters.Parse(ref context);

        private Component(Name name) => Name = name;
        private Component(Parameters parameters) => Parameters = parameters;

        private object _storage;
    }
}
