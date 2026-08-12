# Five rulings — and the one law that decides three of them

All five are answered. Three of them are the same answer, so that goes first.

---

## 0. The law

> **A word that participates in parsing must live in the table the name rules
> run over. A keyword is a name the rules cannot see.**

This is not a new principle. It is `GENERICS-II.md` §8b — *"the registry
generator has to cover the type namespace or the check silently does not run
there"* — stated generally instead of about type registries. Measured, on the
real resolver, with `return` in both costumes:

```
  «return (_)» is a PATTERN
      return value                    -> OK   cost=2      the call
      declare the name «return value»
      return value                    -> OK   cost=1      the NAME now wins
      self-ambiguity check            -> other reading exists: True
      VERDICT                            REFUSED at declaration

  «return» is a KEYWORD
      self-ambiguity check            -> other reading exists: False
      VERDICT                            ACCEPTED -- nothing refuses it
```

Same program, same hazard, opposite verdicts. **The rule did not change; the
table did.** And note the middle line: the capture is not hypothetical — the name
resolves at cost 1 against the call's cost 2, so it wins on minimum lookup,
silently. That is `anchor_prefix.py`'s finding arriving at a keyword.

**Symbols are exempt, and for a reason rather than by fiat.** No name and no
operator may span a symbol, so a symbol cannot be captured at all. Words can.
That is the whole difference, and it is why `=>` costs nothing in §3 below while
`return` costs 0.058%.

This law rules #1 (`return` is not a keyword), #4 (types are not a second table),
and #5 (`optional` stops being a keyword). Each also has its own arithmetic, below.

---

## 1. `return` — a builtin pattern, `return (_)`

**Ruling: `return (_)` joins `for each (_) in (_)` and `old (_)` in `Builtins`.
Not a keyword.**

Three reasons, in order of weight:

1. **The law.** A keyword `return` leaves `return value` declarable, and the
   silent capture above is the result. As a builtin pattern it is refused at
   declaration with the message the rule already produces.
2. **`ONE-LAW.md`.** We spent a whole document collapsing two mechanisms into
   one. Adding a lexer keyword now re-forks it for the sake of one word.
3. **Cost is affordable.** It is anchor-only, so no name may begin `return`:

```
  anchor         refused         %   note
  old                421    0.137%   ruled: pattern, accepted
  return             178    0.058%   THIS RULING
  wait               116    0.038%   ruled: accepted
  previous            79    0.026%
  optional            53    0.017%   THIS RULING
```

0.058% — less than half of a reservation we already took deliberately. Examples
refused are `return_z`, `return_value`, `return_type`. `give (_)` (0.001%) and
`answer (_)` (0.002%) are nearly free, but this language's premise is that the
common case reads like what it is, and `return` is the word every reader knows.
**Pay the 0.058%.**

**Two follow-ons.**

*Bare `return`* for actions is a zero-hole pattern, prefix-related to
`return (_)`. That used to be refused by R6's prefix-free clause — which we
measured deletable and deleted. So it is available now, and this is the first
place that deletion pays. Whether you want early exit at all is a separate
question; if yes, it costs nothing extra.

*`Graph.Return`* — I cannot see the tree, so this is a question, not a claim:
does the runtime's `Return` mean *"the value of this function"* or *"the value of
this graph node"*? If the reactive graph has its own notion, `return (_)` inside
a `when` block needs a ruling and I would rather write it than have it inferred.

---

## 2. `truth` and `nothing` as types

**Ruling: `truth` is an ordinary user-visible type. The action type exists and is
comparable, but it is not the value `nothing` and does not need a surface
spelling.** These are two things and conflating them is what has kept the
question open.

### 2a. `truth`

A type, spelled `truth`, in the one table (§4). Whole-name reservation only —
2 exact collisions in a 460k-identifier corpus, and `truth value`, `truth table`
stay legal, because a type is a **name**, not a pattern.

That unblocks all three rulings verbatim: `SHRINK-TAGGING.md` §1's residue is *a
name spanning a comparison and declared `truth`*; §4's tagging criterion compares
against `truth`; and the operator-side "survives the shrink" group can be
populated the day the type exists.

The literals — `true`/`false`, or something that reads better — are a separate
small decision and do not block anything. Do not let them hold this up.

### 2b. `nothing` — the trap

`NOTHING-AND-INDEXING.md` §1.1 already settled that **`nothing` is a value**: the
no-value constant that `optional T` admits. What the type checker now needs is
something different — *the return type of an action*, so that
`DONT-DO-THAT.md` §3's criterion (*compare the pattern's return type*) is
computable.

**If those are made the same thing, the elimination it exists to justify
breaks.** Give an action call the type of a value and it becomes admissible in
value position, and `send price` goes back to being ambiguous — the exact case
the criterion was written to resolve.

So:

| | |
|---|---|
| the **value** `nothing` | unchanged. Inhabits `optional T`. `x is nothing` keeps working |
| the **action type** | a type in the checker, comparable to every other type, **admitted in no value position** |

The action type needs no surface spelling, because nobody writes it: the
declaration form already says whether a thing is an action or a function. That is
the part I cannot verify — **does the declaration form distinguish them today?**
If it does, this ruling costs one enum case and nothing else. If it does not,
that is a real gap and worth knowing before the type checker rather than during
it.

With that split, `DONT-DO-THAT.md` §3 is exactly right as written and needs no
amendment: the action type differs from every value type, so action-pattern
collisions are all recovered, and the residue is only same-type value
collisions.

---

## 3. Expression-level type ascription — `(x => text)`

**Ruling: yes, and it is not a new construct.** `=>` already attaches a type to a
name in two places — `var n => number`, and `function max of (a) (b => type of
a)`. Ascription is the **same production admitted in one more position**.

```
  form            (expr => Type)
  cost            zero -- «=>» is a symbol, and symbols cannot be captured (§0)
  semantics       a CHECK, never a coercion. It filters candidates; it does not
                  convert. Same discipline as elimination-not-preference
  binding power   loosest. «show x => text» is «(show x) => text»
  repair form     the compiler always emits the BRACKETED form, so nobody has to
                  reason about precedence to accept a suggestion
```

It composes with what is already decided rather than sitting beside it: an
ascription supplies the **expected type** at its position, which is precisely the
outward-in mechanism of `GENERICS-II.md` §4, which is precisely what narrows
`|Candidates| > 1` in `OVERLOADS.md` §3. One mechanism doing three jobs.

**So same-shape overloading is cleared to land** — that was the condition I set,
and it is met. The repair for an overload ambiguity is `show (x => text)`, and it
costs the language nothing it did not already have.

---

## 4. One symbol table, entries carrying a kind — and I am reversing myself

`GENERICS-II.md` §8a says *"I would separate them."* **That was wrong, and I would
withdraw it.** What changed:

**a. `type of x` makes the two tables meet.** Budai's own design decision — *"if
we need the actual type we have compile-time access to it via `type of x`"* —
puts a **type into a value position**. Position-selected lookup is what a second
table depends on, and this is the case it cannot answer.

**b. §8b's failure mode is a consequence of separation, not a caveat on it.**
Measured in §0. Two tables means every name rule — R5′, R6b, R7b, self-ambiguity
— runs twice and must be kept in step, and the failure when it is not is *silent*.
That is the most expensive kind of bug this project has had.

**c. It is the same move as `OVERLOADS.md` §3, and that is not a coincidence.**
There: don't fork derivations to encode a distinction, carry it as a field. Here:
don't fork the table, carry a **kind** on the entry. Both times the forked
version costs more and answers worse.

**d. The prize is small.** A separate table buys back only names spelled exactly
like a type:

```
  truth      Truth, truth                         nothing    Nothing, nothing, ...
  number     Number, _number, number, ...         text       Text, _text, text, ...
  list       List, _list, list_, ...              lookup     Lookup, _lookup, ...

  identifiers recovered by a second table : 33  of 460,030   (0.0072%)
```

**Caveat, stated because it cuts against me:** this corpus is Python, and Python
programmers already avoid naming things `list` and `text`. The true figure for
Ronin is higher than 0.0072%. It is still not two symbol tables' worth.

### What "one table, kinds" means concretely

```
  entry   Spelling  +  Kind : {value, type, pattern}  +  ...

  every name rule runs ONCE, over everything
  a type and a value may not share a spelling  -- and this falls out of the
      self-ambiguity rule for free, correctly: «type of x» can put both in the
      same position, and no bracketing selects between them
  the reserved-words registry needs NO second namespace
  the expiry ledger needs NO second namespace
```

**And one registry for patterns too**, which is the half of §8a that was actually
about glue. A type constructor `list of (_)` and a value pattern `list of (_)`
are then the same shape with two candidates — which is exactly `OVERLOADS.md`
§3's candidate set, and the type filter separates them because one returns a
*type* and the other returns a *list*. The mechanism that makes overloading work
makes this work.

The price §8a was chasing — *"glue words are spent per namespace, which roughly
halves the registry pressure"* — is not recovered. But `GENERICS.md` §1 already
established that type constructors are **anchor-first and cost zero glue**, so
they were never generating much of that pressure. Small price, and it is now
visible instead of assumed.

---

## 5. `optional` becomes the pattern `optional (_)`

**Ruling: yes.**

Under §0 it is not really a choice any more. A modifier keyword `optional` is a
word that parses and is not in the table, so `optional value` is declarable and
captures. Under §4 there is one table for it to live in. And `GENERICS.md` §1
already made every other type constructor a pattern; leaving this one as a
keyword is the fork, not the change.

Cost: **0.017%, 53 names** — the cheapest reservation we have taken. It removes a
keyword from `lexical-structure.md:20` and a modifier from
`grammatical-structure.md:6`, so the shipped-code exposure is a deletion in two
places plus a `Builtins` entry.

**Not in scope of this ruling:** whether `optional` gets special narrowing
behaviour (`if x is nothing` refining the type in the else branch). That attaches
to the *declaration*, not the syntax, and it can be added later without moving
anything decided here.

---

## 6. Summary

| # | question | ruling |
|---|---|---|
| 0 | — | **a word that parses must be in the table the name rules run over.** Symbols exempt, because they cannot be captured |
| 1 | `return` | **builtin pattern `return (_)`**, not a keyword. 0.058%. Bare `return` is now available because R6's prefix-free clause went |
| 2 | `truth` | **yes, an ordinary type.** Whole-name reservation, 2 collisions. Unblocks all three rulings today |
| 2 | `nothing` | **two things — keep them apart.** The *value* is unchanged; the *action type* is a checker-internal type admitted in no value position. Conflating them breaks the elimination it exists to justify |
| 3 | ascription | **`(x => text)`.** Not a new construct — the existing `name => type` production in one more position. Zero cost. A check, never a coercion. Binds loosest; repairs emit brackets. **This clears same-shape overloading to land** |
| 4 | one table or two | **one table, entries carry a kind.** I am reversing `GENERICS-II.md` §8a. `type of x` makes them meet; two tables run every rule twice and fail silently; the prize is 0.0072% |
| 5 | `optional` | **`optional (_)`.** 0.017%, the cheapest yet. Deletes a keyword and a modifier |

**Open, and flagged rather than guessed:** does `Graph.Return` mean the function's
value or a graph node's (§1)? Does the declaration form already distinguish an
action from a function (§2b)?

Probes: `keyword_escape.py`, `reserve_cost.py`.
