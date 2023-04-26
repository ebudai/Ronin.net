using Ronin.Grammar;
using Ronin.Grammar.Compound;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Datatype : Semantics
{
    public Identifier Identifier { get; init; }
    public bool IsOptional { get; init; }

    public List<Datatype> InnerDatatypes { get; } = new();
    public List<Datum> Data { get; } = new();
    public List<Function> Methods { get; } = new();

    public List<Datatype> BaseDatatypes { get; } = new();
    public List<Datatype> Unions { get; } = new();

    public Datatype() { }

    public Datatype(Grammar.Datatype declaration)
    {
        Source = declaration;

        Identifier = declaration.Identifier;

        foreach (var statement in declaration.Body.Values)
        {
            switch (statement.value)
            {
                case Grammar.Function: Methods.Add(new Function(statement));            break;
                case Grammar.Datatype: InnerDatatypes.Add(new Datatype(statement));     break;
                case Grammar.Datum: Data.Add(new Datum(statement));                     break;

                case ImportExport: Errors.Add(new DatatypeCannotJoinNamedScope { Statement = statement });                  break;
                case Assignment: Errors.Add(new DatatypeDefinitionCannotContain<Assignment> { Statement = statement });     break;                
                case Scope: Errors.Add(new DatatypeDefinitionCannotContain<Scope> { Statement = statement });               break;
                case Interval: Errors.Add(new DatatypeDefinitionCannotContain<Interval> { Statement = statement });         break;
                
                case Value value: Errors.Add(value.value switch
                {
                    Literal => new DatatypeDefinitionCannotContain<Literal> { Statement = statement },
                    Arguments => new DatatypeDefinitionCannotContain<Arguments> { Statement = statement },
                    InlineList => new DatatypeDefinitionCannotContain<InlineList> { Statement = statement },
                    InlineLookup => new DatatypeDefinitionCannotContain<InlineLookup> { Statement = statement },
                    Grammar.Delegate => new DatatypeDefinitionCannotContain<Grammar.Delegate> { Statement = statement },
                    Reference => new DatatypeDefinitionCannotContain<Reference> { Statement = statement },
                    _ => new UnknownSyntaxError { Statement = statement }
                }); break;

                default: Errors.Add(new UnknownSyntaxError { Statement = statement }); break;
            }
        }
    }
}

[ExcludeFromCodeCoverage]
internal class UnresolvedDatatype : Datatype
{
    public UnresolvedDatatype(Reference reference) => Source = reference;
}

[ExcludeFromCodeCoverage]
internal class DatatypeCannotJoinNamedScope : Error { }

[ExcludeFromCodeCoverage]
internal class DatatypeDefinitionCannotContain<T> : Error where T : Syntax { }