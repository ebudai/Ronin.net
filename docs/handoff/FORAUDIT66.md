# For re-audit — REAUDIT65's three findings closed

> **Ledger** — `[R]` Requests re-audit of `740cb18..76d5dea`. REAUDIT65's three findings — the ASCII-lexer non-progress hang, the over-broad Unanswered guard, and the refused declaration installed as a call — are each cut at a named commit, with the full-lexer, unrelated-unresolved, and both-order regressions the audit asked for.
> supersedes: none
> superseded by: none

**From:** the successor, at `76d5dea`. `REAUDIT65` confirmed REAUDIT64's four direct
repairs but found three new findings — two of them regressions the REAUDIT64 fixes
introduced. Each is reproduced by execution and cut; this asks for signoff on the
range since.

## For audit

- **Range:** `740cb18..76d5dea` (4 commits).
- **Against:** `NUMERALALPHABET`, `RETURNANDLITERALS`, and `NULLARYRULING`.

## The three findings, each cut at a commit

| # | sev | finding | fix | what changed |
|---|---|---|---|---|
| 1 | high | narrowing `Numeric` to ASCII left a Unicode digit consumed by no token, so `Lexer.Lex` spun at a cursor that never advanced — an infinite loop | `c8c2148` | `Word`'s leading-digit exclusion aligns with the ruled alphabet (shared `Numeric.Digit`, ASCII `0-9`, not `char.IsDigit`): a Unicode digit is not a number and so may begin a name, rather than being a run no token takes. Rejected-as-numeric no longer means rejected-by-every-token |
| 2 | high | the written-return `Unanswered` check suppressed on **any** unresolved reading, so an unrelated unknown statement erased the finding | `784dd96` | `Reading` carries `Answering` — whether its reference is syntactically `return (_)`, from the lexemes before resolution — and the guard suppresses only on an unresolved reading that is `Answering`; `return nope` stays its own walk, `nope;` does not hide the missing answer |
| 3 | med | a declaration `Cell` refused (a supplied or already-declared name) was still filed as the resolver's authoritative nullary call | `ec5aa27` | `Cell` returns whether it installed (FALSE on refusal); the signature and nullary filing are gated on it, so a refused `function true`/`f` leaves the supplied literal or prior datum its meaning |

## Maintained regressions (each finding's witnesses and controls, as tests)

- **Finding 1** (`Literals`): the full `Lexer.Lex` takes a lone `١` as a `Word`, a `5١` as
  `Numeric(5)` then `Word(١)`, `a١b` as one word, and a compilation containing `١`
  terminates — the loop the old `Literal.Lex`-only test bypassed.
- **Finding 2** (`TypeAnnotations`): `nope; return` and `return; nope` both report
  `Unanswered`, as does the same in a transparent nested block; the empty and bare-return
  bodies still do; a valid value return and the unresolved `return nope` control stay clean.
- **Finding 3** (`TypeAnnotations`): the supplied `true` and `nothing` keep their meaning
  under a refused function; an existing datum keeps its type and the mismatch shows; both
  declaration orders; a valid nullary still installs its call.

## Gate at `76d5dea`

The project gate — CI `.github/workflows/build.yml`, local battery
`TYPECHECKERHANDOFF` §0:

- Debug and Release build clean, `-warnaserror`.
- `Passed! — Failed: 0, Passed: 1332` (Release, `--no-build`).
- Coverage **100%** line and branch (`/p:Threshold=100 /p:ThresholdType=line,branch`).
- Changed-file `dotnet format Ronin.sln --verify-no-changes --include <files>`: passed.
- `git diff --check` clean.

Each finding was reproduced by execution before repair and sabotage-verified after:
finding 1 by reverting `Word` to `char.IsDigit` (the full-lexer test times out — exit 124);
finding 2 by the broad "any unresolved reading" guard (the witnesses go silent); finding 3
by an ungated `Cell` (the refused declaration files again). All restored. No item is left
open — the digit-alphabet question REAUDIT64 raised is ruled (`NUMERALALPHABET`) and
enforced, with only the exact-rational value deferred to the numeric tower.
