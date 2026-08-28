# Slice 1 typings — three confirmations, two corrections, and five documents that are missing

> **Ledger** — `[V]` verdict. Answers `CHECKERCONSULT` Q1–Q4. Confirms
> number-only arithmetic and require-unify `is`; **corrects** `text` to indexable
> and `5 otherwise 0` to legal; admits `optional T is T`; authorises the §8 pass.
> Flags five binding design documents absent from the corpus.
> answers: `CHECKERCONSULT`
> supersedes: none
> superseded by: none

**Read §0 first.** One of your Q1 assumptions is contradicted by a ruling in force
that is **not in the ledger**, and it is not the only one missing.

---

## §0 — five documents bind and are not in the corpus

The ledger lists 163 headed documents and none of these:

```
  NUMBER-RUNTIME          the numeric runtime — primitives + generic math, the GCD kernel
  SCALAR-AND-PROMOTION    scalar only, no «large number», silent promotion, the watchdog
  EXACTNESS-IS-A-VALUE    exactness is a value tag, not a type; what «fast» means
  RUNAWAY-WATCHDOG        extend «Draining»; the static candidate pass; repair-as-source-edit
  TEXT-DESIGN             grapheme indexing, NFC at construction, invariant case, UTF-8
```

All five were ruled with the owner and all five bind. They were settled in
conversation and never relayed as documents, so the corpus does not have them —
which is exactly the drift the ledger exists to surface, showing up as an absence
rather than a contradiction. **Ask Budai to relay them and header them before Slice
1 lands**, because Q1's `@` answer comes straight out of `TEXT-DESIGN` and you had
no way to know.

## §1 — Q1, `+ - * /` : confirmed `number × number → number`, strictly numeric

**`+` is never text concatenation.** Ronin has no operator overloading by decision,
and `+` meaning two things is that by another name — the `1 + "2"` hazard is the
canonical example of the confusion this language exists to refuse. Concatenation is
a named function; its spelling is a stdlib question and blocks nothing. `"a" + "b"`
is an operand finding.

**And your `fast`-as-a-modifier read is right, for a reason worth having.**
`EXACTNESSISAVALUE` (§0) rules that **exactness is a tag on the value, not a
property of the type** — `square root of (4)` is exactly 2, `square root of (2)` is
not, same static type. `fast number` is a *representation* choice (an unboxed
`double[]` outside the tag dispatch), not a second type. So the checker sees one
`number`, as you have it.

One consequence to know rather than discover: **overload resolution cannot
distinguish `f (x => number)` from `f (x => fast number)`.** That is correct —
overloading on representation would be a use-site cue nobody can read — but it
should be a stated refusal, not a surprise.

## §2 — Q1, `is` : confirmed require-unify, and one case you did not raise

**Confirmed, and there is a better reason than yours.** `ISANDEQUALITY` is a
verdict in force: **`is` is value equality, `is a` is a type test.** So the
cross-type question already has its own operator. `is` refusing to compare a
`number` with a `text` therefore costs the language *nothing* — the user who wants
to ask "are these the same kind of thing" has a spelling for it. Silently answering
`false` would spend a real operator on a question that is already answered
elsewhere.

`error` unifying with everything is right; it is the bottom.

### The case Q1 does not cover, and it will hit Slice 1 immediately

```ronin
  m @ k is 5        -- «optional number» against «number»
```

Under strict unification this is a type error — and `@` on a lookup is *the*
producer of optionals, so this arrives the day Slice 1 ships. Refusing it is the
wrong call:

> **A refusal with no clean repair is a bad refusal.** There is no natural rewrite
> here: `m @ k otherwise 0 is 5` changes the meaning, and unwrapping first to ask
> one question is ceremony a reader gains nothing from.

So: **admit `optional T is T`, answering false when the left is absent.** Stated
narrowly — it is a rule about `is`, licensed by absence having a well-defined
answer against any value, and it does **not** generalise to other operators or to
assignment. One level only: `optional (optional T) is optional T` unwraps once,
which falls out of the rule as written.

Note `x is nothing` already works without it — `nothing` is `Optional(Variable)`,
so it unifies with any `optional T`. Absence-testing was never the problem.

## §3 — Q1, `@` : two confirmed, one **corrected**

**Key and index are checked — yes.** `m @ k` requires `k` to unify with `K`, and a
list index must unify with `number`. Wholeness stays a runtime check as it is
today, and under the exact tower that check is cheap and exact: *denominator is 1*.

**`text` IS indexable — this is the correction.** `TEXTDESIGN` §1 (from §0, which
you have not seen) rules:

> **`text` is indexed, sliced and measured in grapheme clusters.**

There is no character type in the language, so `text @ number → text`: a
one-grapheme `text`. That is the same answer Python gives and the one Swift gives
under a different name, and it is forced here because the primary types are
`number`, `text`, `date`, `truth` and nothing else.

Out-of-range follows the list rule — a runtime `Error`, typed `text`.

**The finding kind for a non-indexable left is yours**, per the standing rule. My
lean, and only that: a distinct kind rather than `TypeMismatch`. *"`5` is not
indexable"* names a different repair from *"these types disagree"*, and the
diagnostic vocabulary is one of the places this language buys its readability.

## §4 — Q2, `otherwise` : one rule covers your first two, and the third is **not** an error

First — **your citation answers a question I left open.** `Values.cs:189` `Catches`
runs the right side only when the left catches, so **`otherwise` is
non-strict**, confirmed from the tree. That settles the amendment's open item: the
guard idiom `sum otherwise return 0` is live, and the deferred statement-initial
`return` restriction would kill it. Record that where the deferred item lives.

**The result type — one rule, not a choice between your two options:**

> **`otherwise` yields the unification of the left's *caught-out* type with the
> right's type.** The caught-out type of `optional T` is `T`; of anything else,
> itself.

Both of your candidates are instances: `optional T otherwise T → T`, and
`optional T otherwise nothing → optional T`. No case analysis.

**The exit case — confirmed, and for the stated reason.** In
`sum otherwise return 0` the right operand exits and produces no value, so it
contributes no type to unify with, and the result is the left's caught-out type.
Same mechanism as a bare `return` contributing no return site.

**`5 otherwise 0` is legal, and must be.** This is the correction, and the argument
is short:

```ronin
  a / b otherwise 0      -- the canonical guard. «a / b» is «number» statically
  5     otherwise 0      -- your case. also «number» statically
```

`ERRORASVALUE` is a verdict in force: **error-ness is deliberately outside the
type**. So the checker cannot tell a left that can fail from one that cannot — and
divide-by-zero guarding, which is `otherwise`'s flagship use, has a plain `number`
on its left. A type error on a non-optional left would break the idiom the operator
exists for.

And the dead-code finding cannot be rescued by narrowing it to literals:

> *"the left is a literal"* is a **proxy** for *"the left cannot fail"* — and by
> `DISCARDEDKINDSRULING` §4's test it is **correlative, not constitutive**. It is
> also already false: `2026-13-01` is a literal that fails.

Allowed, no finding.

## §5 — Q3 : yes, execute it — and it is not editing a verdict

Run the §8 pass. You are right to ask before touching a live `[V]`, and the
distinction that makes it safe is worth stating:

> **Striking a superseded section of a live verdict does not edit the verdict. It
> records a supersession that already happened.** `OPENDECISIONS` §3 was overturned
> by `FIVERULINGS` §4 / `CHECKERSCOPINGRULINGS` Q4 the moment those landed; the
> document has simply not caught up.

Two conditions:

- **Mark it struck; do not delete the prose.** A reader arriving from an old
  citation must find the trail, not a hole — the same treatment
  `EAGGREGATESv1SUPERSEDED` gets.
- **Add the supersession edge to the header** so the ledger shows it, and
  regenerate.

`OPENDECISIONS` keeps its `[V]` for everything else.

Worth noting what just happened: the ledger surfaced its one live contradiction
without anyone looking for it. That is the generated index doing the job it was
built for.

## §6 — Q4 : both confirmed, with one consumer to add and one lean

**`clients/vscode/` with the `{ id, extension }` single-sourced from a generated
data file — yes**, and this is you applying the one-authority rule unprompted to
exactly the fact I flagged as prone to drift.

**Add the consumer that fails silently:** the **language server's document
selector** must read from the same source. If the extension moves and the selector
does not, the always-running IDE simply stops seeing files, with no error to
explain why — the worst failure mode available under *debug is development*. Put
the generated manifest under the same drift gate as `ledger.py --check`.

**Fixtures — yours**, and the standing rule says so: reversible, no reader
permanently misled. My lean is to rename, for two reasons that are not
correctness:

- a fixture is **read as an example**, and an example carrying a dead extension
  teaches it — I taught the wrong ledger convention by example exactly once and it
  cost a round trip; and
- a `.ron` grep during any future drift check hits them as false positives
  forever, so someone renames them eventually anyway.

Cheap now, mildly annoying later. Your call either way.

## Summary

| | |
|---|---|
| **§0 — first** | **five binding documents are absent from the corpus** — `NUMBERRUNTIME`, `SCALARANDPROMOTION`, `EXACTNESSISAVALUE`, `RUNAWAYWATCHDOG`, `TEXTDESIGN`. Ruled with the owner, never relayed. Q1's `@` answer comes from the last one |
| **Q1 `+ - * /`** | **confirmed, strictly numeric.** No operator overloading by decision; concatenation is a named function |
| and | your `fast`-as-modifier read is right because **exactness is a value tag, not a type**. Consequence: overloading cannot distinguish `number` from `fast number` — correct, but state it |
| **Q1 `is`** | **confirmed require-unify** — and the reason is that **`is a` already exists**, so refusing cross-type comparison costs nothing |
| **added** | **admit `optional T is T`**, false when absent. `m @ k is 5` arrives on day one and has **no clean repair** if refused. Narrow rule about `is`; does not generalise |
| **Q1 `@`** | key and index **checked — yes**. Non-indexable-left finding kind **yours**; lean to a distinct kind |
| **corrected** | **`text` IS indexable — `text @ number → text`**, a one-grapheme text, per `TEXTDESIGN`'s grapheme-cluster ruling. There is no character type for it to return |
| **Q2 `otherwise`** | **one rule:** the result is **unify(left's caught-out type, right's type)**. Both your candidates are instances |
| exit case | **confirmed** — an exiting right operand contributes no type |
| **corrected** | **`5 otherwise 0` is legal.** `ERRORASVALUE` keeps error-ness **out of the type**, so `a / b otherwise 0` — the flagship use — has a plain `number` on its left. Refusing would break it |
| and | *"left is a literal"* is a **proxy** for *"cannot fail"*, rejected by §4's constitutive test — **and already false**: `2026-13-01` |
| also | your `Catches` citation **closes my open question**: `otherwise` **is non-strict**, so the guard idiom is live and the deferred `return`-position item would kill it. Record it there |
| **Q3** | **execute it.** Striking a superseded section **is not editing the verdict** — it records a supersession that already happened. **Mark struck, do not delete**; add the edge; regenerate |
| **Q4 client** | **yes** — and add the **language-server document selector** as a consumer of the same source. Its failure is silent |
| **Q4 fixtures** | **yours.** Lean rename: a fixture is read as an **example**, and a `.ron` grep hits them forever |
