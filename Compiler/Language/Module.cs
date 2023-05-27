using Ronin.Grammar;
using Ronin.Grammar.Compound;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Module : Semantics
{
    public static ConcurrentDictionary<Name, Module> Exported { get; } = new();

    protected Module() { }

    public static Module Declare(Scope scope) 
    {
        Semantics semantics = new Procedure() { Source = scope };
        Name name = null;
        
        foreach (var statement in scope.Values)
        {
            switch (statement)
            {
                case Export export:
                    if (name is not null)
                    {
                        semantics.Errors.Add(new ModuleAlreadyNamed { Statement = statement });
                        continue;
                    }

                    name = export.Name;

                    semantics = Exported.AddOrUpdate(name, semantics, (name, existing) =>
                    {
                        var errors = existing.Incorporate(semantics);
                        existing.Errors.AddRange(errors);
                        return existing;
                    });

                    break;
                case Grammar.FunctionDeclaration function:
                    if (semantics.Find(function.Identifier) is not null)
                    {
                        semantics.Errors.Add(new IdentifierAlreadyExists { Statement = statement });
                        continue;
                    }
                    semantics.Functions.Add(function.Identifier, Function.Declare(function));
                    break;
                case Grammar.DatatypeDeclaration datatype:
                    if (semantics.Find(datatype.Identifier) is not null) {
                        semantics.Errors.Add(new IdentifierAlreadyExists { Statement = statement });
                        continue;
                    }
                    semantics.Datatypes.Add(datatype.Identifier, Datatype.Declare(datatype, semantics));
                    break;
                case Grammar.DatumDeclaration datum:
                    if (semantics.Find(datum.Name) is not null)
                    {
                        semantics.Errors.Add(new IdentifierAlreadyExists { Statement = statement });
                        continue;
                    }
                    semantics.Data.Add(datum.Name, Datum.Declare(datum));
                    break;
                /*case Scope anonymousScope:
                    module.Instructions.Add(new Instruction { Source = anonymousScope });
                    break;
                case Assignment assignment:
                    module.Instructions.Add(new Instruction { Source = assignment });
                    break;
                case Value value:                    
                    module.Instructions.AddRange(GetInstructions(value));
                    break;*/
                default: break;
            }
        }

        return semantics;
    }

    private List<Error> Incorporate(Semantics from)
    {
        List<Error> errors = new();
        foreach (var function in from.Functions)
        {
            if (Add(function) is Error error) errors.Add(error);
        }
        foreach (var datatype in from.Datatypes)
        {
            if (Add(datatype) is Error error) errors.Add(error);
        }
        foreach (var datum in from.Data)
        {
            if (Add(datum) is Error error) errors.Add(error);
        }
        return errors;
    }

    private static List<Instruction> GetInstructions(Value value)
    {
        if (value is Reference reference) return new() { new UnresolvedInstruction { Reference = reference } };

        List<Instruction> instructions = new();
        
        if (value is Lookup lookup)
        {
            foreach (var association in lookup.Values)
            {
                instructions.AddRange(GetInstructions(association.Key));
                instructions.AddRange(GetInstructions(association.Value));
            }
        }
        else if (value is Aggregate<Value> aggregate)
        {
            foreach (var item in aggregate.Values) instructions.AddRange(GetInstructions(item));
        }
        
        return instructions;
    }

    /*public class Name
    {
        public List<string> Names { get; init; } = new();

        public override bool Equals(object obj) => (obj as Name)?.Names.SequenceEqual(Names) ?? false;

        public override int GetHashCode() => hashcode ?? GenerateHashCode();

        private int? hashcode;

        private int GenerateHashCode()
        {
            hashcode = 17;
            foreach (var name in Names) hashcode = HashCode.Combine(hashcode, name.GetHashCode());
            return hashcode.Value;
        }
    }*/
}

[ExcludeFromCodeCoverage]
internal class ModuleAlreadyNamed : Error { }

[ExcludeFromCodeCoverage]
internal class IdentifierAlreadyExists : Error
{
    public Semantics Existing { get; set; }
}