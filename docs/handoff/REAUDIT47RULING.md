# REAUDIT47 — findings 1 and 2 have one cause, and it is a decision I made that the code did not take

Yes, it changes things. **Findings 1 and 2 close by deletion rather than by
repair**, finding 5 goes the other way from what the audit expects, and the
design question the audit correctly says has no owner — *what may be a key* — is
answered here.

---

## 1 & 2. Canonical ordering was never the design, and it is the cause of both

`E-AGGREGATES` §6:

> **Insertion order is preserved for iteration and ignored for equality.** Two
> lookups may therefore be equal and iterate differently.

The implementation canonicalises the stored order by sorting, because
`FRESHAUDIT20` finding 1 treated *"equal lookups expose different iteration
orders"* as a defect. **It is not a defect. It is the trade §6 named** — and it
was named for precisely the reason finding 1 has now demonstrated.

### Nothing needs a total order

```
  consumer                                needs   why
  lookup equality  «a is b»            equality   same key set, same values
  duplicate keys at admission          equality   is this key already present
  cutoff / «old» / «changes»           equality   did the value change
  indexing  «m @ k»                    equality   find the key equal to k
  hashing for fast indexing            equality   order-insensitive combine
  printing / display                    neither   wants DETERMINISM, and
  iteration  «for each»                 neither   insertion order is deterministic

  zero of seven
```

And the two obligations are not the same size. An **equality** over admitted
values is structural, written, and works. A **total order** must order across
every kind and within every kind, forever, including kinds nobody has added yet.

> **Finding 1 is not a bug in the comparer. It is that obligation coming due**,
> and it will come due again every time a kind is added.

### Both of the audit's witnesses are properties of sorting

Reproduced, with the duplicate scan asking language equality of *adjacent* pairs
as the implementation does:

```
  witness                                        by sorting     by equality
  equal maps written in opposite orders            DISAGREE           agree
  equal keys separated by an unequal one           ADMITTED         refused
```

### And finding 2 is the same cause counted differently

```
   depth   comparer walks (no memo)   equality with memo
       4                         47                    6
       8                        767                   10
      12                      12287                   14
```

The audit's 8,192 leaf renderings at depth 12, reproduced. Two remedies exist:
give the comparer its own memo (the audit's recommendation — correct, and it
keeps the total-order obligation alive), or **delete the comparer**, since
nothing needed it.

> **Ruling: drop canonical ordering. Store insertion order. Detect duplicates by
> equality against the keys already taken, not by adjacency after a sort.**

The comparer goes, and with it the `ToString` fallback, the unimplemented null
rule, the missing memo, and the error-compared-by-message-only hole. Four of
finding 1's five sub-problems are deleted rather than fixed.

**Cost, stated honestly:** duplicate detection becomes O(n²) `Same` calls where
sorting was O(n log n) comparisons. For any lookup large enough to matter, hash
first — an order-insensitive combine over entry hashes, consistent with equality
— and `Same` only on collision. A hash consistent with equality is derivable;
an order is not. That asymmetry is the whole finding.

## The design question with no owner: what may be a key

The audit is right that this is unowned. Answering it:

> **`Admit` refuses any value that is not a known runtime kind — as a key *and*
> as a value.**

Not "refuse unknown host values as keys". Refuse them at the boundary. `Admit`
exists to make *"a value the runtime accepts must be one it can compare
honestly"* true, and an arbitrary host object with a custom `Equals` cannot be
compared honestly in any position. Refusing it only in key position leaves it
legal inside a list that is then used as a key — which is exactly the
`[Error("same")]` versus `[Fault("same")]` witness, one level out.

This closes the catch-all kind entirely. There is no kind 8.

**CLR null is not a language value at all.** `nothing` is the language's
no-value. A CLR null reaching `Admit` is the interpreter having already gone
wrong, so it is a **`Fault`**, not an `Error` and not an ordering question. That
also answers the audit's null-ordering witness by removing the case.

**And one refinement to a ruling of mine.** `ERROR-AS-VALUE` §3 said two Errors
are equal when their reasons are equal. Finding 1 shows the omission: **equal
when their *kind and* reason are equal.** A `Fault` is not an ordinary `Error`,
and equality must not say otherwise.

## 3. The `Fault` laundering — agreed, and the general rule

`Fault`'s entire purpose is to be uncatchable, and admission converts it into a
catchable `Error`. That is the defect the type exists to prevent, arriving
through a door built for something else.

> **A `Fault` propagates unchanged through `Admit`.** It is tested before the
> ordinary-error key refusal, and the `Refusal → Error` conversion never applies
> to it.

The general form, because this door will be built again:

> **Anything that converts a failure into a value must ask whether it is a
> `Fault` first.** A `Fault` is the one failure that is not a value, so every
> failure-to-value boundary is a place it can be laundered.

Worth grepping for other conversions with that shape rather than fixing only
this one.

## 4. Type layer — confirmed open

Keep it out of any "Section E complete" claim. And add the gap to the expiry
ledger in the format already agreed — with its **successor**, because an entry
that records only "expires" produces a rewrite instead of a deletion:

```
  rule / gap                  approximates              becomes
  «[]» is always a list       the expected-type rule    «[]» is the empty lookup
                              (E §5), which needs the   where a lookup is
                              type layer                expected
```

"You cannot write an empty lookup" is exactly the kind of gap someone builds a
workaround for and then keeps after §5 lands.

## 5. The miss — my ruling goes against the audit's expectation

The audit says *"On the current reasoning, `Error` is the only result preserving
absent versus present-and-nothing."* **That reasoning is mine and it is wrong**,
so the audit should not be read as independent support for it.

It assumed `optional (optional V)` collapses to `optional V`. It does not:
`FIVE-RULINGS` §5 made `optional (_)` an ordinary type constructor, and
constructors nest. Absent is `nothing` at the outer level; present-and-nothing is
a present value that happens to be nothing.

Two things the `Error` answer would have cost, neither of which I weighed:
`MATCH`'s exhaustiveness (*arms missing one give `optional T`*) stops being
ordinary typing and becomes a bespoke analysis; and a forgotten miss stops being
a compile-time type error and becomes a runtime failure.

So the change is to the **code and the maintained test**, not to the spec. That
is the opposite of what finding 5 anticipated, and it is the one place the audit
should be re-read rather than actioned as written.

### This section replaces `EAGGREGATES2.md` §8 in full

Rather than reissue E, the corrected section is here. **Treat
`EAGGREGATES2.md` §8 as superseded by what follows**; the rest of that document
is unaffected.

```
  xs @ i       list of T,     i => number   -> T
  m  @ k       lookup of K V, k => K        -> optional V
```

> **A lookup miss gives `nothing`, and `m @ k` is typed `optional V`.**

`EAGGREGATES2` §8 was wrong twice, and the second error caused the first: it
typed `m @ k` as `V`, and then argued from there that a miss had to be an
`Error`. `docs/spec/NOTHING-AND-INDEXING.md` had it right, with better reasons
than I credited:

- **`MATCH`'s exhaustiveness is the absence of `nothing`** — arms covering every
  case give `T`, arms missing one give `optional T`. An `Error` is not in `T`, so
  under `Error` that stops being ordinary typing.
- **A forgotten miss becomes a compile-time error**, because `optional V` is not
  `V`, so `m @ k + 1` does not type-check. Under `Error` it compiles and fails
  when it runs. That is `NOTHING-AND-INDEXING` §1.1's own argument reaching one
  stage earlier than I had it reaching.

**This makes one premise load-bearing that has not been decided anywhere:
optionals nest.** `optional (optional V)` is a distinct type from `optional V`.
It falls out of `optional (_)` being a pattern, nothing contradicts it, and the
instinct from other languages is the opposite — so it wants a line in the
reference entry rather than being left to fall out.

**A list index out of range stays an `Error`**, and the difference is in kind
rather than in taste: a missing key is *data*, a question about a table with an
honest answer; an index past the end of a list is a *mistake*. Typing `xs @ i` as
`optional T` would put an `otherwise` on every list index in the language to pay
for a case that is a bug wherever it happens. That reason belongs in both
reference entries, or the split reads as arbitrary.

## Summary

| finding | ruling |
|---|---|
| 1 canonical comparer | **delete it.** Nothing needs a total order; the display-text fallback is the obligation coming due, not a bug |
| 2 exponential unfold | **closes with 1** — no comparer, no unmemoised recursion. A hash consistent with equality replaces the sort |
| — what may be a key | **`Admit` refuses any unknown host value, as key and as value.** No kind 8. CLR null is a `Fault`, not a value |
| — Error equality | refined: equal when **kind and reason** agree, not reason alone |
| 3 `Fault` laundering | **propagate unchanged.** General rule: every failure-to-value boundary must ask "is this a `Fault`" first — grep for the others |
| 4 type layer | open, confirmed. Ledger entry for **"`[]` is always a list"** with §5 as successor |
| 5 miss result | **`nothing`, `m @ k : optional V`.** The audit's supporting reasoning is my own error; the code moves, the spec does not |
| §6 iteration order | **stands as written** — insertion order, equality ignores it. `FRESHAUDIT20` finding 1 should be reclassified as behaving-as-designed |

Probe: `lookup_order.py`.
