# Re-read now, carry the **value** later — and mark the bottom, not the scalars

> **Ledger** — `[V]` verdict. Q1: kill the hand-rolled classifier now; the real fix
> is a literal carrying its **value**, and the numeric tower forces it. Q2: the
> registry marks `error` as the bottom and everything else derives. Adds the
> sweep's stopping condition.
> answers: `DISCARDEDKINDS`
> supersedes: none
> superseded by: none

**Q1: re-read now — but neither of your options is the fix, and the number work is
about to make that unavoidable. Q2: a marker, and it goes on `error`, not on the
scalars.**

And §4 is the part to keep: **the sweep needs a stopping condition**, or it turns
into a crusade against every structural test, some of which are correct.

---

## §1 — Q1: the rule was violated **once**, not twice

Worth separating, because it changes what has to happen.

```
  Sort.Denoted       re-lexes through Lexicon.Literal.Lex   -> consults the AUTHORITY
  Evaluator.Value    text[0] is '"' … double.TryParse       -> a SECOND classifier
```

`Sort.Denoted` does not break the rule. It reads the declared fact — expensively,
by re-running the classifier, but from the one place that knows. **Only
`Evaluator.Value` is a violation**, and it is not a style problem: a
thousands-grouped run, or a date-shaped one, is read one way by the lexicon and
another by `double.TryParse`, and a fourth literal kind would have to be taught to
it separately.

**So: re-read. Kill the hand-roll.** It is a bug, it is small, and it does not
wait on anything.

One thing to say out loud rather than discover: **that is slower than what it
replaces.** Re-lexing per evaluation costs more than a `TryParse`. Take it anyway —
correctness first — and read §2 for why the cost is temporary.

## §2 — but the real defect is that a literal's **value** is computed per evaluation

Neither of your options addresses the thing underneath. Ask why `Evaluator.Value`
is classifying at all:

> **A literal is a constant. Its value is known when it is parsed.** Computing it
> on every evaluation — in a reactive graph, potentially every tick — is the
> defect; the discarded kind is a symptom of it.

And this is about to stop being a performance argument and become a correctness
one. Under the numeric ruling a `number` is an **exact rational**, so the literal
`0.1` must become exactly ¹⁄₁₀ — not `double.TryParse`'s
`0.1000000000000000055…`. **`Evaluator.Value` is not merely drift-prone; it is
about to be wrong**, and threading a *kind* would not have saved it, because the
kind is not what it gets wrong.

So the successor is not "thread the kind." It is:

```
  approximation                   successor                        trigger
  Node.Literal carries text;      Node.Literal carries its VALUE,  the numeric tower —
  consumers re-lex through        minted once by the lexicon at    exact rationals make
  Lexicon.Literal.Lex             parse time                       TryParse wrong, not
                                                                   merely slow
```

Do not build it now. The numeric work must touch this code regardless, and it will
be designed better with the tower's requirements in hand — exact-versus-`fast`,
the rational parse, the date value that currently lexes and does not evaluate —
than it would be guessed at today.

## §3 — the lexeme kind: the collapse is right, the **name** lies

`Lexemes.cs:158` mapping every `Literal` to `LexemeKind.Number` looks like the same
defect and is not. The neighbouring comment says the collapse is deliberate —
*"Date and Text are free atoms for exactly the reason Numeric is"* — so the
resolver **means** to treat all literals alike, and it is right to.

What is wrong is that the kind is called `Number` while meaning *literal*. A name
that lies is how the next reader re-derives the fact incorrectly, which is the
whole failure mode of this sweep. **Rename it to `Literal`.** No behaviour changes
and the next person is not misled into "recovering" a distinction the resolver
deliberately does not want.

## §4 — the sweep's stopping condition, which matters more than either question

You have now found five of these and asked about two that felt different. They
*are* different, and without a rule the sweep will start firing on tests that are
correct. Here it is:

> **Does the structure *constitute* the fact, or merely *correlate* with it?**

```
  «has no shape» -> «is a boolean»        CORRELATION. Every literal is shapeless.
                                          A third one breaks it.  -> a proxy, fix it
  «has no holes» -> «is not a function»   CORRELATION. EMPTYBRACKETS guaranteed
                                          nullary functions.      -> a proxy, fix it
  «has holes»    -> «is a constructor»    CONSTITUTION. A type constructor has holes
                                          BECAUSE it takes arguments. -> correct, leave it
```

A structural test is sound exactly when the structure *is* the property rather than
a side effect of it. That is the same distinction as refusing a denormal to catch
an overflow: the denormal correlates with lost precision; it does not constitute
it.

Apply that test before each remaining candidate, and the sweep terminates instead
of expanding.

## §5 — Q2: mark the **bottom**. The scalars derive

Not "leave it," and not "minus `error`."

**Not "leave it,"** because the trigger is not hypothetical — it is three deep and
already ruled. `date` is a primary type of this language with its literal syntax
settled; `fast number` and `fast text` are ruled and coming. Each one added to the
registry would be filed by `Sort.Of` as a **user `Named` type, in silence.** A
bounded copy is acceptable when nothing is queued behind it; three things are.

**Not "minus `error`,"** because that keeps the exception in the *consumer*. It is
`scalars` with fewer entries: `Sort.cs` would still know a fact about `error` that
the registry does not state, which is the shape being swept.

**So mark the bottom, and let the rest fall out**, because that is where the
exceptional fact actually lives:

> **The registry marks `error` as the bottom.** A supplied `Kind = Type` entry that
> is not the bottom is an ordinary ground type, and `Sort.Of` makes a `Scalar` of
> it.

Three things recommend it over a `scalar` flag on four entries:

- **it states the exception once, where the exception is.** *Error is the bottom* is
  a ruled language property — assignable to everything, nothing assignable to it —
  and it belongs in the language's statement of what it supplies, not in a comment
  in `Sort.cs`;
- **`date`, `fast number` and `fast text` then need no annotation at all.** They are
  scalars by default, which is the correct default; and
- a `scalar: true` flag on everything would have to be remembered for each new
  entry — a second list, wearing a field.

**One thing to check rather than assume**, since my clone predates these fields: if
`SuppliedTypes` includes the **shaped** type constructors — `optional (_)`,
`list of (_)`, `lookup (_) => (_)` — then the derivation is *`Kind = Type`, no
shape, not the bottom → `Scalar`*. Per §4 that extra clause is **constitutive, not
a proxy**, so it is sound: a constructor has holes because it takes arguments.

## Summary

| | |
|---|---|
| **Q1** | **re-read now.** The rule was violated **once** — `Sort.Denoted` consults the authority; only `Evaluator.Value` is a second classifier. Kill the hand-roll |
| say it out loud | re-lexing is **slower** than the `TryParse` it replaces. Take it anyway; §2 makes the cost temporary |
| **the real defect** | **a literal's value is computed per evaluation.** A literal is a constant; its value is known at parse time |
| and it is about to be **wrong** | under the numeric ruling `0.1` must be exactly ¹⁄₁₀, not `double.TryParse`'s. Threading a *kind* would not have saved it |
| so | ledger the successor — **`Node.Literal` carries its value, minted once by the lexicon**. Trigger: the numeric tower, which must touch this code anyway |
| **§3** | `LexemeKind.Number` for every literal — the **collapse is deliberate and right**; the **name lies**. Rename it to `Literal` |
| **§4 — the stopping condition** | **does the structure *constitute* the fact or merely *correlate* with it?** Shapeless→boolean and holeless→not-a-function are correlations. **Holes→constructor is constitution** — leave it alone |
| why it matters | without it the sweep starts firing on tests that are correct |
| **Q2** | **mark the bottom; derive the rest.** Not "leave it" — `date`, `fast number` and `fast text` are all ruled and queued, and each would be filed as a user type **in silence** |
| not "minus `error`" | that keeps the exception in the **consumer** — `scalars` with fewer entries |
| why marking `error` wins | it states the exception **once, where it lives**; new scalars need **no annotation**; and a `scalar` flag on four entries is a second list wearing a field |
| check | if `SuppliedTypes` includes shaped constructors, add *no shape* to the derivation — **constitutive, per §4**, so sound |
