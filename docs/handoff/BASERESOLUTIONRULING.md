# Base resolution — **C**, defer; and the row's wrong premise was mine

> **Ledger** — `[V]` Answers `BASERESOLUTION.md`: defer the base-resolution item to
> the algebra slice. Rewrites the `Test/Expiry.cs` row (premise was the designer's,
> wrong). Adds a ledger-format rule: a row asserting an implementation size carries
> who established it and how. Step 1's Type-term half proceeds.
> supersedes: not yet checked
> superseded by: not yet checked

**Ruling: C.** Defer the whole item to the algebra slice, rewrite the row to say
what your probe found, and proceed with the Type-term half of step 1, which is
independent as you say.

The *"one more node, no other machinery"* framing came from me — I endorsed it in
`TYPEHALFRULINGS` §2 and repeated it in the scoping answer, having checked
nothing. It is a **design document asserting a code shape**, which is the failure
I have been correcting in other people's numbers and in my own all month. §4 has
the remedy.

---

## 1. Why not B, even though B is the honest half-measure

B is a hand-written splitter for `and`/`or`. **`TYPEHALFRULINGS` §3 already ruled
where that split belongs** — the type-mode operator table — and specifically
warned against the shape B would create:

> *"Write it as **'the type-mode operator table, currently empty'** rather than
> 'no operators in type mode' — the second is a hard-coded assumption that §2's
> follow-up has to unpick."*

B is that hard-coded assumption, one level up: a split in the walk that the
operator pass must reconcile with. Same failure, and I would be endorsing it three
weeks after writing the warning.

**And B cannot be done properly today anyway**, which your probe shows without
quite saying it. The right implementation is *not* a splitter — it is `and`/`or`
in the type-mode operator table, letting the resolver produce the split the way it
does for value operators. But the parser peels the record off first, leaving
`«Vehicle and»` — **a trailing operator with no right operand.** No operator table
can resolve that. The parser has already destroyed the association.

So the dependency runs the other way from how the row implied: the operator work
cannot land until the parser stops dangling the `and`.

## 2. Why not A either — a partial diagnostic teaches the wrong rule

A is tempting and I nearly took it, because "the exact condition is narrow enough
to state" is usually the right instinct here. The narrow condition exists: *the
reference contains no `and`/`or`*, one predicate, deleted rather than reconciled
when the operator pass lands.

What stops it is not cost, it is what it teaches:

> `type Truck = Vehicle;` failing on an undeclared `Vehicle` tells a user
> **"undeclared bases are caught."** `type Car = Vehicle and { … }` then silently
> accepting the same undeclared `Vehicle` violates the rule they just learned.

A partial diagnostic that covers the *minority* form is worse than none, because
it establishes an expectation and then breaks it — and the form it misses,
`Base and {record}`, is the form the feature exists for, as you say. Silence is
honest; inconsistency is not.

## 3. What C's successor actually is, so the row is real

"Defer" is only useful if the row names the work. Three parts, in this order,
because the first gates the second:

1. **The parser stops dangling the operator.** `type Car = Vehicle and { … }` must
   leave an algebra whose right operand is the record, not a reference ending in a
   bare `and`. Until this, nothing downstream can resolve the form.
2. **`and`/`or` enter the type-mode operator table**, with rungs on the named
   ladder. This is the `TYPEHALFRULINGS` §3 follow-up arriving as designed
   operators rather than as a bespoke split, and it is *small* — the resolver
   already does operator expressions; what it has never had is a type-mode
   operator to do them with.
3. **`Bases`/`Unions` populate from the resolved tree**, and base resolution's
   findings fall out of it rather than being written separately.

The semantics of an algebra — what `and` and `or` *mean* for a type, and Q3's
deferred alias-over-a-base declaration — sit on top of that and are a further
slice. Resolution is the small half; do not let its size be inferred from the
semantics' size.

**One thing the deferral costs, stated so it is not discovered:** an undeclared
base stays silent this pass. That is tolerable *only because nothing consumes
`Bases` yet* — so a missing base can produce a missing diagnostic but not a wrong
answer. **The moment anything reads `Bases`, this stops being deferrable**, and
that sentence belongs in the row.

## 4. The ledger format needs one more thing, and this is why

Your closing line is right and I would go one further. The row said *"one more
node, no other machinery."* That is a **size claim about the tree**, made in a
design document, by someone who could not run the code — and it sent you at a
naive fix that would have put a finding on correct programs.

I have a rule for this that I wrote for numbers and did not apply to sizes:

> **A number quoted from the tree is a claim about the tree's current state.**

A *size estimate* is the same claim wearing different clothes. So:

> **A ledger row that asserts an implementation size must carry who established
> it and how.** *"one more node"* — probed by whom, at which commit? If the answer
> is "asserted by the designer from a document", the row should say so, and the
> next reader knows to probe before trusting it.

That is cheap — one clause per row — and it is the same fix as everything else
this month: give the claim a consumer that can falsify it. Your probe is now that
provenance; put it in the row.

## 5. Summary

| | |
|---|---|
| ruling | **C — defer the whole item** to the algebra slice |
| not B | it is the hand-written split `TYPEHALFRULINGS` §3 warned against, and it **cannot be done properly today**: the parser leaves a trailing `and` with no right operand, so no operator table can resolve it |
| not A | the narrow condition exists, but a diagnostic that catches the **minority** form teaches a rule the majority form then breaks. Silence is honest; inconsistency is not |
| the successor, in order | **(1)** parser stops dangling the operator → **(2)** `and`/`or` in the type-mode operator table with ladder rungs → **(3)** `Bases`/`Unions` populate, findings fall out |
| size note | (2) is **small** — the resolver already does operator expressions. Do not price resolution by the size of algebra *semantics* |
| the cost of deferring | an undeclared base stays silent, tolerable **only because nothing consumes `Bases` yet**. Put that condition in the row |
| the row's premise | **mine, and wrong.** Rewrite it with your probe as its provenance |
| ledger format | **a row asserting an implementation size must say who established it and how** — a size estimate is a claim about the tree, same as a number |
| step 1's Type-term half | **independent — proceed** |
