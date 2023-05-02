using Ronin.Grammar;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;

namespace Ronin.Language;

internal class Module : Semantics
{
    public static Dictionary<string, Module> All { get; } = new();

    public Dictionary<string, Module> Children { get; init; } = new();
    public Dictionary<Identifier, Datatype> Datatypes { get; init; } = new();
    public Dictionary<Identifier, Function> Functions { get; init; } = new();
    public Dictionary<string, Datum> Data { get; init; } = new();
    
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
                case ImportExport importExport:
                    if (importExport.Direction is not PartOf) break;
                    if (name is not empty)
                    {
                        module.Errors.Add(new ModuleAlreadyNamed { Statement = statement });
                        continue;
                    }

                    foreach (var component in importExport.Components)
                    {
                        foreach (var token in component.Source.Span)
                        {
                            if (token is TextLiteral) name += $" {token.sourcecode[1..^1]}";
                            else name += $" {token.sourcecode}";
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
                    module.Datatypes.Add(datatype.Identifier, new Datatype(datatype));
                    break;
                case Grammar.Datum datum:
                    if (module.Data.ContainsKey(datum.Name.Source.ToString()))
                    {
                        module.Errors.Add(new DatumAlreadyExists { Statement = statement });
                    }
                    module.Data.Add(datum.Name.Source.ToString(), new Datum(datum));
                    break;
                case Scope anonymousScope:
                    module.Instructions.Add(new Instruction { Source = anonymousScope });
                    break;
                case Assignment assignment:
                    module.Instructions.Add(new Instruction{ Source = assignment });
                    break;
                case Value value:
                    List<Instruction> instructions = value switch
                    {
                        Reference reference => new() { new UnresolvedInstruction { Reference = reference } },

                        _ => null
                    };
                    module.Instructions.AddRange(instructions);
                    break;
                default: break;
            }
        }

        return module;
    }

    public Semantics Find()
    {
        return null;
    }
}

internal class ModuleAlreadyNamed : Error { }

