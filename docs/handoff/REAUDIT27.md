# Re-audit 27 — `REAUDIT26` incorporation

**Audited:** `e578678` through `43d2ca9`

**Date:** 2026-08-03

## Result

**No sign-off. Two high-severity runtime findings, three medium findings, and
one low test defect remain.**

The original creation rollback is now eager and survived a stronger mixed
transaction probe. `changes`, shadow advancement, pending writes, member writes,
and recompute cutoff now call the same equality. Nested collection diagnostics
preserve the original error on all maintained shapes. Member ownership is one
dictionary lookup rather than a field scan.

The sealed list representation is also a real improvement: `var`, scalar and
member writes, evaluator-created groups, and graph-body results now become
immutable values; the top-level and nested caller arrays used by those paths no
longer escape; and cycles are detected on those paths.

It is not yet one invariant, however. Type-member seeds and declaration results
are two other value ingress paths and neither is normalised. The depth guard
also makes two accepted equal lists compare unequal, while the recursive
normaliser itself remains capable of overflowing on sufficiently deep host
input. There are three smaller regressions around the new representation and
diagnostic walk as detailed below.

The separate authoritative-document alignment remains outside this audit. Its
uncommitted files were preserved. The settled hand-aligned formatter output is
also not a finding.

## Findings

### 1. List normalisation still misses two value ingress paths

**Severity: high — one path restores silent mutation behind the graph, and the
other makes a declaration-produced list fail when composed with a list operator**

#### 1a. Type-member seeds remain the caller's mutable arrays

`Graph.Type` stores each seed verbatim (`Compiler/Runtime/Graph.cs:202-210`),
and `Create` appends that same object into every new instance's member column
(`Graph.cs:239-246`). This bypasses `List.Of`, unlike `Var`, `Write`, member
`Write`, and `Recompute`.

A focused probe declared:

```csharp
var seed = new object[] { 1d };
graph.Type("Box", ("items", seed));
var box = graph.Create("Box");
```

`graph.Read("items", box)` was still an `object[]`, not a runtime `List`.
The caller retains the exact seed reference; mutating it changes the direct
member read without advancing the member cell or dirtying a derived reader.
Multiple instances also share that mutable seed object. This is the original
confidently-wrong-cache defect through the one graph ingress the sweep missed.

It also makes a list seed unusable by `@`: indexing now correctly accepts only
the immutable runtime type, while this path still supplies the removed array
representation.

#### 1b. A declaration result reaches its surrounding expression before graph normalisation

`Scope.Invoke` returns `declaration.Body(graph, bound)` directly
(`Compiler/Runtime/Scope.cs:111-159`). `Graph.Recompute` normalises only after
the *whole* graph body returns (`Graph.cs:1486-1495`), which is too late for an
operator around the call.

The probe used a declaration returning `new object[] { 7d }` and indexed its
result before returning from the graph body:

```text
expected: 7
actual:   error(«@» indexes a list)
```

This worked with the old representation because `@` accepted `object[]`.
Returning arrays is the host-facing construction route for immutable lists, so
the new representation cannot require a caller to construct the sealed internal
type instead.

**Recommendation:** define one value-normalisation boundary and route *every*
runtime ingress through it. At minimum, normalise all type seeds before any type
state is published, and normalise a declaration's result before `Scope.Invoke`
returns it to its surrounding expression. Audit direct invocation arguments at
the same boundary rather than adding a third local rule later. Regress mutation
plus a cached member reader, two instances sharing a list seed, a cyclic seed,
and a declaration-produced list composed with `@`.

### 2. The depth cap makes accepted equal lists unequal and does not cap normalisation

**Severity: high at the runtime/API boundary — equality is not an equivalence
for values the runtime accepts, and deeper input can still terminate the process**

`List.Of` accepts a 300-level acyclic host array and returns a runtime `List`.
`Builtin.Same` returns `false` for two independently built copies because it
returns false once `depth > 256` (`Compiler/Runtime/Values.cs:266-290`). The
maintained test explicitly pins that wrong answer
(`Test/Unit/Indexing.cs:293-310`).

Focused result:

```text
left runtime type:   List
right runtime type:  List
same contents:       yes
Builtin.Same:        false
```

That is observable today through cutoff, `changes`, and `old`: an equal deep
value advances clocks and may fire an effect. It will also be a wrong answer for
the settled `is` semantics when that operator joins the pipeline. A depth guard
that returns false does not turn "too deep" into a report; it turns it into
"different".

The guard is also on the wrong side of the remaining fatal path. `List.Of`
recurses once per nested host array (`Compiler/Runtime/List.cs:69-96`) and has no
depth limit. A sufficiently deep but acyclic host value can therefore reach
`StackOverflowException` while being normalised, before `Same`'s cap can help.

**Recommendation:** do not admit a value that equality cannot compare honestly.
Either normalise and compare iteratively, or give the immutable list a checked
depth and refuse over-limit construction as an `Error` before a `List` exists.
If existing `List` children may be wrapped in later calls, depth must travel with
the value; a per-call array counter can be bypassed one layer at a time. Keep a
defensive equality guard if desired, but it needs a non-boolean "too deep" path
rather than silently returning `false`.

### 3. The cycle sentinel collapses every ordinary error element

**Severity: medium correctness / design join — the representation change also
changed list error semantics, through an implementation test that cannot tell a
cycle report from a value**

`List.Of` recursively copies an element and then returns it as the result of the
whole list whenever it is any `Error` (`Compiler/Runtime/List.cs:87-92`). The
local is named `cyclic`, but an ordinary division error and a `Fault` satisfy the
same test.

The source-level probe was:

```ronin
[ 1 / 0, 2 ] @ 2
```

It returned the division-by-zero error instead of `2`. Before `43d2ca9`, the
error remained the first list element and indexing the second element returned
the second value. The authoritative definition calls a list an aggregate of
values, and the runtime models an `Error` as a value; no settled document in
scope says list construction is newly lifted over every element.

Even if the designer decides that constructing a list must propagate an element
error, cycle detection should not obtain that semantic accidentally: the same
normaliser is also used for host arrays, and a pre-existing error can hide a
cycle later in the value.

**Recommendation:** make cycle detection return an internal success/cycle
result distinct from the language's `Error` values. Preserve ordinary error
elements unless the designer explicitly settles lifted aggregate construction;
if that is the decision, state and test it independently for source lists, host
arrays, ordinary errors, faults, and a cycle beside an error.

### 4. Recursive diagnostic preservation reintroduces super-linear collection parsing

**Severity: medium pessimization — the diagnostic is now correct, but every
nested collection reflectively rescans the subtree below it**

`Compilation.Errors` justifies reflection because it runs once per file
(`Compiler/Compilation.cs:247-287`). `Compilation.Broken` duplicates the same
walk (`Compilation.cs:290-322`), and `Collection.Parse` now calls it every time a
collection finishes parsing (`Compiler/Grammar/Collection.cs:46-62`).

For a chain of `d` nested lists, the innermost parse walks one level, its parent
walks two, and so on: `1 + 2 + ... + d`, or O(d²), with reflection and a fresh
`HashSet`/`Stack` on every pass. The parser had only just regained the documented
linear collection curve.

On this machine, 80 parses of valid nested list declarations measured after
warm-up:

```text
depth 24:   23.7 ms
depth 48:   64.2 ms
depth 96:  181.8 ms
```

The nesting ceiling bounds the absolute damage but does not make the production
linear or make the repeated diagnostic work free.

**Recommendation:** carry "contains an error" bottom-up as a compositional
property, as `REAUDIT26` recommended, or memoise the reflective result for the
completed syntax nodes during the parse. Add a ratio/work-count regression for
valid and erroneous nested square collections; the existing exponential probe
does not cover this later walk.

### 5. The ownership dictionary adds a new post-mutation failure to `Type`

**Severity: medium at the runtime/API boundary — a rejected declaration leaves
a member node behind while the type itself is absent**

The new O(1) ownership table is correct for valid source names. Its insertion is
not preflighted. A null member reaches `Declare`, is appended to `Members`, and
then throws from `population.Owns[member] = cell`
(`Compiler/Runtime/Graph.cs:183-210`).

Probe:

```csharp
Graph graph = new();
graph.Type("Box", (null, 0d));
```

The call threw `ArgumentNullException`, but `graph.Declared` was `1` and no
`Box` population existed. This contradicts the method's explicit "every
question before anything is written" invariant. It is source-unspellable today,
but that same comment deliberately includes runtime-API inputs for exactly this
reason.

**Recommendation:** validate the type and every member name, and prepare the
ownership entries plus normalised seeds, before declaring the first node. Add a
failure-atomicity assertion on `Declared` and then prove a valid declaration
using the same type name still has no leaked state to collide with.

### 6. The cutoff regression now passes through an `InvalidCastException` fault

**Severity: low test defect — the production repair is real, but a named
regression silently asserts a bug in the new representation**

`AListThatRecomputesToTheSameListWakesNobody` still casts the list read to
`object[]` (`Test/Unit/Indexing.cs:152-189`). Runtime lists are now sealed
`Ronin.Runtime.List` values, so the downstream body returns a caught
`Fault(InvalidCastException...)` on its first evaluation.

The evaluation-count assertion still happens to discriminate the current cutoff
path, and a corrected probe using the runtime list succeeded without another
production finding. The maintained test nevertheless no longer exercises the
element read its name and comments describe, and it never asserts that the
downstream value is not a fault. This is the same class of test fixture that
previous rounds found asserting removed behaviour.

**Recommendation:** consume the read-only list representation (or use `@`) and
assert the returned element/no-fault result before taking the settled evaluation
count. Keep the count assertion; it remains the part that proves cutoff.

## Status of `REAUDIT26`

| prior item | result |
|---|---|
| 1. failed-body creation rollback is lazy | **Fixed.** `Release` is eager. A stronger probe combined two creations, two member writes, removal of a created instance, removal of a pre-existing instance, and a thrown body; both created handles were stale and the pre-existing instance survived. |
| 2. `changes`/`old` use reference equality | **Fixed in production.** `changes` and the shadow use `Builtin.Same`; the effect-count regression discriminates `changes`. The stated `old` path remains currently unobservable because every legitimate source transition already cuts equal lists off before the shadow sees a different reference. |
| 3. lists expose mutable arrays | **Partial.** The sealed type and the normalised paths enforce immutability. Type seeds and declaration results bypass it; accepted depth and error-element handling are inconsistent; findings 1–3. |
| 4. nested element error is reclassified | **Functionally fixed.** All maintained direct/destination/origin cases preserve `expected value`. The implementation rescans nested trees super-linearly; finding 4. |
| 5. member ownership is a linear scan | **Fixed for valid names.** `Cell` performs one dictionary lookup and returns the stored qualified identity. The new dictionary insertion opens a failure-atomicity hole for invalid API names; finding 5. |

## What was rechecked without another finding

- Failed-body rollback is eager and reverse ordered, including staged member
  writes and removals in the same failed firing.
- Equal ordinary lists do not advance recompute cutoff and do not fire a
  `TriggerMode.Changes` body.
- The shadow and trigger implementations now visibly share `Builtin.Same`.
- `Var`, scalar writes, member writes, evaluator groups, and graph-body results
  freeze top-level and nested caller arrays.
- Self- and mutually cyclic arrays on the covered ingress paths become errors
  rather than reaching equality.
- Empty, singleton, multiple, nested, and trailing-comma source lists still
  retain list identity and index correctly.
- Nested collection errors under destinations and origins retain their original
  `expected value` diagnostic; a genuinely mixed collection still gets the
  mixed-kind finding.
- Valid member reads and writes use the O(1) ownership table and preserve stable
  handle behaviour.

## Verification

Temporary probes were removed before the maintained gates.

```text
dotnet restore --locked-mode
  passed

dotnet build --no-restore --configuration Release -warnaserror
  passed — 0 warnings, 0 errors

dotnet test --no-build --configuration Release
  /p:CollectCoverage=true
  /p:CoverletOutputFormat=cobertura
  /p:Threshold=100
  /p:ThresholdType=line%2Cbranch
  /p:ThresholdStat=total
  passed — 976 tests, 100% line, 100% branch, 100% method
```

