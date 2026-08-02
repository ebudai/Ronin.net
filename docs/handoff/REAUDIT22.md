# Re-audit 22 — `REAUDIT21` incorporation

**Audited:** `192c1e4` through `253d010`

**Date:** 2026-08-02

## Result

**Sign-off for the `REAUDIT21` incorporation.  No findings.**

The displacement-cap regression is now real and non-vacuous, the rewritten
`DEFERRALCREDIT-UNOBSERVABLE.md` accurately retracts its former conclusion, and
the stale `advanced` summary is gone.  No behavioral or documentation defect
was found in this incorporation.

This sign-off is for the audited runtime/accounting scope.  Previously disclosed
feature and pipeline work remains outside it and is not silently promoted to
complete here.

## What was rechecked

### Positive displacement-cap exhaustion

`AndOneParkedRunForgivesOneDisplacementNotEveryOneAfterIt`
(`Test/Unit/Waiting.cs:1361-1423`) preserves every load-bearing part of the
`REAUDIT21` reproduction:

- one run is parked through ordinary head execution and its generated wait
  count is asserted to be one;
- the wait is shut during the re-arm gap, so the tail cannot drain between head
  firings;
- the same inherited run is therefore displaced repeatedly;
- the head body count observes the semantic difference directly; and
- the exception pins both the three charged rounds and four physical rounds.

The setup-only `looping` switch does not manufacture the count or credit under
test.  It separates the parking step from the looping step; all head/wait/tick
transitions that exercise the scheduler still travel through ordinary graph
writes and rounds.

### Sabotage

I temporarily removed only:

```csharp
--credit.Displacements;
```

The new focused test failed exactly as intended:

```text
Assert.Equal() Failure: Values differ
Expected: 2
Actual:   3
```

The decrement was restored before all other verification.  This closes the
specific gap from `REAUDIT21`: the suite can no longer pass if displacement
credit becomes unlimited while the inherited run remains parked.

### Corrected disclosure and handoff routing

`docs/handoff/DEFERRALCREDIT-UNOBSERVABLE.md` now:

- states in its title and opening that the previous conclusion was false;
- records the missing closed-wait condition;
- gives the measured two-versus-three head-body result and four-versus-six
  physical-round result;
- withdraws the offer to remove the counter; and
- identifies the positive regression now protecting the `2k` rule.

`docs/handoff/README.md` also routes readers through the refutation rather than
leaving the old title available as an apparently current design statement.

### Runtime cleanup

The obsolete “Whether this round took a run...” XML summary was removed from
`servicing`.  The remaining summary now describes the set it is attached to.

No runtime behavior was changed in this commit beyond that documentation
cleanup; the firing-owned credit implementation signed off above remains the
one audited in `REAUDIT21`.

## Verification

- Targeted decrement sabotage: the new test failed `expected 2, actual 3`.
- Sabotage restored and worktree returned clean before verification.
- Debug: **868 passed**, zero failed, zero skipped.
- Locked restore: passed.
- Exact Release solution build with `-warnaserror`: zero warnings and zero
  errors.
- Exact Release test/coverage gate: **868 passed**, with **100% line, branch,
  and method coverage**.
- `git diff --check 192c1e4..253d010`: clean.
- The worktree was clean before this report was added.

The documented hand-aligned `dotnet format` whitespace differences remain
settled project style and are not a finding.  Formatting was not used as a gate.
