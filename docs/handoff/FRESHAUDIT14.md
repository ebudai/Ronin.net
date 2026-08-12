# Fresh audit 14 — the distinguisher cannot see past the display cap

**Re-audited:** `214290d..695063d`, the two commits addressing
`FRESHAUDIT13`.

**Result:** no sign-off. All three reported reproductions are fixed:

- the 42-term, two-reading source now receives both one-pair repairs without
  growing through the unambiguous argument;
- `exit` is handled only after version/id classification, explicit-null ids are
  requests, and Boolean ids are refused;
- the next candidate's full lexeme charge is checked before it is resolved, so
  the stated budget is now strict.

The new distinguishing-span order has a different completeness boundary. It
decides whether a target span distinguishes the reading by comparing it only
with `Resolution.Alternatives`, which contains the five readings retained for
display rather than every reading counted by `Resolution.Total`. When all five
retained readings share one cheap choice and a more expensive sixth reading
does not, the span that excludes the hidden reading is classified as shared and
deferred behind every narrower shared subtree.

A production-reachable expression with 16 readings demonstrates both failure
modes. With a 30-term unambiguous tail it is 90 lexemes, takes about ten seconds
and 3.13 GB of cumulative allocation, and returns only four repairs for five
displayed readings. With a 55-term tail it is 140 lexemes; the useful span is
ranked 65th, so its grow prefix reaches 270 lexemes and all five repairs vanish
at the resolver's 256-lexeme ceiling. Every displayed reading has an explicitly
verified four-pair repair of only 148 lexemes.

This is one high-severity finding. All maintained gates are green: locked
restore, warning-as-error Release build, all 1,182 tests in Debug, all 1,182
tests in the exact Release coverage gate, 100% line/branch/method coverage for
`Ronin` and `Ronin.Server`, and the transitive NuGet vulnerability audit.

The deliberately open `FRESHAUDIT8` findings 6 and 7 remain outside this
re-audit and are not counted again. The programmer's accepted K-fold cost
residual and decision not to maintain the twenty-child case are also not
findings here: this reproduction has only four ambiguous choices, while the
additional size is entirely unambiguous.

No production, maintained test, or existing documentation file was changed
during this re-audit. This file is the only repository artifact added.

---

## Disposition of `FRESHAUDIT13`

| prior finding | re-audit result |
|---|---|
| 1. narrowest-first growth accumulates irrelevant brackets | **Closed on the exact two-reading reproduction; incomplete when alternatives are capped.** The useful wide call is now prioritized and the maintained allocation guard passes. The same false growth behavior remains when the competing reading is outside `Resolution.Alternatives`; see finding 1. |
| 2. `exit` bypasses envelope validation and id value stands for presence | **Closed.** Envelope classification precedes `exit`; wrong-version notifications are dropped, id-carrying exits are refused without stopping, later messages are read, null-id requests are served, and Boolean ids receive `InvalidRequest` with a null response id. |
| 3. the lexeme counter spends past its stated limit | **Closed.** `bracketed.Count > budget - spent` is tested before the charge, and the six-lexeme boundary case distinguishes the fixed behavior from the former overshoot. |

---

## 1. Hidden alternatives make a necessary span look shared, recreating the cost and ceiling failures

**Severity: high — a legal 140-lexeme production expression reports five
readings but offers zero repairs/actions, although every displayed reading has
a verified 148-lexeme repair. A 90-lexeme version blocks diagnostics/actions for
about ten seconds and still omits one repair.**

`Repairs.For` passes only `ambiguity.Alternatives` into `Search` at
`Compiler/Resolution/Repair.cs:135-162`. `Selecting` then constructs competitor
range sets only from that list at lines 214-224 and gives first priority to a
span absent from at least one of those competitors at lines 237-239.

That list is deliberately capped for presentation. `Resolution.Total` can be
larger, and `Resolution.Bounded` says when alternatives were dropped. A span
present in all five displayed trees is therefore not necessarily present in
all competing readings. The ordering treats “not distinguished from the five I
can see” as “distinguishes nothing,” even though final verification still has
to eliminate the hidden trees the resolver sees.

Production reproduction:

```ronin
function send (n => Number) { return n; }
function send (n => Number) to (m => Number) { return n; }
function print (n => Number) { return n; }
function print (n => Number) to (m => Number) { return n; }
var a to b => Number;
var a => Number;
var b => Number;
var x => Number;
var y => Number;

var result = (send a to b)
           + (print send x to y)
           + (print send x to y)
           + (print send x to y)
           + TAIL;
```

`TAIL` is an ordinary sum of `a` repeated N times. The ambiguity has four
independent binary choices:

- `send a to b` is either `send (a to b)` or `send (a) to b`;
- each of the three `print send x to y` components has the two structural
  readings already covered by the duplicate-rendering regression.

The product is 16 readings. The first `send` reading costs one lookup less than
the second, so all five retained alternatives use `send «a to b»`; the
`send «a» to «b»` competitors are among the eleven readings not retained.

For every displayed target, bracketing `a to b` is necessary to rule out that
hidden left-hand reading. But its range occurs in all four *retained*
competitors, so line 237 classifies it as shared. The unambiguous tail's
one-lexeme names are shared too and sort before the three-lexeme `a to b` span.
The search keeps appending them even after the brackets distinguishing the
visible readings have been added, because the hidden left-hand competitor still
makes the candidate ambiguous.

Two measured boundaries:

| tail terms | source lexemes | total / displayed | useful-span position | grow-prefix lexemes | repairs | production compilation | cumulative allocation |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 30 | 90 | 16 / 5 | 40th of 76 | 170 | 4 | 9.93 s | 3,127 MB |
| 55 | 140 | 16 / 5 | 65th of 126 | 270 | 0 | 10.19 s | 2,065 MB |

Allocation is `GC.GetTotalAllocatedBytes` churn, not retained heap. At 30 tail
terms, the four returned repairs contain 8, 8, 8, and 36 insertions; the budget
is exhausted while trimming accumulated shared spans and before reaching the
fifth target. `Language.Actions` recomputes the file in 9.49 seconds and exposes
four actions.

At 55 terms, the prefix reaches 270 lexemes before the useful span can be
verified. `Resolver.MaxLexemes` is 256, so no target can be selected;
`Compilation` takes 10.19 seconds and reports one ambiguity with 16 total/five
displayed readings but zero repairs. `Language.Actions` takes another 9.83
seconds and returns zero actions.

The absent answers are not large. Exhaustively applying the two choices for
each of the four ambiguous components produces one unique four-pair bracketing
for every displayed target. At 55 tail terms each edited expression is 148
lexemes, resolves uniquely, and its stripped tree structurally matches exactly
one of the five target alternatives. All five were verified independently.

This is not the accepted “fully repairing K independent ambiguities is O(K)”
residual. There are four ambiguous choices and only four necessary bracket
pairs; the 55 tail terms add no readings and require no repair. The extra work
and 61 surplus pairs arise solely because the priority calculation cannot see
the competitor that makes `a to b` useful.

**Recommendation:** do not infer a universal competitor property from the
presentation-capped `Resolution.Alternatives`. The repair layer needs either
decision/competitor information that survives the cap or an ordering strategy
whose correctness and candidate-size bound do not depend on enumerating every
rival tree. In particular, `Resolution.Bounded` must prevent “shared by all
retained alternatives” from being treated as evidence that a span changes no
reading. Maintain this exact 16-reading shape through both `Compilation` and
`Language.Actions`, assert five distinct four-pair repairs that apply to their
structural targets, and extend the allocation guard to a capped-alternative
case with a large unambiguous sibling.

---

## Verification record

- `git diff --check 214290d..695063d` — passed.
- `dotnet restore Ronin.sln --locked-mode` — passed.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror` —
  passed with zero warnings and zero errors.
- Exact maintained Release coverage command — 1,182 passed; 100% line, branch,
  and method coverage for `Ronin` and `Ronin.Server`.
- `dotnet test Ronin.sln --no-restore --configuration Debug` — 1,182 passed.
- `dotnet list Ronin.sln package --vulnerable --include-transitive` — no known
  vulnerable direct or transitive packages in any project.
- Seven targeted changed-area tests — the 42-term compiler/allocation cases,
  strict budget cases, and exit/id envelope cases all passed.
- Direct capped-alternative Release probes — the 30- and 55-term results above;
  candidate order/ranges were independently reconstructed from the production
  trees.
- Manual repair enumeration — all 16 bracket combinations were tried; one
  unique 148-lexeme, four-pair source resolved and matched each of the five
  displayed targets.
- Production boundary probes — the 30-term file produced four compiler repairs
  and four actions; the 55-term file produced zero of either.
- The pre-existing dirty `docs/spec` edits and untracked handoff material were
  preserved. No temporary audit source remains.
