# For re-audit — REAUDIT70's finding closed

> **Ledger** — `[R]` Requests re-audit of `46c02b8..947ce04`. REAUDIT70's one adjacent low finding — a newline inside a text literal (token content, not trivia) still entered the one-line `Unresolved` diagnostic — is cut: the finding renders diagnostic-safe (`\r`/`\n` shown visibly), `Reading.Words` and `Lexemes.Render()` left semantic. The return-classification series stays closed; this is the diagnostic-rendering tail.
> supersedes: none
> superseded by: none

**From:** the successor, at `947ce04`. `REAUDIT69` fixed inter-token trivia in the
`Unresolved` message; `REAUDIT70` found the sibling case — a line break carried *by* a
text-literal token, which `Lexemes.Render()` rightly keeps. It is reproduced and cut;
this asks for signoff on the range since.

## For audit

- **Range:** `46c02b8..947ce04` (2 commits).
- **Against:** `UNRESOLVEDRETURNRULING`.

## The finding, cut at a commit

| # | sev | finding | fix | what changed |
|---|---|---|---|---|
| 1 | low | a newline inside a text literal — `"hello`⏎`world"` — is the token's content, so `Lexemes.Render()` keeps it (correctly) and it still split the one-line `Unresolved` message | `f2436e1` | the two renderings are separated: `Reading.Words` stays the semantic canonical form (a token's own break may remain), and only what the finding **quotes** is made diagnostic-safe — a new `Visible` escapes `\r`/`\n` to their two-character forms. `Lexemes.Render()` is untouched for its other consumers |

## Maintained regression (the auditor's coverage)

`AnUnresolvedReferenceWithAMultilineLiteralStaysOneLine` (`TypeAnnotations`), on an unresolved
reference containing a multiline text literal `send "hello`⏎`world" nope`:

- exactly one `Unresolved`;
- `Diagnostics.Report` contains neither `\r` nor `\n` from token content;
- the displayed form keeps the literal recognizable (`hello\nworld`, the break shown visibly, the
  quotes and content intact); and
- `Primary` still spans the complete original multiline reference (its source slice contains the
  newline).

The REAUDIT69 trivia regression is unchanged, and single-word / bracketed-composite controls still
render as before. The `Reading.Words` comment is corrected to say it removes trivia line breaks, not
breaks a token carries.

## Gate at `947ce04`

The project gate — CI `.github/workflows/build.yml`, local battery `TYPECHECKERHANDOFF` §0:

- Debug and Release build clean, `-warnaserror`.
- `Passed! — Failed: 0, Passed: 1335` (Release, `--no-build`).
- Coverage **100%** line and branch (`/p:Threshold=100 /p:ThresholdType=line,branch`).
- Changed-file `dotnet format Ronin.sln --verify-no-changes --include <files>`: passed.
- `git diff --check` clean.

Reproduced by execution before repair and sabotage-verified after: making `Visible` a no-op lets the
literal's newline back into the report and the test fails; restored. Nothing is left open on this
axis — trivia breaks (REAUDIT69) and token-carried breaks (REAUDIT70) are both rendered visibly by
the one `Visible` seam; `§8` (statement-initial `return`) remains a separate deferred ruling.
