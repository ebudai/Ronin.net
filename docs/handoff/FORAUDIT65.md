# For re-audit — REAUDIT64's four findings closed

> **Ledger** — `[R]` Requests re-audit of `26a6923..740cb18`. REAUDIT64's four high-severity findings are each cut at a named commit, with the maintained regressions the audit asked for. The nullary reference is now a call in the resolver — one production every consumer reads — so findings 1 and 2 close together; finding 4's digit-alphabet note is now a ruling and enforced.
> supersedes: none
> superseded by: none

**From:** the successor, at `740cb18`. `REAUDIT64` audited `15c94b3..26a6923` and did
not sign off — four high findings, all confirmed by reproduction before repair. Each
is cut; this asks for signoff on the range since.

## For audit

- **Range:** `26a6923..740cb18` (7 commits).
- **Against:** `NULLARYRULING`, `RETURNANDLITERALS`, `DISCARDEDKINDSRULING`,
  `FIVERULINGS`, and `NUMERALALPHABET`.

## The four findings, each cut at a commit

| # | sev | finding | fix | what changed |
|---|---|---|---|---|
| 1 | high | a bare nullary function stayed a `Node.Name`, so dependency analysis, cycle detection, and the evaluator did not treat it as a call | `3011aec` | the resolver offers `Node.Call([f], [])` **in place of** the name (guarded to value position, keyed off a scoped `Nullaries` set the checker fills and that merges like `Names`); one production reaches the inference edges, `NeverAnswers`, and the evaluator's invoke. The `Inferred` bridge is deleted |
| 2 | high | the nullary bridge split a rendered name and crashed `Pattern` on a composite keyword (`ready part of world`) | `3011aec` | the `[f]` is built from the **canonical lexemes** in `Atoms`, so `part of` is one segment; the space-splitting bridge is gone (subsumed by finding 1) |
| 3 | high | an explicitly value-returning function could bare-return or fall through with no finding | `75d0561` | folded into `Returns`: a written `=> T` that carries no value out is `Unanswered` — fired only when the body fully resolves, so a `return nope` whose value is unresolved is left to its own walk |
| 4 | high | the re-lexed numeric evaluator threw on a lexer-classified numeric outside invariant `double.Parse` (Arabic-Indic `١`) | `65f3dff`, then `740cb18` | first a transitional `double.TryParse`→`Error` (no throw). Then the designer ruled the digit alphabet (`NUMERALALPHABET`): the lexer now enforces ASCII `0-9`, so `١` is no longer a `Numeric` at all, the transitional guard is removed, and `Value` reads via `double.Parse` again — the lexer, not the evaluator, is the authority |

## Maintained regressions (each finding's witnesses and controls, as tests)

- **Finding 1** (`TypeAnnotations`): dependency order in both declaration orders past a
  shifting `padding`; nullary self- and mutual recursion as `NeverAnswers`; a grounding
  base settles the group; an ordinary value name still reads as a value. Runtime
  invocation (`Evaluations`): a bare nullary reference invokes the declaration and
  returns `5d`, not `«f» is not declared`.
- **Finding 2** (`TypeAnnotations`): the composite-keyword nullary and a
  whitespace-normalised multi-word nullary both check without crashing.
- **Finding 3** (`TypeAnnotations`): bare return, fall-through, a valid value return,
  a nested-block value return, and the omitted-return action control; the unresolved
  `return nope` stays clean. `Unanswered` joins the every-kind corpus and the golden
  wording file.
- **Finding 4** (`Literals`): `12,345,678.99987` is one number and `١` is no literal at
  all — the lexer-level alphabet test that replaced the removed `Value`-level `١` case.

## The digit-alphabet note — now settled, not a gap

REAUDIT64 finding 4 flagged that the lexer admitted every `char.IsDigit` (Unicode
decimal digits) while the evaluator read only `0-9`. That is now decided, with the
designer, in `NUMERALALPHABET` (`[V]`): **source numerals are ASCII `0-9`** in the
`12,345,678.99987` form (one convention resolves the locale ambiguity by fiat; Unicode
digits only add mixing and lookalikes). The lexer is the authority and enforces it
(`740cb18`); culture-aware parsing (`12.443,44929`) is a runtime op keyed on an explicit
locale, a later slice. The remaining `double` value — `0.1` not exactly ¹⁄₁₀ — is the
exact-rational numeric tower's, ledgered there (`DISCARDEDKINDSRULING` §2), not this.

## Gate at `740cb18`

The project gate — CI `.github/workflows/build.yml`, local battery
`TYPECHECKERHANDOFF` §0:

- Debug and Release build clean, `-warnaserror`.
- `Passed! — Failed: 0, Passed: 1330` (Release, `--no-build`).
- Coverage **100%** line and branch (`/p:Threshold=100 /p:ThresholdType=line,branch`).
- Changed-file `dotnet format Ronin.sln --verify-no-changes --include <files>`: passed.
- `git diff --check` clean.

Each finding was reproduced by execution before repair and sabotage-verified after:
finding 4 both mechanisms (revert to `Parse` throws on the Unicode digit; revert the
lexer `Digit` to `char.IsDigit` and `١` lexes as a number); finding 3 both ways (neuter
the finding, and remove the resolve guard); findings 1 & 2 by neutering `Nullary` (four
tests fail). All restored.
