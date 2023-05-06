using Ronin.Grammar;
using Ronin.Grammar.Compound;
using Ronin.Lexicon.Keyword;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Module : Context
{
    public static Module Global { get; } = new();

    public Dictionary<string, Module> Children { get; init; } = new();
    public List<Instruction> Instructions { get; init; } = new();

    protected Module() { }

    public static Module ForwardDeclare(Scope scope) 
    {
        const string empty = "";
        Module module = new();
        var name = string.Empty;
        foreach (var statement in scope.Values)
        {
            switch (statement)
            {
                case ImportExport export:
                    if (export.Direction is not PartOf) break;
                    if (name is not empty)
                    {
                        module.Errors.Add(new ModuleAlreadyNamed { Statement = statement });
                        continue;
                    }

                    name = export.Name;

                    if (Global.Children.TryGetValue(name, out var existing))
                    {
                        if (ReferenceEquals(existing, module)) continue;
                        
                        foreach (var function in module.Functions)
                        {
                            var existingSemantics = existing.Find(function.Key);
                            if (existingSemantics is not null)
                            {
                                existing.Errors.Add(new IdentifierAlreadyExists { Statement = statement, Existing = existingSemantics });
                                continue;
                            }
                            existing.Functions.Add(function.Key, function.Value);
                        }
                        foreach (var datatype in module.Datatypes)
                        {
                            var existingSemantics = existing.Find(datatype.Key);
                            if (existingSemantics is not null)
                            {
                                existing.Errors.Add(new IdentifierAlreadyExists { Statement = statement, Existing = existingSemantics });
                                continue;
                            }
                            existing.Datatypes.Add(datatype.Key, datatype.Value);
                        }
                        foreach (var datum in module.Data)
                        {
                            var existingSemantics = existing.Find(datum.Key);
                            if (existingSemantics is not null)
                            {
                                existing.Errors.Add(new IdentifierAlreadyExists { Statement = statement, Existing = existingSemantics }); 
                                continue;
                            }
                            existing.Data.Add(datum.Key, datum.Value);
                        }
                    }

                    break;
                case Grammar.Function function:
                    if (module.Functions.ContainsKey(function.Identifier))
                    {
                        module.Errors.Add(new FunctionAlreadyExists { Statement = statement });
                        continue;
                    }
                    module.Functions.Add(function.Identifier, new Function(function));
                    break;
                case Grammar.Datatype datatype:
                    if (module.Datatypes.ContainsKey(datatype.Identifier)) {
                        module.Errors.Add(new DatatypeAlreadyExists { Statement = statement });
                        continue;
                    }
                    module.Datatypes.Add(datatype.Identifier, Datatype.ForwardDeclare(datatype));
                    break;
                case Grammar.Datum datum:
                    Identifier identifier = new();
                    identifier.Components.Add(new() { value = datum.Name });
                    if (module.Data.ContainsKey(identifier))
                    {
                        module.Errors.Add(new DatumAlreadyExists { Statement = statement });
                        continue;
                    }
                    module.Data.Add(identifier, Datum.ForwardDeclare(datum));
                    break;
                case Scope anonymousScope:
                    module.Instructions.Add(new Instruction { Source = anonymousScope });
                    break;
                case Assignment assignment:
                    module.Instructions.Add(new Instruction { Source = assignment });
                    break;
                case Value value:                    
                    module.Instructions.AddRange(GetInstructions(value));
                    break;
                default: break;
            }
        }

        return module;
    }

    private void Incorporate<T>(Dictionary<Identifier, T> semantics, Statement statement)
    {
        foreach (var semantic in semantics)
        {
            var existing = Find(semantic.Key);
            if (existing is not null)
            {
                Errors.Add(new IdentifierAlreadyExists { Statement = statement, Existing = existing });
                continue;
            }
            switch (semantic.Value)
            {
                case Function function: Functions.Add(semantic.Key, function); break;
                case Datatype datatype: Datatypes.Add(semantic.Key, datatype); break;
                case Datum datum: Data.Add(semantic.Key, datum); break;
                //default: Errors.Add(new DeveloperMistakeUnhandledSubclassException<Semantics> { Statement = })
            }
            //existing.Functions.Add(function.Key, function.Value);
        }
    }
    private static List<Instruction> GetInstructions(List<Value> values) => values.SelectMany(GetInstructions).ToList();

    private static List<Instruction> GetInstructions(Value value)
    {
        if (value is Reference reference) return new() { new UnresolvedInstruction { Reference = reference } };

        List<Instruction> instructions = new();
        
        if (value is InlineLookup lookup)
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

    public Semantics Find()
    {
        return null;
    }
}

[ExcludeFromCodeCoverage]
internal class ModuleAlreadyNamed : Error { }

[ExcludeFromCodeCoverage]
internal class IdentifierAlreadyExists : Error
{
    public Semantics Existing { get; init; }
}