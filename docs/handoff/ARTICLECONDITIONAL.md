# Yes it can be conditional — and a correction that changes what the rule is *about*

> **Ledger** — `[V]` Yes it can be conditional — and a correction that changes what the rule is *about*
> supersedes: not yet checked
> superseded by: not yet checked

Answering the narrowing question, but the correction has to come first because
it changes the subject: **my "edge glue never captures" result leaked**, and the
leak is the same phenomenon as the article problem rather than a separate one.

---

## 1. Correction: `R5′` is not sufficient on its own

Last round I reported that a name whose glue words are all at an **edge** never
captures, with one apparent counterexample (`not x`) that I attributed to R6b
because that configuration happened to contain the anchor-only pattern
`not (_)`.

Re-run with **no `not (_)` pattern at all** — only the four `is` patterns:

```
  base               x is not x    OK             «x» is not «x»
  + name «not x»     x is not x    TIE -> ERROR
```

So `not x` is edge-glue, is not R6b's, and still breaks a working statement.
My attribution was wrong and the previous config was masking it.

**Why it happens** is the useful part. The rival is not a capture, it is the
*other pattern*:

```
  «x» is not «x»          via  (_) is not (_)      3 lookups
  «x» is «not x»          via  (_) is (_)          3 lookups
```

`(_) is not (_)` is `(_) is (_)` with the literal word `not` inserted at the
start of its trailing hole. A name beginning with that word can occupy the
shorter pattern's hole and swallow the difference. That is **exactly** the
article problem:

```
  (_) is (_)              the base
  (_) is a (_)            + «a»    at the hole boundary  -> «a number» ties
  (_) is not (_)          + «not»  at the hole boundary  -> «not x» ties
  (_) is not a (_)        + both
```

## 2. So the rule is not about articles — it is derivable from the registry

> A name may not begin with the **first word of any literal run that
> distinguishes one pattern from a shorter one at a hole boundary.**

For this family that set is exactly `{a, an, not}`. It is computed from the
pattern table, not chosen, so it goes in the generated registry beside the glue
list and nobody has to remember to update it.

**And it does not include `is`** — there is no pattern `(_) (_)` for `is valid`
to refine, so nothing can absorb it. That is why R5′'s win survives: `is valid`
stays legal, and it was the name shape worth saving.

So the three-rule set from last round becomes:

```
  R5′    no multi-word name may contain a glue word INTERIORLY
  R6b    no name may begin with a pattern's whole word content
  R7b    no name may begin with a pattern-distinguishing word   ← generalises R-art
```

## 3. Your question: yes, R7b can be conditional, and it is exactly as strict

Measured. `a queue` threatens nothing because `queue` is not a type:

```
  with value name «a number»   (remainder IS a type)
      x is a number       TIE -> ERROR
  with value name «a queue»    (remainder is NOT a type)
      x is a number       OK        «x» is a «number»ᵗ
```

Exhaustive sweep over the universe: **four** declarations break anything, and
the conditional predicate accounts for the article case precisely.

The general conditional form: a name `W rest` is refused when `W` is a
distinguishing word and `rest` resolves **in the table the refined hole
expects** — the *type* table for `a`/`an`, the *value* table for `not`.

And it is not a kind of conditionality I object to. `GLUE-AS-WHOLE-NAMES.md` §1
rejected legality conditional on **use sites**. This is conditional on the
**declaration tables**, which is what R5 and R6 already are — `SCOPING.md`
already says an inner pattern can invalidate an outer name and the inner
declaration is the one refused. Same species.

The diagnostic also gets *better*, not worse:

```
  «a number» cannot be declared: «number» is a type, so «x is a number»
  would have two readings. Rename it, or the type.
```

versus "no name may begin with `a`", which explains nothing.

## 4. And I would still take the blanket rule — on arithmetic, not principle

What conditional buys, from the corpus:

```
  R7b blanket   names beginning «a»/«an»     876    0.257%
                names beginning «not»        443    0.130%
                                            ----   -------
                                                    0.387%
  R7b conditional                                  ~0.000%
```

What it costs:

- a rule whose statement takes a paragraph instead of a clause;
- one more way a **later** declaration invalidates an **earlier** one — adding
  a type `queue` retroactively breaks a variable named `a queue`;
- and therefore one more case handed to the import differential check from
  `MODULE-MERGE.md` §4, because across a module boundary a library adding an
  exported type could invalidate an importer's variable name. Under the blanket
  rule that cannot happen, because the name was never legal.

0.387% of identifier space, against a three-word blacklist a programmer can
memorise — *don't start a name with `a`, `an`, or `not`* — and which never
changes under them. I think the blacklist wins, and I notice none of the three
is a word anyone actually wants at the head of a name: `a queue` and `not found`
both read better as `queue` and `missing`.

**But this is a cost/benefit call and it is yours.** The measurement is the
argument; I do not think either choice is wrong.

## 5. The compromise I would actually build

Generate the set from the registry either way, and take the blanket rule **now**
with a documented trigger for revisiting:

- today the set is `{a, an, not}` — three words, blanket is cheap;
- if the stdlib later adds `(_) is like (_)`, `(_) is at least (_)`,
  `(_) is within (_)` and the set reaches five or six, blanket starts costing
  real names and the conditional form earns its complexity.

The generator is identical for both, so switching later is a change to one
predicate and a regenerated registry — not a language change. That makes this a
decision you can defer *cheaply*, which is rare enough to be worth taking.

## 6. Summary

| | |
|---|---|
| my "edge glue never captures" | **corrected** — `not x` leaks via the `is`/`is not` pattern pair, not via R6b |
| what the rule is about | not articles — **pattern-distinguishing words**, derivable from the registry. Today `{a, an, not}` |
| `is valid` | still legal, because nothing refines `(_) is (_)` at its *first* hole |
| can it be conditional | **yes** — measured exactly as strict as the ambiguity, and the diagnostic improves |
| should it be | **I lean blanket**: 0.387% versus a memorable three-word rule and no new invalidation path |
| how to build it | generate the set from the registry either way; blanket now, revisit if the set passes ~5 words |

Probe: `article_conditional.py` — namespace-aware, so the question can actually
be asked; a shared table cannot distinguish the two readings at all.
