# Re-audit 55 — Type-term incorporation and named-container scope

**Audited:** `207409f..3849c5d`, principally `53a3825`, `bbaf4ca`,
`ec9544d`, and `6d5b872`, against `REAUDIT54`, `NAMEDIDENTITY`,
`SCOPEIDENTITYRULING`, `SEMANTICCHECKERSCOPING`, and
`CHECKERSCOPINGRULINGS`.

**Date:** 2026-08-15

## Result

**Not signed off. Two high-severity and four medium-severity findings remain.**

The prior over-limit-annotation defect is closed, `Action` is now a real sort,
the sibling-function witness no longer merges two named types, and semantic
duplicate classification correctly recognizes `number` and `(number)` as the
same parameter type. The ordinary block-hoisting witness for the designer's H
scope ruling also works.

The new container identity is not yet an identity across the boundaries the
ruling names: every module is the empty container, and overload bodies also
collide on one string path. The hoisting walk crosses a named function through a
delegate in its parameter default and makes a function-local type module-global.
The signature classifier computes transient sorts but signatures still store
only strings; the requirement-bearing part of `Variable` remains an unruled
future external map; hoisting reverses declaration provenance in a common mixed
case; and the ruled language change was not added to the specification as the
designer expressly required.

---

## Findings

### 1. High — the string container path is not a unique declaring-container identity

`Sort.Named` now compares `(Container, Name)`, but the root call starts every
compilation with `container = ""` (`Compiler/Compilation.cs:100-114`) and nested
paths append only `Identifier.Words` (`:147-156`). That closes the original two
sibling functions only because `/left` and `/right` happen to differ. It does not
identify the module, and a function name is not enough to identify one declaration
once same-shape overloads exist.

The module case directly contradicts `NAMEDIDENTITY` Q1b, which says two
same-named types in two files are already distinct because their declaring scopes
differ. This audit probe failed:

```csharp
Sort left = Assert.Single(Compilation.Of(
    new SourceText("type token; var x => token;\n", "left.ron")).Types).Type;
Sort right = Assert.Single(Compilation.Of(
    new SourceText("type token; var x => token;\n", "right.ron")).Types).Type;

Assert.NotEqual(left, right); // failed: both Named("", "token")
```

This is not safely deferred with import visibility. The ruling deliberately
separates identity from visibility, and also says before-edit/after-edit comparison
across compilations is the normal always-running case. A module must therefore be
part of the value now, even while which modules can see each other remains deferred.

There is a second collision in the same representation:

```ronin
type a;
type b;
function use (x => a) { type token; var local => token; return x; }
function use (x => b) { type token; var local => token; return x; }
```

The two local `token` sorts both become `Named("/use", "token")` and compare
equal. Yet they come from separate declarations in separate bodies. If overload
variants are distinct named containers, the container identity must distinguish
them. If the overload set is one logical container, H's uniqueness rule requires
the two `token` declarations to be checked together and reported `Shadowed`.
Current code does neither; the temporary `Overloaded` finding merely hides the
collision until that ledger row expires.

Replace the presentation string with a structural container identity whose root
includes the stable module identity and whose named segments are themselves
unambiguous declaration identities. The overload-body choice needs a designer
ruling if “function” means the overload set rather than one declaration. Add both
cross-compilation and overloaded-body regressions; rendering the identity as a path
for diagnostics is separate from what equality hashes.

### 2. High — hoisting crosses a named function boundary through parameter defaults

`Hoisted` intends to stop at a nested named container, but implements that by
filtering the `Body` values returned by `Bodies` to those whose `Container` is
null (`Compiler/Compilation.cs:540-558`). `Bodies` deliberately continues through
syntax **beside** a function body so it can find delegate scopes in parameter
defaults (`:475-481,506-516`). The function body's `Body` is filtered out, but an
anonymous delegate found in its signature still passes the filter. Because this
walk is running for the containing module, types in that delegate are hoisted to
the module rather than the function.

The source-level result is a silent scope leak:

```ronin
function run (callback = (x) => {
    type token;
    var local => token;
    return x;
}) {
    var inside => token;
    return 1;
}
var outside => token;
```

Under `SCOPEIDENTITYRULING` H, the delegate is anonymous and `token` belongs to
the enclosing function `run`; `inside` may name it and `outside` may not. Current
compilation reports **no finding**. All three annotations resolve, all three named
sorts carry the module container `""`, and the module now exports a declaration
written inside a function's default delegate.

The stopping condition must apply to the whole named construct, not only its main
body. Conversely, the named function must collect transparent scopes in both its
body and its ancillary syntax as its own. Add a regression that checks the inside
use, the outside `UnknownType`, and the retained `/run` identity rather than only
checking that some nested block compiles.

### 3. Medium — signature sorts are used transiently but still are not bound to signatures

The previous `number` versus `(number)` behavioral witness is fixed: `Sorted`
resolves each spelling and groups by the resulting `Sort`. That closes the wrong
`Overloaded` diagnostic.

It does not implement the ruled storage change. `Declarations.Signature` remains
`Signature(Blocks Names, Blocks Types)`, where `Blocks` is a nested list of
strings (`Compiler/Grammar/Declarations.cs:540-546`). `Sorted` creates a local
`List<object>` of resolved sorts for one classification call (`:166-218`) and
discards it. The signature carries no semantic parameter sorts, no return sort,
and no spelling-plus-sort pair. `Compilation.Types` remains a separate flat
`(Span, Sort)` list with no declaration/signature owner or modifier relationship.

That is narrower than the programmer's own pre-build memo (“bind each resolved
parameter and return sort to its owning signature, keep the spelling beside it”)
and than `NAMEDIDENTITY` Q2, which keeps signature-sort **binding** in step 1.
The next checker pass still has to re-resolve or reconstruct ownership before it
can consume a signature.

Retain the now-correct semantic grouping, but store the resolved parameter and
return sorts on the owning signature beside their spellings. Add a test over the
signature record itself, not only its downstream diagnostic. `REAUDIT54` finding
3 is behaviorally improved but not structurally closed.

### 4. Medium — `Variable` is still the bare shape the Q1 addition rejected

`Action` is correctly added. `Variable` is `Variable(int identity)` with equality
and hashing by that integer (`Compiler/Checking/Sort.cs:228-249`). Its remarks say
the future constraint pass will keep an external map keyed by the variable.

`CHECKERSCOPINGRULINGS` Q1 was explicit that `Variable(id)` bare has nowhere for
the inferred requirement set and asked for the case to be shaped now so later
construction sites are not rewritten. Choosing a future external constraint
environment may be a reasonable solver architecture, but it is a new decision,
not the confirmed shape, and no such environment or contract exists yet. Any
hashable object could someday be an external key; that does not itself implement
the requested accommodation.

Either give `Variable` an explicit requirements handle/slot now (without building
the constraint machinery), or obtain a superseding designer ruling that the
requirements live in a per-solver environment keyed by variable identity and
state its lifetime/ownership. Until then `REAUDIT54` finding 2 is closed for
`Action` but only partially closed for `Variable`.

### 5. Medium — hoisting reorders declarations and blames the earlier one for a collision

A named scope declares `statements.Concat(statements.SelectMany(Hoisted))`
(`Compiler/Compilation.cs:110-112`). Therefore every direct declaration is
processed before every hoisted declaration, regardless of source order. For:

```ronin
{ type token; }
type token;
```

H correctly makes the two declarations collide in the module, but the emitted
`Shadowed.Primary` is `probe.ron:1:8`—the earlier declaration inside the block.
The later line 2 declaration is recorded as “first declared here.” The diagnostic
asks the author to change the declaration that should have won and reverses the
project's established rule that a collision blames the later declaration.

Lift declarations in source order—either with one ordered walk or by sorting the
combined declaration sites before `Declarations.Of`. Add both orderings of a
direct/anonymous-scope pair and assert primary and related spans, not only the
finding kind.

### 6. Medium — the ruled container-scope semantics were not added to the language specification

`SCOPEIDENTITYRULING` calls H a real language-semantics change and explicitly
directs that this exact rule go in the spec rather than land as a silent
tightening:

> A type declaration belongs to its nearest named container—a module, type body,
> or function. A type name is unique within that container, across its anonymous
> sub-scopes.

The implementation now rejects formerly legal sibling-block declarations and
widens visibility outside the declaring block, but `207409f..3849c5d` changes no
file under `docs/spec`, and a search of the current specification finds no nearest-
named-container or container-wide type-uniqueness rule. Only the handoff ruling
states it.

Add the rule to the appropriate scope/declaration section of
`docs/spec/grammatical-structure.md` and link its consequences for visibility and
`Shadowed`. This is required documentation for an intentional language change,
not retrospective implementation commentary.

---

## Disposition of `REAUDIT54`

| Prior finding | Reassessment |
|---|---|
| 1. named identity is spelling-only | **Still open.** Sibling functions are now distinct, but the chosen container value collapses all modules and same-named overload bodies. Finding 1. |
| 2. `Action` and requirement-bearing `Variable` absent | **Partially closed.** `Action` is correct and `Variable` has identity/equality, but the requirement-set addition was replaced by an unruled future external map. Finding 4. |
| 3. signature sorts not bound | **Partially closed.** Semantic sort grouping fixes the exact duplicate-classification witness, but the signature record still stores only spelling and no return sort. Finding 3. |
| 4. over-limit annotation disappears | **Closed.** `TooLong` produces `OversizeType` at the annotation span and no fabricated sort; the source-level finding is maintained. |

## What the implementation gets right

- `Action` is a proper sort rather than null, with equality and hash coverage.
- `Variable` is a distinct identity-bearing sort rather than null; only the ruled
  requirement-set accommodation remains unsettled.
- `OversizeType` turns the resolver ceiling into a production diagnostic and the
  maintained aggregate findings fixture includes it.
- Named sort equality and hashing now include a container as well as the spelling;
  the original two-sibling-function reproduction is guarded and passes.
- Declaring-container provenance survives inheritance through `Declared.Container`.
- The ordinary H-wide block case works: a deeply block-nested type is nameable
  elsewhere in its function, and same-named sibling-block declarations produce
  `Shadowed`.
- Duplicate signature classification now compares semantic sorts, preserving
  block arity and keeping the permanent/expiring ledger partition correct for
  equivalent annotation spellings.
- The base-resolution deferral remains properly ruled and nothing has begun
  consuming `Bases` or `Unions`.

## Verification record

Temporary audit probes were removed before this report was written.

- Inspected the complete `207409f..3849c5d` production, test, ruling, ledger, and
  report diff, plus adjacent scope discovery, annotation traversal, declaration
  provenance, signature grouping, and specification text.
- Cross-compilation identity probe: **failed as finding 1 predicts**—same-named
  module types from `left.ron` and `right.ron` compared equal.
- Parameter-default boundary probe: **failed as finding 2 predicts**—a type inside
  a function default delegate became module-visible, all three uses carried the
  module container, and no finding was emitted.
- Hoisted-order probe: **failed as finding 5 predicts**—the line 1 declaration was
  primary and the later line 2 declaration was treated as first.
- Overload-container probe: **failed as finding 1 predicts**—two local declarations
  in distinct overload bodies produced equal `/use/token` sorts.
- `dotnet restore Ronin.sln --locked-mode`: passed.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror`:
  passed with zero warnings and zero errors.
- Full Debug suite: **1,266 passed, 0 failed, 0 skipped**.
- Exact Release coverage suite: **1,266 passed, 0 failed, 0 skipped**;
  **3,968/3,968 lines**, **2,751/2,751 branches**, and 100% methods.
- `dotnet list Ronin.sln package --vulnerable --include-transitive`: no known
  vulnerable direct or transitive packages in the configured source.
- Exact changed-file `dotnet format --verify-no-changes`: passed, formatted zero
  files, and emitted no diagnostics.
- `git diff --check`: passed before the report was added.
- No production or maintained test file was changed by this audit. The working
  tree was clean before this report was added; this report is the only audit
  artifact.
