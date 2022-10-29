using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Reference : RepeatingSyntax<Reference.Component>, IParsable
{
    public static Syntax Parse(Parser parser)
    {
        buffer.Clear();

        parser.Cursor = -1;
        while (parser.IsNotEmpty)
        {
            ++parser.Cursor;
            if (parser[0] is Trivium) continue;
            var component = Component.Parse(parser);
            if (component is Error or null) return component;
            buffer.Add(component as Component);
        }

        return buffer.Count is 0 ? null : new Reference { Elements = buffer.ToArray(), Tokens = parser.Tokens };
    }

    internal class Component : Syntax, IParsable
    {
        internal Name Name
        {
            get => _discriminator is Discriminator.Name ? _name : null;
            set
            {
                _name = value;
                _discriminator = Discriminator.Name;
            }
        }

        internal Value Value
        {
            get => _discriminator is Discriminator.Value ? _value : null;
            set
            {
                _value = value;
                _discriminator = Discriminator.Value;
            }
        }

        public static Syntax Parse(Parser parser) => Name.Parse(parser) ?? Value.Parse(parser);

        private Component(Name name) => Name = name;
        private Component(Value value) => Value = value;

        public static implicit operator Component(Name name) => new(name);
        public static implicit operator Component(Value value) => new(value);

        public static implicit operator Name(Component component) => component.Name;
        public static implicit operator Value(Component component) => component.Value;

        private Name _name;
        private Value _value;

        private Discriminator _discriminator;

        private enum Discriminator { Name, Value };
    }
}