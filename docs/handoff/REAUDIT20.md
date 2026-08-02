# Re-audit 20 — `REAUDIT19` incorporation

**Audited:** `84f3712` through `be07539`

**Date:** 2026-08-02

## Result

This is not yet a sign-off.  Both `REAUDIT19` findings are incorporated
correctly at the level they addressed:

- one per-wait `Credit` record now owns both balances, and a drain clamps unused
  displacement credit to the inherited runs still standing; and
- `RunawayCascade` reports charged and physical rounds separately and treats the
  last firings as likely sources rather than proven culprits.

The new five/six-round control is the exact drain-before-new-deferral ordering
from the audit and passes.  The pre-existing displace-then-drain control also
continues to prove that the two approved exemptions remain separate.

The programmer's observation about the recurring error shape was productive,
because the same ownership loss still exists one level above the new record.
The balances are now attached to the correct wait and generation, but the loop
collapses their result into the graph-wide booleans `advanced` and
`Throttled()`.  A credit owned by one chain consequently makes the **whole
round** free, including unrelated work in other chains and ordinary `when`s.

This is a fourth form of the same subsidy and directly falsifies two maintained
claims: no healthy chain can pay for another chain's spinning, and a genuine
runaway is caught after exactly `cascades` created-work rounds, undelayed.

## Finding

### High: owned per-wait credit becomes an unowned whole-round voucher

`Consumed()` correctly spends the `Credit` belonging to the exact counter
(`Compiler/Runtime/Graph.cs:1524-1537`), but records the result in one graph-wide
bit:

```csharp
--credit.Drains;
advanced = true;
```

Likewise, `Throttled()` spends one exact position's displacement credit but
returns only `true` (`Graph.cs:1494-1509`).  At the round boundary all ownership
has disappeared:

```csharp
if (advanced is false && Throttled() is false) ++counted;
```

Every reaction selected by `Triggered()` fires before that one decision
(`Graph.cs:711-744`).  Several independent chains and ordinary `when`s therefore
share a round.  If any one of them consumes or displaces inherited work, every
other firing in that round inherits its exemption.

#### Reproduction 1 — inherited drains subsidise an independent runaway

The temporary full-path probe used:

- a two-position `queue` chain with three real runs parked at a false wait in
  earlier ordinary steps; and
- an unrelated `Changes` reaction whose body increments the very cell its
  trigger reads, so it creates its successor every round.

With `cascades: 2`, the tested step opens the queue's wait and starts the
self-loop together:

| physical round | queue | independent self-loop | current charged total |
|---:|---|---|---:|
| 1 | consumes inherited run 1 | fires and creates round 2 | 0 |
| 2 | consumes inherited run 2 | fires and creates round 3 | 0 |
| 3 | consumes inherited run 3 | fires and creates round 4 | 0 |
| 4 | — | fires and creates round 5 | 1 |
| 5 | — | fires again | 2, then throws |

The runaway fires **five** times instead of the configured two.  The exception
confirms the mismatch:

```text
the graph did not settle: 2 rounds of work it created, out of 5 in all
```

The self-loop created its successor in all five rounds.  `2` is the number the
current implementation *charged*, not the number of rounds containing created
work.

The delay is arbitrarily large in the relevant sense: a healthy queue may be
thousands deep, so `k` inherited drains buy `k` extra firings for an unrelated
runaway.  This is precisely what
`docs/spec/grammatical-structure.md:289-301` says cannot happen.

#### Reproduction 2 — Q2's two credits buy two unrelated firings

A second probe parked one inherited run.  In the tested step its chain head was
re-armed and returned while the tail was ready, so round 1 displaced the run;
round 2 drained it.  The same independent `cascades: 2` self-loop fired beside
the chain in every round.

Current result: the displacement makes round 1 free, the drain makes round 2
free, and the self-loop receives four firings before the two charged rounds are
exhausted.  The message reports `2 ... out of 4` although all four rounds
created its successor.

This does **not** challenge the settled decision that displacement and drain are
two exemptions.  It shows that each exemption has escaped its owner: one old
run in one chain buys two rounds for unrelated work.  For `k` inherited runs,
the documented `2k` throughput allowance can become up to a `2k` delay in the
safety limit.

#### The new diagnostic test already contains the mixed-round shape

`AndARunThatHasAlreadyDrainedPaysForNothingAfterIt`
(`Test/Unit/Waiting.cs:1173-1232`) asserts that the exception says five rounds of
created work out of six physical rounds.  But its first round both drains the
inherited continuation **and** fires `phasing`, which writes `w`, `h`, and
`phase` to create the next round (`Waiting.cs:1197-1216`).  The test correctly
pins the intended five/six boundary for the same-position repair, but its
diagnostic assertion also pins the new semantic contradiction rather than
detecting it.

## Design point that must be made explicit

The maintained spec currently contains both rules:

1. any round consuming inherited work, or displacing it, is not counted
   (`grammatical-structure.md:270-288`); and
2. no chain pays for another chain's spinning, and a genuine runaway is caught
   after exactly `cascades` created-work rounds, undelayed
   (`grammatical-structure.md:289-301`).

A mixed round makes those statements incompatible if an exemption is a voucher
for the entire round.  The implementation follows rule 1 and violates rule 2.
The diagnostic now calls the charged count “rounds of work it created,” which
assumes rule 2 while reporting rule 1's result.

The designer should settle mixed rounds explicitly.  Given the already
maintained ownership and undelayed-safety claims, the consistent answer appears
to be:

> An inherited drain or owned displacement exempts the servicing work; it does
> not exempt unrelated created work that happened to share its physical round.
> A mixed round containing non-exempt work is charged.

If instead an inherited run intentionally buys whole physical rounds, then the
spec must retract both “no chain pays for another” and “undelayed,” document an
up-to-`2k` safety delay, and make the diagnostic say **charged rounds** rather
than created-work rounds.  That would be a substantial semantic change from the
current maintained claims, not merely an implementation choice.

## Recommendation

Do not reduce an owned credit to a graph-wide boolean before the round is
classified.  Preserve which firing/chain supplied an exemption and whether the
same round performed non-exempt work.  A round should be free only when all work
that would otherwise charge it is covered by owned inherited servicing; an
independent ordinary reaction or another chain's created work must make the
mixed round count.

At minimum add these full-path controls:

1. three inherited drains beside an independent `cascades: 2` self-loop: the
   self-loop fires twice, the step throws after two physical rounds, and the
   third inherited run remains pending;
2. one inherited run displaced then drained beside the same self-loop: the two
   credits do not buy two extra self-loop firings;
3. the queue-only controls still drain arbitrarily deep inherited work without
   spending the limit; and
4. the existing Q2 control still proves displacement and later consumption are
   separately free when no unrelated created work shares those rounds.

The diagnostic should say “charged rounds” until the implementation can prove
that every charged count is exactly the set of rounds containing created work.

## Status of `REAUDIT19`

| prior item | result |
|---|---|
| drained run's stale same-wait allowance | **Fixed.** The per-wait record and `min(Displacements, Drains)` clamp close the cross-generation transfer. |
| five throws / six settles in seven | **Fixed and pinned.** The new test is the full real-path reproduction. |
| displace then drain receives both exemptions | **Preserved.** The Q2 control remains non-vacuous and passes. |
| physical-versus-charged diagnostic | **Mechanically fixed, semantically exposed.** Both numbers are now shown, but `counted` is mislabeled as created-work rounds because mixed rounds are uncharged. |

The per-wait `Credit` record is a genuine structural improvement.  The runtime
state sweep found no additional concrete lifetime drift in `membership`,
`consumes`, `active`, `stalled`, the adoption stack, or the accumulation fields
owned by `Split`.  The remaining defect is the later loss of credit ownership
at round aggregation, not another split table inside the wait.

## Verification

- Two temporary full-path probes reproduced the whole-round subsidy and were
  removed:
  - three inherited drains: the independent runaway fired 5 times at
    `cascades: 2`;
  - one displacement plus one drain: it fired 4 times at `cascades: 2`.
- Debug: **866 passed**, zero failed, zero skipped.
- Locked restore: passed.
- Exact Release solution build with `-warnaserror`: zero warnings and zero
  errors.
- Exact Release test/coverage gate: **866 passed**, with **100% line, branch,
  and method coverage**.
- `git diff --check 84f3712..be07539`: clean.
- The worktree was clean after probe removal and before this report was added.

The documented hand-aligned `dotnet format` whitespace differences remain
settled project style and are not a finding.  Formatting was not used as a gate.

The previously disclosed feature and pipeline work remains outside this
re-audit and is not repeated as findings.

## Recommended order

1. Ask the designer to settle mixed-round ownership explicitly.
2. Preserve credit ownership through round classification; do not let one
   chain's exemption erase unrelated work.
3. Add both independent-runaway controls and correct the diagnostic label.
