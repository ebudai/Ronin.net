using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
internal class Datatype : Semantics
{
    public Identifier Identifier { get; init; }
    public bool IsOptional { get; init; }

    public List<Datatype> InnerDatatypes { get; } = new();
    public List<Datum> Data { get; } = new();
    public List<Function> Methods { get; } = new();

    public List<Datatype> Parents { get; } = new();
    public List<Datatype> Unions { get; } = new();

    public Datatype() { }

    public Datatype(DatatypeDeclarationSyntax declaration)
    {
        Source = declaration;

        Identifier = new(declaration.Identifier);

        foreach (var statement in declaration.Body.Values)
        {
            switch (statement.value)
            {
                case FunctionDeclarationSyntax:     Methods.Add(new Function(statement));         break;
                case DatatypeDeclarationSyntax:     InnerDatatypes.Add(new Datatype(statement));  break;
                case DatumDeclarationSyntax:        Data.Add(new Datum(statement));               break;

                case ImportExportSyntax:    Errors.Add(new DatatypeCannotJoinNamedScope { Statement = statement });                         break;
                case AssignmentSyntax:      Errors.Add(new DatatypeDefinitionCannotContain<AssignmentSyntax> { Statement = statement });    break;                
                case Scope:                 Errors.Add(new DatatypeDefinitionCannotContain<Scope> { Statement = statement });               break;
                case IntervalSyntax:        Errors.Add(new DatatypeDefinitionCannotContain<IntervalSyntax> { Statement = statement });      break;
                
                case Value value: Errors.Add(value.value switch
                {
                    LiteralSyntax => new DatatypeDefinitionCannotContain<LiteralSyntax> { Statement = statement },
                    Arguments => new DatatypeDefinitionCannotContain<Arguments> { Statement = statement },
                    InlineListSyntax => new DatatypeDefinitionCannotContain<InlineListSyntax> { Statement = statement },
                    InlineLookupSyntax => new DatatypeDefinitionCannotContain<InlineLookupSyntax> { Statement = statement },
                    DelegateSyntax => new DatatypeDefinitionCannotContain<DelegateSyntax> { Statement = statement },
                    Reference => new DatatypeDefinitionCannotContain<Reference> { Statement = statement },
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