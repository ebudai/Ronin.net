# For re-audit — REAUDIT72's finding closed

> **Ledger** — `[R]` Requests re-audit of `ed49e6c..c928691`. REAUDIT72's one adjacent low finding — the visible encoding passed a literal backslash through, so a carried break and source that spells its escape quoted identically — is cut: the backslash introducer is doubled, making the encoding injective. This should close the diagnostic-escaping axis: the seam is now a complete escape (backslash, control characters, and Unicode line/paragraph separators).
> supersedes: none
> superseded by: none

**From:** the successor, at `c928691`. `REAUDIT71` made every line boundary render
visibly; `REAUDIT72` found that the escape was lossy — it did not escape its own
introducer. It is reproduced and cut; this asks for signoff on the range since.

## For audit

- **Range:** `ed49e6c..c928691` (2 commits).
- **Against:** `UNRESOLVEDRETURNRULING`.

## The finding, cut at a commit

| # | sev | finding | fix | what changed |
|---|---|---|---|---|
| 1 | low | `Visible` escaped a carried control/separator with a backslash sequence but passed an existing backslash through, so an actual LF and source spelling `\n` (two characters) quoted identically — the encoding was lossy | `4ca3358` | the backslash introducer is doubled — a literal `\` renders as `\\` — so a carried break and its spelled form are distinct (`hello\nworld` vs `hello\\nworld`). Standard escape-the-escape, same single pass; `Reading.Words` / `Lexemes.Render()` stay semantic |

## Maintained regression (the auditor's pairs)

`AnUnresolvedDiagnosticDistinguishesACarriedBreakFromLiteralBackslash` (`TypeAnnotations`), a Theory
pairing an actual carried boundary against source that spells its escape — LF vs `\n`, CR vs `\r`,
and NEL / LS / PS vs their `\uXXXX` spellings. For each pair:

- both emit exactly one `Unresolved`;
- their quoted `Words` differ (the encoding is injective); and
- both reports remain one physical line.

The REAUDIT71 line-boundary Theory, the REAUDIT69 trivia case, and the single-word / composite
controls are unchanged.

## Gate at `c928691`

The project gate — CI `.github/workflows/build.yml`, local battery `TYPECHECKERHANDOFF` §0:

- Debug and Release build clean, `-warnaserror`.
- `Passed! — Failed: 0, Passed: 1345` (Release, `--no-build`).
- Coverage **100%** line and branch (`/p:Threshold=100 /p:ThresholdType=line,branch`).
- Changed-file `dotnet format Ronin.sln --verify-no-changes --include <files>`: passed.
- `git diff --check` clean.

Reproduced by execution before repair and sabotage-verified after: removing the backslash arm makes
the pairs collide and the test fails; restored. The `Unresolved` diagnostic's visible encoding is now
a complete, injective escape — backslash, control characters, and Unicode line/paragraph separators —
so the diagnostic-rendering tail should be done. `§8` (statement-initial `return`) remains a separate
deferred ruling.
