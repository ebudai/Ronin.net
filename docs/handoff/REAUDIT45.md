# Re-audit 45 — shared soundness and deterministic R7 safeguards

**Re-audited:** `24249e4..d87aa7d`

**Date:** 2026-08-05

## Result

**No findings. Sign-off on the REAUDIT44 incorporation.**

All three findings are resolved at their actual boundaries, and each safeguard
rejects the regression it was added to prevent. The registry filters once,
before every classification, through the same `Rules.Sound` predicate used by
diagnostic validation. The empirical cheaper-reading and first-hole R7 tests
are restored as independent semantic facts. The scaling guard no longer reads
wall-clock time and its allocation signal fails when refinement derivation is
moved back inside the name loop.

No adjacent regression was found in pattern soundness, R7 relationship
derivation, registry generation, diagnostic rendering, or lookup identity.

## Disposition of REAUDIT44

| Prior finding | Re-evaluation |
|---|---|
| 1. Registry publishes relationships from forbidden patterns | **Resolved.** `Rules.Sound` is shared, and `Glue.Registry` filters its input once before glue, free-pattern, anchor-prefix, and refinement classification. Removing that filter makes the maintained cross-path test fail. |
| 2. Ordering rewrite deleted two R7 safeguards | **Resolved.** Both original facts are restored. The cheaper-reading test exercises the resolver and compares readings and costs; the first-hole source test includes the matching name and requires exactly one R6 finding. Re-enabling first-hole R7 makes that test fail with the unwanted second finding. |
| 3. Wall-clock scaling test is flaky | **Resolved.** The test uses thread-local allocation over a deliberately allocating refinement table. It is independent of scheduler time and detects the original per-name derivation shape. |

## Adversarial checks

### Registry soundness

The filter is applied to the materialised `declared` table in
`Compiler/Diagnostics/Glue.cs`, so every downstream section consumes the same
eligible set. It is not attached only to the R7 call that originally exposed
the mismatch. `Rules.Sound` is exactly the inverse of the structural predicate
used by validation: reserved `old` segments, word operators, and protected
injection words used as glue are all excluded.

I temporarily removed `.Where(Rules.Sound)` from `Glue.Registry`. The focused
test failed because the forbidden `send (_) to otherwise (_)` pair again
produced a refinement-prefix count of one. The mutation was reverted.

### Restored R7 semantics

I temporarily changed the refinement scan from
`shorter.Anchor.Count + 1` back to `shorter.Anchor.Count`. The restored source
test failed with the exact recurrence it was written for:

```text
AnchorPrefix
NameAbsorbsRefinement
```

instead of the single `AnchorPrefix` finding. The mutation was reverted. The
separate resolver test also retains the stronger premise behind the blanket
rule: both readings resolve, but the absorbing name is strictly cheaper and
wins silently.

### Deterministic scaling guard

I temporarily moved `Rules.Refinements(patterns).ToLookup(...)` from before the
name loop back inside it. The replacement allocation test failed:

```text
one name allocated 295544 bytes and twenty allocated 3062936
```

against its `many < one * 9` bound. That is the precise cubic/per-name
derivation regression the former timing test intended to constrain, without a
wall clock or cross-test contention. The mutation was reverted.

## Verification performed

- Inspected both incorporating commits and their complete production/test diff.
- Focused `NameShadowing`, `GlueRegistry`, `Shadows`, and finding-renderer suite:
  **86 passed**.
- Two consecutive ordinary Debug full-suite runs: **1,086 passed** each.
- `dotnet restore --locked-mode`: passed.
- `dotnet build --no-restore --configuration Release -warnaserror`: passed with
  zero warnings and zero errors.
- Exact Release coverage gate: **1,086 passed, 0 failed, 0 skipped; 100% line,
  branch, and method coverage**.
- `git diff --check`: passed; `git diff -- Compiler Test` is empty after all
  mutation cleanup.

The pre-existing `docs/spec` edits and untracked handoff/design files were
preserved.

## Scope of sign-off

This signs off the REAUDIT44 incorporation and the adjacent implemented
registry/R7 surface. It does not declare separately disclosed future work
complete:

- the lookup representation/runtime work needed before the earlier finding 9
  can be completed;
- the future differential declaration check and multi-word operator work;
- owner-authorized warning suppressions reserved for their dedicated round.

The documented hand-aligned `dotnet format` whitespace differences remain
settled project style and are not a finding.
