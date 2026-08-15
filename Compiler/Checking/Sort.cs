// Copyright © 2026 Eric Budai

using System;
using System.Collections.Generic;
using System.Linq;

namespace Ronin.Compiler;

/// <summary>
///     The semantic type of a value — what the checker will unify and compare,
///     read from a resolved annotation.
/// </summary>
///
/// <remarks>
///     <para>
///     The design documents call this «Type». The name here is <c>Sort</c> because
///     «Type» is already three things — the grammar's datatype declaration
///     (<see cref="Grammar.Type"/>), the lexicon's «type» keyword, and
///     <c>System.Type</c> — and a fourth in the same namespace would be ambiguous at
///     every use. A sort is what classifies a term; that is exactly this.
///     </para>
///     <para>
///     No subtyping (TYPE-HALF-RULINGS §4), so identity is structural EQUALITY and
///     nothing else — two sorts are the same type or they are not. Hand-rolled like
///     <see cref="Node"/> rather than a record, and for the same reason: a record's
///     generated members are unreachable code under the coverage gate.
///     </para>
///     <para>
///     Nine cases. Seven an annotation can name; the ACTION type an inferred
///     no-return body yields and the inference VARIABLE a return or an aggregate
///     leaves under-determined are the other two. No annotation spells either, so
///     <see cref="Of"/> never produces them — but they are shaped here now (Q1),
///     not deferred, so the pass that constructs them adds no case and rewrites no
///     site. «fast» never enters: it qualifies a NUMBER occurrence and is an
///     attribute stored beside the type, so this term holds exactly one number
///     (CHECKER-SCOPING-RULINGS Q1).
///     </para>
/// </remarks>
internal abstract class Sort
{
    /// <summary>Structural equality — the whole of type identity, there being no subtyping.</summary>
    public override bool Equals(object other) => other is Sort sort && sort.GetType() == GetType() && Same(sort);

    /// <summary>Same shape as another of the same kind — the caller has checked the kind.</summary>
    protected abstract bool Same(Sort other);

    public abstract override int GetHashCode();

    /// <summary>The three ground scalars. «error» is apart because it is the bottom, not a scalar.</summary>
    private static readonly HashSet<string> scalars = ["number", "text", "truth"];

    /// <summary>
    ///     The sort a resolved annotation names, or NULL where the words resolve but
    ///     are not one well-formed type: a multi-part group «(a, b)» or a keyed one
    ///     «(a = b)» standing in a single-type position, whose arity and multiplicity
    ///     a later pass refuses (TYPE-HALF-DECISIONS §3). Null propagates — a
    ///     constructor or a function with such a part is itself no sort.
    /// </summary>
    ///
    /// <remarks>
    ///     Every operation is the function arrow: it is the one operator type mode
    ///     has, until «and»/«or» arrive with the algebra slice. A bracketed hole
    ///     «(T)» is T; a group of any other arity falls through to null, which is the
    ///     multiplicity case one pass early.
    /// </remarks>
    public static Sort Of(Node node) => node switch
    {
        Node.Name { Words: "error" } => new Error(),
        Node.Name name when scalars.Contains(name.Words) => new Scalar(name.Words),
        Node.Name name => new Named(name.Words),

        Node.Call call when call.Pattern.Equals(SymbolTable.Listing)
            => Of(call.Arguments[0]) is Sort element ? new List(element) : null,
        Node.Call call when call.Pattern.Equals(SymbolTable.Optional)
            => Of(call.Arguments[0]) is Sort inner ? new Optional(inner) : null,
        Node.Call call when call.Pattern.Equals(SymbolTable.Lookups)
            => Of(call.Arguments[0]) is Sort key && Of(call.Arguments[1]) is Sort value ? new Lookup(key, value) : null,

        Node.Operation arrow => Signature(arrow),

        Node.Group { Kind: Node.Grouping.Group, Parts: [{ Key: null } hole] } => Of(hole.Value),

        _ => null,
    };

    /// <summary>A function type, or null when a parameter or the result is not one sort.</summary>
    private static Sort Signature(Node.Operation arrow)
    {
        IEnumerable<Sort> operands = arrow.Left is Node.Group { Kind: Node.Grouping.Group } list
            ? list.Parts.Select(part => Of(part.Value))
            : [Of(arrow.Left)];

        List<Sort> parameters = [];

        foreach (var operand in operands)
        {
            if (operand is null) return null;

            parameters.Add(operand);
        }

        return Of(arrow.Right) is Sort result ? new Function(parameters, result) : null;
    }

    /// <summary>A ground scalar — «number», «text», or «truth». One number, always.</summary>
    internal sealed class Scalar(string name) : Sort
    {
        public string Name { get; } = name;

        protected override bool Same(Sort other) => ((Scalar)other).Name == Name;

        public override int GetHashCode() => HashCode.Combine('s', Name);
    }

    /// <summary>
    ///     The bottom type «error» — assignable to every type and nothing to it
    ///     (ERROR-AS-VALUE §2), one-directional, and named so «x is an error» reads.
    /// </summary>
    internal sealed class Error : Sort
    {
        protected override bool Same(Sort other) => true;

        public override int GetHashCode() => 'e';
    }

    /// <summary>«list of (_)».</summary>
    internal sealed class List(Sort element) : Sort
    {
        public Sort Element { get; } = element;

        protected override bool Same(Sort other) => ((List)other).Element.Equals(Element);

        public override int GetHashCode() => HashCode.Combine('l', Element);
    }

    /// <summary>«optional (_)», which nests: «optional (optional V)» is not «optional V».</summary>
    internal sealed class Optional(Sort inner) : Sort
    {
        public Sort Inner { get; } = inner;

        protected override bool Same(Sort other) => ((Optional)other).Inner.Equals(Inner);

        public override int GetHashCode() => HashCode.Combine('o', Inner);
    }

    /// <summary>«lookup (_) => (_)».</summary>
    internal sealed class Lookup(Sort key, Sort value) : Sort
    {
        public Sort Key { get; } = key;
        public Sort Value { get; } = value;

        protected override bool Same(Sort other)
        {
            var lookup = (Lookup)other;

            return lookup.Key.Equals(Key) && lookup.Value.Equals(Value);
        }

        public override int GetHashCode() => HashCode.Combine('m', Key, Value);
    }

    /// <summary>«(sig) => result» — a function type, its parameters possibly none.</summary>
    internal sealed class Function(IReadOnlyList<Sort> parameters, Sort result) : Sort
    {
        public IReadOnlyList<Sort> Parameters { get; } = [.. parameters];
        public Sort Result { get; } = result;

        protected override bool Same(Sort other)
        {
            var function = (Function)other;

            return function.Result.Equals(Result) && function.Parameters.SequenceEqual(Parameters);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add('f');
            hash.Add(Result);
            foreach (var parameter in Parameters) hash.Add(parameter);
            return hash.ToHashCode();
        }
    }

    /// <summary>
    ///     A declared «type X» — opaque this pass, unifying only with itself. That is
    ///     not an approximation of a strong alias; it is what one is — same
    ///     representation, different type, no conversion either way
    ///     (CHECKER-SCOPING-RULINGS Q3).
    /// </summary>
    internal sealed class Named(string name) : Sort
    {
        public string Name { get; } = name;

        protected override bool Same(Sort other) => ((Named)other).Name == Name;

        public override int GetHashCode() => HashCode.Combine('n', Name);
    }

    /// <summary>
    ///     The action type an inferred no-return body yields, inadmissible in a value
    ///     position (FIVE-RULINGS §2b). No spelling, so no annotation names it — a case
    ///     rather than a null return, so «the action type differs from every value
    ///     type» stays a comparison and not a null-check at every site that asks.
    /// </summary>
    internal sealed class Action : Sort
    {
        protected override bool Same(Sort other) => true;

        public override int GetHashCode() => 'a';
    }

    /// <summary>
    ///     An inference variable a return or an aggregate leaves under-determined —
    ///     «nothing» is «Optional(Variable(fresh))» and «[]» is «List(Variable(fresh))»,
    ///     each pinned later by unification.
    /// </summary>
    ///
    /// <remarks>
    ///     Two are the same variable by IDENTITY, and identity is the whole of its
    ///     equality. The inferred requirement set — the operations a body applies to
    ///     the parameter, GENERICS-II §5, the interface checked at the call boundary —
    ///     is ACCOMMODATED through that identity: the constraint pass keys it by the
    ///     variable rather than storing it on the type, so the set's representation is
    ///     settled with that pass and no construction site here is rewritten when it
    ///     lands. Shaped now (CHECKER-SCOPING-RULINGS Q1), as a case and not a null.
    /// </remarks>
    internal sealed class Variable(int identity) : Sort
    {
        public int Identity { get; } = identity;

        protected override bool Same(Sort other) => ((Variable)other).Identity == Identity;

        public override int GetHashCode() => HashCode.Combine('v', Identity);
    }
}
