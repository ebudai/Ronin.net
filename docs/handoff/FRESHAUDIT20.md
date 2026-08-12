# Fresh audit 20 — Section E lookup implementation

**Audited:** `45b2d8e..a5341b8`, the four commits implementing
`EAGGREGATES2.md` §§1–9.

**Date:** 2026-08-11

## Result

**No sign-off. Three high-severity runtime findings and three medium findings
(runtime semantics, completeness, and structural safeguarding) remain.**

The resolver half is substantially sound. `=` now has a structural lexeme kind,
a lookup is carried as keyed entries through the resolution tree and evaluator,
and independent ambiguities in a key and value form the full product. Temporary
source probes also verified that an ambiguity in either lookup cell receives two
applicable repairs. Runtime admission correctly shares list and lookup traversal
state, refuses cycles and duplicate keys by value, preserves DAG sharing, and
measures one depth across both aggregate kinds.

The runtime value is not complete, however. `@` has no lookup branch at all;
aggregate typing and expected-type construction of an empty lookup do not exist;
and an `Error` is admitted as a key despite the settled error-as-value decision.
The implemented equality/iteration combination also follows the sentence that
`LOOKUPEQUALITY.md` expressly revised: two lookups compare equal while exposing
different orders. In the reactive graph, that makes cutoff suppress a real
downstream change. Independently, lookup equality omits the shared-pair memo that
list equality uses and takes exponential time on the DAGs admission deliberately
preserves.

All maintained gates pass: 1,215 tests in Debug and Release, the locked restore,
the warning-as-error Release build, 100% line and branch coverage, and the direct
plus transitive package vulnerability audit. The failures are relationships and
missing paths that the maintained examples do not ask about.

---

## Findings

### 1. Equal lookups can expose different iteration orders, so graph cutoff leaves derived values stale

**Severity: high — a write changes an observable lookup result, but a dependent
`let` retains the result from before the write.**

`Lookup` stores and enumerates associations in insertion order
(`Compiler/Runtime/Lookup.cs:30-34, 63-72`), while `Builtin.Same` deliberately
ignores that order (`Compiler/Runtime/Values.cs:313-354`). The graph uses that
same equality to suppress propagation (`Compiler/Runtime/Graph.cs:970, 1094`).

A temporary production-runtime probe constructed this graph:

```csharp
graph.Var("reverse", false);
graph.Let("table", scope => Equals(scope.Read("reverse"), true)
    ? Pairs(("b", 2d), ("a", 1d))
    : Pairs(("a", 1d), ("b", 2d)));
graph.Let("first key", scope => ((Lookup)scope.Read("table"))[0].Key);

graph.Write("reverse", true);
graph.Step();
```

After the step, directly reading `table[0].Key` gives `"b"`, proving the table
was recomputed in the new order. `first key` remains `"a"`: equality called the
new table unchanged and cutoff did not wake the dependent.

This is the exact conflict documented in `LOOKUPEQUALITY.md:12-68`. That document
explicitly says it revises the earlier insertion-order decision and requires a
content-derived canonical order. The implementation instead repeats the
superseded trade in its type comment.

**Recommendation:** canonicalise association order at admission using the
defined total order over key kinds, then expose only that order. Equal lookups
must enumerate identically. Add a graph-level regression like the probe above;
a direct equality test plus a direct iteration test cannot establish the cutoff
relationship.

### 2. Lookup equality re-proves a shared DAG once per path and is exponential

**Severity: high — ordinary accepted values can freeze the always-running
runtime during equality, duplicate-key admission, or graph cutoff.**

Admission intentionally preserves a repeated raw aggregate as one shared value
(`Compiler/Runtime/List.cs:156-166, 217-255`). List equality correspondingly
memoises each `(left, right)` list pair (`Compiler/Runtime/Values.cs:357-400`).
Lookup equality has no such state: every nested comparison re-enters the public
`Same` method and walks the subtree again (`Values.cs:333-354`).

The temporary probe built two independently admitted depth-12 lookup DAGs. At
each level, keys `left` and `right` both referenced the same child. The values
were equal and `Builtin.Same` returned `true`, but the one leaf comparison ran
**4,096 times** rather than at most 13 times. That is `2^depth`; the admitted
depth ceiling is 256, so construction does not bound this work to a usable
amount. The same comparison is also used while checking duplicate compound keys
at `List.cs:244-247`.

The maintained sharing test proves admission does not expand a DAG and then
compares only the two shared references inside one already-admitted outer value.
Reference equality makes that case constant-time, so it cannot detect comparing
two distinct but structurally equal DAGs.

**Recommendation:** carry one comparison context through every aggregate kind,
memoising `(left aggregate, right aggregate)` pairs across list-to-list,
lookup-to-lookup, and mixed nesting. Cycles are refused, so treating a repeated
pair as already proved has the same justification the list implementation
documents. Maintain the independently admitted DAG probe and assert linear leaf
comparisons.

### 3. Lookup indexing is absent

**Severity: high — lookup literals can be built but their values cannot be
retrieved through the language's indexing operation.**

Section E §8 requires `m @ k` to return the value under `k`, and the current
terms define indexing over either a list or lookup. `Builtin.Indexing` still has
only list cases (`Compiler/Runtime/Values.cs:239-258`). Every lookup falls to:

```text
error(«@» indexes a list)
```

The direct probe admitted `["a" = 1]` and applied the built-in `@` operator with
`"a"`. Expected `1`; actual was the error above. There is no maintained lookup
indexing test and no other lookup consumer in production.

There is a design conflict only for a **miss**: `EAGGREGATES2.md:299-311` says
`Error`, while `docs/spec/NOTHINGANDINDEXING.md` says `nothing`. It does not
affect a hit or the key relation. `LOOKUPEQUALITY.md:89-111` settles that relation:
`@` finds the association whose key `is` the index, meaning `Builtin.Same`,
including structural list and lookup keys.

**Recommendation:** implement lookup hits now with the same key relation used by
duplicate detection and equality, including compound-key tests. Reconcile the
two miss documents before fixing the miss result, and maintain separate tests
for scalar hits, structural-key hits, misses, and invalid left operands.

### 4. Section E's typing and expected-type empty lookup remain unimplemented

**Severity: medium completion blocker — this is a known project-wide dependency,
not a regression, but it makes the claim that Section E is fully implemented
incorrect.**

Section E §5 requires `[]` under an expected `lookup of K V` type to produce
`Lookup.Empty`; §7 requires unifying list element types and lookup key/value
types. The resolver unconditionally constructs an empty square group as
`Node.Grouping.List` (`Compiler/Resolution/Resolver.cs:309-317`). `Lookup.Empty`
is reached only when a host/runtime caller submits an empty pair carrier
(`Compiler/Runtime/List.cs:219-221`). No expected type is consulted.

More fundamentally, `Compilation` records that types resolve against a table
which does not exist (`Compiler/Compilation.cs:165-175`) and that inference is
waiting for a type to unify into (`Compilation.cs:232-235`). No code in the
implementation range adds type resolution, aggregate unification, mismatch
findings, or outward-in selection of an empty aggregate kind.

**Recommendation:** either narrow the delivery statement to the resolver and
runtime subset of Section E, explicitly leaving §§5 and 7 pending on the type
layer, or implement that layer and test source-level homogeneous and
heterogeneous lists/lookups plus an annotated empty lookup. The existence of an
unreachable `Lookup.Empty` singleton does not implement expected-type
construction.

### 5. `Error` values are admitted as lookup keys

**Severity: medium — a failed key computation becomes a real map key and
participates in duplicate detection by failure message.**

`ERRORASVALUE.md:103-111` settles that an `Error` may not be a lookup key and is
to be refused at construction, beside the depth and cycle refusals already in
`List.Admit`. `Associated` normalises a key and checks only whether it is the
private `Refusal` sentinel (`Compiler/Runtime/List.cs:229-247`). A runtime
`Error` is an ordinary value there.

The probe admitted one pair whose key was `new Error("boom")`. Expected an
`Error` refusal mentioning the key; actual was a `Lookup` containing the failed
key. Since `Error.Equals` compares kind and message, two independent failures
with the same text can also be declared the same lookup key — exactly the claim
the design rejects.

**Recommendation:** after normalising a key and before evaluating duplicate-key
membership, refuse an ordinary `Error` with a key-specific message and propagate
a `Fault` without admitting or laundering it. Keep errors legal as lookup
**values**, just as they remain legal list elements. Add both rows so the refusal
does not accidentally restore the old error-as-sentinel bug for aggregate
values.

### 6. The new group kind does not make the key/kind disagreement unrepresentable

**Severity: medium structural safeguard — production currently constructs valid
groups, but the representation still permits the exact two-field disagreement
the design says the kind removes.**

`Node.Entry.Key` remains nullable and `Node.Group` accepts any entry sequence
with any `Grouping` value (`Compiler/Resolution/Node.cs:243-286`). There is no
construction validation. Identity and rendering consult keys only when `Kind`
is `Lookup` (`Node.cs:288-310, 339-343`), while `Whole` walks every non-null key
regardless of kind (`Node.cs:321-334`).

A direct internal probe built two `Grouping.List` nodes with the same value and
different non-null keys. `Node.Same` returned `true`, although `Whole` exposed
the different key subtrees. Thus derivation identity says the trees are the
same while repair traversal says they contain different nodes. The mirror state,
a `Grouping.Lookup` entry with a null key, dereferences null while computing its
shape at `Node.cs:342`.

This is not a request for unreachable defensive code. The implementation comment
claims there is “no per-entry state to disagree with,” but the nullable key is
exactly that state.

**Recommendation:** make valid construction structural: for example, private
constructors/factories with distinct unkeyed and keyed entry types, so a list or
group cannot receive a key and a lookup cannot receive an unkeyed entry. If the
single `Entry` carrier is retained, its constructor boundary must enforce the
kind/key invariant and have a test; otherwise the kind is still a tag beside a
nullable convention.

---

## Adversarial verification

Temporary tests were added only in a detached audit worktree and were not added
to the maintained tree.

| Probe | Result |
|---|---|
| Lookup hit through `@` | **Failed:** expected `1`, received `error(«@» indexes a list)` |
| Graph cutoff after reversing equal lookup order | **Failed:** table's first key became `b`; dependent remained `a` |
| Two independently admitted depth-12 shared lookup DAGs | **Failed:** equality was true but compared the leaf 4,096 times |
| `Error("boom")` as a lookup key | **Failed:** admitted as `Lookup`, not refused as `Error` |
| Two list-kind nodes carrying different hidden keys | **Failed:** `Node.Same` returned true |
| Independent ambiguities in lookup key and value | **Passed:** exact total and four retained readings |
| Source ambiguity in a lookup key | **Passed:** two repairs, both recompiled cleanly |
| Source ambiguity in a lookup value | **Passed:** two repairs, both recompiled cleanly |

## Verification record

- Inspected all four implementation commits and their complete production/test
  diff, plus the resolver, evaluator, graph cutoff, admission, equality, error,
  indexing, current spec, and superseding lookup-equality decision.
- Focused aggregate, lookup-value, indexing, and statement-shape suites:
  **185 passed**.
- `dotnet restore --locked-mode`: passed.
- `dotnet build --no-restore --configuration Release -warnaserror`: passed with
  zero warnings and zero errors.
- Full Debug suite: **1,215 passed, 0 failed, 0 skipped**.
- Full Release suite: **1,215 passed, 0 failed, 0 skipped**.
- Exact Release coverage gate: passed; Cobertura reports **3,705/3,705 lines**
  and **2,511/2,511 branches** (100% each).
- `dotnet list Ronin.sln package --vulnerable --include-transitive`: no known
  vulnerable direct or transitive packages in the configured source.
- `git diff --check`: passed.
- `git diff -- Compiler Test`: empty after probe isolation; no production or
  maintained test file was changed by this audit.

The pre-existing `docs/spec` edits and untracked handoff/design files were
preserved. This report is the only repository artifact added by the audit.
