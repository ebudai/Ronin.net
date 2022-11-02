using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Identifier : RepeatingSyntax<Identifier.Component>, IParsable
{
    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        t_buffer.Clear();

        parser.Cursor = -1;
        while (parser.IsNotEmpty)
        {
            ++parser.Cursor;
            if (parser[0] is Trivium) continue;
            var component = Component.Parse(ref parser);
            if (component is Error or null) return component;
            t_buffer.Add(component as Component);
        }

        return t_buffer.Count is 0 ? null : new Identifier { Elements = t_buffer.ToArray(), Tokens = parser.GetTokens(ref context) };
    }

    internal class Component : Syntax, IParsable
    {
        internal Name Name
        {
            get => _storage as Name;
            set => _storage = value;
        }

        internal Parameter Parameter
        {
            get => _storage as Parameter;
            set => _storage = value;
        }

        internal Parameters Parameters
        {
            get => _storage as Parameters;
            set => _storage = value;
        }

        public static Syntax Parse(ref Parser context)
        {
            Parser parser = context;
            return Name.Parse(ref parser) ?? Parameter.Parse(ref parser) ?? Parameters.Parse(ref parser);
        }

        private Component(Name name) => Name = name;
        private Component(Parameter parameter) => Parameter = parameter;
        private Component(Parameters parameters) => Parameters = parameters;

        public static implicit operator Component(Name name) => new(name);
        public static implicit operator Component(Parameter parameters) => new(parameters);
        public static implicit operator Component(Parameters parameters) => new(parameters);

        public static implicit operator Name(Component component) => component.Name;
        public static implicit operator Parameter(Component component) => component.Parameter;
        public static implicit operator Parameters(Component component) => component.Parameters;

        private object _storage;
    }
}
