# Re-audit 47 — canonical lookup order and the limits of display ordering

**Re-audited:** `a5341b8..80accb4`, the commit incorporating
`FRESHAUDIT20` findings 1, 2, 3, 5, and 6.

**Date:** 2026-08-11

## Result

**No sign-off. Two high-severity runtime findings and three medium findings
remain.**

The incorporation closes the exact maintained cases for four findings. Lookups
whose keys have a defined content order are canonicalised at admission, and the
graph-level reversed-string-key reproduction no longer leaves a dependent stale.
Equality carries one memo through lists and lookups, so two independently
admitted lookup DAGs compare linearly. `@` now finds scalar and structural keys
using `Builtin.Same`. An ordinary `Error` is refused as a key while remaining
legal as a value, and the `Node.Group` constructor enforces the kind/key
invariant.

The canonical-order implementation does not yet satisfy the relation it needs,
however. Its catch-all orders arbitrary host keys by `ToString()`. Display text
is neither an identity nor a total order: unequal keys may print alike, and equal
keys may print differently. That makes equal maps compare unequal and, more
seriously, lets duplicate equal keys evade the adjacent-only duplicate check.
The same comparer recursively unfolds shared aggregate DAGs without the memo
that fixed equality, moving the prior exponential from comparison into
construction.

The ordinary-error refusal also catches `Fault` and converts it through the
private refusal sentinel into an ordinary, catchable `Error`, hiding an
interpreter defect. The type-layer portion of Section E remains explicitly
unimplemented. Finally, lookup misses now return `Error`, coherently following
EAGGREGATES2 §8, but the conflicting `docs/spec/NOTHINGANDINDEXING.md` statement
was not reconciled and still says `nothing`.

All maintained gates pass: 1,221 tests in Debug and Release, locked restore,
warning-as-error Release build, 100% line and branch coverage, and the direct
plus transitive vulnerability audit.

---

## Disposition of FRESHAUDIT20

| Prior finding | Re-evaluation |
|---|---|
| 1. Equal lookups expose different iteration orders | **Closed for keys whose comparer is a real total order; not closed for admitted host/compound keys.** The maintained graph reproduction passes, but two equal host-key maps written in reverse order canonicalise differently and compare unequal. Finding 1 below. |
| 2. Lookup equality is exponential on shared DAGs | **Equality is closed; canonical ordering reintroduces the same exponential during construction.** The new equality test is strong. `Lookup.Compare` has no corresponding memo. Finding 2 below. |
| 3. Lookup indexing is absent | **Functional path closed.** Scalar and structural hits use `Builtin.Same`, misses return a value, and invalid operands remain errors. The miss result still has two contradictory design owners; see finding 5. |
| 4. Typing and expected-type empty lookup are absent | **Open and explicitly outside the incorporating commit.** No type-layer code changed. Finding 4 below. |
| 5. Errors are admitted as keys | **Ordinary Error closed; Fault mishandled.** A `Fault` key becomes a new ordinary `Error`, which `otherwise` catches. Finding 3 below. |
| 6. Group kind and nullable keys can disagree | **Closed.** The constructor copies then validates every entry, and maintained tests cover keyed list/group entries and an unkeyed lookup entry. |

---

## Findings

### 1. The canonical comparer is not compatible with key equality

**Severity: high — equal maps can compare unequal, and a lookup can again contain
two equal keys with two different answers.**

Canonical sequence equality requires this contract:

```text
Compare(a, b) == 0  if and only if  Builtin.Same(a, b)
```

Together with antisymmetry and transitivity, that is what makes sorting produce
one unique sequence for one unordered map and what makes equal keys adjacent.
`Lookup.Compare` does not establish it.

For every unrecognised host value, `Kind` returns 8 and the comparison falls back
to ordinal comparison of `ToString()` (`Compiler/Runtime/Lookup.cs:72-92,
144-156`). Two consequences were reproduced:

1. Two unequal host keys both printed `same`. Lookups containing the same two
   associations in opposite written orders remained in opposite orders after
   sorting, and `Builtin.Same` returned **false**.
2. Two equal host keys printed `a` and `c`, with an unequal key printing `b`
   between them. Sorting placed the equal keys on opposite sides of `b`.
   The adjacent-only duplicate scan at `Compiler/Runtime/List.cs:258-265` missed
   them and returned a `Lookup`, not an `Error`. `@` consequently has two matching
   associations and silently returns the first.

The problem is not limited to foreign classes. Errors compare by message only
at `Lookup.cs:84`, while runtime equality compares both exact error type and
message (`Compiler/Runtime/Values.cs:46-47`). Direct errors are refused as keys,
but errors remain legal inside list and lookup values, and those aggregates are
legal compound keys. The probe compared `[Error("same")]` with
`[Fault("same")]`: `Builtin.Same` correctly returned false while
`Lookup.Compare` returned zero.

The disclosed null rule is not implemented as stated either. `null` enters the
catch-all kind 8, after `Nothing`, booleans, numbers, strings, instances, errors,
lists, and lookups; it therefore does not sort before everything. Within the
catch-all it also compares equal to any host object whose `ToString()` returns
null. More importantly, neither admitting CLR null as a language key nor
admitting arbitrary mutable host objects has a design owner yet.

The maintained “total order” test uses one friendly value per kind and one
`DateTime` pair whose renderings differ. It shows those examples can be sorted;
it does not test the comparator/equality law on which canonicalisation and the
duplicate scan depend.

**Recommendation:** define and test a strict total order modulo `Builtin.Same`.
For every admitted key pair, comparer zero must be exactly language equality.
There is no generic way to derive that order for an arbitrary host object's
custom `Equals` from its display text. Refuse unknown host values and CLR null as
keys until the designer defines a key protocol or a canonical content identity.
For known runtime types, include every equality component in ordering (including
the exact Error kind when it occurs inside a compound value). Add algebraic
tests plus the reversed-equal-map and separated-duplicate witnesses above.

### 2. Canonical comparison unfolds a shared aggregate DAG exponentially

**Severity: high — lookup construction can freeze before it either admits or
refuses an otherwise depth-valid compound key.**

`Builtin.Same` now correctly carries a proved-pair set through both aggregate
kinds (`Compiler/Runtime/Values.cs:335-430`). `Lookup.Compare` independently
recurses through lists and lookups with no state (`Compiler/Runtime/Lookup.cs:
106-142`). `List.Admit` invokes that comparer from `Array.Sort` before checking
duplicates (`Compiler/Runtime/List.cs:252-265`).

The audit built two independently admitted depth-12 lookup DAGs. At every level,
both entries referenced the same child. It then used those equal DAGs as the two
keys of an outer raw lookup, crossing the real construction path. Admission
eventually refused the duplicate as it should, but canonical sorting rendered
the one leaf **8,192 times** first. Work doubles with each layer; the admitted
depth ceiling is 256, so the ceiling does not make this remotely bounded in
practice.

The maintained depth-20 DAG test calls only `Builtin.Same`, where the new memo is
present, and therefore cannot detect that the sort immediately beside it repeats
the old traversal.

**Recommendation:** give canonical comparison its own context shared across list
and lookup recursion. Cache the integer result for each aggregate pair, not only
“already equal,” because an ordering comparison may prove a non-zero result that
another path reaches again. Maintain an admission-level compound-key DAG probe;
testing equality alone guards a different caller.

### 3. Refusing a Fault key launders the defect into a catchable Error

**Severity: medium — `otherwise` can hide an interpreter defect after that defect
is used as a lookup key.**

`Fault` derives from `Error`, but its defining invariant is that it is not caught
or converted into a program value (`Compiler/Runtime/Values.cs:59-75,
446-455`). The new key check matches every `Error`, including `Fault`, and returns
the private `Refusal` sentinel (`Compiler/Runtime/List.cs:235-242`). The public
admission boundary converts every `Refusal` into a new ordinary `Error`
(`List.cs:104-106`).

The probe admitted a pair keyed by `new Fault("interpreter defect")`. Actual:
an exact `Error`, not a `Fault`; `Builtin.Otherwise(result, 9)` then returned 9.
The defect was therefore made catchable, the behavior the `Fault` type exists to
prevent. The maintained error-key test covers only ordinary `Error`.

**Recommendation:** propagate a `Fault` unchanged before applying the ordinary
error-key refusal. Maintain both assertions: an ordinary `Error` key becomes the
specific key refusal, while a `Fault` remains the same uncaught fault through
admission and `otherwise`.

### 4. Section E's type-layer requirements remain open

**Severity: medium completion blocker — unchanged from FRESHAUDIT20 finding 4.**

The incorporating commit expressly names findings 1, 2, 3, 5, and 6. It adds no
type resolver or inference layer. `Compilation` still states that the type table
does not exist (`Compiler/Compilation.cs:165-175`), `[]` still resolves
unconditionally as `Node.Grouping.List`, and no expected type can select
`Lookup.Empty`. Section E §7's list-element and lookup-key/value unification is
likewise absent.

**Recommendation:** keep this explicitly out of any “Section E complete” claim,
or implement the type layer and the source-level tests listed in FRESHAUDIT20.
The runtime singleton remains useful groundwork, not expected-type behavior.

### 5. The implemented miss result still contradicts the current spec document

**Severity: medium specification consistency — the code is coherent with
EAGGREGATES2, but the language still has two authoritative answers.**

`Builtin.Found` returns an `Error` on a miss (`Compiler/Runtime/Values.cs:
266-294`), and the maintained test pins that result. This follows
`EAGGREGATES2.md:292-311`: returning `nothing` cannot distinguish absence from a
present optional value that is itself `nothing`.

`docs/spec/NOTHINGANDINDEXING.md:36-48` still says a lookup miss yields
`nothing`. The programmer explicitly disclosed choosing the E result pending a
designer ruling, so this is not an overlooked branch. It is also not reconciled:
source, test, handoff, and spec do not yet state one language.

**Recommendation:** obtain the designer ruling and update the losing document
and maintained test in the same change. On the current reasoning, `Error` is the
only result preserving absent versus present-and-nothing, but the choice belongs
to the language owner.

---

## Safeguards that now hold

- The maintained graph test crosses actual cutoff and proves reversed string-key
  literals canonicalise to the same visible order.
- The maintained independently admitted depth-20 DAG test proves equality now
  shares a memo; the old million leaf comparisons collapse to one.
- Lookup indexing covers scalar hits, structurally equal list and lookup keys,
  an Error miss, continued list indexing, and invalid left operands.
- Ordinary Errors are refused as keys and remain legal values.
- The group constructor copies and validates its entries, and all three invalid
  key/kind combinations from FRESHAUDIT20 are maintained.

## Adversarial verification

Temporary probes were isolated in a detached worktree and removed after the
audit.

| Probe | Result |
|---|---|
| Equal host-key maps written in opposite orders | **Failed:** `Builtin.Same` returned false |
| Equal host keys separated by an unequal display key | **Failed:** duplicate map was admitted |
| Depth-12 shared lookup DAGs used as outer keys | **Failed:** 8,192 leaf renderings during admission |
| `[Error("same")]` versus `[Fault("same")]` as compound keys | **Failed:** unequal values compared at order zero |
| Fault used directly as a key | **Failed:** returned ordinary `Error`, then `otherwise` caught it |
| Null versus a host value rendering null | **Failed:** comparator returned zero rather than ordering null first |

## Verification record

- Inspected the complete incorporating commit and adjacent admission, ordering,
  equality, indexing, graph, error, and resolver paths.
- Focused aggregate, lookup-value, indexing, and statement-shape suites:
  **191 passed**.
- `dotnet restore --locked-mode`: passed.
- `dotnet build --no-restore --configuration Release -warnaserror`: passed with
  zero warnings and zero errors.
- Full Debug suite: **1,221 passed, 0 failed, 0 skipped**.
- Full Release suite: **1,221 passed, 0 failed, 0 skipped**.
- Exact Release coverage gate: passed; Cobertura reports **3,769/3,769 lines**
  and **2,601/2,601 branches** (100% each).
- `dotnet list Ronin.sln package --vulnerable --include-transitive`: no known
  vulnerable direct or transitive packages in the configured source.
- `git diff --check`: passed.
- `git diff -- Compiler Test`: empty after probe isolation; no production or
  maintained test file was changed by this audit.

The pre-existing `docs/spec` edits and untracked handoff/design files were
preserved. This report is the only repository artifact added by the re-audit.
