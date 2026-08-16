# Q2 — both. And my objection was wrong, not merely outweighed

> **Ledger** — `[V]` Q2 — both. And my objection was wrong, not merely outweighed
> supersedes: none
> superseded by: none

The falsifying test was the right thing to run and it did its job: the exemption
survives, and my enumeration had a hole. Q2 is settled below, against my own
argument.

---

## 1. The case I missed, and why I missed it

> it consumed other inherited work, or it consumed newly created work

**A round can consume nothing.** Firing a chain's head is a *producer* round —
and with `return` in the head it produces nothing either. My enumeration was
over "what did this round consume", which silently assumed consumption is the
only thing a round does. It is not, and the case that falls through the gap is
the exact one `REAUDIT17` found.

The lesson is the one this project keeps re-teaching in new costumes: **an
enumeration over one axis assumes that axis is total, and that assumption needs
its own check.** I did not check it, and two cases felt exhaustive because I had
not asked what else a round could be.

## 2. Q2: allow both, and the reason is stronger than the trade

His measurement settles the mechanism question — one-credit-whichever-first is
numerically *identical* to no exemption at all, because `k` runs generate `k`
deferrals and `k` consumptions, so demand is `2k` against supply `k` and half
the rounds charge however they are ordered. So the choice is two credits or
none, and none reinstates an accidental depth cap at `2k + 2` against `k + 2`.

That would be enough to decide it. But the argument I made against two credits
is also simply wrong, and that matters more than the trade:

> one unit of pre-existing work buying two rounds does dilute what `cascades`
> means

**It does not, because free rounds do not spend budget.** They are not counted,
so they cannot displace anything. A step containing 1000 parked runs *and* a
genuine runaway still detects the runaway after exactly `limit` created-work
rounds — the free rounds are spent servicing inherited work and never touch the
counter. Detection is not delayed by one round.

What grows is the number of rounds a step may take: bounded at `limit + 2k`,
since Q3 caps each run at one deferral and consumption happens once. That is a
**throughput** property, not a safety one, and it is proportional to real
pre-existing work.

So his framing is right and I would sharpen it one step further. Rather than
"the limit plus an allowance", state the invariant the limit always had:

> `cascades` bounds the rounds a step spends on work **created during that
> step**. Rounds spent servicing work the step inherited — consuming it, or
> being displaced by the head that owns it — are not in scope. At most `2k` of
> them exist, for `k` inherited runs.

That is a documented property with a proof rather than a number with a bonus,
and it explains why the two-credit answer is not a concession.

## 3. The fourth test, inverted

The test I proposed encodes Q2, and with Q2 settled it should assert the
opposite of what I wrote: **one inherited run, deferred once and consumed later
in the same step — both rounds free.** Worth having precisely because it is the
one an optimisation would break by "tidying" the two allowances into one.

## 4. On keeping the zero test

Renaming it to what it proves and keeping it as the hang test is better than
what I suggested. My generalisation holds — if a cap test would still pass with
the cap logic deleted, it is not testing the cap — but the corollary is not
"delete it", it is "call it what it is". A test that proves *nothing inherited
is forgiven nothing* is worth having; it was only ever the name that claimed
more.
