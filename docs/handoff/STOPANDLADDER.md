# `stop`, and the ladder — plus a prior question the megabytes were hiding

Both answered. **Question 2's answer is that the price is not what it looks
like**, and I would check that before choosing a number against it.

Three corrections of mine acknowledged first, since they are all his: `disarm`,
that the leading-hole refusal is `LeadingHole` and not R6, and that
`REPAIR-COMPLETENESS` should generate the third row rather than inherit
`LEADING-FREE-HOLES` §3's count. All three taken. The last one especially — *"the
difference between the claim being measured twice and being measured once and
quoted"* is the rule, and it is the rule I broke in `AMBIGUITY-AS-ERROR.md` when
I cited a probe that was reporting FAIL.

---

## 1. `stop` — a nullary builtin pattern, reserved globally, legal only in a `when`

Yes, and *"the runtime has an operation is not the same as the language has a
word"* is the right instinct — that distinction is what §0 is about.

```
  form            «stop», nullary. Same shape as bare «return»
  reserved        GLOBALLY. Whole-name only, so 5 exact collisions in a
                  460,030-identifier corpus; «stop word», «stop loss»,
                  «stop time» all stay legal
  legal           only inside a «when» body
  elsewhere       not "unknown name" -- the builtin in the wrong place, with a
                  message pointing at «return»
```

**Why global rather than scoped to a `when`.** Scoped is tempting, and it is
wrong for a reason we have already paid for: the self-ambiguity check is
deliberately **pessimistic** so that it is order-independent — any word run
*could* be a name, judged without regard to where you are standing. A
`when`-scoped reservation gives that check two different answers for the same
span depending on position, and lets a name declared outside a `when` be captured
inside one. That is the boundary-capture class, and it is why a keyword cannot
live outside the table.

**Keep the reservation and the legality separate**, exactly as `return (_)` does.
That is what buys a five-name cost *and* a good error, instead of choosing.

### One semantic question I would settle while building it

`Graph.Return` sets `returned`; `Graph.Stop` removes the node. **Does `stop` also
end the current firing?**

If it does not, statements after `stop` run inside the body of a `when` that no
longer exists — which has no meaning I can construct, and produces the kind of
bug that is unreadable from a stack trace. So: **`stop` should imply `return`.**
Remove the node *and* end this firing.

And then code after `stop` at the same level is unreachable, which is precisely
the ephemeral-warning class Budai proposed for the single-variable case rather
than a finding.

## 2. The ladder — check the price before paying it

His framing is the right one: *"it makes the ladder's size a decision with a
measurable price rather than a matter of taste."* Agreed, and that is why I went
at the price rather than the number.

### §1 — the bp dimension is not carrying information

Two schemes, same grammar, genuine ambiguity (`+` and `-` share a level, so a
chain has Catalan-many readings and set equality is a real test):

```
   tokens   parses   A keys   B keys   identical?
        5        2        6        6   yes
        9        4       15       15   yes
       13        8       28       28   yes
   mismatches: 0
```

- **A** keys the memo by `(span, minbp)` — the dimension.
- **B** keys by `(span)` and has each derivation **carry its own top binding
  power as a tag**, comparing at combination time.

Identical parse sets. The dimension memoises a *filter*, and the filter's input
is one integer the derivation can hold itself.

### §2 — and the growth he measured is a *different* thing again

**A correction to my own model, which is the most useful thing in this
document.** I predicted A's key count would grow with the level count. Measured,
it is flat: a lazily memoised span is only ever reached with the minbps its
parents actually ask for, which is a small fraction of the level set.

That refutes my model, not his measurement — and separating them is the result.
His table *"carries a column per level the recurrences **can** ask for"*, so it
is allocated over the whole level set rather than filled on demand:

```
   tokens   spans   eager: spans x levels   lazy A keys
        5      15                      45             6
        9      45                     135            15
       13      91                     273            28
```

So there are **two independent savings**, and they were hiding behind one number:

| | removes |
|---|---|
| **allocate lazily** | the "column per level the recurrences *can* ask for". Levels stop being a multiplier on the whole table. **No change to the parser's shape** |
| **tag instead of key** | the dimension entirely — §1 shows the answers are identical |

Either alone makes a rung approximately free. I would take the first, because it
is a smaller change and it is a saving that exists whether or not the ladder ever
grows.

### §3 — so the number is a readability question again, and it is eight

That is the right question for it to be. The rungs a reader actually
distinguishes:

```
      8  postfix / tightest        units, «(_) reversed»
      7  multiplicative
      6  additive
      5  comparison                «is» -- ALREADY RULED at 5
      4  range / interval
      3  logical and
      2  logical or
      1  loosest                   ascription «(x => Text)»
```

**`is` lands at 5 without moving**, and that is a check on the ladder rather than
a coincidence — 5 was chosen for its own reasons two documents before this one.

For scale: C has 15 and is the standard example of too many; Haskell has 10;
Smalltalk has one and is the standard example of too few. Eight is inside the
range that works, and **named** is what stops it drifting — an author writes
*binds like multiplication*, never `70`.

## 3. On §5's closing line

*"A rule scheduled to shrink to a residue nobody had ever expressed. Same shape
as the check that reported PASS over zero cases."*

That is the same defect and it is worth having the general form, because it has
now bitten three times — `which_rules.py`'s zero cases, `repair_complete.py`
cited while failing, and this:

> **A claim about a set is untested until a member of that set exists.** "Six
> fixtures survive, ten expire" is the first time the surviving group has been
> more than a definition.

Which is also why §4's point about `REPAIR-COMPLETENESS` generating its own third
row is right and I would not argue it down.

## 4. Summary

| | |
|---|---|
| `stop` | **nullary builtin pattern**, same shape as bare `return` |
| reservation | **global**, whole-name only, 5 collisions. Scoped breaks the pessimism the self-ambiguity check needs |
| legality | only inside a `when`; elsewhere it is the builtin misplaced, not an unknown name |
| open, and worth settling in the same slice | **does `stop` end the current firing?** It should — otherwise the body runs on inside a `when` that is gone. Code after it is an ephemeral unreachable warning |
| the 4.5 MB per level | **two separate causes** — eager allocation over the level set, and keying by bp. Neither is intrinsic |
| lazy allocation | collapses it with **no change to the parser's shape**. Take this one first |
| tag instead of key | removes the dimension; identical parse sets over genuinely ambiguous input |
| my model of scheme A | **wrong** — I predicted growth, it was flat. That is what separated the two causes |
| how many rungs | **eight**, chosen for readability now that the price is not real. `is` at 5 falls in place unmoved |
| naming them | **named, not numeric** — the coordination argument survives even though the resource one dissolved |

Probe: `bp_columns.py`.
