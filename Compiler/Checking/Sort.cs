// Copyright © 2026 Eric Budai

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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
    public static Sort Of(Node node, Func<string, Container> container) => node switch
    {
        Node.Name { Words: "error" } => new Error(),
        Node.Name name when scalars.Contains(name.Words) => new Scalar(name.Words),
        Node.Name name => new Named(container(name.Words), name.Words),

        Node.Call call when call.Pattern.Equals(SymbolTable.Listing)
            => Of(call.Arguments[0], container) is Sort element ? new List(element) : null,
        Node.Call call when call.Pattern.Equals(SymbolTable.Optional)
            => Of(call.Arguments[0], container) is Sort inner ? new Optional(inner) : null,
        Node.Call call when call.Pattern.Equals(SymbolTable.Lookups)
            => Of(call.Arguments[0], container) is Sort key && Of(call.Arguments[1], container) is Sort value
                ? new Lookup(key, value)
                : null,

        Node.Operation arrow => Signature(arrow, container),

        Node.Group { Kind: Node.Grouping.Group, Parts: [{ Key: null } hole] } => Of(hole.Value, container),

        _ => null,
    };

    /// <summary>A function type, or null when a parameter or the result is not one sort.</summary>
    private static Sort Signature(Node.Operation arrow, Func<string, Container> container)
    {
        IEnumerable<Sort> operands = arrow.Left is Node.Group { Kind: Node.Grouping.Group } list
            ? list.Parts.Select(part => Of(part.Value, container))
            : [Of(arrow.Left, container)];

        List<Sort> parameters = [];

        foreach (var operand in operands)
        {
            if (operand is null) return null;

            parameters.Add(operand);
        }

        return Of(arrow.Right, container) is Sort result ? new Function(parameters, result) : null;
    }

    /// <summary>
    ///     The sort a VALUE has, inferred bottom-up — the other half of <see cref="Of"/>,
    ///     which reads a type from an annotation. Null where a node's case is not inferred
    ///     yet, so a caller unifies only what it can name and leaves the rest.
    /// </summary>
    ///
    /// <remarks>
    ///     A literal is the base case, and it denotes itself: «5» is a «number» and a text
    ///     literal a «text». The kind the resolver folded into one «denotes-itself» lexeme
    ///     is recovered by re-lexing the literal's own text — the lexicon already classifies
    ///     it, so this reads its answer rather than second-guessing it. A date literal lexes
    ///     but «date» is no prelude type this pass, so it is left null with the rest.
    /// </remarks>
    public static Sort Infer(Node node) => node switch
    {
        Node.Literal literal => Denoted(literal),
        _ => null,
    };

    /// <summary>The scalar a resolved literal node denotes, its kind re-read from its own text.</summary>
    private static Sort Denoted(Node.Literal literal)
    {
        Lexer lexer = new(literal.Text);

        return Denoted(Ronin.Lexicon.Literal.Lex(ref lexer));
    }

    /// <summary>
    ///     The scalar a lexical literal denotes — a «number» or a «text» — or null for a
    ///     date, which lexes but is no prelude type this pass, and for a run that lexed to
    ///     none. Shared by <see cref="Infer"/>, which re-lexes a resolved node, and the
    ///     initializer check, which reads the token a lone literal still carries.
    /// </summary>
    internal static Sort Denoted(Ronin.Lexicon.Literal token) => token switch
    {
        Ronin.Lexicon.Numeric => new Scalar("number"),
        Ronin.Lexicon.Text => new Scalar("text"),
        _ => null,
    };

    /// <summary>
    ///     A sort spelled as its annotation would be written — «number», «list of number»,
    ///     «lookup text =&gt; number» — for a diagnostic to name the type a value has. NULL
    ///     where this pass does not spell the sort: a shape with no written form at all —
    ///     the action and inference-variable sorts — and, for now, a function, a named
    ///     type, or the bottom «error», each its own later slice; an aggregate carrying one
    ///     is null with it rather than half-spelled.
    /// </summary>
    internal static string Render(Sort sort) => sort switch
    {
        Scalar scalar => scalar.Name,
        List list => Render(list.Element) is { } element ? $"list of {element}" : null,
        Optional optional => Render(optional.Inner) is { } inner ? $"optional {inner}" : null,
        Lookup lookup => Render(lookup.Key) is { } key && Render(lookup.Value) is { } value
            ? $"lookup {key} => {value}"
            : null,
        _ => null,
    };

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
    ///
    /// <remarks>
    ///     Identified by its declaring <see cref="Container"/> AND its name, not the
    ///     name alone (SCOPE-IDENTITY-RULING, H): two «token»s in two functions are two
    ///     types, and the container — the module it is in and the named scopes it
    ///     belongs to — is what tells them apart. The name alone made them one, which
    ///     was REAUDIT54 finding 1. The container is rooted in a <see cref="ModuleIdentity"/>,
    ///     a type and not a string, so two same-named types in two modules are two
    ///     types and two unsaved buffers are two modules.
    /// </remarks>
    internal sealed class Named(Container container, string name) : Sort
    {
        public Container Container { get; } = container;
        public string Name { get; } = name;

        protected override bool Same(Sort other)
        {
            var named = (Named)other;

            return named.Name == Name && named.Container.Equals(Container);
        }

        public override int GetHashCode() => HashCode.Combine('n', Name, Container);
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
    ///     Minted from a <see cref="Supply"/> and never constructed directly, so no
    ///     two are one: an inference variable's identity is the engine's to MINT,
    ///     freshness the whole of the property, and equality is REFERENCE. The invalid
    ///     state a public constructor allowed — two «Variable(7)» equal yet owning
    ///     independent requirement sets — cannot be built, because the state is
    ///     unconstructible, not merely detectable (CONTAINER-IDENTITY-RULING, VARIABLE-
    ///     AND-MODULE Q4a; REAUDIT56 finding 4). Two are the same variable only when
    ///     they are the same variable.
    ///     <para>
    ///     <see cref="Requirements"/> is the set the constraint pass records into: the
    ///     operations a body applies to the parameter (GENERICS-II §5), the interface
    ///     checked at the call boundary. Each is a whole <see cref="Requirement"/>
    ///     record deduped whole, not a bare pattern (VARIABLE-AND-MODULE Q4b). Owned
    ///     and empty until that pass fills it — the shape it will fill, without a new
    ///     construction site; the machinery it drives is deferred.
    ///     </para>
    /// </remarks>
    internal sealed class Variable : Sort
    {
        private Variable(int identity) => Identity = identity;

        public int Identity { get; }

        public ISet<Requirement> Requirements { get; } = new HashSet<Requirement>();

        protected override bool Same(Sort other) => ReferenceEquals(this, other);

        public override int GetHashCode() => HashCode.Combine('v', Identity);

        /// <summary>The supply an inference run mints its variables from — a fresh one each call, no two one.</summary>
        internal sealed class Supply
        {
            private int minted;

            public Variable Fresh() => new(minted++);
        }
    }
}

/// <summary>
///     One requirement on an inference variable: a pattern that must resolve for a
///     tuple of type terms, and the site that induced it (GENERICS-II §5).
/// </summary>
///
/// <remarks>
///     Deduped WHOLE and by STRUCTURE — two requirements sharing a pattern over
///     different operands, or induced at different sites, are two, while two built
///     apart from the same pattern, operand sorts, and site are one, which neither a
///     set of bare patterns nor a tuple compared by list reference could tell apart.
///     The operands are the tuple the pattern resolves for, OWNED here so an operand
///     stored in a set cannot change under it; the provenance is the site the call-
///     boundary diagnostic names. Three fields and no solver behind them: the shape
///     the constraint pass fills, not the machinery (REAUDIT56 finding 4, GENERICS-II §5).
/// </remarks>
internal sealed class Requirement(Pattern pattern, IReadOnlyList<Sort> operands, Span provenance)
{
    public Pattern Pattern { get; } = pattern;
    public IReadOnlyList<Sort> Operands { get; } = [.. operands];
    public Span Provenance { get; } = provenance;

    public override bool Equals(object other)
        => other is Requirement requirement
        && requirement.Pattern.Equals(Pattern)
        && requirement.Provenance.Equals(Provenance)
        && requirement.Operands.SequenceEqual(Operands);

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Pattern);
        hash.Add(Provenance);
        foreach (var operand in Operands) hash.Add(operand);

        return hash.ToHashCode();
    }
}

/// <summary>
///     What a named type belongs to: the module it is in, then a segment per
///     enclosing named scope. Compared as a STRUCTURE — never a joined string a
///     module or a segment holding the separator could collide on (CONTAINER-
///     IDENTITY-RULING §3).
/// </summary>
internal sealed class Container(ModuleIdentity module, IReadOnlyList<string> segments)
{
    public ModuleIdentity Module { get; } = module;
    public IReadOnlyList<string> Segments { get; } = [.. segments];

    /// <summary>The same container one named scope deeper.</summary>
    public Container Within(string segment) => new(Module, [.. Segments, segment]);

    public override bool Equals(object other)
        => other is Container container && container.Module.Equals(Module) && container.Segments.SequenceEqual(Segments);

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Module);
        foreach (var segment in Segments) hash.Add(segment);

        return hash.ToHashCode();
    }
}

/// <summary>
///     The identity of the module a type is rooted in — a TYPE, not a string, so that
///     nothing can parse one form back as another and the "never parse a rendered
///     identity back" rule holds by construction (CONTAINER-IDENTITY-RULING §3,
///     VARIABLE-AND-MODULE Q5).
/// </summary>
///
/// <remarks>
///     A saved file is its <see cref="Path"/> — a location. An unsaved editor buffer
///     is a <see cref="Buffer"/> — a stable token belonging to the editor's document,
///     so an unsaved file's types have an identity of their own and two unsaved
///     buffers are two modules (VARIABLE-AND-MODULE Q5a). The ledger's successor, a
///     declared module name, is the third case this type has room for.
/// </remarks>
internal abstract class ModuleIdentity
{
    public override bool Equals(object other) => other is ModuleIdentity module && module.GetType() == GetType() && Same(module);

    protected abstract bool Same(ModuleIdentity other);

    public abstract override int GetHashCode();

    /// <summary>A saved file, by its path — a location, stable only while the file does not move.</summary>
    internal sealed class Path(string location) : ModuleIdentity
    {
        public string Location { get; } = location;

        protected override bool Same(ModuleIdentity other) => ((Path)other).Location == Location;

        public override int GetHashCode() => HashCode.Combine('p', Location);
    }

    /// <summary>An unsaved buffer, by a token that belongs to its document rather than any one snapshot.</summary>
    internal sealed class Buffer(object token) : ModuleIdentity
    {
        public object Token { get; } = token;

        protected override bool Same(ModuleIdentity other) => ReferenceEquals(((Buffer)other).Token, Token);

        public override int GetHashCode() => HashCode.Combine('b', RuntimeHelpers.GetHashCode(Token));
    }
}
