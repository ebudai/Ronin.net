# The inference pass — validated, with one pointer and one bound to drop

> **Ledger** — `[V]` Validates the gather/infer/check split; rules the inferred sort onto the signature; points §5 at the existing Tarjan in `Cascades.Components`; withdraws the round bound as a misapplication of `MIDSESSIONDESIGN` §5.
> answers: INFERENCEPASS
> supersedes: none
> superseded by: none

**The plan is faithful and the seam is right.** Three answers, and the second one
is the one that changes your implementation: **the SCC you asked about exists, is
generic, and is iterative on purpose.** You could not easily have found it — it is
`private` in `Runtime/Cascades.cs` and you are working in `Checking`.

---

## §1 — where the inferred sort lives: **on the signature**

Your worry is well-posed and it dissolves on one distinction.

`MIDSESSIONDESIGN` §3's *"one invalidation design, not two"* governs **things that
survive an edit**. A value that is discarded together with the thing it describes
needs no invalidation at all — it is not stale, it is *gone*.

So ask what happens to a signature on an edit. At the commit I can see, a
compilation is built from a `SourceText` and the declaration tree is constructed
whole; the signature object does not survive. **Neither does the written return
sort stored beside it** — and nobody calls that a second store with no
invalidation, because invalidation-by-reconstruction is invalidation.

> **Store it where it dies with the thing it describes. Put it in the graph when it
> must outlive it.**

The inferred return sort dies with the signature → **field**. The
`(function, instantiation)` cache is *designed* to survive edits — that is the
entire point of caching inference across a session — so **that** is what §3 was
written about, and that is what rides the graph.

And ledger the boundary, because it is a real approximation with a real trigger:

```
  approximation                      successor                 trigger
  the inferred sort is a field,      graph-backed, with the    the first incremental
  safe because the declaration       cache's invalidation      rebuild of the
  tree is rebuilt whole              semantics                 declaration tree
```

The moment the tree stops being rebuilt whole, the field becomes a survivor and
§3 starts applying to it. Until then it does not.

## §2 — §5: use `Cascades.Components`. It is already what you need

I wrote in `RECURSIVERETURN` §3 that *"the compiler already computes [the SCC] to
order everything else."* I went and checked, because a claim about the tree is a
claim about the tree — and it holds, more usefully than I remembered:

```
  Compiler/Runtime/Cascades.cs
    private static List<List<string>> Components(
        Dictionary<string, HashSet<string>> edges, IEnumerable<string> nodes)
```

**Tarjan, and generic.** Nothing in its signature knows about `Effects`; it takes
edges and nodes. Its own comment says it is **iterative rather than recursive**
because *"a chain of a thousand whens each writing what the next reads is a
thousand stack frames"* — the exact deep-chain case a return-inference pass has.

**Extract it to a shared home and call it. Do not copy it, and do not re-derive it
by iteration** — two Tarjans, like two window predicates, later disagree.

### And the fixpoint is not merely more expensive — its *diagnostic* is wrong

Your fixpoint finds the same answer; iterating a monotone function to a least
fixpoint is order-independent, so correctness is not the issue. The issue is what
it can *say* when it stalls.

A stalled round leaves the union of every unresolved group **plus every function
merely waiting behind one**. Telling those apart — *"this two-member group has no
base case"* from *"these twelve are downstream of it"* — requires knowing the
groups, which is the condensation you were avoiding computing.

```
  fixpoint stalls  ->  "these 14 functions are unground"
  SCC             ->  "«f» and «g» call only each other and neither has a base case"
```

The second is the finding this language ships. The first is the finding it
refuses. Since the machinery is already written, there is no trade to make.

## §3 — drop the round bound. `MIDSESSIONDESIGN` §5 was about a different quantity

This one I need to correct, and it is my fault for not scoping the sentence.

§5's bound — *"a check must mint a bounded number of instantiations"* — is about
**monomorphisation**, where the chain can be genuinely infinite: polymorphic
recursion instantiates at ever-new types and never terminates on its own. That is
a real unbounded case and it needs a real bound.

**Return inference has no unbounded case.** The function set is finite; each
productive round stores at least one; a round that stores nothing stops the loop.
It terminates structurally, and with `Components` there are no rounds at all.

So a fixed round bound here would do nothing but **manufacture false refusals on
deep chains** — a legitimate call chain longer than the constant would be reported
as a failure of the program rather than of the checker. That is the shape this
language refuses everywhere.

If you want a *responsiveness* guard so the editor cannot freeze, that is fine and
separate — but it must report as itself:

> **A resource bound must never masquerade as a semantic verdict.** *"Checking took
> too long"* and *"your program is unground"* are different findings, and only one
> of them is about the source.

## §4 — the seam is right; stop preserving pass order and remove the dependency on it

Gather → Infer → Check is faithful. The checks being pure functions of
`(declared, read, sorts, function)` is exactly why a strict phase order works, and
it works for a reason worth stating: **with no subtyping and unification as
equality, checking produces no information inference needs.** There is no
back-edge, so three phases suffice and no outer fixpoint over them is required.

Moving the omitted-return work into Infer is the right seam, and it makes
`RETURNANDLITERALS` §1c literal exactly as you say — the same walk stores the sort
or reports why it cannot. Confirmed.

**But step 2 of your plan will move the golden files, and your invariant says it
must not.** Recording contexts outer-first preserves order *within* the Check
phase. It cannot preserve order *between* phases: after the move, a
`DivergentReturns` in the last scope precedes an `UnknownType` in the first, where
today they interleave per scope.

The fix is not to be careful. It is to remove what you are being careful about:

> **Order findings by source position, not by pass order.**

That is better independently — a reader wants findings in file order, not in
whichever sequence the compiler happened to visit — and it makes this restructure
and every future one free, because no pass reordering can move the output. Land it
as step 0, as its own behaviour-preserving commit, and steps 1–4 stop being
fragile.

Same shape as several rulings already: make the invalid state unconstructible
rather than carefully avoided.

## Summary

| | |
|---|---|
| **§4 store** | **on the signature.** `MIDSESSIONDESIGN` §3 governs values that **survive an edit**; one discarded with its container is not stale, it is gone — and the written return sort lives there on the same terms |
| the line | **store it where it dies with the thing it describes; put it in the graph when it must outlive it.** The `(function, instantiation)` cache is the survivor, and §3 was written about that |
| ledger it | approximation: a field, safe while the declaration tree is rebuilt whole. Trigger: **the first incremental rebuild** |
| **§5 SCC** | **it exists** — `Cascades.Components`, Tarjan, **generic** over edges and nodes, and **iterative on purpose** for thousand-deep chains. Extract and call it; do not copy and do not re-derive |
| why not the fixpoint | not correctness — **the diagnostic.** A stalled round cannot tell *"this group has no base case"* from *"these twelve are behind it"* without the condensation it was avoiding |
| **the bound** | **drop it.** `MIDSESSIONDESIGN` §5 was about **instantiation minting**, which can be genuinely infinite. Return inference over a finite function set terminates structurally |
| my fault | that sentence was unscoped. Applied here a fixed round bound would **manufacture false refusals on deep chains** |
| the rule | **a resource bound must never masquerade as a semantic verdict.** A responsiveness guard is fine and reports as itself |
| **the seam** | **faithful.** No subtyping and equality unification means checking feeds nothing back to inference — so three phases suffice, with no outer fixpoint |
| **but** | step 2 **will** move the golden files — order is preserved within a phase, not between phases |
| the fix | **order findings by source position, not pass order.** Land it as step 0. Better independently, and it makes every future reordering free |
