# Four answers, one correction to how I pictured R7b, and a name for what Budai spotted

> **Ledger** — `[R]` Four answers, one correction to how I pictured R7b, and a name for what Budai spotted
> supersedes: not yet checked
> superseded by: not yet checked

Answering in the order that unblocks him. §2 is measured and actionable today;
§1 is the one I want to take properly rather than answer off the cuff.

---

## 0. His per-scope implementation is right and my framing was wrong

> *"I compute the relation per scope rather than generating a fixed word set"*

That is better than what I described, and the difference matters. I kept saying
"generate the set from the registry", which quietly assumes the pattern table is
**fixed at build time**. It is not — patterns are user-declarable, so a fixed
word set is a snapshot of the prelude and is wrong the moment a program declares
a pair.

The correction is to my documents, not his code: **R7b is a relation over the
patterns and operators in scope, not a word list.** The registry can *print*
today's set as a convenience, but the printed list is a report, not the rule.
`R7B-CONDITION.md` §1's "generate the set; if empty, defer" is exactly the
registry-style thinking he is describing — his answer (empty today, but computed
per scope so a program can still trigger it) is the right shape and makes §2 and
§3 of that document non-deferrable, as he says.

## 1. Leading holes — I want to take this properly, and it should go first

Highest value on the list because it **deletes** work rather than ordering it: if
leading free holes land, B₁ is not built, and R7b's operator half, B₂ and C all
change shape.

Not answering it in a paragraph, but here is the lead, so he knows the direction:

**R6's refusal of a leading free hole may be a property of the matching strategy,
not of the language.** `R6-AND-INFIX.md` §1 established the mechanism — a leading
hole gives an empty anchor run, and the empty tuple is a prefix of every anchor
run, so R6 refuses it against everything. That is a real problem for a
*predictive* matcher, which must decide what it is matching before it has matched
it. A **chart** parser does not have that problem: it offers every candidate and
lets cost decide, and indistinguishability surfaces as a **tie**, which is
already an error.

The evidence is already sitting in this session's probes. Every `dp_resolver` run
with `(_) otherwise (_)`, `(_) is (_)`, `(_) is not (_)` as **patterns** resolved
correctly, ties counted and reported. `POSTFIX-DIAGNOSIS.md` established that
`Resolver.cs` has the same `Ac`/`Ao` structure, so it is a chart parser too.

If that holds, R6's leading-hole clause is vestigial — inherited from a matcher
Ronin no longer has. Big enough to need a sweep rather than an argument, so I
will run it. **Taking it next, ahead of E**, because a decision that deletes B₁
should be made before B₁ is built.

## 2. `is`'s binding power — **5, left-associative**

Measured rather than reasoned by analogy (`is_binding_power.py`):

```
  is = 5    a + b is c + d          ((a + b) is (c + d))
            a is total otherwise 0  (a is (total otherwise 0))
            sum of a is b           ((sum of a) is b)

  is = 8    a is total otherwise 0  ((a is total) otherwise 0)      wrong
            sum of a is b           sum of (a is b)                 wrong

  is = 11   a + b is c + d          ((a + (b is c)) + d)            wrong
```

Two constraints, both decisive:

**Below `PatternBindingPower` (7).** At 8, `sum of a is b` reads as
`sum of (a is b)` — a trailing free hole parses its argument at the pattern's own
level, so the pattern swallows the comparison. Every comparison written after a
pattern call would be wrong.

**Below `otherwise` (6).** At 8, `a is total otherwise 0` reads as
`(a is total) otherwise 0` — the fallback catches the comparison's result, which
is a truth and can never be nothing. The thing that might be nothing is `total`,
and only `is` < `otherwise` attaches the fallback to the operand.

```
   1-4    reserved: «and», «or» -- looser than comparison, so
          «a is b and c is d» groups as two comparisons
   5      is, is not, is a, is an, is not a, is not an   (left)
   6      otherwise
   7      pattern calls
   10-21  arithmetic, indexing
```

**Write the reservation down beside the number.** Nothing today distinguishes 5
from 1, because `and`/`or` do not exist — so without a note the next person
simplifies it to 1 and the room disappears.

Two readings that are identical at every candidate and worth recording:

- `not a is b` → `(not a) is b`, because `not (_)` is a *pattern* at 7. Not what
  the English suggests, and the argument for `is not` being its own operator
  rather than composed — already the plan.
- `a is b is c` → `(a is b) is c`: a truth compared to `c`. A **type** error
  rather than a parse error, which is the right place for it. "You compared a
  truth to a number" beats "unexpected `is`".

## 3. The type table for `is a` — part of C, and C is where it gets created

I cannot see the frontend, so this is a design answer rather than a report: the
separate type table was **recommended and never implemented** — `GENERICS-II.md`
§8a proposed splitting types from values for reasons unrelated to `is a` (halving
glue pressure). Nothing has built it.

So C carries it, consistent with the slice table already listing C's dependency
as *"B + a type table"*. **B₀ does not need it and should not wait.**

Worth flagging while he is there: that split has now paid for itself three times
— glue pressure, then `?` in type position not colliding with partial
application, now `is a`'s namespace selector. Three independent reasons is enough
to stop treating it as optional.

## 4. E — after leading holes

Unchanged in content, changed in order. Leading holes first, for §1's reason; E
immediately after. D landing meanwhile is what E's equality half needs.

---

## 5. Budai's two points — and the second is a real design contribution

**On §4 — agreed, and it generalises into a principle rather than a ruling:**

> Ronin protects you from **changes to what you already wrote**. It does not
> protect you from what you write next — that is what reading is for.

Defensible, predictable, and the same line the import differential check draws.

**On colouring — this belongs in the spec, not the IDE backlog.** The whole
grammar rests on a premise: *the reader knows the names in scope*. That is what
makes `base price` a name rather than two words. Every argument I have made about
"silent" capture this week has quietly leaned on the reader **not** knowing — and
if the environment colours a multi-word name as one span, the premise stops being
an assumption and becomes a guarantee.

Concretely: colour the **span** of a resolved name as one unit. That is exactly
the information minimum lookup used to decide, made visible at no extra cost,
because the resolver already computed it. It turns "you have to know the table"
from a tax into a feature.

**On ephemeral warnings — yes, and I would not call them warnings.** Look at what
has accumulated:

| | |
|---|---|
| an import re-reads a statement | `MODULE-MERGE.md` §4 |
| a declaration swallows a phrase written earlier | `TIME-TO-LIVE.md` §3 |
| an edit narrows an inferred generic interface | `GENERICS-II.md` §6 |

Three documents, one shape: **an edit changed the meaning of something you did
not edit.** That is not a warning — nothing is wrong, and calling it one invites
people to suppress it. It is a **change report**, and it belongs to the
always-running environment rather than the compiler:

> a third channel beside errors and warnings: *what this edit changed elsewhere*.
> Ephemeral, dismissible, showing the before and after readings.

Two consequences worth having now:

1. **The differential machinery has one consumer, not three.** Build it once;
   feed it from imports, declarations and interface inference alike.
2. **It collapses several parked decisions.** `time to live` gets cheap the moment
   the channel exists, because the answer stops being "refuse the name" and
   becomes "tell them what moved". Same for R7b's conditionality, and for the
   import boundary.

So I would put it on the roadmap as its own item rather than as IDE polish, and
let the parked decisions land on top of it.

Probe: `is_binding_power.py`.
