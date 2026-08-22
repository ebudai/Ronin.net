# For re-audit — REAUDIT66's finding closed

> **Ledger** — `[R]` Requests re-audit of `76d5dea..d908c25`. REAUDIT66's one adjacent finding — the unresolved value-return classifier read only the outer reference's first lexeme — is cut: `Reading.Answering` is now depth-aware, matching the resolved `Called` walk, with the nested, grouped, and deeper witnesses the audit asked for.
> supersedes: none
> superseded by: none

**From:** the successor, at `d908c25`. `REAUDIT66` closed all three REAUDIT65
findings and found one adjacent medium — a follow-on from finding 2's fix, that
`Reading.Answering` was shallow. It is reproduced by execution and cut; this asks
for signoff on the range since.

## For audit

- **Range:** `76d5dea..d908c25` (2 commits).
- **Against:** `RETURNANDLITERALS`.

## The finding, cut at a commit

| # | sev | finding | fix | what changed |
|---|---|---|---|---|
| 1 | med | `Reading.Answering` recognised only a reference *beginning* with `return`, so an unresolved value-return **nested** in another call (`send (return nope)`) was missed and `Unanswered` wrongly fired | `f0860b7` | `Answering` reads the whole flattened reference for the `SymbolTable.Answer` anchor (no longer the copied word `"return"`) followed by a value, at **any** depth — a value being anything but a close, separator, or `=`. It now covers the same structural positions the resolved `Called` walk does |

## Maintained regressions (the auditor's controls, as tests)

Extending the `Unanswered` test in `TypeAnnotations`:

- direct unresolved `return nope` — clean (unchanged);
- grouped `(return nope)` — clean;
- `send (return nope)` and `send (send (return nope))` (nested more than one call deep) —
  clean;
- the resolved `send (return 5)` control — clean, by the ordinary site path;
- unrelated unresolved statements before, after, and inside a transparent block, plus an
  unrelated unresolved **call** `send (nope)` — still `Unanswered`;
- the empty and bare-return bodies — still `Unanswered`;
- a bare **nested** `send (return)` — still `Unanswered` (the anchor is followed by a
  close, not a value, so it is no value-return).

## Gate at `d908c25`

The project gate — CI `.github/workflows/build.yml`, local battery
`TYPECHECKERHANDOFF` §0:

- Debug and Release build clean, `-warnaserror`.
- `Passed! — Failed: 0, Passed: 1332` (Release, `--no-build`).
- Coverage **100%** line and branch (`/p:Threshold=100 /p:ThresholdType=line,branch`).
- Changed-file `dotnet format Ronin.sln --verify-no-changes --include <files>`: passed.
- `git diff --check` clean.

Reproduced by execution before repair and sabotage-verified after: reverting `Answering`
to the shallow first-lexeme check makes the nested witnesses fail; restored. No item is
left open; the digit-alphabet question is ruled and enforced, with only the exact-rational
value deferred to the numeric tower.
