# Re-audit 25 — `FRESHAUDIT5` incorporation, findings 1–8

**Audited:** `27a23cf` through `42b76b1`

**Date:** 2026-08-03

## Result

**No sign-off.  One high-severity correctness finding, four medium findings,
one low-severity pessimization, and one regression-test gap remain.**

The central fixes are real.  Member writes now obey purity and firing rollback,
stage stable handles rather than dense indices, and cannot expose or replace a
whole member column.  Removal now lands at a round boundary and dirties cached
readers.  Member cells are qualified by type, sibling types can share a source
member name, square collections retain their identity through evaluation, and
operator words are rejected in patterns as well as names.

The incorporation nevertheless stops one structural effect short of its own
rule: `Remove` calls itself a write, but uses a graph-wide set rather than the
write transaction.  A pure `let` can therefore remove an instance, and a
failing `when` removes one while reporting that none of its writes applied.

Finding 8 is also not closed.  The collection alternatives were correctly
folded into one production for valid input, but the requested late-failure work
bound remains exponential.  The new maintained regression explicitly asserts
that exponential curve instead of the linear ratio required by the finding and
the designer's adjudication.

This re-audit deliberately excludes `FRESHAUDIT5` finding 9.  The user assigned
the authoritative-document alignment to a separate instance and requested a
separate audit for it.  Its uncommitted work was preserved and was not reviewed
as part of this report.

## Findings

### 1. Structural removal bypasses purity and the firing transaction

**Severity: high — a pure cell mutates the world, and a failed body commits an
effect its fault says was discarded**

The member-write repair introduces a firing-local `staging` table and merges it
only after `Consumed` succeeds (`Compiler/Runtime/Graph.cs:1150-1204`).  It also
rejects a member write while a `let` is being evaluated
(`Graph.cs:267-301`).

`Remove`, despite correctly describing itself as a write, does neither.  It
adds directly to the graph-wide `leaving` set (`Graph.cs:226-243`).  `Propagate`
later applies that set whether the caller was outside a body, a pure body, or a
firing that threw.

Two temporary probes reproduced both violations:

```text
let impure = { Remove(box); return 1 }

expected Read("impure"): Error (a let may not mutate)
actual Read("impure"):   1
after Step:               box is removed
```

and:

```text
when armed {
    Remove(box)
    throw InvalidOperationException("defect")
}

faults:                   1, saying none of its writes were applied
expected box.cash:        0
actual box.cash:          Error(stale handle)
```

This is the same failure shape as `FRESHAUDIT5` finding 1, now on the structural
write added while findings 2 and 3 were repaired.  `Create` has the same purity
seam by inspection: it mutates the population and member columns immediately
with no `reading` or firing-context check (`Graph.cs:208-215`).  The settled
"handle is immediate to its creator" rule does not require a failed body's
creation to commit or permit a `let` to create one.

**Recommendation:** give structural effects the same context as scalar and
member writes.  At minimum, reject removal/creation while `reading` is
non-empty; stage removals in a firing-local set and merge it into `leaving` only
after the body finishes.  Creation needs an equally explicit commit/rollback
story while still making its provisional handle usable inside the successful
body.  Pin pure, failed-body, and successful-body cases for both operations.

### 2. The late-failing collection parser is still exponential

**Severity: medium — bounded hostile input still consumes the million-group
budget, and the regression now pins the defect**

The new `Collection` production removes the `List`/`Lookup` ordered alternative
and valid nested collections now parse once.  That is a genuine improvement.
It does not close finding 8's adversarial shape: a nested collection whose body
fails late is still parsed through `Value.Parse`'s reference/temporary paths
exponentially.

The maintained test states the result exactly
(`Test/Integration/StatementShapes.cs:539-585`):

```text
depth 10: 2046 group attempts = 2^11 - 2
depth 20: more than 100 times depth 10
```

It then asserts `2046` and asserts that the larger case remains more than one
hundred times worse.  This is the inverse of the designer-requested regression
(`Work(20) / Work(10) < 3`) and of `FRESHAUDIT5`'s recommendation that
late-failing square nests grow linearly.  The test's own comment says the
assertion it is named for is not made.

This is therefore not merely a missing optimization discovered adjacent to the
fix; it is the original finding, explicitly measured as still present.

**Recommendation:** continue the single-parse boundary through the
reference/temporary decision so a failed nested value is not rebuilt at every
level.  Replace the exact exponential assertions with the ratio bound from
`AGGREGATEPARSE.md`.  Retain `MaxGroups` as a defensive ceiling, not as the
normal stopping condition for this known curve.

### 3. A missing lookup value is misreported as a mixed collection

**Severity: medium — the new diagnostic hides the actual syntax error and
recommends the wrong repair**

An association whose right-hand value is absent becomes
`Collection.Element.ExpectedValueError`.  That error has `Origin == null`
(`Compiler/Grammar/Collection.cs:68-96`).  The collection-kind decision then
counts every null origin as an ordinary list entry (`Collection.cs:53-57`).

Consequently:

```ronin
var lookup = [a =, b = 2];
```

does not report `expected value`.  A full `Compilation.Of` probe produced:

```text
entry 1 is a value and entry 2 is an association, so this is neither a list nor a lookup
```

Entry 1 is not a value; it is a malformed association.  Constructing the
top-level `Mismatched` error also discards the element error, so the reflective
diagnostic walk has no remaining path to the more specific finding.

**Recommendation:** do not run kind classification over erroneous elements.
Return the parsed collection with its element errors intact, or carry an
explicit element kind that distinguishes "association with missing value" from
"list value".  Regress missing-value plus valid-association in both positions,
and a missing value beside a genuine list value, so syntax errors cannot be
masked by the mixed-kind diagnostic.

### 4. Parser-valid trailing separators are rejected by the resolver

**Severity: medium — a language-valid list reaches `NoParse` on the maintained
source-to-value route**

`Aggregate` deliberately permits a trailing separator, and the guide's examples
rely on that rule.  A compilation probe confirmed that this source has no
findings:

```ronin
var list = [10, ];
```

`Resolver.Group`, however, treats every separator as having an expression on
both sides.  For the trailing comma it asks for the empty span after the last
separator and returns without offering the group
(`Compiler/Resolution/Resolver.cs:244-283`).

The lexer/resolver/evaluator probe used by the new indexing tests therefore
does this:

```text
[10, ] @ 1
expected: 10
actual resolution: NoParse
```

The maintained cardinality tests cover empty, singleton, multiple, and nested
lists, but none uses the grammar-permitted trailing form.

**Recommendation:** have `Resolver.Group` recognise a final top-level separator
as trailing rather than as the start of an empty part.  Preserve rejection of
leading and doubled separators.  Add the case to the full written-list indexing
table, not only a hand-built `Node.Group` test.

### 5. Type declaration can still fail after publishing earlier members

**Severity: medium at the runtime API boundary; not source-spellable today —
the atomicity claim is still structurally false**

The new preflight checks a duplicate type and duplicates within the incoming
member array (`Compiler/Runtime/Graph.cs:152-176`).  It does not ask the same
`Unique` question that each later `Declare` asks against existing nodes and
constants.  A late qualified collision can therefore still throw after earlier
members have been published:

```text
Var("Broken.occupied", 0)
Type("Broken", ("leaked", 0), ("occupied", 0)) -> InitialisationFailure

expected Declared: 1
actual Declared:   2       // Broken.leaked remains
```

The dot cannot occur in a source name, so this witness is an internal/runtime
API case rather than current Ronin source.  That narrows its reach but does not
make the method atomic: `Graph` accepts the name, `InitialisationFailure` is a
handled failure, and the graph is left in a state no successful declaration
can produce.  It also exposes that `Cell` decides membership with
`nodes.ContainsKey(Qualified(...))` rather than the population's declared
members (`Graph.cs:304-306`); the string is being used as both identity and
membership proof.

**Recommendation:** preflight every qualified member through the full
node/constant uniqueness check before the first mutation.  Prefer checking
member ownership from `Population.Members` rather than the global node table.
If qualified strings are intentionally valid only for trusted lowering, make
that invariant unforgeable or validate it at every public graph declaration
entry point; relying on source spelling inside a lower-level API leaves the
handled partial-failure path open.

### 6. List-valued cutoff compares array identity, not list content

**Severity: low — unchanged list results wake their whole downstream graph**

Square collections now evaluate to a fresh `object[]`
(`Compiler/Runtime/Evaluator.cs:97-112`).  `Graph.Recompute` uses ordinary
`object.Equals` to decide whether the value changed (`Graph.cs:1404-1409`).
.NET arrays use reference equality there, despite the adjacent comment claiming
that array equality is O(n) (`Graph.cs:1425-1429`).

A probe made a list-valued `let` depend on `tick` while always returning
`[1]`, and counted a downstream reader.  Changing `tick` caused the list to
recompute to the same content; the downstream reader nevertheless ran again:

```text
expected downstream evaluations: 1
actual downstream evaluations:   2
```

This is a pessimization rather than a wrong value today, but it defeats the
cutoff optimization on every list literal that is reconstructed after an
unrelated dependency changes.  The new collection runtime value makes the
comment's future case current.

**Recommendation:** settle list value equality before joining more
collection-valued cells.  If language equality is structural, use a structural
comparison or a stable digest/version; if full comparison is deliberately too
expensive, represent immutable lists with stable identity or explicitly exempt
them from cutoff and remove the false O(n) claim.  Pin downstream evaluation
count, not merely the returned elements.

## Regression-test gap: stale-error equality is not tested by the named test

The implementation of `Error.Equals` is currently sound: it compares exact
failure kind and message (`Compiler/Runtime/Values.cs:21-55`).  No current code
finding was reproduced there.

The new test named `AndACellThatStaysStaleStopsWakingItsReaders`, however, does
not make a stale cell recompute twice (`Test/Unit/Instances.cs:287-319`).  It
removes the instance, reads the derived cell once, and then performs two steps
with no change that dirties that cell.  The evaluation count stays fixed even
under reference equality because no second evaluation is attempted.

Add another dependency to the stale reader, change it after the first stale
result, and observe that a downstream reader does not wake when the regenerated
error has the same kind/message.  That pins the cutoff property the test name
currently claims rather than only the absence of unrelated dirtiness.

## Status of `FRESHAUDIT5` findings 1–8

| prior item | result |
|---|---|
| 1. member writes bypass effect context | **Fixed for member writes.** Structural removal/creation still bypass the same context; finding 1 above. |
| 2. staged member write loses instance identity | **Fixed.** Writes retain `Instance` until propagation and both compaction mirrors are covered. |
| 3. removal leaves cached readers live | **Fixed.** Removal is buffered, advances member nodes, and dirties dependents. Its transaction placement remains wrong; finding 1. |
| 4. sibling types cannot share member names | **Fixed on the source path.** Type-qualified cells represent both sibling values independently. |
| 5. failed type declaration is partial | **Partial.** Duplicate incoming members are preflighted; a late existing qualified-node/constant collision still leaks an earlier member; finding 5. |
| 6. square collection identity is lost | **Core fix is real.** Empty/singleton/multiple/nested lists retain collection identity. A valid trailing form still fails at the resolver join; finding 4. |
| 7. operator words remain legal in patterns | **Fixed.** The rule is derived from the operator table, produces `InfixInPattern`, and excludes the invalid pattern from glue reservation. |
| 8. list/lookup parser remains exponential | **Not closed.** Valid-input duplication is fixed; late-failing nesting remains exponential and is asserted as such; finding 2. |

## What was rechecked without another finding

- Stable-handle writes apply before same-round removal and are then discarded
  with the target; neither compaction mirror retargets or indexes past the end.
- Removal is observable at the next round, not immediately, and derived readers
  become stale with the direct reader.
- `Error` equality distinguishes `Error` from `Fault` and distinguishes changed
  messages; the implementation concern raised during adjudication is resolved.
- Scalar member access is refused in both directions, so the backing list cannot
  be acquired or replaced through the scalar overloads.
- Empty, singleton, multiple, and nested square collections evaluate as lists;
  parenthesised singleton grouping continues to collapse.
- Mixed valid value/association collections produce the same directed diagnostic
  in both orders.  The masking defect is confined to erroneous elements.
- `InfixInPattern` is included in finding rendering and the golden output, and
  the structural filter prevents the invalid pattern from reserving glue words.

## Verification

- Six temporary adversarial probes reproduced the findings: pure removal,
  failed-body removal, late type collision, trailing-separator resolution,
  missing association value beside a valid association, and unchanged-list
  cutoff.  The temporary file was removed before repository gates.
- Locked restore: passed.
- Exact Release solution build with `-warnaserror`: zero warnings and zero
  errors.
- Exact Release test/coverage gate: **950 passed**, zero failed, zero skipped,
  with **100% line, branch, and method coverage**.
- `git diff --check 27a23cf..42b76b1`: clean.

The gate count includes the separate instance's uncommitted documentation-
alignment tests because that worktree was already shared.  None touches the
runtime, collection, resolver, or rule behavior audited here; its contents are
not signed off by this report.

The documented hand-aligned `dotnet format` whitespace differences remain
settled project style and are not a finding.  Formatting was not used as a gate.
