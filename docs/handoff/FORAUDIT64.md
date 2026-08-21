# For audit — REAUDIT63's five findings closed, and the nullary + discarded-kinds rulings actioned

> **Ledger** — `[R]` Requests re-audit of `15c94b3..26a6923`. REAUDIT63's five findings are each cut at a named commit; the nullary-signature gap finding 4 exposed is closed per `NULLARYRULING`, and the §4 structural-proxy sweep it opened is finished per `DISCARDEDKINDSRULING`. Names the ledgered deferrals the audit should not re-file.
> supersedes: none
> superseded by: none

**From:** the successor, at `26a6923`. `REAUDIT63` audited `ede64ce..15c94b3` and
did not sign off — four high findings and one medium. All five are cut, and the
one witness among them that could not be closed by a local repair (finding 4's
bare no-arg reference) opened a design question, which the designer ruled and I
then actioned. This asks for signoff on the range since.

## For audit

- **Range:** `15c94b3..26a6923` (14 commits).
- **Against:** `NULLARYRULING` and `DISCARDEDKINDSRULING` for the new work; and for
  the five repairs, the rulings each answers — `NOTHINGSPELLINGRULING`,
  `RECURSIVERETURN`, `MONOMORPHANDRETURN`, `FIVERULINGS`, `RETURNANDLITERALS`,
  `INFERENCEPASSVALIDATION`, and the container-identity ruling.

## §1 — REAUDIT63's five findings, each cut at a commit

| # | sev | finding | fix | what changed |
|---|---|---|---|---|
| 1 | high | `true`/`false` have no value sort — three positions admit them silently | `2dd1a18` | `Compilation.Inferred` reads the supplied literal's sort from the registry's `Denotes`, as `Truths` does — no second truth list in the checker |
| 2 | high | a delegate's returns are attributed to its enclosing function | `e0c79a4` | return ownership is its own object, independent of `Grammar.Function`; entering a delegate replaces the return owner while the transparent type container stays — per `NULLARYRULING` §5, "return ownership is a different axis" |
| 3 | high | omitted-return inference drops variable-bearing sites (`[]`, `nothing`) instead of unifying | `088c05b` | every non-null return sort accumulates through `Sort.Unify` with no `Render` gate; `Sort.Ground` runs after all sites and recursive constraints; render only for the diagnostic |
| 4 | high | a no-answer body never receives the action sort | `3e5f5af` | `Infer` constructs `Sort.Action` for a callable with no value-carrying site, owner found independent of site count, and the value filter refuses it — `f 5` fired here; the **bare** `f` witness needed the nullary work below |
| 5 | med | nested return sites compared in traversal order, reversing "earlier"/"later" | `225b6fa` | a callable's `Site`s are sorted by `Answer.Offset` **before** inference, so the established/blamed roles — chosen during inference, not at presentation — are truthful (`INFERENCEPASSVALIDATION` §4; the general form in `NULLARYRULING` §5) |

`REAUDIT63` gave explicit witnesses and controls for each; all are maintained as
tests.

## §2 — the nullary-signature gap, and the §4 sweep it opened

Finding 4's bare witness `var x => number = f` (with `function f { return; }`)
stayed silent after the local repair, because a nullary function had **no
signature at all** — `TryPattern` fails on a hole-less identifier, so `f` was filed
as a bare value name, its return type dropped. That is a design question, not a
bug, so it was consulted (`NULLARYSIGNATURE`) and ruled (`NULLARYRULING`):

- **`ac9423b`** — routing moves off `TryPattern` (hole count, a proxy for *is a
  function* that `EMPTYBRACKETS` guaranteed would go stale) onto member kind: a
  `Grammar.Function` files its `Signature` in `Overloads` keyed by its `[f]`
  pattern whatever its arity, keeping the name reservation it already had. A bare
  nullary reference reads as a **call** (Q1). A nullary overload set larger than
  one is refused (no cue at the use site). Finding 4's bare witness now fires as
  the `ActionInValue` case already built (Q3) — no change to the action machinery.
- **`b8a791b`** — the §4 sweep's fourth structural proxy: `Group.Flattened` read
  `part.Key` (correlation) where its siblings read `Group.Kind` (the declared
  fact). Now reads `Kind`.

`NULLARYRULING` §4 named the last three findings as one defect — a consumer
re-deriving a declared fact from a structural stand-in — and asked for a sweep.
That sweep is complete (§3 below); its stopping condition is `DISCARDEDKINDSRULING`
§4: **does the structure *constitute* the fact, or merely *correlate* with it?**

## §3 — the discarded-kinds ruling, actioned

The sweep surfaced two more of the same rule by a different mechanism — a fact
discarded then recovered, and a fact copied — consulted (`DISCARDEDKINDS`) and
ruled (`DISCARDEDKINDSRULING`):

- **Q1 `e1119fd`** — `Evaluator.Value` re-lexes through `Lexicon.Literal.Lex`
  instead of hand-rolling a second classifier (`text[0] is '"'`, `double.TryParse`)
  that could read a thousands-grouped or date-shaped run differently than the
  lexer. Both consumers now read the one authority.
- **§3 `faf26e6`** — `LexemeKind.Number` → `Literal`. The collapse of every literal
  to one kind is deliberate and kept; only the lying name changed.
- **Q2 `6bbb701`** — the registry marks `error` the bottom (`Descriptor.Bottom`,
  `SymbolTable.Bottom`); `Sort.Of` derives the scalars from `SuppliedTypes`, and
  the hand-kept `["number","text","truth"]` set is deleted.

## §4 — ledgered deferrals — please do not re-file these as gaps

Each is recorded in its ruling with a named trigger, deliberately not built now:

- **A literal's value is computed per evaluation** (`DISCARDEDKINDSRULING` §2). Q1
  removes the drift-prone classifier but a literal is still re-valued every tick,
  and under the exact-rational tower a `double` will be the *wrong* `number`
  (`0.1` ≠ ¹⁄₁₀). Successor: `Node.Literal` carries its value, minted once. Trigger:
  the numeric tower, which must touch this code regardless.
- **A named function cannot be spelled as a value** (`NULLARYRULING` §1 residue).
  No arity can today; a bare name cannot be that spelling since it must work for
  shaped functions too. Trigger: the first stdlib or user need to pass a named
  function.
- **`date` as a runtime value and sort** — it lexes and resolves but
  `Evaluator.Value` returns an unread-literal `Error` and `Sort.Infer` leaves it
  null; `date` is no prelude value/sort this pass. Carried by the same numeric/date
  work.

## Gate at `26a6923`

The project gate — CI `.github/workflows/build.yml`, local battery
`TYPECHECKERHANDOFF` §0:

- Debug and Release build clean, `-warnaserror`.
- `Passed! — Failed: 0, Passed: 1325` (Release, `--no-build`).
- Coverage **100%** line and branch (`/p:Threshold=100 /p:ThresholdType=line,branch`).
- Changed-file `dotnet format Ronin.sln --verify-no-changes --include <the six
  files>`: passed, formatted zero.
- `git diff --check` clean.

(Whole-solution `dotnet format` still reports the settled pre-existing
WHITESPACE/IDE1006 debt CI does not gate — `build.yml:55-64`, and not-a-finding in
`REAUDIT2`/`3`/`14`/`15`. None of it is in this range.)

The five findings each carry the witnesses and controls `REAUDIT63` required as
tests; Q2 was sabotage-verified three ways (`error`→scalar, scalar→named, marker
removed). The behaviour-preserving refactors — Q1, §3, `Group.Flattened` — cannot
be sabotaged into a failing test and say so in their commits.
