# Headering the five — one marker corrected, the chain pinned, and two open questions buried inside verdicts

> **Ledger** — `[V]` verdict. Answers `RELAYEDDOCHEADERS` Q1–Q3. Confirms three
> markers, **corrects `TEXTDESIGN` to `[V]`**, pins the internal chain (an
> `answers` edge, not a supersession), and names two unanswered designer questions
> the headers must carry. Flags one missing edge outside the five.
> answers: `RELAYEDDOCHEADERS`
> supersedes: none
> superseded by: none

**Q1: three confirmed, `TEXTDESIGN` corrected. Q2: the chain below — and one of
your two is an `answers` edge, not a supersession. Q3: none of the five overturns
a live `[V]`, but there is a missing edge elsewhere and one document I cannot
check.**

---

## §1 — Q1: the markers

**`EXACTNESSISAVALUE` `[V]`, `SCALARANDPROMOTION` `[V]`, `RUNAWAYWATCHDOG` `[V]`,
`NUMBERRUNTIME` `[R]` — all four confirmed.** Your readings match the criterion:
`[V]` iff the document records a decision that was ruled and relayed.
`NUMBERRUNTIME` in particular is right to stay `[R]` — its primitives
recommendation and its Herbie ruling were never explicitly ratified, and under the
citation rule *no ruling event ⇒ `[R]`*.

**`TEXTDESIGN` is `[V]`, not `[R]`. That is the correction.**

Its `[R]` is **self-marking** — the author's opening claim, written before the
answer came back. The ruling event is Budai's reply, verbatim: **"yes to all of
it. no stringzilla, utf-8, update to dotnet 10."** That is an explicit ratification
of the whole document, which is exactly the citation the marker follows.

And leaving it `[R]` is a **live inconsistency**, not a nicety: `SLICEONETYPINGS`
§3 is a `[V]` that cites `TEXTDESIGN` as **binding** for `text @ number → text`. A
verdict resting on a recommendation is the shape the ledger exists to surface.

### Two verdicts carry unanswered designer questions — the prose must say so

This is the part worth more than the markers. `LEDGERRULING` §1 requires a `[V]`
document's prose to name the part that does not bind, and two of these have one:

- **`RUNAWAYWATCHDOG` §6 — never answered.** It asks whether the runtime watchdog
  survives into a **release build**, with my lean (yes, because its value is
  highest exactly where the developer is not) and the note that it needs a
  destination and must never become an error. The conversation moved to the ledger
  backfill and it was never returned to. Header prose must carry *"§6 open."*
- **`SCALARANDPROMOTION` §7–§8 — the `fast number` edges.** The **denormal** (I
  recommended *not* making it an error, against the owner's suggestion) and the
  **underflow** sibling I raised (`1e-300 * 1e-300` is a plausible-looking zero)
  were neither contested nor confirmed. The ruled parts bind; those two do not.

Both are `fast number` / deferred-tower items, so neither blocks Slice 1 — but a
`[V]` that silently contains an unanswered question is how one gets implemented by
guess.

## §2 — Q2: the chain, and one of your two is the wrong edge kind

```
  NUMBERRUNTIME        [R]   answered by: SCALARANDPROMOTION
  SCALARANDPROMOTION   [V]   answers: NUMBERRUNTIME
                             superseded by: EXACTNESSISAVALUE §2
  EXACTNESSISAVALUE    [V]   supersedes: SCALARANDPROMOTION §2
  RUNAWAYWATCHDOG      [V]   supersedes: none          <- your read, confirmed
  TEXTDESIGN           [V]   supersedes: none | superseded by: none
```

**`EXACTNESSISAVALUE` → `SCALARANDPROMOTION` §2.** Certain. That document opens
*"§2 of the last document was wrong"*, and `SCALARANDPROMOTION` §2 is the section
that made the exactness boundary a **type** boundary (roots returning
`fast number`). Wire the pair.

**`SCALARANDPROMOTION` → `NUMBERRUNTIME` is an `answers` edge, not a
supersession.** `NUMBERRUNTIME` §4 asked the owner two questions — what happens at
64 bits, and how an exact `1/3` prints — and `SCALARANDPROMOTION` is where both
answers were recorded (silent promotion; ellipsis). By `ANSWEREDEDGE` that is
*answered by*, and it explains the `[R]`: a memo that asks stays `[R]` and its
answer governs.

The always-running-mitigates correction inside it is **prose, not an edge** — a
document correcting an argument is not superseding a ruling. **One check for you,
because my workspace was reclaimed and I cannot re-read the file:** find the
sentence in `NUMBERRUNTIME` claiming the always-running IDE *mitigates* silent
promotion. If it is in the document, note the section in
`SCALARANDPROMOTION`'s prose; if it turns out to have been said conversationally,
there is nothing in the corpus to point at.

> **General rule, since it will recur: a document correcting something said in
> conversation gets `supersedes: none`.** There is no corpus fact to supersede,
> and inventing an edge to a turn nobody can open is worse than no edge.

## §3 — Q3: none of the five overturns a live `[V]` — but two things to do

**Checked and clear:** `DISCARDEDKINDSRULING` *cites* the exact-rational ruling
(its §2 argues `0.1` must become ¹⁄₁₀) rather than contradicting it — a citation,
not an edge. `NUMERALALPHABET` is about the source alphabet and is orthogonal.

**One I cannot check.** `NUMERICANDWRITERS` is a live `[V]` whose summary reads
*"Two answers: write sets, and the numeric tower."* I have never seen that
document. **Read its numeric-tower half against `SCALARANDPROMOTION` and
`EXACTNESSISAVALUE`**, and if it states a tower position those overturn — a
representation, a rung, an exactness rule — wire the reciprocal `superseded by`.
If it only sets direction, leave it. This is the one place the drift could still be
hiding, and it is a five-minute read.

**And a missing edge that has nothing to do with the five.** `MODIFIERNAMES` `[V]`
reads *"refuse a modifier at a name head, for every modifier — **the earlier
allowance is retired**."* The earlier allowance is `FASTRESERVATION`'s — *"a
modifier that may begin a name by design"* — and `FASTRESERVATION` carries no
`superseded by`. Wire it. Pass 2 missed it because the retirement is named in the
superseding document and not in the superseded one, which is the direction that is
easy to miss.

## §4 — and the Pass 2 worklist closes

`TEXTDESIGN` is the ledger's only remaining Pass 2 row. It is the first text
document and nothing has overturned it: **`supersedes: none | superseded by:
none`.** With that and the four headers, both worklists go to zero.

## Summary

| | |
|---|---|
| **Q1** | `EXACTNESSISAVALUE` **`[V]`**, `SCALARANDPROMOTION` **`[V]`**, `RUNAWAYWATCHDOG` **`[V]`**, `NUMBERRUNTIME` **`[R]`** — all four confirmed |
| **corrected** | **`TEXTDESIGN` is `[V]`.** `[R]` was **self-marking**; the ruling event is *"yes to all of it. no stringzilla, utf-8, update to dotnet 10."* And `SLICEONETYPINGS` §3 already cites it as **binding** |
| **the prose must say** | **`RUNAWAYWATCHDOG` §6 is unanswered** — does the watchdog survive into a release build. And **`SCALARANDPROMOTION`'s denormal and underflow questions** were never ruled. Both `[V]`, both with an open part |
| **Q2 chain** | `EXACTNESSISAVALUE` **supersedes** `SCALARANDPROMOTION` §2 — certain, wire the pair |
| **edge kind corrected** | `SCALARANDPROMOTION` **answers** `NUMBERRUNTIME` — it records the owner's answers to §4's two questions. Not a supersession, and it explains the `[R]` |
| your other two | **confirmed `none`** for `RUNAWAYWATCHDOG` and `NUMBERRUNTIME` |
| one check | locate the *always-running mitigates* sentence in `NUMBERRUNTIME` — **my workspace was reclaimed and I cannot re-read it.** If conversational, no edge |
| the rule | **a document correcting something said in conversation gets `supersedes: none`** — inventing an edge to a turn nobody can open is worse than none |
| **Q3** | **none of the five overturns a live `[V]`.** `DISCARDEDKINDSRULING` cites rather than contradicts; `NUMERALALPHABET` is orthogonal |
| **check this one** | **`NUMERICANDWRITERS`** — a live `[V]` whose summary names *"the numeric tower"*, which I have never seen. Read it against the two new rulings and wire an edge if it states a position they overturn |
| **missing edge, unrelated** | **`MODIFIERNAMES` retires `FASTRESERVATION`'s allowance** and `FASTRESERVATION` has no `superseded by`. Pass 2 missed it because the retirement is named only in the **superseding** document |
| **Pass 2 closes** | `TEXTDESIGN`: **`supersedes: none | superseded by: none`**. Both worklists go to zero |
