# For re-audit — REAUDIT71's finding closed

> **Ledger** — `[R]` Requests re-audit of `947ce04..ed49e6c`. REAUDIT71's one adjacent low finding — the diagnostic seam escaped only CR/LF, so Unicode NEL/LS/PS still entered the one-line `Unresolved` message — is cut: `Visible` is now defined by character semantics (every control character and the Unicode line/paragraph separators), covering every line boundary the lexer admits. The return-classification series stays closed; this is the diagnostic-rendering tail.
> supersedes: none
> superseded by: none

**From:** the successor, at `ed49e6c`. `REAUDIT70` fixed CR/LF in the `Unresolved`
message; `REAUDIT71` found the sibling — the Unicode line separators a text literal
also admits. It is reproduced and cut; this asks for signoff on the range since.

## For audit

- **Range:** `947ce04..ed49e6c` (2 commits).
- **Against:** `UNRESOLVEDRETURNRULING`.

## The finding, cut at a commit

| # | sev | finding | fix | what changed |
|---|---|---|---|---|
| 1 | low | `Visible` escaped only CR and LF, so a text literal carrying NEL (U+0085), LINE SEPARATOR (U+2028), or PARAGRAPH SEPARATOR (U+2029) still split the one-line `Unresolved` diagnostic | `1313538` | `Visible` is defined by character **semantics** rather than a chosen pair — it escapes every control character and the Unicode line/paragraph separators (`\n`/`\r` plainly, the rest as `\uXXXX`). `Reading.Words` / `Lexemes.Render()` stay semantic; only what a finding quotes is escaped |

## Maintained regression (the auditor's table)

`AnUnresolvedDiagnosticRendersEveryLineBoundaryVisibly` (`TypeAnnotations`), a Theory over an
unresolved reference whose text literal carries each boundary — LF, CR, CRLF, NEL, LS, PS. For every
row:

- exactly one `Unresolved`;
- `Diagnostics.Report` contains no raw boundary character (`\n`, `\r`, U+0085, U+2028, U+2029);
- the displayed form keeps the literal recognizable (the break shown visibly, content intact); and
- `Primary` still spans the complete original reference.

The single-LF REAUDIT70 case is folded in as the `\n` row; the REAUDIT69 trivia regression and the
single-word / composite controls are unchanged.

## Gate at `ed49e6c`

The project gate — CI `.github/workflows/build.yml`, local battery `TYPECHECKERHANDOFF` §0:

- Debug and Release build clean, `-warnaserror`.
- `Passed! — Failed: 0, Passed: 1340` (Release, `--no-build`).
- Coverage **100%** line and branch (`/p:Threshold=100 /p:ThresholdType=line,branch`).
- Changed-file `dotnet format Ronin.sln --verify-no-changes --include <files>`: passed.
- `git diff --check` clean.

Reproduced by execution before repair and sabotage-verified after: reverting `Visible` to CR/LF-only
lets NEL/LS/PS back into the report and the three Unicode rows fail; restored. This should close the
line-boundary axis for good — the seam is now the whole control-and-separator set, not an
enumeration of encodings — and `§8` (statement-initial `return`) remains a separate deferred ruling.
