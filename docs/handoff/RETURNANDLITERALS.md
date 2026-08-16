# `return`, the empty slot, and the literals — three answers and two notes back

> **Ledger** — `[V]` `return`, the empty slot, and the literals — ruled. The inference rules it implies for the checker are recommendations.
> supersedes: not yet checked
> superseded by: not yet checked

Your reading in §1 is right, your §2 is right and I want to restate *why* so the
asymmetry stops being something we keep re-worrying, and §3's scheduling note is
correct enough that I am just ruling the literals here rather than deferring them
again.

Both code notes land. One of them resolves an open question in `OVERLOADS.md`
and the other is evidence for `FIVE-RULINGS.md` §4 arriving from a direction I
did not anticipate — and it changes what §4's *kind* field is for.

---

## 1. `return` in a `when` body

**Your reading is confirmed: bare `return` is the runtime's existing operation,
and they are two builtins.** Three things follow, and the third is the one that
answers "same commit or not".

### 1a. The arity split is mechanically free

You noted they are prefix-related and that R6's deleted clause is what permits
it. Prefix-related is not the same as ambiguous, and the difference is
measurable:

```
  patterns = { return (_),  return,  sum of (_) }

  return               -> OK   cost=1        the nullary one, only reading
  return x             -> OK   cost=2        the one-hole one, only reading
  return sum of x      -> OK   cost=3
  return value         -> OK   cost=2
```

No tie anywhere, and for the reason we keep leaning on: **there is no
juxtaposition**, so bare `return` followed by `x` is not a composition — it is
not a reading at all. So `return x` can only be the one-hole pattern and
`return` alone can only be the nullary one.

The split costs nothing at the parser. It is a naming question, not a parsing
one.

### 1b. They are one concept at two arities, and the legality rule is positional

They are not two unrelated operations that collided on a word. Both mean **leave
this body now**; they differ in whether there is an answer to carry. That is
C#'s unification and it is read correctly by everyone.

What Ronin adds is that it has *two kinds of body*, so the rule is about where
each is legal:

| body | `return (_)` | bare `return` |
|---|---|---|
| a function that answers | **required** | refused — leaving without the answer |
| a function that never answers (an action) | refused — nothing to answer for | **legal** |
| a `when` body | **refused** — nobody consumes a reaction's value | **legal** — your `Graph.Return` |

**A body has exactly one exit flavour**, decided by whether any `return (_)`
appears in it. Mixing the two in one body is refused, and that refusal is not an
extra rule — it is the same check that stops the return type from having two
answers.

The message for the `when` case is worth writing once and reusing, because it is
the case a newcomer will hit:

> `«return (_)» in a «when» body — a reaction has nobody to answer. Use «return»
> to end this run, or «stop» to disarm the «when».`

### 1c. So: same commit — because the legality rule *is* the inference pass

This is the answer to what you actually asked. Collect the `return (_)` sites in
a body:

```
  none        -> the body's answer type is the ACTION type
                 and bare «return» is the legal exit
  some        -> unify their argument types into the answer type
                 and bare «return» is refused
```

**One walk, two outputs.** §2's inference and §1b's legality rule are the same
computation reading the same collected set. Shipping `return (_)` alone means
shipping that walk with one of its two outputs discarded, then reopening it when
inference lands. Ship both.

### 1d. One readability flag, which is Budai's call and not mine

`stop` disarms the `when`; `return` ends this run of it. Two short words, similar
shapes, different permanence — and the difference is invisible at the call site.
This is precisely the class of thing this language exists to avoid, and it will
cost someone an afternoon.

Measured alternatives for the valueless exit, so the option is priced rather than
hypothetical: `done` costs **4** exact collisions in a 460k-identifier corpus.
`return` bare costs nothing extra, since `return (_)` already pays the anchor.

```
  return x     the answer          }  reads as three different things,
  done         end this run        }  because it is three different things
  stop         disarm the «when»   }
```

I would rather `done` than `return` for the valueless case, on the grounds that
`return` with nothing after it is the one place the word lies. But it costs a
reservation and a rename in shipped code to buy clarity in a rare position, so I
am putting it up rather than ruling it. **If it is not taken, the `stop`/`return`
pair needs one documented sentence, in the reference, in both entries.**

---

## 2. The empty slot — right, and here is why it was never an asymmetry

Budai's ruling is correct. I want to restate it in a form that stops it coming
back, because "omission means generic in one position and inferred in another"
sounds like an exception and is not one.

> **Omission means *not written down*. What it becomes is whatever inference can
> determine. The two positions differ because they have different information
> available, not because they have different rules.**

```
  parameter position   inference has NOTHING to work from -- a parameter is an
                       input. So it yields a type variable, which is what
                       "generic" is. GENERICS-II §3 unchanged.

  return position      inference has THE BODY. So it yields a concrete type, or
                       the action type if the body never answers, or a type
                       variable if the answer depends on a parameter -- in which
                       case the function is generic in its return type too, and
                       nothing special happened.
```

One rule. The parser comment stays true — a consumed `=>` commits — and the
"opposite meaning in two positions" problem dissolves rather than being excepted.

**Two consequences worth having before you build it.**

**Recursion needs a written return type.** A body whose answer depends on its own
call has no fixed point for a first-pass inference to find. Requiring the
annotation there is standard, cheap, and produces a good message; discovering it
via a stack overflow is not. One line in the rule, now.

**An inferred signature is invisible at the call site**, which matters more here
than in most languages because a caller's own type then depends on a body it
cannot see. In a language with a batch compiler that would argue for requiring
annotations at module boundaries. Here it argues for something better and
cheaper: **the IDE displays the inferred return type inline**, the same way
Budai wanted implicit brackets shown on hover. Always-running IDE, so the
information is never stale. I would not add a rule for this; I would add it to
the tooling list.

---

## 3. Truth literals — ruled, so they can land with the type

You are right that the type without the literals is untestable, and I was wrong
to file them as separable. Ruling them here rather than deferring again.

**`true` and `false`.**

```
  literal      exact collisions in 460,030 identifiers
  true             5        also leaves «true positive», «true north» alone
  false            5
  yes              3
  no               5
  on               6
  off              4
```

A literal is a **nullary entry — a name, not a pattern** — so it reserves its own
spelling and nothing else. Every candidate is affordable; there is no budget
attached to this and it is purely a readability choice.

`true`/`false` over `yes`/`no` for two reasons: it matches the type name
`truth` morphologically, and `x is yes` reads worse than `x is true` in the
position these actually appear in. `on`/`off` are *state*, not truth, and will be
wanted later for something else.

Not a place to be inventive. The readability budget belongs in statements.

---

## 4. Your two code notes

### 4a. `Overloads` was already a candidate set — good, and one follow-up

`OVERLOADS.md` §3 flagged "whether declaration lookup returns at-most-one by
construction" as something I could not see. Answered: it never did, and
`Scope.Invoke`'s `«{pattern}» is ambiguous after type filtering` is the
`|Candidates| > 1` diagnostic written before the question was asked. The
candidate set is not a missing field. Good — that removes the only part of §3
that was a change rather than a description.

**One follow-up, and one question.**

The follow-up: that error is now repairable, which it was not when it was
written. `FIVE-RULINGS.md` §3 rules expression-level ascription in, so the
diagnostic should carry `(x => Text)` as a **selectable suggestion**, in the same
form and the same UI as the bracketing suggestions in `AMBIGUITY-AS-ERROR.md`.
Same shape, same ranking discipline — *cost may order suggestions, never choose
among them*.

The question, because I cannot see the tree: **is `Scope.Invoke` a compile-time
resolution step or a runtime one?** The name reads runtime. If overload ambiguity
is only detected when the call executes, then it is not ambiguity-as-error — it
is a runtime failure on a program the editor said was fine, and the selectable
repair has nowhere to appear. If it is compile-time, ignore this.

### 4b. Type annotations against the value table — this sharpens §4

This is better evidence for one-table-with-kinds than anything in the ruling,
because it is the failure happening rather than being predicted, and it arrived
from a direction I did not anticipate: not *the rules don't run*, but *resolution
has nowhere correct to send the question*.

And it tells us what the kind field is actually for, which the ruling
under-specified. **A type position does not select a different table. It supplies
an expected *kind*, and the candidate set narrows on kind exactly as it narrows
on type.**

```
  resolve      Call / reference carries a candidate SET
  filter       narrow by KIND   (type position admits kind=type only)
               narrow by TYPE   (the ordinary elimination)
               -- one pass, two predicates, same code
```

So your prune becomes principled rather than a stop-gap: it is not a prune, it is
the kind predicate, and it belongs in the same filter as the type predicate
rather than beside it. That is the third job for the mechanism in `OVERLOADS.md`
§3 — overload narrowing, reading elimination, and now kind selection — which is
a good sign that the mechanism is the right one rather than a coincidence.

And the payoff you half-saw: an annotation whose words are genuinely ambiguous
**as types** then gets a proper ambiguity error quoting type readings, instead of
either a wrong answer or silence.

---

## 5. Summary

| | |
|---|---|
| bare `return` | **yes, it is your `Graph.Return`.** Two builtins, one concept, two arities |
| the arity split | **mechanically free** — measured, no tie, because there is no juxtaposition |
| `return (_)` in a `when` body | **refused.** A reaction has nobody to answer |
| exit flavour | one per body, decided by whether any `return (_)` appears. Mixing refused |
| same commit? | **yes** — §1b's legality rule and §2's inference are one walk with two outputs |
| `stop` vs `return` | flagged. `done` costs 4 names and reads honestly; if not taken, both entries need a documented sentence |
| the empty slot | omission = *not written*; inference yields what the position's information supports. **One rule, two outcomes** — not an asymmetry |
| recursion | **needs a written return type.** Decide now, not via a stack overflow |
| invisible signatures | a tooling answer (IDE shows the inferred type inline), not a rule |
| truth literals | **`true` / `false`**, ruled now so they land with the type. 5 collisions each; no budget attached |
| `Overloads` already a set | confirmed — removes the only *change* in `OVERLOADS.md` §3. Add the ascription repair as a selectable suggestion |
| is `Scope.Invoke` compile-time? | **question.** If it is runtime, overload ambiguity is not ambiguity-as-error and the repair has nowhere to appear |
| annotations vs the value table | **kind is a filter, not a prune** — same pass as the type filter. Third job for one mechanism |

Probes: `return_arity.py`, and `reserve_cost.py` for the corpus method.
