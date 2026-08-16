# The withdrawal is right, the narrowing is right, and it is not the whole law

> **Ledger** — `[R]` The withdrawal is right, the narrowing is right, and it is not the whole law
> answered by: NAMEVSANCHOR-RESULT
> supersedes: none
> superseded by: none

Three parts. The retraction stands and the test behind it is the correct one.
The narrowed rule — *a declared name that spans an operator takes that
operator* — is R5's shape and it is sound. But it is **not complete**: an
exhaustive sweep turns up a second class of silent capture that R5 does not
cover, R6 does not cover, and no document has ever mentioned. It lives in
exactly the pattern shape three of my documents recommended as free.

`nothing`'s spelling is unaffected either way — that part of his conclusion
holds.

---

## 1. The retraction, confirmed

The test he applied is the right one: **the hazard is a working program
changing meaning, not a broken program starting to work.** Invalid → valid is
monotone and harmless; the reader of the new text has no old meaning in mind.

`name_capture.py` runs that test exhaustively rather than on two examples.
Universe of five words, base names `a`/`b`/`nothing`, pattern
`(_) otherwise (_)`; every declaration of length 2–3 against every source of
length 1–4:

```
  150 candidate declarations x 780 sources = 117000 transitions

    UNCHANGED      116700
    EXTENSION         291      <- NO PARSE before, parses after
    CAPTURE             9      <- parsed before, parses DIFFERENTLY after
    BREAK               0

    dangerous transitions with    a glue word in the name: 9
    dangerous transitions WITHOUT a glue word in the name: 0
```

And the specific case:

```
  a otherwise nothing            OK        -> OK          unchanged
  a otherwise nothing found      NO PARSE  -> OK          EXTENSION
  nothing found                  NO PARSE  -> OK          EXTENSION
```

All nine captures are of his narrowed shape — the declared name spans the
operator:

```
  declare «a otherwise nothing»  on  a otherwise nothing
      before: «a» otherwise «nothing»
      after : «a otherwise nothing»
```

So: **withdrawal correct, narrowing correct as far as it goes.**

## 2. Where it stops going

My first control run was degenerate and I nearly shipped it. I tested
`send (_) to (_)` against names containing `to` and got zero captures — and
the reason is not that medial glue is safe, it is that **with no rival
`send (_)` in scope the literal `to` is mandatory, so a name spanning it has
nowhere to go.** R5's hazard needs a rival reading to exist. Supplying the
rival (`name_capture2.py`, config B) changed the answer:

```
  print (_)  |  (_) otherwise (_)

    CAPTURE            18
    dangerous WITH    a glue word in the name:  4
    dangerous WITHOUT a glue word in the name: 14      <- R5 explains none of these
```

The fourteen:

```
  declare «print a»  on  «print a»
      before: print «a»          2 lookups
      after : «print a»          1 lookup      <- name wins

  declare «print a»  on  «nothing otherwise print a»
      before: «nothing» otherwise print «a»
      after : «nothing» otherwise «print a»
```

**A name costs one lookup. A pattern call costs one plus its arguments, so at
least two.** Therefore any declared name whose tokens begin with a pattern's
anchor run, and whose remainder is a parseable argument, *always* beats the
call on minimum lookup. Both readings are valid, so it is silent — not a tie,
not an error.

Neither existing rule reaches it:

| | subject | fires here? |
|---|---|---|
| R5 | glue words vs multi-word names | no — an anchor run is not glue |
| R6 | anchor runs prefix-free | no — R6 compares **patterns with patterns** |

This is pattern-vs-**name**, and nothing in `SCOPING.md` covers it.

### And it is exactly the shape I called free

`ZERO-GLUE.md` lists "anchor-only — all words before the first hole" as the
first of three free shapes. `LOOP-INDEX-AND-GLUE.md` rule 1 says "a pattern
whose words all precede its first hole reserves nothing … this should be the
default shape." Both are **incomplete as written**, and the reason is
uncomfortable: a glue-bearing pattern is *protected* by R5, because any name
that could shadow the whole call must contain the glue and is already refused.
Only the glue-free patterns are exposed.

So the true statement is narrower than the one I wrote:

> An anchor-only pattern reserves **no word anywhere**. It does reserve its own
> anchor run as a **name prefix**.

`RESERVED (0)` is still a true word count. It is not a complete account of what
patterns cost names, and the registry should say so.

## 3. The exact law, and it is cheaper than the obvious one

The blanket repair — *no name may begin with any anchor run* — would kill
`item count`, `sort order`, `round trip`, `filter text`, `send queue`,
`join key`, `split point`. That is a serious loss for a language sold on
readability, and it is unnecessary. `anchor_rule_shape.py` measures the
smallest rule that still closes the gap:

> **R6b.** No declared name may have the **entire word content** of a visible
> pattern as a proper prefix.

Entire word content, not anchor run. That is only satisfiable by patterns
shaped `w1 … wk (_)` — all words, then one free trailing hole. Any pattern with
glue needs its glue word inside the name, which R5 already refuses. Any pattern
with a bracketed hole cannot be shadowed by a name at all, because a name is a
word-only span and cannot straddle a bracket.

Measured over five configurations:

```
  item (_) of (_)                      captures= 0   R5= 0  R6b= 0   unexplained=0
  item (_) of (_) + count of (_)       captures= 1   R5= 1  R6b= 0   unexplained=0
  print (_)                            captures=15   R5= 0  R6b=15   unexplained=0
  sort (_) by (_)                      captures= 0   R5= 0  R6b= 0   unexplained=0
  sum of (_) + (_) otherwise (_)       captures= 2   R5= 1  R6b= 1   unexplained=0

  [PASS] R6b + R5 closes every silent capture
```

What survives that a blanket ban would have killed:

```
  «item count»    rival item (_) of (_)          has glue -> R5 territory
  «sort order»    rival sort (_) by (_)          has glue -> R5 territory
  «round trip»    rival round (_) to (_) places  has glue -> R5 territory
  «filter text»   rival filter (_) where (_)     has glue -> R5 territory
  «send queue»    rival send (_) to (_)          has glue -> R5 territory
```

What it costs, against the seed registry — the anchor-only patterns are
`print`, `broadcast`, `rounded`, `while`, `sum of`, `count of`, `average of`,
`length of`, `first of`, `last of`, `wait until`:

```
  dead:  print job     print queue    broadcast list    while loop
         rounded value sum of squares first of month    wait until dawn
```

**The single-word ones are the whole bill** — `print …`, `broadcast …`,
`while …`, `rounded …`. Every other anchor-only pattern is two words, and a
name beginning `sum of` or `wait until` is already odd. That is a real cost
and it is a small one; `print job` is the only casualty I would miss.

R6b over-refuses slightly — `print print` is refused though it never captures,
because its tail is not a name. That is the same trade R5 already makes:
predictable beats precise, since the alternative is legality that depends on
which unrelated names happen to be declared.

## 4. Where the error goes

Same convention as R5 and R6 in `SCOPING.md`: check against the **merged**
table, and **reject the declaration that arrives second**, naming both sites.

```
«print job» cannot be declared: «print (_)» from prelude would be shadowed
by it. Rename the variable, or respell the pattern.
```

Symmetrically, a *pattern* whose word content prefixes an existing name is the
one refused, per the existing inner-breaks-outer convention.

**The alternative, stated fairly**, since it is not absurd: let the name win
and leave it, on the grounds that a human reading `print job` in a scope where
`print job` is a variable would read it as the variable too, and the call is
recoverable by bracketing — `print (job)` is unambiguous because a name cannot
contain a bracket. What decides against it is **flat merged symbol tables**: an
outer declaration is visible in every inner scope, so the meaning change lands
at arbitrary distance from the edit that caused it, silently, and the repair
has to be made at the site that did not change. That is precisely the failure
mode this language has refused everywhere else.

## 5. `nothing`

Unaffected, and his conclusion holds: **the spelling is an ordinary choice with
no lexical hazard attached.**

- `nothing` is a *name*, not a pattern. Names have no anchor run, so R6b never
  fires on it and `nothing found` stays legal — confirmed by run 1, where every
  glue-free declaration produced extensions and zero captures.
- A user declaring `nothing` exactly is a **duplicate symbol** under
  no-shadowing — the collision machinery already there, with the right message
  (*name already declared*), and no reservation needed.

So it can go into `docs/spec/` beside `optional` as written, and the choice of
word is free.

## 6. Summary

| | |
|---|---|
| the `nothing found` withdrawal | **correct** — invalid → valid is the right test, confirmed over 117k transitions |
| "a name that spans an operator takes it" | **correct, and it is R5** |
| "that is the whole law" | **no** — R5 covers glue-bearing patterns; anchor-only patterns are exposed |
| my "anchor-only reserves nothing" | **incomplete** — reserves no word, does reserve a name prefix. `ZERO-GLUE.md` and `LOOP-INDEX-AND-GLUE.md` need the qualification |
| R6b | recommended: no name may have a pattern's entire word content as a proper prefix. Measured complete; costs `print job` and little else |
| `nothing`'s spelling | free |

Probes: `name_capture.py`, `name_capture2.py`, `anchor_prefix.py`,
`anchor_rule_shape.py`. The first control run in `name_capture.py` §2 is
**wrong by omission** and left in deliberately with the diagnosis attached —
it is worth seeing how a passing sweep can mean nothing.
