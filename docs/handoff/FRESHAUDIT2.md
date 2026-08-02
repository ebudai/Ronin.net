# Fresh audit — counted chains and the new diagnostics

Audited at `6308846` (`Say what the handoff folder is, and fix the stale
desugaring`), against the previous signed-off commit `2f8d0bc`.

This pass treated `docs/spec/`, `docs/guide/`, the implementation, and `Test/`
as authoritative. Files in `docs/handoff/` were read chronologically as design
correspondence, not as specification.

There are **two release-blocking runtime findings**, followed by five smaller
diagnostic, maintainability, documentation, configuration, and performance
findings. The programmer's disclosed unfinished work is listed separately and
is not counted as audit findings.

## Release-blocking findings

### 1. Two positions in one chain can advance in the same round and lose an activation

**Severity: high — ordinary counted-chain execution silently drops work**

`Graph.Chain` gives every continuation its own `When`. `Triggered()` returns
all true continuations in a round, and `Fire()` commits each body's staged
writes into one last-write-wins `pending` dictionary.

Adjacent segments both write the counter between them:

- the earlier segment increments it when it advances; and
- the later segment decrements it when it consumes a run.

Both read the same settled front value. If both positions are ready in one
round, one absolute write replaces the other. The same collision is possible
between the head and first continuation. It is not merely an inaccurate
counter: an activation disappears (or, under the opposite ordering, can be
duplicated).

A focused runtime witness established this sequence:

1. one run is parked at wait 2;
2. a second run is parked at wait 1;
3. both wait conditions become true in one step.

Both continuation bodies run in the first round. The run from wait 1 should
then reach and execute the final segment in the next round. The observed body
log was:

```text
x, y, x, y, z
```

instead of:

```text
x, y, x, y, z, z
```

Both counters ended at zero, confirming that the second run was lost rather
than merely delayed.

This also reaches author cells. The frontend requirement deliberately groups
all segments as one writer for single-writer analysis, so two simultaneously
firing positions may legally read and write the same user cell. They will read
the same front value and one staged write will replace the other.

**Recommendation:** make "one run per round" apply to the written chain as a
whole, not independently to every generated continuation, or provide an
equivalent transaction model that prevents both internal-counter and
user-cell collisions. Combining internal counter deltas alone is insufficient:
it preserves activations but still loses conflicting user writes that the
grouped single-writer rule intentionally permits. Add regressions for:

- head and wait 1 ready together;
- two adjacent waits ready together;
- two ready positions that both update the same author cell; and
- the later run eventually reaching the tail, not merely the final count.

### 2. The inherited-work exemption still attributes consumption to the wrong run

**Severity: high — the runaway backstop can be bypassed or delayed by unrelated work**

The cross-chain pooling defect found during this audit was fixed in `56f5f36`:
`Split.Inherited` now keeps one quota per chain. That is narrower than the
required identity in two ways.

#### 2a. Runs are fungible at one wait, not across every wait in a chain

`Step()` snapshots the sum of every counter in a chain, and `Advanced(name)`
spends that shared quota when any continuation fires. A run parked at wait 2
therefore pays for newly created work consumed at wait 1 of the same chain.

The focused witness used `cascades: 2`:

- control: a new head run immediately proceeds through a true wait and reaches
  `RunawayCascade` at the documented limit;
- witness: adding one old run blocked at wait 2 makes the same new work complete
  without an exception.

Nothing consumed the old run. Its quota was merely spent at another position.

#### 2b. A failed continuation claims progress that never commits

The generated continuation calls `Advanced(name)` immediately after staging
its counter decrement and before invoking the author body. If that body throws,
`Fire()` correctly discards the decrement, but `Split.Inherited` has already
been decremented and the round remains marked as progress.

With three inherited runs, a continuation body that always fails, and an
independent runaway `when`, a `cascades: 2` graph reported failure after **5**
rounds rather than **2**. The three failed, unconsumed runs each bought an
exemption.

**Recommendation:** snapshot inherited quota per wait counter and have the
generated body identify the `arrived` counter it consumed. Commit that
consumption to the quota only after the whole body transaction succeeds, beside
the staged writes. Regressions should keep the controls beside both witnesses:
without the parked run, and with a successful continuation body.

## Correctness and diagnostic findings

### 3. The type-scope `when` re-read can blame a later `when` for an earlier invalid member

**Severity: medium — the newest diagnostic suppresses the source-ordered error**

`Type.Parse` first tries the real `Aggregate<..., Member, ...>`. If that fails,
it reparses the entire body as `Statement` and reports `WhenInType` when it can
find any direct reactive scope. The re-read does not identify which element
made the member aggregate fail.

Consequently both of these produce one `WhenInType` pointing at the later
`when`:

```ronin
type Box { if ready { return 1; } when ready { return 1; } }
type Box { while ready { return 1; } when ready { return 1; } }
```

The `if`/`while` is already an invalid type member and occurs first. Removing
the diagnosed `when` leaves the original parse failure in place. The existing
"genuine syntax error still says so" test covers a body with no `when`, so it
cannot detect this combination.

**Recommendation:** special-case `WhenInType` only when the first element the
real member aggregate cannot accept is a `when`; otherwise preserve the
source-ordered malformed-member result. Add invalid-before-when and
when-before-invalid regressions.

### 4. Nonpositive runtime limits are accepted and produce nonsensical execution

**Severity: low — invalid configuration fails late or silently changes the step contract**

The primary `Graph` constructor stores `cascades` and `settling` without
validation.

- A zero or negative cascade limit can skip the mandatory first round. With no
  pending write `Step()` returns zero rounds; with one it throws before applying
  the finite write.
- A zero or negative settling window compares windows every step while emitting
  a message that says a count did not fall in `0` or a negative number of steps.

Focused constructor assertions for all four nonpositive cases failed because
no exception was thrown.

**Recommendation:** reject values below one at construction with
`ArgumentOutOfRangeException`, and add the four boundary tests.

## DRY and authoritative-documentation findings

### 5. The injected-name registry is another hand-built copy of the real path

**Severity: medium maintainability — the completeness gate cannot detect an omitted injection**

The same two injection schemes are encoded independently in at least three
places:

- actual construction in `Declarations.Bind`/`Declarations.Cell`, using
  `Declarations.Index` and `SymbolTable.Shadowed`;
- protected words in `Rules.Injected`; and
- rendered inventory in `Glue.Shapes`.

`EveryInjectedShapeJoinsOnAProtectedWord` iterates `Glue.Shapes`, so a future
real injector omitted from `Glue.Shapes` leaves the test green. It verifies the
copy, not completeness of the implementation. It also checks only
`shape.Split('«')[0]`, so generated words after the first placeholder would be
ignored.

There is already small factual drift: the registry describes `old «a name»` as
"a reactive declaration's previous value", while `Declarations.Cell` injects
`old` for every nonconstant `Datum`, including imperative `var` cells.

**Recommendation:** define one structured injection descriptor and make the
real declaration builders, protected-word rule, and generated registry consume
it. The test should enumerate those real descriptors and inspect every literal
word outside placeholders. This is the same class of defect as a hand-built
token chain standing in for source: it proves the sample is internally
consistent, not that the production path is complete.

### 6. Authoritative code, tests, guide, and spec still disagree about the settled chain model

**Severity: medium documentation/test reliability — these exact contradictions already misled one audit**

The new handoff README correctly says the handoff files are correspondence, but
the surfaces it names as authoritative still contain superseded or contradictory
statements:

- `Test/Unit/Waiting.cs` names a test "`stop` ends its own run and leaves the
  others, and the `when` armed", but its body calls `scope.Return()`. The behavior
  is a `return` regression wearing a `stop` name.
- `Graph.Chain` still says restart is the default, says the first segment clears
  every flag, links a nonexistent `InFlight`, and comments that `stop` ends one
  run. The implementation now counts runs and uses `return` for one run.
- Much of `Waiting.cs` still describes flags being set/cleared, including the
  display name "each flag cleared", although the implementation and spec use
  counts.
- `docs/guide/README.md` says an accumulating chain settles each step "so
  nothing reports it". The new low-water detector exists specifically to report
  it, and the spec says it does.
- `Graph.cs` and `Glue.cs` each contain a duplicated opening `<summary>` tag.

The authoritative spec has two additional ambiguities that should be settled
before source lowering or instances are joined:

1. It says both `return` and `stop` let the body finish, including writes after
   the word, then says `return` leaves the body and does not do the rest. The
   runtime `Return()` method only marks non-advancement; it cannot itself skip
   later delegate statements, and no current test puts a write after it.
2. It says `stop` disarms the `when` entirely and phrases the type-scope meaning
   as "this rule is off now, for every instance", but later says type-scope
   `stop` clears only the caller's liveness bit and keeps the shared node until
   the mask empties.

**Recommendation:** sweep the authoritative surfaces for `restart`, `ignore`,
`flag`, `InFlight`, `stop ends`, and `nothing reports`; rename the false test;
and state explicitly whether `return` skips following statements and whether a
type-scope `stop` applies to the current instance or every instance.

## Performance finding

### 7. Every round scans and allocates for every `when`, even when almost none are dirty

**Severity: low today, potentially material at source scale — missing sparse-update optimization**

`Triggered()` allocates a new list and reads every entry in `whens` on every
round. Dirty propagation already walks the exact dependent set, but sink
selection discards that sparsity. A step touching one of `W` independent whens
is therefore `O(W)`, and a cascade of `R` rounds is `O(W * R)` before useful
body work.

This is at odds with the runtime's strongest performance case: sparse UI/event
updates where only a few nodes out of many changed. It predates today's chain
work, but the new `WhileTrue` continuations increase both `W` and `R`.

**Recommendation:** maintain a set/queue of dirty trigger nodes from
`MarkDirty`, plus the continuations that must remain active across rounds.
`Triggered()` can then process dirty/active sinks rather than the whole table.
Benchmark before changing it, and retain deterministic firing order.

## Disclosed and previously acknowledged work — not audit findings

The following were stated up front or remain in the signed-off backlog. This
audit verified their boundaries where relevant but did not count them as new
defects:

- two source `when`s with the same condition still collide at runtime; both
  should eventually coexist and fire;
- source does not yet split a wait chain, though tests pin the requirement that
  segments are one writer for single-writer analysis and distinct for cascade
  analysis;
- `wait until`, source `return`/`stop`, the four placement rejections, and the
  no-value-across-wait diagnostic await the pipeline join;
- type-scope `when` is deliberately refused pending instances;
- `IFASEXPRESSION.md` is not implemented;
- resolution and later semantic/runtime phases are not joined to
  `Compilation`, including `NoParse` surfacing;
- remaining dangling `=>`/return-type work;
- the numeric tower and exactness rules;
- nullable and the stronger analyzer backlog;
- bounded exponential brace parsing;
- resolver allocation/pooling wins; and
- the remaining `FAILUREMODES.md` work.

The stricter-than-final `LeadingHole` and anchor-run form of R6 remain
unobservable under current syntax, as disclosed, and are not findings.

## Verification

- Focused temporary probes reproduced findings 1, 2a, 2b, 3, and 4. Each had a
  control where one was needed, and the probe file was removed afterwards.
- Debug: **839 passed**, zero failed, zero skipped.
- Release: **839 passed**, zero failed, zero skipped.
- Exact Release solution build with `-warnaserror`: zero warnings and zero
  errors.
- Exact Release coverage gate: **100% line, branch, and method**.
- `git diff --check 2f8d0bc..6308846`: clean.
- The worktree was clean after probe removal and before this report was added.

The formatter's known hand-aligned continuation differences remain a settled,
documented, non-gating style choice and are **not a finding**. The formatter
process did not return in the audit tool wrapper and was terminated; the build,
tests, coverage, and diff checks above completed normally.

## Recommended order

1. Prevent simultaneous positions of one source chain from colliding.
2. Make inherited quotas per counter and transactional.
3. Correct the authoritative test/code/spec/guide drift while the model is
   fresh, including the two control-flow clarifications.
4. Make the type-scope diagnostic preserve the first invalid member.
5. Replace the hand-built injection inventory with descriptors used by the real
   path.
6. Validate runtime limits.
7. Benchmark dirty-trigger scheduling against the all-`when` scan.
