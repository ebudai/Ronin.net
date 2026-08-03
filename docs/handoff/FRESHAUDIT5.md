# Fresh audit 5 — instances, indexing, bracket migration, and scope rules

**Audited:** `8798edc` through `27a23cf`

**Date:** 2026-08-03

## Result

**Six high-severity correctness findings, two medium findings, and one
documentation finding.  No sign-off.**

The largest risks are in the new instance subsystem.  Grouped storage preserves
the intended node-count invariant, but member writes are outside both purity
enforcement and the per-body transaction; staged writes are keyed by the dense
index that removal deliberately invalidates; removal does not invalidate cached
readers; and global bare member keys disagree with the frontend's type scopes.

The bracket migration has two independent integration gaps.  The parser chooses
`Lookup` for `[]`, despite the settled empty-list default, and the
resolver/evaluator erases the distinction between a square-bracket list and an
ordinary group.  Consequently `[10] @ 1` reports that its left operand is not a
list.  The maintained indexing tests do not see this because they inject an
`object[]` directly.

The new `otherwise` reservation correctly rejects names containing the word,
but it does not reject patterns containing it.  A source-declared pattern
`x otherwise (_)` therefore ties the built-in operator on `x otherwise y`.

The one-parse/one-decision aggregate parser remains outstanding.  This one is
not a new discovery: `BRACENEST-MEASURED.md` already records it.  It remains in
this report because it is a real current pessimization and the authoritative
spec says it has been completed.

## Findings

### 1. Member writes bypass both purity and the firing transaction

**Severity: high — pure cells can mutate state, and a failed effect body applies
writes it says were discarded**

Scalar assignment enforces all three boundaries in one path: it rejects
derived/shadow nodes, rejects a write while a `let` is being read, and writes to
the firing-local `staged` dictionary when a `when` body is running
(`Compiler/Runtime/Graph.cs:745-761`).

The new member overload does none of those things.  It writes directly to the
graph-wide `arriving` table (`Graph.cs:227-245`).  `Fire` publishes only the
scalar `staged` table on success and discards it on failure
(`Graph.cs:1047-1093`); it neither owns nor clears member writes.

Two focused probes reproduced both violations:

```text
let bad = { write Box.cash = 7; return 1 }

expected Read("bad"): error(pure let may not assign)
actual Read("bad"):   1
cash after Step():    7
```

and:

```text
when ready {
    write Box.cash = 7
    throw InvalidOperationException("boom")
}

reported fault: "none of its writes were applied"
actual cash:     7
```

The second path is worse when two bodies address the same member/index: a failed
body can overwrite a successful body's entry in `arriving`, so the failed value
lands and the successful one disappears.

The scalar/member boundary is open in the other direction too.  Calling
`Write("cash", 7d)` on a `NodeKind.Member` passes the scalar guards, replaces the
member's entire `List<object>` on propagation, and makes the next instance read
fail its cast in `Values`.  A bare scalar `Read("cash")` likewise exposes the
mutable backing list.

**Recommendation:** make member assignment consume the same effect context as
scalar assignment.  Reject it while `reading` is non-empty; stage per-member
writes in a firing-local table; merge that table into `arriving` only after
`Consumed` succeeds; and make the scalar overloads reject `NodeKind.Member`.
Regressions must observe both the returned error/fault and the member value after
the next step.

### 2. A staged write can silently move to another instance—or crash—after removal

**Severity: high — stable external identity is discarded at the exact point it
matters**

`Write(member, instance, value)` resolves the stable handle to a dense index
immediately and stores only that index (`Graph.cs:232-245`).  `Remove` is
swap-with-last and deliberately changes dense indices (`Graph.cs:192-209`,
`Compiler/Runtime/Instances.cs:75-99`).  `Propagate` later trusts the stored
index without revalidation (`Graph.cs:924-944`).

The silent witness is two instances, `removed` at dense index 0 and `survivor`
at dense index 1:

```text
Write(cash, removed, 7)
Remove(removed)              // survivor moves to dense index 0
Step()

expected survivor.cash: 0
actual survivor.cash:   7
```

The mirror shape writes the survivor at old index 1 and then removes index 0.
After compaction the array has length one, so `Propagate` indexes position 1 and
throws `ArgumentOutOfRangeException` out of `Step`.

This is the identity failure the generation-bearing handle was introduced to
prevent, reintroduced by converting the handle to a location before the write
settles.

**Recommendation:** stage the stable `Instance` (or slot plus generation), not
the dense index, and translate only when propagation applies the write.  The
language/runtime still needs an explicit same-turn write/remove policy—discard
or reject a write to the removed target are both safe; retargeting and an
uncaught bounds exception are not.  Pin both the silent-retarget and stale-index
variants.

### 3. Removing an instance leaves derived reads cached as if it were live

**Severity: high — a cached normal value survives the handle it came from**

An instance member read records a dependency on the grouped member node through
`Read(member)` (`Graph.cs:213-224`).  Member writes advance that node's clock and
dirty its dependents in `Propagate`.  `Remove`, however, mutates every member
array directly and advances no node (`Graph.cs:192-209`).

The probe was:

```text
let observed = box.cash
Read(observed)     -> 0
Remove(box)
Read(observed)     -> 0       // cached
Read(cash, box)    -> Error   // direct path knows it is stale
```

The stable-handle guarantee therefore depends on whether the value was read
directly or through a derived cell.  The latter can remain confidently wrong
forever if no later member write happens to dirty it.

**Recommendation:** instance removal is a change to each grouped member cell.
Advance/dirty those nodes when removal occurs so readers re-run and obtain the
stale-handle `Error`.  Decide and test the corresponding observability rule for
creation before population enumeration is added; otherwise the same omission
will recur on that path.

### 4. Bare global member keys reject ordinary same-named members in sibling types

**Severity: high — the runtime storage model disagrees with the compiler's
scope model**

The frontend declares each type body as its own nested scope.  This source is
accepted with no findings:

```ronin
type Box    { var name => Number; }
type Person { var name => Number; }
```

The runtime cannot represent it.  `Graph.Type` declares each member node under
the bare source name, and `seeds`, `belonging`, and `arriving` use the same bare
string (`Graph.cs:152-170, 258-265`).  Declaring the second `name` therefore
throws `InitialisationFailure` through the global `Unique` check.

The maintained test explicitly expects this clash
(`Test/Unit/Instances.cs:112-126`), but that pins a runtime restriction the
source declaration rules do not have.  Member names such as `name`, `id`, and
`value` being reusable across unrelated types is not a corner case.

**Recommendation:** give a member cell an internal identity containing its type
and declaration identity—at minimum `(type, member)`—and use that identity for
nodes, seeds, ownership, arriving writes, and edges.  Keep the source spelling
for diagnostics.  Add one source-to-runtime test with two sibling types sharing
a member name; a direct runtime-only assertion cannot establish agreement with
the frontend.

### 5. A failed type declaration leaves half a type in the graph

**Severity: medium — a handled initialisation failure corrupts later declarations**

`Graph.Type` declares and registers members one at a time, then publishes the
population only after the loop (`Graph.cs:152-170`).  A collision on any member
after the first throws without rolling back the earlier nodes or their
`seeds`/`belonging` entries.

```text
Var("occupied", 0)
Type("Broken", ("leaked", 0), ("occupied", 0)) -> InitialisationFailure

expected Declared: 1
actual Declared:   2       // "leaked" remains
```

`Broken` itself is absent from `populations`, while its first member says it
belongs to `Broken`.  A later valid type cannot use `leaked`, and the graph is
in a state no successful declaration can produce.

**Recommendation:** preflight the type name, every internal member identity,
duplicates within the incoming member set, and all collisions before mutating
any store.  A transaction/rollback also works, but preflight is simpler and
keeps the failure path allocation-free.  Test a late collision and successful
reuse of the earlier names afterward.

### 6. Square-bracket collection identity is lost at both parser and evaluator joins

**Severity: high — the new source spelling does not reliably produce a list for
the new indexer**

There are two independent manifestations.

First, `Temporary.Parse` tries `Lookup` before `List`
(`Compiler/Grammar/Value.cs:43-50`).  Both aggregates accept zero elements, so
`var empty = [];` has a `Lookup` initializer.  The settled bracket decision says
the empty default is a list, and there is no maintained empty-square-aggregate
test.

Second, the resolver represents all bracketed forms as the same `Node.Group`,
and `Evaluator.Grouped` treats a one-part group as its scalar
(`Compiler/Runtime/Evaluator.cs:97-105`).  The real lexer/resolver/evaluator
witness is:

```text
source:   [10] @ 1
expected: 10
actual:   error(«@» indexes a list)
```

A two-element group happens to become `object[]`, so `[10, 20] @ 1` can work by
accident.  A singleton list cannot, and an empty group has no list value to
produce.  Parenthesis grouping and square-bracket collection syntax need
different runtime behavior, but that distinction does not survive into the
tree.

The new indexing helper supplies `new object[] { 10d, 20d, 30d }` directly
(`Test/Unit/Indexing.cs:22-33`).  It proves the operator on hand-built data, not
the source feature.  The bracket rewrite also left the six rows of
`TwoValuesWithNothingBetweenThemAreStillRefused` using old brace values
(`Test/Integration/StatementShapes.cs:539-558`); they now exercise blocks and no
longer test the missing-list-separator cases their names and comments claim.

**Recommendation:** preserve aggregate kind through resolution—e.g. distinct
list/input/lookup nodes, or at least the opener/kind on `Node.Group`.  A list
must always evaluate to its list representation at cardinalities zero, one, and
many; input grouping may retain its current one-item collapse.  Make the parser
choose the stated empty-list default, then add full source-path indexing tests
for empty, singleton, multiple, and nested lists.  Rewrite the stale separator
rows with square brackets and retain their trailing-name cases.

### 7. `otherwise` is reserved in names but remains legal inside patterns

**Severity: high — a legal declaration makes an ordinary operator expression
ambiguous**

`Rules.Infixes` checks only the `Declared` name collection
(`Compiler/Diagnostics/Rules.cs:225-257`).  No corresponding check examines
pattern segments.  The following is accepted by the real grammar and
declaration builder:

```ronin
var x => Number;
var y => Number;
function x otherwise (value => Number) { return value; }
```

Resolving `x otherwise y` against that compilation's real symbol table returns
`Ambiguous`: it is both the built-in operation and a call to
`x otherwise (_)` at the same cost.  The authoritative spec says
`otherwise` is an operator and not a pattern, but the implemented reservation
only closes the cheaper-name capture that motivated the rule.

This is general to future word operators because `Rules.Infix` is derived from
the operator table while only `Infixes(names)` consumes it.

**Recommendation:** reject every word operator as a literal segment of a user
pattern as well as inside a name.  Give the case a pattern-directed diagnostic
rather than reporting an ambiguity at each call site.  Add a source-level
regression using `x otherwise (_)`; a manually constructed symbol table is not
enough because the missing enforcement is in declaration diagnostics.

### 8. List/lookup parsing still reparses bracket nests exponentially

**Severity: medium — bounded hostile input can still occupy roughly half a
second, and the spec claims the optimization exists**

The bracket move fixed the brace side, but did not build the stated
one-parse/one-decision collection production.  `Temporary.Parse` still attempts
`Lookup.Parse` and then `List.Parse`, and `Association` recursively parses a
value inside the first attempt.  The same late-failing nested body is therefore
reparsed under three alternatives.

`Parser.MaxGroups` still exists solely to cap this work at one million group
attempts (`Compiler/Parser.cs:78-95`).  Its comment still names brace nests, but
the ambiguity now opens on `[`.  The current measured record in
`BRACENEST-MEASURED.md` reports the same curve moved from braces to brackets,
reaching the approximately 600 ms cap at depth ten.

This is already disclosed and is not counted as a surprise from this audit.
It remains a current pessimization, and the authoritative statement that list
and lookup use “one parse and one decision”
(`docs/spec/grammatical-structure.md:499-507`) is false today.

**Recommendation:** parse a square aggregate once as values with an optional
association tail, decide kind from the first element, and diagnose a mixed
aggregate.  Pin the empty-list decision at the same layer.  Keep `MaxGroups` as
a general defensive ceiling, but replace its brace-specific explanation and add
a work-count regression showing that late-failing square nests grow linearly.

### 9. Authoritative and user-facing documentation still describes the removed model

**Severity: documentation — the documents identified as authoritative disagree
with both the new syntax and the new runtime**

The drift is broader than examples:

- The heading rule still explains `{ 1 }` as a list and prescribes
  `if takes ({ 1 })` (`grammatical-structure.md:93-109`).  Heading is explicitly
  dormant in `Parser` now; braces open blocks only.
- The reference and anonymous-value sections still define bracket indexer
  suffixes, curly lists, and `indexer` as an anonymous value
  (`grammatical-structure.md:572-603`).  Indexing is now the `@` operator.
- The lexical structure still calls `[`/`]` start/end indexer
  (`docs/spec/lexical-structure.md:78-81`), the spec table of contents links the
  deleted indexer production (`docs/spec/README.md:27-37`), and the introduction
  uses nested square indexers (`docs/spec/introduction.md:36`).
- The instance section says identity is an index, even though the implementation
  correctly makes external identity a `(type, slot, generation)` handle and an
  index only an internal transient location
  (`grammatical-structure.md:205-212`, `Compiler/Runtime/Instances.cs:9-30`).
- The `WhenInType` message and grammar comments still say the instance binding
  model is not built.  Grouped storage and stable handles now are built; the
  remaining blocker is joining type-scope reactions to them.
- The new diagnostic XML comments were pasted together: three consecutive
  `<summary>` blocks for reserved prefixes, infix names, and name/pattern capture
  all attach to `NameShadowsPattern`, while `InfixInName` has none
  (`Compiler/Diagnostics/Finding.cs:151-192`).

The list/lookup class examples and several test comments also retain curly-list
spellings.  These are not executable defects, but they make the declared source
of truth teach syntax the compiler removed and obscure which remaining work is
actually unimplemented.

**Recommendation:** perform one syntax-directed sweep across `docs/spec/`,
`docs/guide/`, XML comments, and renamed tests for curly collection literals,
square index suffixes, and “indexer” terminology.  Update the instance section
to distinguish stable handles from dense indices, and rewrite the type-`when`
diagnostic around the actual remaining join.  A link/anchor check for the spec
README would make the deleted-production link fail automatically.

## What was checked and did not produce a finding

- `@` is left-associative at the intended precedence.  Whole, zero, fractional,
  out-of-range, non-list, non-number, and incoming-error paths return values
  rather than throwing when the left operand is a real list.
- `otherwise` remains lazy, fault-excluding, and below pattern calls.  The new
  name reservation is derived from the operator table rather than duplicated.
- R6b's proper-prefix decision and anchor-only filter match the settled rule on
  the maintained source paths.  Renderer totality and the finding golden file
  include both new finding kinds.
- Brace block separation and the changed `}`-only elision behavior remain
  covered.  The old heading machinery is deliberately dormant for the planned
  expression-block change; its mere continued presence is not a finding.
- The two deliberately stricter pattern restrictions called out before the
  audit remain unobservable under current source syntax and were not counted.

## Verification

- Temporary adversarial probes reproduced the findings above through the real
  APIs and, where available, the real lexer/parser/declaration/resolver/evaluator
  path.  The temporary test file was removed before repository gates.
- Locked restore: passed.
- Exact Release solution build with `-warnaserror`: zero warnings and zero
  errors.
- Exact Release test/coverage gate: **930 passed**, zero failed, zero skipped,
  with **100% line, branch, and method coverage**.
- `git diff --check 8798edc..27a23cf`: clean.
- The worktree was clean before this report was added.

The documented hand-aligned `dotnet format` whitespace differences remain
settled project style and are not a finding.  Formatting was not used as a gate.
