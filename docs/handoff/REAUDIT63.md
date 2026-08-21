# Re-audit 63 — Return inference, callable ownership, and supplied-value sorts

**Audited:** `ede64ce..15c94b3`, with `503f444` as the repair of
`REAUDIT62`, principally the semantic-checker and return-inference work from
`366bdae` through `7695911`, against `CHECKERSCOPINGRULINGS`,
`RETURNANDLITERALS`, `RECURSIVERETURN`, `MONOMORPHANDRETURN`,
`INFERENCEPASSVALIDATION`, and `NOTHINGSPELLINGRULING`.

**Date:** 2026-08-20

## Result

**Not signed off. Four high-severity findings and one medium-severity finding
remain.**

`REAUDIT62` is closed. Finding identity now includes the complete participant
set, so sibling conflicts sharing one inherited primary both survive while a
repeated classification of the same complete set is still deduplicated.

The new checker foundation is substantial and the maintained gate is green.
Declarations, readings, inference, and checking now have explicit phase
boundaries; inferred return sorts drain through call chains and mutually
recursive components; nested blocks contribute return sites to their enclosing
function; empty-list variables can be pinned by siblings and expected types;
`nothing` is supplied as an optional of a fresh variable; and findings are
presented in source order.

Five gaps remain at the boundaries between those pieces. Truth literals are
supplied but have no value sort. A delegate's return sites are assigned to an
enclosing function. Omitted-return inference discards variable-bearing answers
instead of unifying them. A body with no answer never receives the action sort.
Finally, return sites from nested blocks are compared in traversal order rather
than source order, making the diagnostic say that an earlier return is later.

The first four silently accept invalid source or reject valid source and leave
the semantic model with the wrong or missing answer sort. The fifth reports the
right disagreement at the wrong participant with a factually reversed message.

---

## Findings

### 1. High — `true` and `false` have no semantic value sort

The supplied descriptors correctly say that `true` and `false` denote `truth`
(`Compiler/Resolution/Resolver.cs:1985-1986`), and `SymbolTable.Truths` now
derives from that property rather than from the proxy «has no shape»
(`:2044-2047`). That repaired the registry defect `NOTHINGSPELLINGRULING` §2
identified.

The checker does not consume the same fact. `Compilation.Inferred` handles one
supplied name explicitly:

```csharp
Node.Name name when name.Words == SymbolTable.Absent
    => new Sort.Optional(variables.Fresh()),
Node.Name name => sorts.GetValueOrDefault(name.Words),
```

(`Compiler/Compilation.cs:775-783`). `sorts` contains declared values and their
enclosing values, not supplied literals. Therefore `true` and `false` fall
through the second arm and produce null rather than `Sort.Scalar("truth")`.
Every checking path treats null as «not inferred yet» and remains silent.

The following three independent positions all compile with no findings:

```ronin
var x => number = true;
```

```ronin
function f => number { return false; }
```

```ronin
function f (x => number) => number { return x; }
var y => number = f true;
```

Each should report `TypeMismatch`: the value/return/argument is `truth`, not
`number`. This is a missing type at the common literal base case, not merely a
missing diagnostic in one construct.

Derive supplied value sorts from the descriptor's `Denotes` property, just as
`Truths` does, rather than adding a second hand-maintained truth list to the
checker. `nothing` may still require the special fresh-variable construction,
but the registry must remain the authority for which fixed ground sort each
other supplied literal denotes.

Maintain all three witnesses above, plus matching controls for
`var x => truth = true` and `false`, so initializer, return, and argument paths
cannot drift apart.

### 2. High — a delegate's returns are attributed to its enclosing function

`Compilation.Bodies` represents a delegate body as a transparent `Body` with no
`Function` owner (`Compiler/Compilation.cs:1197-1200`). When `Scope` recurses a
body whose `Container` is null, it passes the current `home` as `within`
(`:339-345`). The child then computes:

```csharp
var home = function ?? within;
```

(`:291-303`). For a delegate inside a function, `function` is null and `within`
is the enclosing `Grammar.Function`, so the delegate's checking context records
that enclosing function as its owner.

`Sites(function)` later gathers every return from every context whose `Owner`
is that same function (`Compiler/Compilation.cs:953-963`). The delegate's
returns consequently participate in the enclosing function's inference and
written-return checks.

This valid program is rejected:

```ronin
function f {
    var callback = () => { return "text"; };
    return 5;
}
```

Current behavior is one `DivergentReturns` at the delegate's `"text"`, saying
it disagrees with the function's `number`. The delegate and `f` are separate
callables; each has its own return type. The same contamination is possible from
a delegate in a parameter default, because ancillary delegate scopes receive
the same `within: home` propagation.

The type-container ruling does not justify this ownership. Delegates are
transparent for **named-type container identity** under H-wide; they are not
transparent control-flow blocks. A return from an `if`, loop, or ordinary block
belongs to the enclosing callable. A return from a delegate belongs to the
delegate.

Represent return ownership independently of `Grammar.Function`, so both
functions and delegates can be callable owners. Entering a delegate must replace
the current return owner while preserving the existing transparent type
container. Nested blocks inside that delegate then inherit the delegate owner.

Maintain the witness above and controls for:

- a delegate in a function parameter default;
- a delegate nested in another delegate;
- an `if` or loop inside a delegate, whose return still belongs to that delegate;
- an ordinary `if` or loop directly in a function, whose return continues to
  belong to the function.

### 3. High — omitted-return inference drops variable-bearing sites instead of unifying them

The ruled algorithm is explicit: all return sites unify, recursive sites
contribute information, and the result is required to be ground only after
solving (`RECURSIVERETURN` §§1-3, reinforced by `MONOMORPHANDRETURN` §1 and
`NOTHINGSPELLINGRULING` §3).

`Compilation.Infer` currently admits a return site only if its sort already has
a written rendering:

```csharp
if (Inferred(site.Answer, site.Sorts, site.Declared) is not { } actual
    || Sort.Render(actual) is not { } rendered)
    continue;

if (inferred is null) (inferred, first) = (actual, rendered);
else if (inferred.Equals(actual) is false) ...
```

(`Compiler/Compilation.cs:925-935`). `[]` is `List(Variable(fresh))` and
`nothing` is `Optional(Variable(fresh))`; neither renders until something pins
the inner variable. Both are therefore discarded before agreement is tested.
The remaining sites can establish and publish a return type as if the discarded
return did not exist.

Both invalid functions compile clean:

```ronin
function f { return []; return 5; }
```

```ronin
function f { return nothing; return 5; }
```

The first asks one function to return both `list of ?` and `number`; the second
both `optional ?` and `number`. Neither pair can unify. Current behavior skips
the first return and infers `number` from the second.

This is also the live version of the exact failure the recursion ruling warned
about. The checker now has `Sort.Unify` with variable binding
(`Compiler/Checking/Sort.cs:185-204`), but return inference still uses `Equals`
and gates on `Render` before reaching it.

Accumulate every non-null return sort with `Sort.Unify`, without requiring a
rendering first. Render only when constructing a diagnostic. After all sites
and recursive constraints have contributed, call `Sort.Ground`; publish only a
ground answer. A failed unification involving an under-determined shape may need
a diagnostic that can name the known outer kinds (`list`, `optional`) without
pretending the unknown inner type was determined.

Maintain both witnesses above, plus the positive pinning controls:

```ronin
function f (xs => list of number) { return []; return xs; }
function g (o => optional number) { return nothing; return o; }
```

Both should infer their respective ground aggregate sorts. Include a recursive
control in which an empty base is pinned by another return in the recursive
group; that is the case the ruling was written to preserve.

### 4. High — a no-answer body never receives the action sort

`Sort.Action` exists and its contract is correct: it is the inferred sort of a
no-return body and is inadmissible in value position
(`Compiler/Checking/Sort.cs:355-365`). The inference phase never constructs it.

`Infer` starts with `Sort inferred = null` and changes it only for a return site
whose answer has an inferred and renderable value sort
(`Compiler/Compilation.cs:921-935`). A bare `return` contributes a `Site` whose
`Answer` is null (`:1074-1088`); a body with no return contributes no sites.
Both leave `inferred` null. In the no-site case the method also declines to find
the owning pattern because that search is guarded by `sites.Count > 0`
(`:937-950`). No action sort is published to `answers`.

The observable result is a silent value-position admission:

```ronin
function f { return; }
var x => number = f;
```

Current behavior is no finding. `f` has no value to assign to `x`; under
`FIVERULINGS` §2b and `RETURNANDLITERALS` §1c its inferred sort is `Action`,
which is admitted in no value position.

Infer `Sort.Action` whenever a callable has no value-carrying return site,
whether it has a bare `return` or falls through. Find the owner independently of
site count. The value checker/type filter must then treat an action sort as an
inadmissible value, not as an unrenderable sort to leave silent. This likely
wants an action-specific finding or reading elimination rather than spelling a
surface type that the ruling deliberately keeps unspellable.

Maintain the witness above and controls for fall-through, explicit bare return,
and a delegate action once finding 2 gives delegates their own callable owner.
Also keep the mixed-exit checks: a body with any `return (value)` is answering,
so a bare `return` in it remains `MixedExits`, not evidence that the whole body
is an action.

### 5. Medium — nested return sites are compared in traversal order, reversing “earlier” and “later”

The phase restructuring correctly sorts completed findings by primary source
offset (`Compiler/Compilation.cs:1677-1688`). Return inference chooses its
established type before a finding exists, however, and its site sequence is not
in source order.

`Scope` records the current context before recursing into its child bodies
(`Compiler/Compilation.cs:297-303`, then `:328-442`). `Sites` iterates the
`checks` list in that order and concatenates each context's returns without
sorting them (`:959-963`). A direct return in the function body is therefore
seen before a lexically earlier return inside an `if` body, because the function
context precedes the nested-block context.

For:

```ronin
function f {
    if c { return "text"; }
    return 5;
}
```

current behavior constructs a `DivergentReturns` whose `Value` is `text` and
whose `Established` type is `number`. Its message says:

> This return is a `text`, and an earlier return is a `number`.

The `text` return is earlier in the source and the `number` return is later. The
finding also points at the earlier participant when the diagnostic's stated
model is to establish the earlier return and blame the later disagreement.
Sorting the completed finding collection cannot change those already-chosen
roles.

Sort a callable's `Site`s by `Answer.Offset` before inference and written-return
processing. This keeps the agreement operation semantically symmetric while
making the diagnostic's presentation roles truthful and deterministic across
scope nesting. Maintain the witness above with assertions for `Value`,
`Established`, and the primary span, plus an inverse placement control where the
direct return really is first.

---

## Disposition of `REAUDIT62`

| Prior finding | Reassessment |
|---|---|
| finding identity omits related sites and collapses sibling conflicts | **Closed.** `Compilation.Add` now compares kind, message, and the complete primary-plus-related participant set. Distinct sibling overload/duplicate sets survive because each contains a different local participant; repeated traversal of the same set is still removed by subset identity. The maintained sibling controls and the original `REAUDIT61` deduplication control pass. |

## What the implementation gets right

- The gather → infer → check split is real. Checks no longer depend on which
  function body happened to be visited first.
- The extracted iterative Tarjan implementation is shared by cascades and
  return inference, retains dependency-first component order, and avoids a
  recursive stack proportional to program depth.
- Non-recursive inferred-return chains drain in dependency order.
- Mutually recursive groups with a scalar or already-ground aggregate base reach
  a fixed point independent of member declaration order.
- A base-less self-recursion or mutually recursive group is refused as one
  semantic cycle condition rather than by a per-function self-call test.
- Returns in transparent control-flow blocks are gathered for the enclosing
  function. The remaining ownership defect is specifically callable bodies
  such as delegates.
- Declared initializers, written returns, and call arguments use one general
  disagreement path, and ground scalar/non-scalar sorts are named consistently.
- Empty-list elements can be pinned by a determined sibling in either order.
- Expected `list`/`optional` types pin `[]`/`nothing` through general unification.
- `nothing` is a nullary supplied value without contaminating `Truths`; its
  reservation remains whole-name-only, so names such as `nothing found` remain
  legal.
- Date literals now accept four-or-more-digit years, preserve the four-digit
  minimum, consume the longest year digit run, and leave range checking separate
  from token kind as ruled.
- Findings are stably presented by primary source offset after the phase split.
- The handoff ledger is complete and reproducible: 140/140 design documents
  headed, no Pass 1 or Pass 2 worklist entries, and generated `LEDGER.md` matches
  `ledger.py` exactly.

## Verification record

Temporary audit probes were removed from the worktree before this report was
written.

- Inspected the complete `ede64ce..15c94b3` production and maintained-test diff,
  using `503f444` as the direct `REAUDIT62` repair boundary.
- Read the generated ledger and the binding semantic-checker, inference,
  recursion, error-value, equality, and `nothing` rulings.
- Reproduced finding 1 through initializer, written-return, and argument
  positions; all three invalid truth-as-number sources produced zero findings.
- Reproduced finding 2 with a delegate returning `text` inside a function
  returning `number`; the valid source produced `DivergentReturns` at the
  delegate return. A nested delegate outside a function did not contaminate an
  outer function, confirming the inherited `Grammar.Function` owner is the
  mechanism.
- Reproduced finding 3 with both `return []; return 5;` and
  `return nothing; return 5;`; both produced zero findings.
- Reproduced finding 4 with an explicit bare-return action assigned to a number;
  it produced zero findings.
- Reproduced finding 5 with an earlier nested `text` return and later direct
  `number` return; the finding reported `text` as the later value and `number`
  as the earlier established type.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror`:
  **passed**, 0 warnings and 0 errors.
- Release coverage gate from `.github/workflows/build.yml`:
  **1,318 passed, 0 failed, 0 skipped**; `Ronin` and `Ronin.Server` each report
  **100% line, 100% branch, and 100% method coverage**.
- `python3 docs/handoff/ledger.py` output is byte-for-byte identical to
  `docs/handoff/LEDGER.md`.
- `git diff --check` and `git show --check HEAD`: **clean before this report**.

The only worktree addition made by the audit handoff is this report. No
production or maintained test file was changed.
