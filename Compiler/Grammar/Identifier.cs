using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Identifier : RepeatingSyntax<Identifier.Component>, IParsable
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

        return new Identifier { Values = components.ToArray(), Source = parser.Commit(ref context) };
    }

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

        public static implicit operator Component(Name name) => new(name);
        public static implicit operator Component(Parameters parameters) => new(parameters);

        public static implicit operator Name(Component component) => component.Name;
        public static implicit operator Parameters(Component component) => component.Parameters;

        private object _storage;
    }
}
