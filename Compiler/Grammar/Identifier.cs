using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Identifier : Syntax, IParsable
{
    public List<Component> Components { get; init; }

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

        return new Identifier { Components = components, Source = parser.Commit(ref context) };
    }

    public class Component : Syntax, IParsable
    {
        public static Syntax Parse(ref Parser context)
            => Name.Parse(ref context) 
            ?? Parameters.Parse(ref context);

        public static implicit operator Component(Name name) => new() { _storage = name };
        public static implicit operator Component(Parameters parameters) => new() { _storage = parameters };

        public static implicit operator Name(Component component) => component._storage as Name;
        public static implicit operator Parameters(Component component) => component._storage as Parameters;

        /*public bool Matches(Value value)
        {
            Scalar scalar = value;
            if (scalar is not null) return Matches(scalar);
            {
                if (_storage is Parameters parameters)
                {
                    //return parameters.Values.Length is 1 && 
                }
            }


            return false;
        }

        private bool Matches(Scalar scalar)
        {
            if (_storage is Parameters parameters)
            {

            }
        }*/

        private object _storage;
    }
}
