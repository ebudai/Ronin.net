# For re-audit — UNRESOLVEDRETURNRULING (A) implemented; the token route is gone

> **Ledger** — `[R]` Requests re-audit of `d908c25..350bb56`. The designer took (A): an unresolved reference is its own `Unresolved` finding, `Unanswered` suppresses on any unresolved reading, and the `Answering` token heuristic is deleted. Answers the four §9 verification items. This is the fix for the whole REAUDIT65–68 class, not a fifth heuristic.

**From:** the successor, at `350bb56`. `REAUDIT68` found the fourth follow-on of one
problem; rather than a fifth token guess I consulted (`UNRESOLVEDRETURN`), and the
designer ruled (A) in `UNRESOLVEDRETURNRULING`. It is implemented; this asks for
signoff.

## For audit

- **Range:** `d908c25..350bb56` (the REAUDIT68 record + consultation, the ruling, and
  the implementation).
- **Against:** `UNRESOLVEDRETURNRULING`, `RETURNANDLITERALS`, `NAMEVSANCHOR`, `TAILSUGAR`.

## What changed (`85cc779`)

| part | change |
|---|---|
| the finding | an unresolved reference reports `Unresolved` — trigger `NoParse`, one per reference, at the reference span, stating only that it did not resolve (no culprit word, no repair; `AMBIGUITYASERROR` tone). The silence REAUDIT65–68 kept circling |
| the suppression | `Unanswered` suppresses on a body containing any `NoParse` reading. A body emits `Unresolved` **or** `Unanswered`, never both — the same `NoParse` decides; a test asserts the exclusion |
| the deletion | `Reading.Answering` and its word-run scan deleted outright. The resolved-tree walk `Called` is unchanged — exact on the name-vs-anchor boundary `NAMEVSANCHOR` settles at declaration time |

## §9 verification items

- **VER-1 — pre-sugar.** `Unanswered` runs against pre-sugar syntax: `function f => number { 5 }`
  reports `Unanswered`, i.e. it is not read as `{ return 5; }`. So `TAILSUGAR` is not applied on
  this path. §3's `{ nope }` witness still holds, but via the simpler route — `nope` is a `NoParse`
  reading regardless of sugar, so it suppresses and reports `Unresolved`. The §7 *resolved* row
  `function f => number { 5 }` is gated on VER-1 and is therefore **omitted** (under pre-sugar it is
  `Unanswered`, not clean). *Flagged, not fixed here:* that `{ 5 }` draws an `Unanswered` at all is a
  latent `TAILSUGAR`-not-implemented matter, separate from this ruling and untouched by it.
- **VER-2 — NAMEVSANCHOR constrains neither §4 nor §5; it underpins the kept walk.** R6b is the
  declaration-time rule that refuses a name shadowing a pattern's whole word content, so a resolved
  name is a `Node.Name` and a return a `Node.Call(Answer)` — which is exactly why `Called` (kept by
  §5) is exact, and why a medial `return` in `customer return policy` is a name, not an anchor. It
  is orthogonal to §4 (the `Unresolved` trigger is `NoParse`). Cited now, as it should have been in
  the consultation.
- **VER-3 — deleted clean.** Nothing but `Unanswered` read `Answering` (the single guard). No
  consumer to adapt.
- **VER-4 — 21 maintained tests expected the old silence; each updated.** Parsing suites
  (`StatementShapes` ×4, `ScopeHeadings` ×1) now assert the `Unresolved` their undeclared references
  report, structural checks preserved; the empty-list, initializer, and `return nope` controls in
  `TypeAnnotations`; the full-lexer `١`; the every-kind corpus and the golden wording file. One
  latent call-syntax bug surfaced and was fixed — `f (1, 2)`, the two-argument form, where the test
  had written the non-resolving `f 1 2`.

## Regression coverage (§7)

- **Unresolved — one `Unresolved`, never `Unanswered`** (`AnUnresolvedReferenceIsItsOwnFinding`):
  `return nope`; `send (return nope)`; `send return nope`; `send send return nope`;
  `send (customer return policy) nope; return;`; `send customer return policy nope`;
  `nope; return;` (REAUDIT65, expectation changed per §6); `nope` (tail position, VER-1 pre-sugar).
- **Resolved, clean:** `return 5`; nested `if c { return 5; }`; `send (return 5)`; `send return 5`;
  `send send return 5`.
- **Resolved, no value site — `Unanswered` fires:** the bare and fall-through bodies, and the
  resolved declared-name control `send (customer return policy); return;` (confirming the site walk
  still reads a name as a name).
- **Structural:** exclusion asserted — every unresolved case is exactly one `Unresolved`.

## Deferred, not implemented — §8

Restricting `return` to statement-initial position (and refusing it at a name head) is severed and
**not** implemented, per §8: it would not close REAUDIT68 anyway, it cuts against `ZEROGLUE`, and it
is a grammar carve-out that needs its own ruling. Recorded there for whoever picks it up.

## Gate at `350bb56`

The project gate — CI `.github/workflows/build.yml`, local battery `TYPECHECKERHANDOFF` §0:

- Debug and Release build clean, `-warnaserror`.
- `Passed! — Failed: 0, Passed: 1333` (Release, `--no-build`).
- Coverage **100%** line and branch (`/p:Threshold=100 /p:ThresholdType=line,branch`).
- Changed-file `dotnet format Ronin.sln --verify-no-changes --include <files>`: passed.
- `git diff --check` clean.

Sabotage-verified: neuter the `Unresolved` emission (the finding, golden, and every-kind tests
fail); remove the suppression (a body with an unresolved reading draws both findings and the
exclusion test fails). Both restored.
