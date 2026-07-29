# Zero glue words is achievable — your instinct was right

Supersedes the framing in `LOOP-INDEX-AND-GLUE.md` §B, which treated glue as a
tax to be minimised. It can be driven to zero. `zero_glue.py`, 3/3.

You went to `iterate shoes => shoe` to avoid glue words. **That was the correct
move for the correct reason**, and it generalises further than you thought —
there are three free mechanisms, not one, and between them they cover every
construct we have discussed including the loop.

---

## Why a symbol separator is free — the reason matters

R7 makes symbols their own lexeme class. A name is a **word-only span** — in
the resolver it is literally one line:

```python
if all(k == WORD for k, _ in t[i:j]):
```

So a symbol can never be part of a name. Not "loses on cost" — **the reading
does not exist.** `iterate shoes => shoe` has an empty glue set and always
will, no matter what anyone declares anywhere.

## The same line gives you a third mechanism: brackets

A bracket token is not a word either. So a name cannot straddle one:

```
send hello to alice        with «hello to alice» declared:  2 lookups, CAPTURED
send (hello) to alice      with «hello to alice» declared:  4 lookups, safe
```

The capturing reading is not merely more expensive in the second case; it
cannot be constructed. **A word sitting next to a bracket is unswallowable, and
therefore does not need reserving.**

That has an immediate consequence for control flow. If blocks are
brace-delimited:

```
if (_) then (_) otherwise (_)      glue = {then, otherwise}     costs 2 words
if (_) { … } otherwise { … }       glue = {}                    costs nothing
```

`otherwise` sits between two braces. No name can reach it from either side.

## And it buys back `in`

`for each (_) in (_)` costs `in` only because hole 1 can grow leftward across
an earlier `in`. **Pin the loop variable to one token — multi-word ones
bracketed — and the split point is fixed by construction:**

```
for each bank in banks                  split forced after «bank»
for each (in flight order) in orders    the bracket blocks the straddle
```

One candidate split, always. Nothing can compete, so `in` needs no reservation.

The entire bill is that a multi-word loop variable takes brackets. That is a
much smaller price than reserving `in` in every program forever — and it is the
difference between "Ronin reserves a handful of words" and **"Ronin has no
reserved words."** The second is a far better sentence, and it is true.

---

## So the three free shapes are

| shape | why it is free |
|---|---|
| **anchor-only** — all words before the first hole | words before a hole are matched before any hole opens; nothing can straddle them |
| **symbol separator** — `=>`, `..`, `:` | a symbol is not a word; no name can contain one |
| **bracket-delimited hole** — `{ … }`, `( … )` | a bracket is not a word; the neighbouring word is unswallowable |

Eleven of the twenty-four seed patterns are already anchor-only. The remaining
thirteen each want one of the other two shapes, or a respelling.

## What this changes in the stdlib

| current | glue | fix |
|---|---|---|
| `if (_) then (_) otherwise (_)` | then, otherwise | brace the blocks |
| `for each (_) in (_)` | in | pin the declaring hole to one token |
| `item (_) of (_)` | of | `item (_) in (_)` — or brace |
| `repeat (_) times` | times | `repeat (_) { … }` |
| `sort (_) by (_)`, `filter (_) where (_)`, `join (_) with (_)`, `split (_) on (_)`, `send (_) to (_)` | by, where, with, on, to | anchor-first respelling, symbol, or bracket |
| `round (_) to (_) places` | to, places | `round (_) to (_)` + brace, or anchor-first |
| `(_) rounded` | rounded | postfix is the worst shape — make it `rounded (_)` or a symbol |
| `every (_) seconds` | seconds | `every (_)` with a duration literal |

---

## The honest caveat, and it is not small

**Mechanism 3 is a proposed weakening of R5, and R5 is the rule that was
validated by exhaustive search** — 2,382,240 resolutions, 0 ties, with R5
reserving *all* non-leading segment words unconditionally. The argument above
says some of those reservations are unnecessary. The argument is a structural
one about the word-only invariant, and I believe it, but:

- R5's blanket form is what the fuzzer actually verified.
- The refinement is exactly the kind of narrowing that already went wrong once
  — the first draft of R5 judged "non-leading" by word value instead of
  position, and the fuzzer caught what inspection did not.
- `ronin_grammar_probe.py` cannot currently express bracket tokens inside
  patterns, so `fuzz_verify.py` cannot test this as it stands.

**So: do not implement the refinement on my say-so.** The sound order is

1. Adopt the *shapes* now — they need no rule change. Anchor-first, symbol
   separators, braced blocks. That alone takes the seed stdlib from 13 reserved
   words to a small handful, purely by respelling, with R5 untouched.
2. Extend the probe to carry bracket tokens in patterns.
3. Re-run the exhaustive search with the refined R5.
4. Only then relax the rule and pin the loop variable.

Step 1 gets you most of the way with zero risk. Steps 2–4 are what turn "few
reserved words" into "none", and I'd rather they were earned by the fuzzer than
by my reasoning — that distinction has mattered in this project before.

## Which leaves the loop syntax question genuinely open again

`LOOP-SYNTAX.md` said `for each bank in banks` is safe *because* R5 reserves
`in`. That is still true and still shippable today. But if the pinned-hole
refinement survives the fuzzer, `for each bank in banks` becomes safe **without
reserving anything** — and then there is no argument left for `iterate shoes =>
shoe` at all, because the readable spelling has become the free one too.

I would ship `for each … in …` now under R5 as written, and treat the
refinement as the thing that later removes the last reserved word rather than
as a reason to delay.
