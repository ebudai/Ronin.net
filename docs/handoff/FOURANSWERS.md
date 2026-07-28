# Four answers

## 1. Multiple `when` bodies writing one cell

My triage said "already illegal by the ownership rule". That was too quick —
the ownership rule is about *owners*, and two `when`s inside one type share an
owner, so it doesn't reach this case. The programmer is right that it needs a
rule of its own.

**The rule: `when` bodies are unordered relative to each other, so any cell
written by more than one `when` is a declaration error.**

Functions are exempt, and the difference is the whole justification. Two
functions writing `health` is fine because their call sites impose an order:

```
function take damage (amount) { health = health - amount; }
function heal (amount)        { health = health + amount; }
```

Two `when`s have no such order — they fire in the same round, and nothing in
the language says which first. Declaration order would make it deterministic
but silent, which is worse than an error: one write lands, the other vanishes,
and the program looks fine.

Write sets are computed **transitively through calls**, and attributed to the
`when` that caused them. `Cascades.cs` already computes exactly these sets for
tier-1 cycle detection — the same data answers this question.

The fix in the diagnostic is the one we already chose for multi-writer cases:

```
«game state» is written by two whens:
    when player dies      (Player.ron:14)
    when timer expires    (Clock.ron:8)
Whens fire in one round with no order between them.
Derive it instead:   let game state = if is dead or timer expired
                                      then game over else playing;
```

That formulation is also the better program, which is a good sign for the rule.

---

## 2. Numeric semantics

**All-double is the current implementation, not a proposal, and it is a
deviation from the design.** Restating what was settled:

- `number` is **exact**. int64 where it fits, **integer** rationals when
  division warrants — numerator and denominator both integers, never floats.
- `fast number` is float64, opted into explicitly.
- Roots and transcendentals return `fast number` — that is the boundary, not
  division. Division is exact in the rationals.
- Overflow never wraps. Widen, or trap.
- No `precise number`; if `number` is exact it has nothing left to say.

### What to build now, before the full tower

Do not ship "double, documented as a deviation". Silent precision loss is the
exact failure the numeric design exists to prevent, and a documented deviation
is still silent at the point it bites.

Ship the semantics complete and the **range** limited:

- integers as int64 with **checked** arithmetic, trapping on overflow
- division producing an int64/int64 rational, normalised by gcd, trapping if a
  component overflows
- `fast number` as double
- roots and transcendentals returning `fast number`

That is perhaps a hundred lines and it is correct-by-construction. Where the
finished design would widen to arbitrary precision, this traps — loudly, with
the operation named. The principle is the same one behind never wrapping:
**limit the range, never the correctness.** A trap is honest; a rounded answer
is not.

Two immediate consequences, both one-liners:

- **`1/0` is an error value**, not Infinity or NaN. That is the error model,
  and Infinity silently poisoning a computation is precisely the spreadsheet
  failure the model exists to make visible.
- **Dates that lex but do not evaluate should say so**, not fall through to a
  default. An unimplemented feature reporting itself is a diagnostic; one
  falling through is a bug.

---

## 3. Should a body that throws become an Error?

**No — and the question has the wrong shape.** Catching everything and calling
it `Error` makes the interpreter undebuggable: every null-reference bug in the
evaluator surfaces as a user-facing spreadsheet error, indistinguishable from
a genuine division by zero.

Two kinds are needed:

| | | |
|---|---|---|
| **Error** | a program failure | a value, flows through the graph, `otherwise` catches it |
| **Fault** | an interpreter defect | caught so the session survives, reported as a bug, **never** caught by `otherwise` |

The real target is that **user-program failures never throw in the first
place.** The evaluator should *return* an Error for a bad cast, a missing key,
an overflow — not raise and get caught. Catching is the backstop for the ones
not yet converted.

But keep the backstop, and keep it broad, because always-running mode means one
bad node must not kill the session. Just tag what it produces as a Fault so it
never masquerades as a result. A fallback for a program error is a fallback; a
fallback for an interpreter bug is a hidden crash.

---

## 4. Graph or lift? Both — and the doc overstates

The programmer is right on both counts. The implementation is faithful to the
reference; the reference and the doc disagree.

**Both mechanisms are needed, and they do different jobs.** Demonstrated in
`error_model.py`:

**lift alone is insufficient** — a body can read an error and simply not use
it:

```python
g.let('sloppy', lambda e: (e.read('ratio'), 42)[1])   # reads error, returns 42
```
```
unguarded graph lets the error be discarded:  42
```

No operator was involved, so `lift` never saw anything. The error vanished.

**Adoption alone is insufficient too** — a body doing raw arithmetic on an
error raises, and the graph catches it as a Fault, which is the *wrong
diagnosis*: a program error reported as an interpreter bug.

```
FAULT(TypeError: unsupported operand for +: 'RoninError' and 'int')
```

So: **`lift` keeps errors inert inside a body so nothing explodes; the graph
guarantees adoption regardless of what the body does with them.** Neither
covers the other's case.

### The doc is wrong and should be corrected

> **says:** "a node whose dependency is an error becomes an error *without
> running its body*"

Not achievable. An opaque callable cannot be aborted without exceptions.

> **should say:** "a node that reads an error *adopts* it — whatever its body
> returns is discarded. The body may still execute, but because `let` bodies
> are pure, running one and throwing the result away has no observable effect."

The correction is worth making precisely because it names **purity** as the
thing that makes the achievable guarantee equal to the promised one. Without
purity, "the body may still execute" would be a hole rather than a footnote.

Implementation is small: a per-evaluation slot recording the first error read,
checked after the body returns. `GuardedGraph` in `error_model.py` is the whole
change.
