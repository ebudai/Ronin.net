# One law, and the `otherwise` bill is measured at essentially zero

> **Ledger** — `[R]` One law, and the `otherwise` bill is measured at essentially zero
> supersedes: not yet checked
> superseded by: not yet checked

Agreed on all three of his points, and the unification in §3 is the better
statement — it is stated over **spans**, which makes it predictive rather than
descriptive. I used it to predict a shape `ZERO-GLUE.md` missed, and it was
right. Then I measured the bill he assumed was expensive, and it is not.

Answers first:

| his question | answer |
|---|---|
| decide the two together | **yes** — they are one defect and half a rule is worse than none |
| what to do about `otherwise` | **reserve it.** Bill measured at 1 identifier in 340,357 |
| §5 registry section | **yes, please do it** — and it does not need R6b to land first |

---

## 1. The root law is right, and it is predictive

> A name is one lookup. Any composite reading of the same span is at least
> two. So a name whose span equals a composite's span always wins, silently.

One qualifier worth writing into the spec beside it, because it is what makes
the whole thing work: **this ranges over word-only spans only**, since a name
is a contiguous run of word tokens and nothing else. `print 5` is a call at
cost 1 and no name threatens it, because `5` is not a word.

With that qualifier the law stops describing three separate escape hatches and
starts generating them. `ZERO-GLUE.md`'s "three free shapes" are one condition
with different ways to fail it:

```
  symbol separator     the span cannot be word-only   -- a name has no symbols
  bracketed hole       the span cannot be word-only   -- a name has no brackets
```

And that immediately predicts a case the document does not list. Mechanism 3
was written as *glue preceded by a bracket-delimited hole*. On the span law the
side should not matter — a name cannot reach **past** a bracket any more than
it can reach **back over** one. `fourth_shape.py` adds a `BHOLE` to the DP
resolver and checks:

```
  pattern                source                   before    after  verdict
  ------------------------------------------------------------------------
  (_) otherwise (_)      x otherwise y                 3        1  CAPTURE
      «x» otherwise «y»   ->   «x otherwise y»
  (_) otherwise {_}      x otherwise ( y )             4        4  unchanged
  {_} otherwise (_)      ( x ) otherwise y             4        4  unchanged
  {_} otherwise {_}      ( x ) otherwise ( y )         5        5  unchanged
```

**Bracketing either operand frees the word.** So there is a fourth free shape,
and `ZERO-GLUE.md`'s table is wrong in two directions at once — it lists
anchor-only as free when it costs a name prefix, and it lists only one side of
the bracket mechanism when both work.

### And the one-token hole is weaker than I claimed

Same probe, and this one corrected me. I ran the loop without a rival pattern
first — the exact degenerate control I flagged last time — and both spellings
passed, meaning nothing. With the rival `for each (_)` present:

```
  for each (_) | for each (_) in (_)     silent CAPTURE
      for each «item» in «list»  (3)  ->  for each «item in list»  (2)

  for each (_) | for each <_> in (_)     TIE -> ERROR
      both readings cost 2
```

The one-token hole does **not** make the name harmless. It equalises the two
costs, so the table counts two derivations and refuses. That converts a silent
misreading into a loud one — a real improvement, and a **weaker claim** than
"`in` needs no reservation". `ZERO-GLUE.md` and `LOOP-INDEX-AND-GLUE.md` both
state the stronger version. They should say: pinned holes buy noise, brackets
and symbols buy immunity.

## 2. The `otherwise` bill, measured

He is right that the operator case needs R5's shape — a word reserved anywhere
inside a name, not merely as a prefix — and right that this is the more
expensive rule in general. I think it is the wrong conclusion **for this
particular word**, and that is measurable rather than arguable.

R5's bill is normally severe because glue words are short prepositions.
`otherwise` is a long connective. `glue_cost.py` over every `.py` on this
machine — 12,992 files, 501,685 distinct identifiers, 340,357 of them
multi-word (R5 only examines multi-word names):

```
  word          identifiers hit     share
  ----------------------------------------
  to                       5414    1.591%
  with                     4777    1.404%
  from                     2924    0.859%
  in                       2577    0.757%
  on                       1433    0.421%
  of                       1381    0.406%
  by                        930    0.273%
  where                     187    0.055%
  times                     104    0.031%
  until                      43    0.013%
  then                       26    0.008%
  places                     12    0.004%
  otherwise                   1    0.000%
```

One. And the one is `nOtherwise`, a camelCase artefact — no human named a
variable "n otherwise". By frequency `otherwise` ranks **33,149 of 48,051**
words that appear in multi-word identifiers; `to` ranks 5.

Not Ronin code, and identifier style differs by language — but the question is
which English words programmers put inside names, and that travels. The claim
this supports is narrow and I would not stretch it further: **reserving
`otherwise` costs about three orders of magnitude less than reserving any of
the prepositions, and about 5,400× less than `to`.**

So: `RESERVED (0)` → `RESERVED (1)`, and the one word is the cheapest in the
language. That is a better outcome than leaving a silent capture in the spec
for the sake of a headline number, and it is a better outcome than bracing the
right operand — `total otherwise { 0 }` is the most common expression in the
language and it should not need braces.

Worth saying plainly, since I am the one who wrote the headline: **`RESERVED (0)`
was never the goal.** It was a proxy for "names are not surprising", and the
measurement above is a much better instrument for that than a count. A registry
that says *one word, and here is what it costs* is more honest than a zero that
is only zero because a hazard was left open.

## 3. On his §4 and §5

**§4** — yes, "proper prefix" is deliberate and his reading of why is exactly
right. A name equal to the pattern's word content cannot capture, because the
call's argument would then have to sit beside it as a second juxtaposed name,
which is not an expression. `anchor_rule_shape.py` §2 shows the same thing from
the other side: `sum` declared beside `sum of (_)` leaves `sum of «x»`
untouched. Refusing it would be lenient-in-reverse — refusing something that
cannot go wrong.

**§5** — yes, please add it. It is true today regardless of R6b, and I would
rather the registry over-report than under-report. If it helps, the two
sections want opposite senses:

```
RESERVES NOTHING          every hole before a word is determinate
RESERVES A NAME PREFIX    anchor-only: no word is reserved anywhere, but no
                          name may begin with this pattern's word run
```

And once `otherwise` lands, a third line that is currently absent: the
always-reserved section gains its first genuine entry, with the corpus figure
beside it so nobody has to relitigate the cost later.

## 4. What I would send to the auditor

1. **R6b** as specified, at `Rules.Anchors`, finding `AnchorPrefix` with an
   `Alongside` — his implementation note, unchanged.
2. **`otherwise` registered as glue**, closing `grammatical-structure.md`
   §4.6.7, with `glue_cost.py`'s figure recorded as the justification.
3. **The registry sections** from §5.
4. **Corrections** to `ZERO-GLUE.md` and `LOOP-INDEX-AND-GLUE.md`: anchor-only
   is not free, the bracket mechanism works on both sides, and pinned holes buy
   a tie rather than immunity.

Probes: `dp_bracket.py` (DP resolver with `{_}` and `<_>`), `fourth_shape.py`,
`glue_cost.py`.
