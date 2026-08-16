# The `GlueInInjectedName` removal — accept, with one rule added

> **Ledger** — `[R]` The `GlueInInjectedName` removal — accept, with one rule added
> supersedes: not yet checked
> superseded by: not yet checked

Answering the item flagged in `f97a653`. Short version: **the removal is right,
your reasoning has one hole, and the hole is worse than the diagnostic you
deleted.** Also, the change is less broad than you think — and the thing it
reveals is broader than either of us had written down.

---

## 1. It is not a language change. It surfaces one that was already there.

You wrote that reserving glue as whole names "is a real language change …
broader than `in`". I think it is narrower than that, and the reason matters.

`var seconds` was **already illegal** before your commit, given
`every (_) seconds`:

```
var seconds          declares «seconds»
                     injects «old seconds»          (multi-word)
R5 examines it       contains glue «seconds»        -> rejected
```

`SCOPING.md` says explicitly that R5 is *not* suppressed on injected names, and
that a shadow can fail where its source passes. So R5-plus-shadow-injection had
already reserved every glue word against every single-word **reactive** name.
Nobody noticed, because the only route to the error was a diagnostic about a
name the programmer never wrote.

Your change does three things, and only the third is new:

| | |
|---|---|
| moves the error from the shadow to the declaration | strictly better message |
| moves it from resolve-time to declare-time | strictly better timing |
| extends it to names that get no shadow — `constant seconds` | **the only actual broadening** |

That third one is small and, I'd argue, correct: a rule whose scope depends on
whether a declaration happens to be reactive is a rule nobody can predict.

So: **accept.** `GlueInInjectedName` was a diagnostic for a gap, the gap is
closed, and a dead kind is worse than a deleted one.

---

## 2. The hole: `old` can itself be glue

Your argument was that `old X` contains glue `X` lacks only when the glue is
`old`, which is already refused. That holds only if **no pattern can use `old`
as glue.** If `old` is not protected, a user may declare

```
restore (_) old (_)
```

and `old` becomes glue. Then for *every* reactive declaration in scope:

```
var anything         injects «old anything»
                     multi-word, contains glue «old»    -> rejected
```

Not one diagnostic — **one per variable in the scope**, all pointing at a
pattern the author of those variables may not own and cannot change. That is
the worst diagnostic outcome in the language, and it is reachable from ordinary
source.

I do not know whether your implementation already prevents it, so please check
rather than take this as a claim about the code. If it does not:

### New rule: words that form injected names may not be used as glue

> No pattern may use `old` as a non-leading segment.

Rejected at the **pattern's** declaration, where the mistake is, with its own
finding:

> «restore (_) old (_)» may not use «old» as glue: «old» forms the shadow name
> of every reactive declaration. Respell the pattern.

One error at the offending site, instead of N errors at innocent ones. And it
is what makes `GlueInInjectedName` unreachable *by construction* rather than by
a coincidence of two other rules — which is the difference between deleting a
kind safely and deleting it luckily.

**This generalises.** Any word the compiler uses to build an injected name joins
the protected set. If `index of «loop variable»` is adopted from
`LOOP-INDEX-AND-GLUE.md` §7b, then `index` and `of` join it — and note `of` is
glue *today*, from `item (_) of (_)`. That is the same collision, found in
advance rather than in the field.

The protected set is the dual of the glue registry: **glue words may not be
names; injection words may not be glue.** Both belong in `glue.py` output.

---

## 3. What this reveals, which is the important part

State the true cost of a glue word plainly, because neither of us had:

> A glue word is not merely reserved inside multi-word names. **It cannot be an
> ordinary variable name at all.**

The registry's twelve words are twelve forbidden variable names. `seconds`,
`places`, `times`, `where`, `by`, `with`, `on`, `of` — every one of those is a
name somebody wants, and `var seconds` is illegal today because
`every (_) seconds` exists somewhere in the standard library.

That roughly doubles the price of every glue word, and it lands on names people
actually reach for rather than on the multi-word constructions R5 was written
to protect. It is the strongest argument yet for driving glue to zero — which
is the next thing you should read.

---

## 4. Your other items

All correct, nothing needed from me:

- **The `Pattern` constructor throwing on ordinary source** is the better find
  than the one I filed. Emergent-via-empty-tuple was my reading of the *Python
  model*; that the C# was killing the compiler from valid input is the same
  class as `PatternTooWide` and worth a sweep for others — any constructor that
  can be reached from source text and throws rather than producing a finding.
- Splitting the header on the **last** `in` is right, and for the reason you
  give: R5 permits at most one in a well-formed header, so the choice only
  shows up on input that is already wrong, and the last one leaves something to
  complain about.
- `Identifier` owning its token extent, `Name.Span` going away, test 8, B1, B2,
  and the spec rewording — all good.

## 5. `ZERO-GLUE.md` — read it now

You asked when. Now, with one caveat attached: it is **direction, not
instruction**. It shows three mechanisms that make a construct cost no reserved
words — anchor-first patterns, symbol separators, and bracket-delimited holes —
of which the first two need no rule change and can be applied to the standard
library immediately. The third is a *weakening of R5*, and R5's blanket form is
what the exhaustive search actually verified. The probe cannot express bracket
tokens in patterns yet, so that mechanism has no coverage at all.

Apply the shapes. Do not relax the rule until the fuzzer has been extended and
re-run.
