# Re-audit 21 — `REAUDIT20` incorporation and the claimed unobservable cap

**Audited:** `be07539` through `192c1e4`

**Date:** 2026-08-02

## Result

The `REAUDIT20` runtime repair is correct.  An inherited exemption is now
attached to the exact firing it services, and a physical round is free only
when every firing in it is covered.  The two independent-runaway reproductions
are real, non-vacuous tests and now stop at exactly the configured two charged
rounds.  The diagnostic also accurately calls its first number **counted**
rather than pretending it is a direct measurement of created work.

No current behavioral defect was found in that implementation.

This is not quite a sign-off, because the central claim in
`DEFERRALCREDIT-UNOBSERVABLE.md` is false and caused the only positive
displacement-cap regression to be deleted.  Displacement-credit exhaustion is
observable with the round rule in place.  Deleting only the decrement still
leaves all 867 maintained tests green, but changes how many head bodies execute,
the committed graph state, the firing trace, and the physical-round diagnostic
before the same `RunawayCascade` occurs.

There is also one stale XML summary left by the `advanced` replacement.

## Findings

### Medium: displacement-credit exhaustion is observable, but no maintained test exercises it

The disclosure says that removing the displacement counter changes no outcome
(`docs/handoff/DEFERRALCREDIT-UNOBSERVABLE.md:18-51`) and removes
`AndOneParkedRunForgivesOneDeferralNotEveryOneAfterIt` because no non-vacuous
replacement could be constructed (`DEFERRALCREDIT-UNOBSERVABLE.md:53-60`).

The missed shape is close to attempt 3, with one additional condition: **close
the tail during the re-arm gap and open it only when the head is ready.**  The
tail then does not drain in the gap.  The same inherited run is displaced on
successive pure head rounds, separated by charged driver rounds.

#### Full-path reproduction

The temporary probe used only ordinary runtime declarations and steps:

1. A two-position chain parks one real run at a false wait in an earlier step.
2. In loop mode, the head body closes its own trigger and the wait, then changes
   `tick`.
3. A `Changes` driver reacting to `tick` re-opens the head and wait together.
4. The driver round sees the wait closed, so the tail cannot drain.  In the next
   round the head and tail are ready together; declaration order runs the head
   and displaces the same inherited tail again.

With one inherited run and `cascades: 3`, the implemented counter gives:

| physical round | firing | displacement credit | charged total |
|---:|---|---|---:|
| 1 | head; closes head/wait and changes `tick` | spends the run's one credit | 0 |
| 2 | driver; re-opens head/wait | none | 1 |
| 3 | head; displaces the same run again | exhausted | 2 |
| 4 | driver | none | 3, then throws |

Exactly **two** head bodies execute, and the message begins:

```text
the graph did not settle: 3 rounds counted against the limit, out of 4 in all
```

Sabotaging only this line in `Displaced()`:

```csharp
--credit.Displacements;
```

makes every head round free.  The sequence becomes head/driver three times;
**three** head bodies execute before the third driver round exhausts the same
budget.  The focused assertion fails `expected 2, actual 3`.

That difference is observable several ways on the current runtime surface:

- the body has executed one additional time, including any resource effect;
- the head's `tick` write is propagated one additional time;
- `Graph.Fired` contains another head/driver pair; and
- the exception reports six physical rounds instead of four.

It therefore does not depend on source-to-runtime joining.  It is observable at
the same `Graph` API level on which every maintained wait semantic is currently
specified and tested, and the future pipeline would expose the body effects
rather than erase them.

#### Sabotage result

The programmer's other observation is correct and important: with the decrement
removed, **all 867 current tests pass**.  I independently repeated that sabotage
and result.  The suite therefore has no guard for the maintained rule that one
inherited run forgives at most one displacement round, or for the spec's `2k`
bound (`docs/spec/grammatical-structure.md:284-297`).

The current implementation at `Compiler/Runtime/Graph.cs:1513-1529` is correct;
the finding is the false unobservability conclusion and the resulting missing
regression.  That gap matters because the disclosure explicitly offers to
remove the counter and clamp (`DEFERRALCREDIT-UNOBSERVABLE.md:80-86`), which the
probe shows would be a user-observable semantic change rather than a harmless
simplification.

**Recommendation:** restore a positive exhaustion control using the alternating
head/driver shape above.  Assert both sides of the boundary, or at minimum:

- `cascades: 3` throws after four physical rounds;
- the head body ran exactly twice; and
- removing `--credit.Displacements` changes the result to three head firings.

Correct `DEFERRALCREDIT-UNOBSERVABLE.md` to record the fourth attempted shape and
withdraw the offer to remove the counter.  No new design decision is required:
the current code and authoritative `2k` rule already implement the right answer.

### Low: the removed `advanced` field left its summary attached to `servicing`

`Compiler/Runtime/Graph.cs:1474-1476` currently contains two consecutive
`<summary>` elements:

```csharp
/// <summary>Whether this round took a run the step began with.</summary>
/// <summary>What fired this round in service of work the step inherited.</summary>
private readonly HashSet<string> servicing = [];
```

The first describes the removed boolean `advanced`, not the `servicing` set.
This makes generated documentation give one member two contradictory summaries
and leaves the old whole-round model immediately above the replacement that was
introduced to eliminate it.

**Recommendation:** delete the first summary.

## Status of `REAUDIT20`

| prior item | result |
|---|---|
| inherited drains subsidise an independent runaway | **Fixed.** The runaway fires exactly twice at `cascades: 2`. |
| displacement plus drain buys two unrelated firings | **Fixed.** Both credits remain owned by their servicing firings. |
| mixed-round rule absent from the spec | **Fixed.** The authoritative spec now states that every firing in a free round must be inherited service. |
| charged versus physical diagnostic | **Fixed.** The report uses “rounds counted against the limit” and retains the physical total. |

The `servicing` and `claimed` structures preserve ownership through round
classification.  Stop cleanup removes deferred members before displacement is
classified, consumption remains transactional, and no additional defect was
found in those paths.

## Verification

- A temporary full-path probe demonstrated positive displacement-cap exhaustion
  and was removed.
- With the real decrement present: two head firings and `3 counted / 4 in all`.
- With only the decrement sabotaged away: the probe failed with three head
  firings, while the unmodified **867-test suite still passed in full**.
- Debug after restoration: **867 passed**, zero failed, zero skipped.
- Locked restore: passed.
- Exact Release solution build with `-warnaserror`: zero warnings and zero
  errors.
- Exact Release test/coverage gate: **867 passed**, with **100% line, branch,
  and method coverage**.
- `git diff --check be07539..192c1e4`: clean.
- The worktree was clean after restoring the sabotage and removing the probe,
  before this report was added.

The documented hand-aligned `dotnet format` whitespace differences remain
settled project style and are not a finding.  Formatting was not used as a gate.

The previously disclosed feature and pipeline work remains outside this
re-audit and is not repeated as findings.

## Recommended order

1. Add the alternating closed-wait/re-arm positive-cap regression.
2. Correct the unobservability disclosure and keep the displacement counter.
3. Remove the stale `advanced` summary.
