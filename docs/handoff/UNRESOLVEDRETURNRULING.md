# `Unanswered` and the unresolved value-return — take (A), and the token route is closed for a reason

> **Ledger** — `[V]` Answers `UNRESOLVEDRETURN.md` Q1/Q2: **(A)**. An unresolved
> reference gets its own finding; `Unanswered` suppresses on any unresolved reading;
> `Answering` and all four token heuristics are deleted. The token route is not
> under-tuned, it is unable to carry the distinction — §2. A case none of REAUDIT65–68
> has named decides against the narrower fixes: under `TAILSUGAR`, `{ nope }` is a
> value-return attempt containing no `return` lexeme — §3. `{ nope; return; }` changes
> result; that reverses REAUDIT65 deliberately — §6. One language item is severed and
> deferred, and is **not** to be implemented — §8.
> answers: UNRESOLVEDRETURN
> answered by: UNRESOLVEDRETURNAMENDMENT
> supersedes: none
> superseded by: UNRESOLVEDRETURNAMENDMENT §1.2

**From:** the designer. **To:** the programmer, actioning `REAUDIT68` at `acb0aea`.

You were right to stop and ask rather than ship a fifth heuristic. The fourth attempt
you described would have failed as you predicted, and the reason generalises past the
case you named. Do not attempt a fifth.

---

## §1 — the ruling

Take **(A)**.

1. An unresolved reference gets a finding of its own.
2. `Unanswered` suppresses on a body containing **any** unresolved reading.
3. `Answering`, and every token heuristic that computes it, is **deleted**.
4. The resolved-tree walk is unchanged. It was always exact.

(B) is refused as unnecessary once (A) lands. (C) is refused as too broad, as you
judged. (D) is refused — see §2; it is not a bounded inaccuracy.

---

## §2 — why the token route is closed, not merely unfinished

The four attempts read as four near-misses. They are one structural failure, and the
audit trail already contains its proof. Two witnesses:

```ronin
send return nope                    -- «return» preceded by the word «send»,    IS an anchor
send customer return policy nope    -- «return» preceded by the word «customer», IS a name word
```

Identical local lexical shape. Opposite correct answers. Both inside an unresolved
statement, so in both the resolver has produced nothing to consult. The information
that separates them is not present in the token stream, so no predicate over the token
stream can recover it. That is a proof, not a gap in the search.

A heuristic strong enough to separate them would have to munch maximally against the
symbol table **and** reproduce the fewest-lookups disambiguation of
`AMBIGUITYASERROR`, including its tie rule. At that point it is not a fallback; it is a
second resolver, and a second resolver will diverge from the first and generate a new
bug class of its own — with the divergence appearing exactly on the already-broken
source where nobody looks.

This is why (D) is refused. It is not "one narrow inaccuracy standing." It is an
unbounded queue: REAUDIT69, 70, 71, each individually reasonable, each fixed by a rule
that meets the next case. You named the next break before shipping the fourth attempt.
Treat that as the signal it is.

---

## §3 — the case that decides it: tail sugar

`TAILSUGAR` is a verdict in force: `{ x }` is `{ return x; }`. Then:

```ronin
function f => number { nope }
```

This body **attempts a value-return** — it is `{ return nope; }` by that ruling — and
the attempt fails to resolve, so there is no tree. Under the maintained cascade policy
it must suppress. But there is no `return` lexeme in the source at all.

Every heuristic in §2 of your package searches for the word «return». All four report
`Answering = false`. `Unanswered` fires. Same bug class as REAUDIT66 and REAUDIT68,
reachable today, not in any existing witness.

**This is what severs the language route from the checker route.** Restricting where
`return` may appear (§8) eliminates `send return nope`; it does nothing here, because
the defect is not where `return` sits but that none was written. Closing this case
lexically needs a *second* rule — final statement, no terminator, no action marker —
and the history of this file is that every new lexical rule meets a new case.

(A) is indifferent to how the body attempted to answer. `return nope`,
`send return nope`, bare `nope` — all are unresolved readings, all suppress, by one
rule that does not inspect shape.

**Verify before building — VER-1 in §9.** If `TAILSUGAR` desugars during resolution,
the above holds as written. If `Unanswered` runs against pre-sugar syntax, this
particular case does not arise; (A) is still the ruling on §2 alone, but say so in your
reply so the reasoning on record is accurate.

---

## §4 — what to build

**The finding.** An unresolved reference reports. Today `nope;`,
`customer return summary` (undeclared) and `send return nope` all compile to silence;
that silence is a defect independent of `Unanswered` and is the gap REAUDIT65–68 kept
circling.

**Trigger.** Any reading whose `Resolution` is `NoParse`.

**Scope — one finding per unresolved reference, not per unresolved name.** Ruling on
Q2's second half. `send customer return policy nope` emits **one**, not four. The
programmer fixing it needs to know the statement did not resolve; enumerating each
word that failed to bind is noise, and on a multi-word-name language it is heavy noise.

**Span.** The reference, not the file and not the individual lexeme.

**Wording.** State that the reference did not resolve. Do **not** speculate about which
word is at fault, and do **not** suggest a repair — the fewest-lookups rule means the
intended reading is genuinely unknown at that point. `AMBIGUITYASERROR`'s per-rule
ledger is the model for tone.

---

## §5 — what to delete

- `Compiler/Compilation.cs:516-532` — the `Answering` computation and its word-run
  guard. Delete outright. Do not preserve it behind a flag.
- `Compiler/Compilation.cs:926-940` — the false-flag path allowing `Unanswered`.
  Replaced by the unresolved-reading suppression of §1.2.
- `Compiler/Compilation.cs:1150-1164` — the resolved-tree walk. **Keep unchanged.**
  It is exact on every resolved body and every boundary already respected by
  `ReadsAs` / `Atoms`.

A body must emit the unresolved finding **or** `Unanswered`, never both. That
mutual exclusion is the point of the ruling; a test should assert it directly.

---

## §6 — the policy change, stated plainly

```ronin
function f => number { nope; return; }
```

now reports the undeclared `nope` and **not** `Unanswered`. This reverses what
REAUDIT65 expected, deliberately. The reasoning: the unresolved reference is fixed
first and the body re-checked, at which point `Unanswered` fires correctly if the
answer is still missing. That is ordinary cascade behaviour and it is what the rest of
the compiler already does.

Update the REAUDIT65 control to the new expectation rather than deleting it. It stays
valuable as the witness for this ruling.

The trade is worth naming so the audit can weigh it: the deferred language item in §8
would have *preserved* REAUDIT65's result where (A) overturns it. That is the one thing
the narrower route bought, and §3 is why it does not buy enough.

---

## §7 — regression coverage required

Keep every control from REAUDIT65–68. Add the marked cases.

**Unresolved — all must suppress `Unanswered` and emit the unresolved finding:**

| case | provenance |
|---|---|
| `return nope` | direct |
| `send (return nope)` | REAUDIT66 |
| `send return nope` | REAUDIT68 witness |
| `send (customer return policy) nope` | REAUDIT67 |
| `send customer return policy nope` | **new** — breaks the fourth heuristic |
| `send send return nope` | **new** — two levels, unparenthesized |
| `function f => number { nope }` | **new** — tail sugar, §3, gated on VER-1 |
| `{ nope; return; }` | REAUDIT65, **expectation changed** per §6 |

**Resolved — all must compile clean, no finding of either kind:**

`send return 5` · `send (return 5)` · `send send return 5` ·
`function f => number { 5 }` (gated on VER-1)

**Resolved, no value site — `Unanswered` must still fire:**

A fully resolved body with no `Answer` call, and the REAUDIT67 declared-name witness
in a fully resolved statement, confirming §5's walk still sees names as names.

**Structural:**

No body emits both findings. Coverage gates hold at 100% line and branch for `Ronin`
and `Ronin.Server` as at `acb0aea`.

---

## §8 — severed and deferred: `return`'s position

The designer is inclined to restrict `return` to statement-initial position, with the
companion rule that `return` may not head a declared name. **Do not implement this.**
It is severed from the present ruling and needs its own, for three reasons on record:

- Per §3 it would not close REAUDIT68's finding, so it cannot be the answer to this
  package.
- It cuts against `ZEROGLUE`, a verdict in force. Reserving a word at name-head
  position is a retreat from zero reserved words and must be ruled as such, not
  arrived at sideways through a checker repair.
- It is a carve-out from a grammar that, since `POSTFIXPATTERNS`, deliberately admits
  anchors mid-statement. "Except `return`" is a real special case in a uniform system.

In its favour, for whoever picks it up: `MODIFIERNAMES` — *refuse a modifier at a name
head, for every modifier* — is near-exact precedent for the companion rule, so the
mechanism exists and is ruled. It should be reconciled with `FASTRESERVATION`, which
reads as a modifier permitted at a name head by design. The motivating argument is
readability, not correctness: statement-initial `return` puts every exit on the left
edge of a function, which is the trade Ronin exists to make. Nested `return` is
additionally dead under strict evaluation — `send return 5` answers `f` and never calls
`send` — so the expressiveness given up is arguably negative.

**Against it — the strictness argument does NOT hold for `otherwise`** (`UNRESOLVEDRETURNAMENDMENT`
§4; confirmed from the tree in `SLICEONETYPINGS` §4). `otherwise` is **non-strict**: its
`Catches` runs the right operand only when the left is caught (`Compiler/Runtime/Values.cs:189`),
so `sum otherwise return 0` is a **live** guard idiom — the fallback that exits — not dead code.
A statement-initial `return` restriction would kill it. That is a real expressiveness cost the
"arguably negative" line above did not account for, and it points the other way: leave §8 deferred.

None of that is ruled here. It is recorded so the reasoning is not lost.

---

## §9 — verification items, answer in the audit request

- **VER-1** — Is `TAILSUGAR` desugaring applied before the resolved-tree walk sees the
  body, or does `Unanswered` run against pre-sugar syntax? Decides whether §3's witness
  and its two test rows are live. Report the answer either way.
- **VER-2** — `NAMEVSANCHOR` is a verdict in force, with a `-RESULT` measurement at
  `4ccfddc`, and it is the prior ruling on precisely the name-versus-anchor distinction
  this file turns on. Its ledger line ends *"and it is not the whole law."* Read it
  before building and report whether it constrains §4's scope choice or §5's deletions.
  It is not cited anywhere in `UNRESOLVEDRETURN.md` and it should have been.
- **VER-3** — Confirm no consumer other than `Unanswered` reads `Answering`. If one
  does, raise it rather than adapting it; §5 deletes the flag outright.
- **VER-4** — Report whether the new finding fires anywhere in the maintained suites
  that currently expect silence. A large count is expected and is not by itself a
  defect, but the number should be on record before it lands.
