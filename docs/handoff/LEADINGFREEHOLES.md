# Leading free holes — the clause is deletable, and what replaces it already exists

> **Ledger** — `[V]` Leading free holes — the clause is deletable, and what replaces it already exists
> supersedes: none
> superseded by: none

`truth`, `true`/`false` confirmed and nothing to add — that is the recommendation
taken verbatim, so it is settled.

While he finishes, I took the piece I committed to in `OPEN-DECISIONS.md` §1:
R6's refusal of a **leading free hole**, which four things wait on. Result first.

> **Delete the clause. Admit a leading free `(_)` — on condition that a pattern
> with an operand on its left declares a binding power, exactly as `is` does at
> 5.**

The lead was *"R6's refusal may be a property of the matching strategy, not of
the language."* That turned out to be half right, and the half that was wrong is
the useful part.

---

## 1. It is not free, and the cost has one shape

`leading_free.py`, sweeping the whole space:

```
  anchored + anchored             349,440 resolutions      6 ties   0.00%
  anchored + LEADING FREE HOLE    322,560 resolutions    120 ties   0.04%
```

So it is not vestigial in the way I guessed. But **every one of the 120 is the
same shape**, and it is not exotic:

```
  patterns «sum of (_)» and «(_) reversed», name «a»

    sum of a reversed        TIE, 3 lookups
        «sum of («a» reversed)»     the prefix pattern's argument swallows it
        «(sum of «a») reversed»     the postfix pattern's leading hole does

  the prefix-versus-postfix clash -- the first realistic composition anyone
  would write, not a corner case
```

## 2. What decides it is the thing symbols already have

Binding powers decide precisely this question, and Ronin already has them —
`is` sits at 5, left-associative. What it does not have is a binding power **per
pattern**: `dp_resolver` has one `pattern_bp` shared by all of them, so today
there is nothing for the clash to be decided *by*.

Enumerate every reading, then apply the declared powers as a filter:

```
  source                      readings  survive   verdict
  sum of a reversed                  2        1   UNIQUE
  sum of d per t                     2        1   UNIQUE
  a reversed per t                   1        1   UNIQUE
  sum of a reversed per t            3        1   UNIQUE
  a reversed reversed                1        1   UNIQUE
```

Nine readings in, five out — one per statement, every time.

**A correction to how I first measured this**, because it matters: my first
version compared a Pratt parser against a Pratt parser with different numbers. A
Pratt parser never ties; it always picks. So that run measured nothing and I
rewrote it to enumerate first and filter second, which is what the resolver
actually does.

## 3. The line this depends on, stated once

The obvious objection is that a binding power is cost choosing a winner, which
the whole ambiguity-as-error design refuses. It is not, and the distinction is
worth writing into the record because it will come up again:

> **A binding power is part of the *declaration*. Cost is a property of the
> *search*. Declared structure may choose between readings; search cost may
> not.**

`a + b * c` is not an ambiguity anyone reports. Not because cost broke a tie —
because the grammar says what it means, in public, once, and the other reading is
one bracket away.

Checked rather than asserted. Every filtered-out reading, and there are four
across the five statements:

```
  sum of a reversed        loser «sum of «a»» reversed        REACHABLE
    via ( sum of a ) reversed
  sum of d per t           loser «sum of «d»» per «t»         REACHABLE
    via ( sum of d ) per t
  sum of a reversed per t  two losers                         both REACHABLE

  unreachable readings: 0
```

## 4. What it deletes

This is why it was worth taking ahead of E — it removes work rather than ordering
it:

- **B₁.** `(_) is (_)` stops being a special case needing its own construct.
- **postfix units.** `5 metres`, and `wait for 5 minutes` from `WAIT-AND-EVERY.md`.
- **`metres per second`**, as an ordinary infix word pattern.
- **the anchor-first fallback.** `UNITS-RESEARCH.md` §4 priced it as `quantity of
  (_) in (_)` and called it a form field. Gone.

Four dependents, all of which were waiting on exactly this.

## 5. What it costs, and the one hazard I would design against now

The declaration cost is real but small: **a pattern with an operand on its left
must declare a binding power.** A new required field, and a set of numbers the
standard library chooses once.

The hazard is not the field. It is that **two library authors will pick numbers
independently**, and integers give them no way to coordinate. Symbols get away
with a fixed table because it is small and closed; a user-extensible operator set
is neither.

> **Make them named levels, not integers.** An author writes *binds like
> multiplication*, choosing a position in a published ladder, rather than
> inventing `47`. The ladder is a stdlib artefact, it is readable in the
> declaration, and it makes "tighter than what?" answerable at the point someone
> asks it.

That is a decision worth taking now, because the difference between a ladder and
an integer is a migration once anyone has written a number down.

## 6. Summary

| | |
|---|---|
| `truth`, `true`/`false` | confirmed, nothing to add |
| R6's leading-free-hole clause | **delete it** |
| is it free? | **no** — 120 ties, 0.04%, up from 0.00% |
| but every tie is one shape | prefix-vs-postfix, which binding powers exist to decide |
| the condition | a pattern with an operand on its **left** declares a binding power |
| after the filter | 9 readings → 5, one per statement |
| is that a silent pick? | **no** — 0 unreachable readings. Declaration may choose; search cost may not |
| deletes | B₁, postfix units, `metres per second`, the `quantity of (_) in (_)` fallback |
| design against now | **named levels, not integers** — otherwise independent authors collide and it is a migration later |
| my lead was | *half right*. Not vestigial — replaceable, by a mechanism already present for symbols |

Probes: `leading_free.py`, `leading_bp.py`.
