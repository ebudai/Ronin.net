# Deferral credit — three questions, one answer, and a prior question

REAUDIT18's three questions all have the same root, and I think the honest
answer is that **the exemption should probably not exist at all.** Failing that,
the three answers are forced and they are all the same answer.

The implementation defect is real and independent of any of this — the auditor
is right that the code does not enforce the rule it documents, and
`quota.Values.Sum()` is exactly the cross-chain pooling that per-counter quota
was introduced to remove. Fix that regardless of what follows.

---

## 0. The prior question: does deferral need an exemption?

`ROUND-LIMIT.md` §2 gave one rule:

> A round does not count if it consumed an activation that was already pending
> when the step began.

**Deferral is not consumption.** So under that rule, a round that defers
inherited work is charged, and no exemption exists to own, pool, or reuse.

Work through the cases. A round that defers an inherited continuation must have
run *something* instead:

- it consumed **other inherited work** → already free, by the rule, with no new
  mechanism;
- it consumed **newly created work** → the step is creating and consuming inside
  one settle, which is precisely the shape the limit exists to count.

I cannot construct a third case, which makes me suspect the exemption is
patching a symptom. So before making it more precise, the question worth
answering is whether it is needed:

> **Falsifying test:** a step that settles legitimately, and that throws
> `RunawayCascade` when the deferral exemption is removed entirely — with the
> `ROUND-LIMIT` consumption rule still in place, and without hand-seeded
> counters.

If that program exists, the exemption is real and §1 below applies. If it does
not, delete the exemption and all three questions evaporate. Given the record
this round — I have been right on rules and wrong on mechanisms twice — I would
rather he ran that than took my word for it.

## 1. If it survives, the three answers are forced

They are one answer: **an inherited run is one unit of pre-existing work, and it
buys exactly one free round, ever.**

**Q1 — owned by the counter, not aggregate.** The README is right and the
implementation is wrong. Aggregate credit means a chain's parked queue subsidises
a different chain's newly created work, which is the same cross-chain subsidy
per-counter quota was made to prevent, reintroduced one level up. The auditor's
"bounded, but the bound can be arbitrarily large" is the sharp form: a healthy
inherited queue of thousands silently buys thousands of rounds of runaway
tolerance somewhere unrelated.

**Q2 — no. Not both.** If one run forgives its deferral round *and* its
consumption round, one unit of pre-existing work buys two rounds. The budget
stops measuring what it is for. One credit, spent on whichever happens first.

**Q3 — no. At most one, per run, ever.** Repeated deferral forgiving repeated
rounds is unbounded forgiveness from bounded work, which is the hazard in Q1
without needing a second chain to do it.

The three together reduce to a decrement: each inherited run's counter holds one
credit at step start; spending it is the only way a round is forgiven; a spent
credit is gone.

## 2. The test critique is the sharpest thing in the report

> `AndItBuysNoMoreOfThoseThanItInheritedRuns` … proves only that zero inherited
> runs buy zero forgiveness.

That is worth generalising, because it is a shape rather than an oversight:
**a boundary test placed at zero does not exercise the boundary.** The
interesting behaviour of a cap is at `N` and `N+1`; at `0` the code path that
implements the cap is never entered, so the test passes for a reason unrelated
to what it claims.

The same criticism would apply to any future quota, budget, or limit test in
this codebase, and it is cheap to check for: if a cap test would still pass with
the cap logic deleted, it is not testing the cap.

His three recommended tests are right. I would add a fourth, and it is the one
that pins Q2 and Q3 rather than Q1:

> one inherited run, deferred once and consumed later in the same step. Exactly
> one of those two rounds is free.

## 3. Order

The auditor's order is right, with the prior question inserted first:

0. run the falsifying test in §0 — if the exemption is unnecessary, everything
   below is moot and a mechanism disappears;
1. otherwise, remove graph-wide pooling and attach credit to the counter that
   owns it, one per run, non-reusable;
2. replace the zero-cap test with positive ownership, exhaustion, and the
   deferred-then-consumed control above.

Step 0 is cheap and, if it lands, is the best outcome available: the accounting
question stops being answerable-in-three-ways because there is nothing to
account for.
