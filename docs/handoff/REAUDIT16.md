# Re-audit 16 — `FRESHAUDIT2` incorporation

**Audited:** `6308846` through `638e7c1`  
**Date:** 2026-08-02

## Result

This is **not a sign-off**. The original lost-activation case is fixed, the
per-counter transactional quota is fixed, and invalid scheduler bounds are now
rejected correctly. The new scheduler, however, has two unhandled interactions
with deferred chain positions, one of which throws after a legal `stop`. The
type-scope diagnostic repair also turns two forms of malformed source into an
uncaught `NullReferenceException`.

Six findings remain: two fatal correctness failures, one same-step scheduling
failure, one substantial chain-heavy pessimization, one incomplete DRY repair,
and residual authoritative-code/test prose that still describes the superseded
semantics.

## Findings

### 1. Release blocker: stopping a chain can leave removed continuations in `woken`, then the next candidate sort crashes

`Triggered()` defers every second ready position of a chain by adding it back to
`woken` (`Compiler/Runtime/Graph.cs:784-790`). `Stopped()` removes the chain's
`whens`, its `level` entries, and its nodes, but never removes those names from
`woken` (`Graph.cs:865-882`). On the next round or step, `Triggered()` sorts the
candidate names by indexing `whens[left]` and `whens[right]` **before** its later
`TryGetValue` guard (`Graph.cs:741-762`). Two stale names are enough to make the
comparison throw.

The temporary probe used a three-segment chain and established this state:

1. one old run waiting at wait 2;
2. another old run waiting at wait 1;
3. the head and both wait conditions become true in one step;
4. the head calls `Stop()`.

The head claims the source chain, both continuations are deferred into `woken`,
and `stop` correctly removes the entire chain. A following empty `Step()` then
fails with:

```text
System.InvalidOperationException: Failed to compare two elements in the array.
  ---> System.Collections.Generic.KeyNotFoundException:
       The given key 'chain (after wait 1)' was not present in the dictionary.
  at Ronin.Runtime.Graph.Triggered() ... Graph.cs:745
```

With another live level candidate or another pending write, only one stale name
is needed and the failure can occur in the next round of the **same** step.

**Recommendation:** removal must remove a trigger from every scheduler index,
including `woken`. Also filter candidate names through `whens.TryGetValue`
before sorting; the existing guard is too late to provide the tolerance its
comment promises. Add a regression with multiple ready positions, a head
`stop`, and a subsequent round/step. This needs a multi-position case: one stale
candidate does not invoke the sort comparison and falsely passes.

### 2. Release blocker: malformed `when` syntax inside a type crashes compilation

The first-invalid-element repair asks whether the first non-member is a
`Scope` whose `Reacts` flag is true (`Compiler/Grammar/Type.cs:68-75`). Reactive
**parse-error nodes** inherit the same scope/reactive classes, so malformed
syntax also satisfies that test. Those error nodes have no `Opened` token. The
code copies the null token into `ReactiveMemberError`, and
`Compilation.Malformed` calls `Where(reactive.Opened)` unconditionally
(`Compiler/Compilation.cs:475-478`).

Both of these source inputs reproduced the uncaught `NullReferenceException`:

```ronin
type Box { when { return 1; } }
type Box { when changing { return 1; } }
```

The stack ends at `Compilation.Where(Token)` from
`Compilation.Malformed(IError)`. These are malformed programs and must produce
a `Malformed` finding; source text must not be able to terminate the compiler.

**Recommendation:** only synthesize `ReactiveMemberError` from a successfully
parsed reactive statement, not from an `IError` subtype, and require a real
`Opened` token before constructing the named refusal. Add malformed controls for
both `when` productions beside the valid `WhenInType` rows. The existing `+`
control does not exercise this inheritance boundary.

### 3. High: a deferred ready continuation is not part of the settle condition, so `return` can postpone it to an unrelated step

The new one-position-per-chain rule re-adds a deferred position to `woken`, but
the settle loop continues only while `pending` contains writes
(`Compiler/Runtime/Graph.cs:682`). `woken` is scheduler work, yet it is not part
of that condition or the final non-settlement check.

The temporary probe did the following:

1. parked an old activation at a two-segment chain's wait;
2. reset the head edge;
3. in one step, made the head condition and the old activation's wait condition
   true;
4. made the head body call `Return()` for the new activation.

The head fires first and claims the chain. The old continuation is ready but is
deferred into `woken`. Because the head's `return` deliberately writes no next
count and the body writes nothing else, `pending` is empty and `Step()` returns
after one round. The old tail has not run. A later empty `Step()` finally runs
it.

That contradicts both relevant settled rules:

- runs beside the returning run are unaffected; and
- a satisfied wait proceeds in the same step
  (`docs/spec/grammatical-structure.md:338-342`).

In an event-driven host that schedules a step only when something changes, the
continuation may wait indefinitely for an unrelated event rather than merely
one frame.

**Recommendation:** make deferred scheduler work keep the settle loop alive,
and include it in the termination invariant. The round-limit accounting needs
to distinguish this finite deferred work from a round that creates more work;
simply adding `woken.Count != 0` to the loop without revisiting the limit and
post-loop check would leave edge cases at a limit of one. Add the conditional
head-`return`/old-ready-tail regression explicitly—the existing collision test
always produces a pending counter write, which masks this path.

### 4. Medium: finding 7 is fixed for ordinary `when`s but not for chains; every inactive continuation is still scanned, sorted, and allocated every round

Ordinary edge triggers now use the dirty wake set, and the existing 5,000-`when`
measurement demonstrates a real improvement. Every chain continuation is also
put in `level`, however, and `Triggered()` appends **all** of `level` to every
candidate list (`Compiler/Runtime/Graph.cs:179-184,741`). It then sorts that
whole list and reads every continuation even when all counts are zero and no
guard changed.

A temporary allocation probe built 5,000 inactive two-segment chains, primed
them, warmed one step, and measured 100 empty steps with
`GC.GetAllocatedBytesForCurrentThread()`. Both Debug and Release allocated:

```text
89,168,000 bytes / 100 no-op steps
891,680 bytes per no-op step
```

This retains the original O(total triggers) step cost for chain-heavy programs,
and adds a sort. The current regression in `Test/Unit/Events.cs:188-212` contains
only ordinary `when`s, so it cannot detect this half.

The continuation does not inherently need a perpetual scan. Arrival changes
its count and wakes it; a guard change wakes it; a successful firing decrements
its count and therefore wakes it for the next drain round. The exceptional case
that needs an explicit policy is a failed body whose unconsumed run should be
retried on a later step.

**Recommendation:** benchmark inactive and sparsely active **chains**, not only
ordinary `when`s. Drive continuations from their dependencies and deliberately
requeue only the cases whose semantics require another attempt. At minimum,
avoid rebuilding and sorting every zero-count continuation on every turn.

### 5. Low/DRY: the injection descriptor still does not drive the real shadow name in the resolver or runtime

`Injection.Shadow` now drives diagnostic declaration metadata, the registry,
and part of the protection rule. That is useful, but the actual resolver symbol
and runtime node still come from a second definition:

- `Injection.Shadow` hard-codes `"old"`
  (`Compiler/Diagnostics/Injection.cs:68-69`);
- `SymbolTable.Old` and `SymbolTable.Shadowed` independently hard-code the same
  word and prefix (`Compiler/Resolution/Resolver.cs:1195-1197`);
- `SymbolTable.Declaring` constructs the resolved shadow with
  `Shadowed + name` (`Resolver.cs:1133-1150`);
- `Graph.Shadow` constructs the runtime node the same independent way
  (`Compiler/Runtime/Graph.cs:210-218`); and
- the special all-segments protection still starts from `SymbolTable.Old`
  (`Compiler/Diagnostics/Glue.cs:127-130`).

Consequently, changing the descriptor can change the diagnostic declaration,
registry, and generated reserved-word document while the resolver and runtime
continue using `old `. The new test iterates `Injection.All`, but it does not
assert that these real paths consume the descriptor. This is the exact drift
class finding 5 was intended to remove.

**Recommendation:** put the canonical injected-name definitions in a neutral
layer consumed by declarations, `SymbolTable`, the runtime, rules, and registry;
remove the independent constants or derive them from the descriptor. Add a
direct invariant that the shadow name installed in `SymbolTable` and allocated
by `Graph` equals `Injection.Shadow.Of(name)`.

### 6. Low: the authoritative code and tests still contain the superseded `stop` and quota descriptions

The spec and guide now state the settled meanings correctly, but the sweep
missed several nearby statements in the authoritative code/test surfaces:

- `Compiler/Runtime/Graph.cs:414-418` says “`stop` ends THIS run”, “the `when`
  stays armed”, and runs beside it are untouched. The flag tested immediately
  below is set by `Return()`; `stop` disarms the whole `when` and abandons every
  pending run.
- `Graph.cs:656-667` first says runs are fungible “WITHIN a chain”, then the new
  paragraph correctly says they are fungible only at one wait.
- `Test/Unit/Waiting.cs:779-783` repeats the now-refuted within-a-chain quota
  explanation.
- `Split.Flags` (`Graph.cs:1189-1194`) and
  `AndTheWhenIsGoneNotPresentAndFlagged` retain the old boolean vocabulary for
  counts/disabled state.

The first item is especially risky because this exact `return`/`stop`
misdescription has already caused design and audit confusion. Correct the
comments and names while this model is fresh. This is not a request to rewrite
historical handoff correspondence.

## Status of the seven `FRESHAUDIT2` findings

| prior finding | result |
|---|---|
| 1 — simultaneous chain positions lose a run | **Partial.** The original loss is fixed and its regression passes, but deferred positions cause findings 1 and 3 above. |
| 2 — inherited quota scope and failed-body spending | **Fixed.** Quota is per counter and is spent only after a successful body. Adversarial inspection found no remaining cross-wait or failed-body exemption. |
| 3 — type-scope diagnostic blames a later `when` | **Partial.** The first-element rule is fixed, but malformed reactive nodes now crash compilation (finding 2). |
| 4 — nonpositive runtime bounds | **Fixed.** Both constructor arguments reject values below one at construction. |
| 5 — hand-built injection inventory | **Partial.** Descriptors drive several consumers, but not the resolver/runtime shadow construction (finding 5). |
| 6 — code/test/guide/spec drift | **Mostly fixed.** The maintained spec and guide are coherent; residual wrong code/test prose is finding 6. |
| 7 — all-`when` scan/allocation | **Partial.** Ordinary `when`s are sparse; all chain continuations remain eagerly scanned (finding 4). |

## Verification

- Temporary focused probes reproduced findings 1 through 4 and were removed.
- Debug: **848 passed**, zero failed, zero skipped.
- Locked restore: passed.
- Exact Release solution build with `-warnaserror`: zero warnings and zero
  errors.
- Exact Release test/coverage gate: **848 passed**, with **100% line, branch,
  and method coverage**.
- `git diff --check 6308846..638e7c1`: clean.
- The worktree was clean after probe removal and before this report was added.

The documented hand-aligned `dotnet format` whitespace differences are settled
project style and are **not a finding**. I did not use formatting as a gate.

The disclosed work that still awaits pipeline/feature joins is not re-reported
here: duplicate source `when` identity, source chain splitting, source
`wait`/`return`/`stop` and their placement diagnostics, type-scope instances,
`IFASEXPRESSION`, resolver-to-compilation joining, remaining dangling return
types, numeric exactness, nullable/analyzers, brace parsing, resolver pooling,
and the remaining `FAILUREMODES.md` work.

## Recommended order

1. Remove stale wake entries on `stop` and filter live candidates before sort.
2. Preserve deferred ready work inside the current step and pin the
   `return`/old-tail case.
3. Exclude reactive parse-error nodes from the type-scope named refusal.
4. Finish sparse scheduling for chain continuations with a chain-heavy
   allocation regression.
5. Finish centralizing shadow injection and clean the contradictory terminology.
