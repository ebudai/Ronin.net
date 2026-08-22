# For re-audit — REAUDIT67's finding closed

> **Ledger** — `[R]` Requests re-audit of `d908c25..acb0aea`. REAUDIT67's one adjacent finding — the depth-aware `Answering` scan mistook `return` inside a legal multi-word name for a return call — is cut: the scan gains a word-run boundary guard, so a `return` counts only where it starts a word-run, the same boundary the resolver's `ReadsAs` refusal turns on.
> supersedes: none
> superseded by: none

**From:** the successor, at `acb0aea`. `REAUDIT67` closed REAUDIT66's nested-return
finding and found one adjacent high — a follow-on from that fix, that the flattened
scan lost name boundaries. It is reproduced by execution and cut; this asks for
signoff on the range since.

## For audit

- **Range:** `d908c25..acb0aea` (2 commits).
- **Against:** `RETURNANDLITERALS`, and the `SymbolTable.Answer` / `ReadsAs` name-vs-pattern
  law in `Resolver.cs` / `Rules.cs`.

## The finding, cut at a commit

| # | sev | finding | fix | what changed |
|---|---|---|---|---|
| 1 | high | the depth-aware `Answering` scan treated the word `return` **inside a legal multi-word name** (`customer return policy`) as a return call, wrongly suppressing `Unanswered` and leaving a written `=> number` promise with zero findings | `2e5c0c1` | `return` anchors a pattern, not a nullary supply — no name may *begin* with it, but a name may *contain* it medially. A `Name` is a maximal run of word lexemes, so the scan now counts `return` only where it **starts** a word-run (index 0, or after a non-word lexeme); a `return` after a word is interior to a name and no return. This is provably the same boundary the `ReadsAs` refusal uses (`return policy` reads as the pattern and is refused; `customer return policy` does not and is a legal name) |

## Why the structural guard, not the resolver

The resolved `Called` walk already respects name boundaries — a name is a `Node.Name`, a
return a `Node.Call` of `SymbolTable.Answer`. But it needs a resolved tree, and the failing
case (an unresolved value) produces none: `Resolution` exposes no partial tree on `NoParse`,
and `Match` yields no `Answer` call when the hole does not resolve. The word-run boundary is
the resolution-free equivalent — a `Name` is a maximal `Word`-run, so the anchor is a real
`return` exactly when it opens one — and it reads the whole flattened reference, so a nested
return at any depth is still found.

## Maintained regressions (the auditor's controls, as tests)

Extending the `Unanswered` test in `TypeAnnotations`:

- a declared multi-word name containing `return` (`customer return policy`) inside an
  otherwise unresolved outer call, beside a bare return — now `Unanswered` (the witness);
- the same name in a **resolved** call — `Unanswered` (it is a `Name`, never an answer call);
- another accepted `return`-medial name shape (`annual return summary`) — `Unanswered`;
- the equivalent unresolved outer call with a name **not** containing `return` — `Unanswered`;
- and unchanged: direct, grouped, nested `send (return nope)`, deeper, resolved `send (return
  5)`, bare `send (return)`, the unrelated-unresolved and empty/bare bodies.

## Gate at `acb0aea`

The project gate — CI `.github/workflows/build.yml`, local battery
`TYPECHECKERHANDOFF` §0:

- Debug and Release build clean, `-warnaserror`.
- `Passed! — Failed: 0, Passed: 1332` (Release, `--no-build`).
- Coverage **100%** line and branch (`/p:Threshold=100 /p:ThresholdType=line,branch`).
- Changed-file `dotnet format Ronin.sln --verify-no-changes --include <files>`: passed.
- `git diff --check` clean.

Reproduced by execution before repair and sabotage-verified after: dropping the word-run
boundary guard makes the name's `return` suppress again and the witness fail; restored. No
item is left open.
