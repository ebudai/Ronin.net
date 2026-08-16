# Re-audit 56 — Container ruling incorporation and type-term boundaries

**Audited:** `3849c5d..a5bce23`, principally `eb58ca0`, `8d9a50a`,
`699cda9`, `ada4e7d`, `be312c0`, and `a5bce23`, against `REAUDIT55`,
`SCOPEIDENTITYRULING`, the new `CONTAINERIDENTITY` relay and
`CONTAINERIDENTITYRULING`, `NAMEDIDENTITY`, `SEMANTICCHECKERSCOPING`,
`CHECKERSCOPINGRULINGS`, `GENERICSII`, and the amended language specification.

**Date:** 2026-08-15

## Result

**Not signed off. Three high-severity and two medium-severity findings remain.**

The programmer has correctly implemented the designer's central decision: an
overload set is one named container. This audit does **not** revive variant-specific
containers or the sort-based identity rejected by `CONTAINERIDENTITYRULING`.
Path-bearing modules now distinguish their named types, container equality is
structural, ordinary overload bodies enforce B, hoisted declarations preserve source
order, signatures retain parameter and return sorts, the H rule is in the language
specification, and the original parameter-default leak into the module is closed.

The remaining defects sit at boundaries those changes did not cover. A type belonging
to a function is still not visible in that function's own signature. The cross-body B
check omits types declared in parameter-default delegates. The new return-sort capture
casts every pattern-declaring member to `Function`, so ordinary invalid input can kill
the compiler. The inference-variable requirement slot splits one semantic identity
over independent mutable sets and cannot yet represent the ruled requirement. Finally,
all pathless sources still share one module identity despite `SourceText` expressly
supporting pathless editor buffers.

---

## Findings

### 1. High — a patterned non-function declaration terminates compilation

`Declarations.Declare` accepts a pattern from any `Member`, then constructs a
signature with `Returned(member)` (`Compiler/Grammar/Declarations.cs:369-423`). The
new helper unconditionally casts that member to `Function`
(`Compiler/Grammar/Declarations.cs:234-238`). `Datum.Parse` accepts the general
`Identifier` grammar, so source can reach this path as a `Datum` even though the
language specification restricts a datum's identifier to words.

The source-level audit probe was:

```ronin
var provide (x => number) => number;
```

`Compilation.Of` threw:

```text
System.InvalidCastException: Unable to cast object of type
'Ronin.Grammar.Datum' to type 'Ronin.Grammar.Function'.
  at Ronin.Grammar.Declarations.Returned(Member member)
```

Invalid source must become a finding, not terminate the compiler or language-server
request. Making `Returned` return null for a non-function avoids the exception but is
not the complete fix: it would silently install a datum pattern which
`docs/spec/grammatical-structure.md` §4.5.1 does not permit. Refuse holes on datum and
datatype identifiers at their grammar/declaration boundary, retain function patterns,
and add a compilation-level regression which asserts a finding and no exception for
each non-function member kind.

### 2. High — H-wide types are not visible in their owning function's signature

The amended specification says a datatype is usable **throughout** its nearest named
container. `SCOPEIDENTITYRULING` chose that wide reading, and
`CONTAINERIDENTITYRULING` expressly treats the signature as part of the function.
Current scope construction instead builds and resolves a function's signature in the
enclosing declaration table (`Compiler/Grammar/Declarations.cs:113-122,
219-232`). Only afterwards does `Compilation.Scope` enter the body and build the table
containing types lifted into the function (`Compiler/Compilation.cs:128-182`). The
annotation walk consequently reads parameter and return annotations against the
module, not the named function container (`Compiler/Compilation.cs:150-164`).

This direct body-local witness produced two `UnknownType` findings and left both
stored signature sorts null:

```ronin
function run (value => token) => token {
    type token;
    return value;
}
```

The same failure occurs when `token` is declared inside a parameter-default delegate
and used by another parameter and the return:

```ronin
function run
    (callback = (x) => { type token; return x; })
    with (value => token) => token
{
    return value;
}
```

That source also produced two `UnknownType` findings. The maintained default-delegate
test proves the delegate body and main body can see the type, but never exercises the
signature whose container the ruling includes.

Build the complete named-function type table—body, transparent body scopes, and
ancillary signature scopes—before resolving that function's parameter/return
annotations and before storing its `Signature` sorts. Add direct-body and ancillary
witnesses covering parameter and return annotations, their absence from module scope,
and the resulting `Named([module, function], name)` values.

### 3. High — overload-wide uniqueness omits parameter-default delegates

The implementation correctly lifts ancillary types into each individual function
scope (`Compiler/Compilation.cs:120-135`) and correctly checks ordinary body types
across bodies sharing one name. But the B check collects only `TypesOf(body)`
(`Compiler/Compilation.cs:191-214`); `TypesOf` reads the body's statements and their
transparent descendants, not `body.Ancillary`. Consequently a same-named type in two
parameter-default delegates is never compared across overload bodies.

The audit probe was:

```ronin
function use (x => number)
    with (callback = (y) => { type token; return y; })
{ return x; }

function use (x => text)
    with (callback = (y) => { type token; return y; })
{ return x; }
```

Compilation emitted only the temporary `Overloaded` finding and no `Shadowed`.
Under the designer's B ruling both declarations belong to the one `use (_) with (_)`
container, carry the same named-sort identity, and must be rejected by H's uniqueness
rule. The temporary overload refusal does not substitute for that permanent rule; it
is explicitly ledgered to expire.

Make the cross-body inventory use the same complete body-plus-ancillary declaration
set used to build each named container. Test ancillary/ancillary and body/ancillary
pairs, block nesting within an ancillary delegate, source-order provenance, and a
different type name as the negative control.

### 4. Medium — equal inference variables have divergent requirement state

`Variable` is equal and hashes solely by `Identity`, while every construction creates
a fresh mutable `HashSet<Pattern>` (`Compiler/Checking/Sort.cs:248-269`). Thus two
terms which the type system says are the same inference variable can answer different
questions about their interface:

```csharp
var left = new Sort.Variable(7);
var right = new Sort.Variable(7);

Assert.Equal(left, right);
left.Requirements.Add(Pattern.Parse("print _"));
Assert.Empty(right.Requirements); // current result
```

This breaks substitutability precisely in the state the designer required the
variable to carry. A later pass that reconstructs, copies, deserializes, or otherwise
encounters the same identity through another term can silently lose accumulated
requirements.

The selected element type is also too narrow for the ruled interface.
`GENERICSII` §5 defines a requirement as **a pattern resolving for a tuple of types**,
with provenance; its `max` example requires one operation over a pair of variables.
An `ISet<Pattern>` records neither operand relationships nor the source/propagation
chain needed for the required call-site diagnostic. The source comment itself says
the element type remains to be confirmed, so this is not yet the shaped final handle
the ruling requested.

Give each semantic variable identity one shared requirement cell/handle and define a
requirement value capable of carrying the pattern, participating type terms, and
provenance. If construction guarantees object uniqueness instead, make that an
enforced invariant and align equality with it; do not leave equal independently
mutable values.

### 5. Medium — every pathless source is the same module

The ruled path-bearing case is fixed, but module construction uses
`container ??= [Source.Path]` without a pathless fallback
(`Compiler/Compilation.cs:100-111`). `SourceText.Path` is deliberately optional:
its contract says editor buffers and tests may have none and that the source object's
identity remains valid (`Compiler/Diagnostics/Span.cs:42-54`). Two independent
pathless compilations therefore both produce `Named([null], "token")` and compare
equal:

```csharp
Sort left = Assert.Single(Compilation.Of(
    new SourceText("type token; var x => token;\n")).Types).Type;
Sort right = Assert.Single(Compilation.Of(
    new SourceText("type token; var x => token;\n")).Types).Type;

Assert.NotEqual(left, right); // fails: both containers are [null]
```

That reopens the module half of the identity collision for the exact pathless buffer
case the source abstraction promises. Preserve the ruled path identity when present,
but give a pathless source a distinct structural module token (or reject pathless
compilation and change the contradictory `SourceText` contract). Do not render a
synthetic token into a string and parse it back; the new ruling's structural-identity
prohibition still applies.

---

## Disposition of `REAUDIT55`

| Prior finding | Reassessment |
|---|---|
| 1. container identity collapses modules and overload bodies | **Mostly closed.** Path-bearing modules are distinct, identity is structural, and the designer's B ruling is correctly enforced for ordinary body declarations. Pathless modules still collapse (finding 5), and the B uniqueness extent omits ancillary declarations (finding 3). |
| 2. parameter-default delegate leaks its type into the module | **Original witness closed.** Body and delegate now share the function container and the module cannot name the type. The complete H-wide boundary is still absent from the owning signature (finding 2), and ancillary declarations are absent from cross-overload uniqueness (finding 3). |
| 3. signature sorts not bound | **Closed, with a regression.** Parameter and return sorts are now retained beside spellings and the classifier consumes them. The new unconditional return extraction creates finding 1. Function-local signature types remain unresolved because of finding 2. |
| 4. `Variable` lacks a requirement handle | **Mechanically closed, semantically incomplete.** A slot exists, but equal variables own divergent state and `Pattern` alone cannot represent the ruled requirement (finding 4). |
| 5. hoisting reverses source provenance | **Closed.** Mixed direct/hoisted declarations are ordered by source and both maintained directions blame the later site. |
| 6. H absent from the language specification | **Closed.** `docs/spec/grammatical-structure.md` now states container-wide visibility and uniqueness. |

## What the implementation gets right

- The designer's B ruling is accepted as written: an overload set is one named
  container. The maintained ordinary-body collision test emits `Shadowed` at the
  later declaration.
- The module path is now present for path-bearing sources and the approximation is
  honestly recorded in `Test/Expiry.cs` with the declared-module-name successor.
- `Sort.Named` owns and compares a structural segment sequence, so delimiter
  collisions cannot manufacture type equality.
- The original parameter-default leak is fixed: its declaration belongs to the
  function, body and delegate uses resolve there, and an outside module use is
  `UnknownType`.
- Hoisted declarations are processed in source order and retain later-site blame.
- Every stored function signature now carries resolved parameter sorts and its return
  sort beside the original spellings; semantic duplicate classification reads that
  stored value rather than resolving again.
- The H language change is documented in the normative grammar.
- The type term's existing structural equality/hash coverage and the path-bearing
  cross-module witness remain sound.

## Verification record

Temporary audit probes were removed before this report was written.

- Inspected the complete `3849c5d..a5bce23` production, test, specification,
  ruling, ledger, and handoff diff, plus adjacent parsing, annotation traversal,
  declaration ownership, signature grouping, and source identity contracts.
- Patterned-datum probe: **failed as finding 1 predicts** with
  `InvalidCastException` in `Declarations.Returned`.
- Function-body type in owning signature: **failed as finding 2 predicts** with two
  `UnknownType` findings.
- Parameter-default type in sibling parameter and return: **failed as finding 2
  predicts** with two `UnknownType` findings.
- Same ancillary type in two overload bodies: **failed as finding 3 predicts**—only
  `Overloaded`, no `Shadowed`.
- Equal-variable requirement-state probe: **failed as finding 4 predicts**—mutating
  one equal value left the other set empty.
- Pathless-module identity probe: **failed as finding 5 predicts**—both sorts were
  `Named([null], "token")` and equal.
- A same-words/different-hole-layout container probe also produced `Shadowed`, but
  the declarations independently violate `AnchorPrefix`; it is invalid source and
  is deliberately **not** a finding here.
- `dotnet restore Ronin.sln --locked-mode`: passed.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror`:
  passed with zero warnings and zero errors.
- Full Debug suite: **1,272 passed, 0 failed, 0 skipped**.
- Exact Release coverage suite: **1,272 passed, 0 failed, 0 skipped**;
  **4,016/4,016 lines**, **2,801/2,801 branches**, and 100% methods.
- `dotnet list Ronin.sln package --vulnerable --include-transitive`: no known
  vulnerable direct or transitive packages in the configured source.
- Exact changed-file `dotnet format --verify-no-changes`: passed, formatted zero
  files, and emitted no diagnostics.
- `git diff --check`: passed before the report was added.
- No production or maintained test file was changed by this audit. The working tree
  was clean before this report was added; this report is the only audit artifact.
