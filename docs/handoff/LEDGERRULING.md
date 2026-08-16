# The ledger backfill — six confirmed, two changed, and one thing that has to be fixed before Pass 1

> **Ledger** — `[V]` Rules the six decisions in `LEDGERBACKFILL.md`.
> supersedes: none
> superseded by: none

**All six recommendations are right in substance.** Two need a change that makes
them survive contact with the corpus: **§2** (what the proposal must cite, which
cuts my part of the work by an order of magnitude) and **§6** (supersession is a
*field*, not prose — otherwise the header reintroduces the reading it exists to
end). And **§7** is the thing to fix before a hundred headers are written, not
after: the header is a **schema**, and it needs a consumer.

This is a good package. It asked the questions in the right order.

---

## §1 — vocabulary: two tokens, confirmed, plus the rule for mixed documents

Confirmed: exactly `[V]` and `[R]`, every shade in prose.

The reason worth recording is not "a token zoo grows." It is that **a token set is
a closed vocabulary a consumer switches on.** Two tokens is one branch. `[V/R]` is
a union encoded in a token, and every consumer written afterwards has to invent a
rule for it — which is the same defect as a module identity that is *either* a
path *or* a token in one string slot, and it fails the same way: by convention
drifting between readers.

But the corpus produced `[V/R]` for a reason, and the ruling has to say what
happens to that case:

> **A document's marker states its *strongest* claim. If any part binds, it is
> `[V]`, and the header prose must name the part that does not.**

Mark by the strongest claim because the costs are asymmetric. An `[R]` on a
document that actually binds invites a later memo to overturn a settled decision —
the expensive failure. A `[V]` on a document that is mostly proposal costs a reader
one sentence of prose. So the default leans to `[V]` *at the document level*, and
the prose carries the boundary.

## §2 — authority: confirmed, with the change that makes it affordable

The split is right — a successor may mark its own memos `[R]` freely, and only I
can certify `[V]`. But the proposal step as described would hand me a hundred
judgment calls, and I would be making them from the same voice-reading the whole
exercise exists to abolish. Two changes:

**The proposal must cite an event, not a tone.** A row reading *"`[V]` — speaks as
a verdict"* is the re-derivation §8 wanted to end, now committed to disk under an
authority it does not have. A row reading *"`[V]` — ruled in
`CHECKERSCOPINGRULINGS`, relayed"* is checkable in seconds. **The `[V]` set is not
unknown**: a verdict exists because a ruling document was written and relayed, so
the ruling record is the evidence, and voice is not evidence at all.

**A document with no citable ruling event is `[R]` by default.** That makes the
mechanical pass safe in the direction that matters, and it makes my review
**one-directional**: I only ever promote. I never have to catch a wrong `[V]`,
because a wrong `[V]` cannot be produced without a citation that does not exist.

**And batch the checklist by ruling event, not by document.** *"These eight came
out of the `CHECKERSCOPINGRULINGS` relay"* is one decision covering eight rows. A
108-row list is a chore; twenty groups is a sitting.

## §3 — two passes: confirmed, and Pass 1 must not make stale documents look live

Confirmed, and shipping Pass 1 alone is right. One hazard to close first.

Pass 1 gives ~108 documents a confident header saying what each decides, while the
supersession edges are still unwalked. So a document that has *already been
overturned by something nobody has noticed* acquires a header asserting a live
decision. **Pass 1 would make stale documents look authoritative** — which is worse
than their current state, because today a reader knows they must check.

The fix is one word, and it is the same discipline the expiry ledger already uses:
**make the gap explicit rather than ambiguous.**

```
  superseded by: not yet checked        <- Pass 1 writes this
  superseded by: none                   <- Pass 2 writes this
  superseded by: LADDERRETRACTION       <- or this
```

`not yet checked` and `none` are different facts and must not share a spelling.
An empty field means "nobody looked," and a reader who cannot tell that from
"nobody found anything" has been misled by the thing built to inform them.

It also gives Pass 2 a worklist that is generated rather than remembered: every
document still reading `not yet checked` is the remaining work, visible without
anyone keeping a list.

## §4 — corpus boundary: confirmed, and the reason is authority, not format

Confirmed: Ledger headers on design memos and rulings; audit reports keep their
native form; scripts are skipped.

**On the sub-question — no, audit reports should not carry `[V]`/`[R]`.** Your read
is right and the reason is stronger than "they already have a format":

> **The `[V]`/`[R]` axis is authority over the design. An audit has none — it
> observes.** A finding is a measured claim about the tree at a commit range. It is
> not binding and it is not a proposal awaiting ratification; it is *evidence*, and
> it expires when the code moves rather than when a later document overturns it.

An audit's recommendation is not an `[R]` either, because it awaits the
*programmer's* implementation, not my ratification. Different axis, different
reader, different expiry.

And the genres already carry the right thing each: **an audit carries
*disposition*** — `REAUDIT56`'s "Disposition of `REAUDIT55`" table, finding by
finding — **and a memo carries *supersession*.** Neither needs the other's
vocabulary. Where an audit's finding does become a design question, the pipeline
already separates them correctly: the audit stays a finding, the consultation
package and the ruling are memos, and those get markers.

## §5 — granularity: confirmed, with the condition for when inline is required

Confirmed: header-level for the backfill; do not inline-mark a hundred
single-decision memos.

One condition, so "optional" does not become "never": **inline marks are required
when a single document's claims have genuinely different statuses and a consumer
must tell them apart** — which is exactly why `SEMANTICCHECKERSCOPING` has them.
That is the same test used everywhere else here: a distinction with no consumer
is not worth recording, and one with a consumer must be recorded where the
consumer can reach it.

## §6 — superseded: not a token, agreed — but a **field**, not prose

This is the one recommendation I am changing, and the package's own principle is
the argument.

The header exists because status *"carried only by voice"* forced every reader to
interpret. Putting supersession in free prose puts one of the three facts straight
back into voice: a reader asking *"is this still live?"* — the most common question
anyone will ask this corpus — would have to read a sentence and judge it.

But you are right that it must not be a third token, because it is not a status;
it is a **lifecycle edge**, orthogonal to whether the document binds. A `[V]` that
has been superseded was binding and is not now, and that is two facts, not a third
marker value.

So: **fixed fields with defined values.** Decision 1 governs the *marker*
vocabulary; it does not forbid the header having structure.

```
> **Ledger** — [V] <one line: what this decides, or what it asks>
> supersedes: <docs/sections | none | not yet checked>
> superseded by: <doc | none | not yet checked>
```

Two lines, three facts, all scannable without judgment — which is the whole
requirement §8 stated.

## §7 — what to fix before Pass 1, not after: the header is a schema, so give it a consumer

The one addition, and the reason it is worth a paragraph of delay.

**A fact with no consumer cannot be kept true.** A hundred headers written once and
read by nobody will drift on the first edit, and then the corpus has a hundred
confident-looking claims of unknown accuracy — strictly worse than none, because
they will be trusted. This is the same rule that put the reference text in a
declaration field with a **generated** `reference.md` rather than in a document
somebody maintains.

So:

**Fix the field names and their legal values now** — before the pass, not after.
Retrofitting a schema onto 108 hand-written headers is the pass done twice, and
this is the only part of the decision that is genuinely expensive to defer.
Re-cut the existing 14 to match; inconsistency at the top of a file is worse than
inconsistency anywhere else in it, because that is where the convention is learned.

**Then generate an index from them** — a script that walks the corpus and emits the
live-verdict list, alongside the 69 already tracked. That is what keeps the headers
true: a malformed or missing header becomes a visible defect in a generated
artefact rather than an invisible one in a file nobody opened. It also produces the
thing a successor actually wants, which is not a hundred headers but **one page
listing what currently binds.**

The generator does not block Pass 1 — it can land immediately after. The **schema**
does block it.

## Summary

| | |
|---|---|
| **1 vocabulary** | **confirmed** — exactly `[V]`/`[R]`, shades in prose. A token set is a vocabulary a consumer switches on; `[V/R]` is a union encoded in a token |
| added | a document's marker states its **strongest** claim — if any part binds it is `[V]`, and the prose names the part that does not. The costs are asymmetric |
| **2 authority** | **confirmed** — successor proposes, designer ratifies the `[V]`s |
| changed | the proposal must **cite a ruling event, not a tone**; no citable event ⇒ `[R]` by default. My review becomes **one-directional** — I only promote |
| and | **batch the checklist by ruling event**, not by document. Twenty groups, not 108 rows |
| **3 two passes** | **confirmed** — ship Pass 1 without Pass 2 |
| added | Pass 1 must not make stale documents look live. `not yet checked` and `none` are **different facts and need different spellings** — and the first doubles as Pass 2's generated worklist |
| **4 boundary** | **confirmed.** Sub-question: **no** — audits stay out. The `[V]`/`[R]` axis is *authority over the design*, and an audit **observes**; its recommendation awaits the programmer, not me |
| and | **audits carry disposition, memos carry supersession.** Neither needs the other's vocabulary |
| **5 granularity** | **confirmed** — header-level. Inline **required** only where one document's claims differ in status *and a consumer must tell them apart* |
| **6 superseded** | **changed** — not a token, agreed, but not prose either. Free prose puts *"is this live?"* back into voice, which is the defect the header exists to fix. Make it a **field with defined values** |
| **7 added — blocking** | **fix the field names and legal values before the pass.** Retrofitting a schema onto 108 hand-written headers is the pass done twice. Re-cut the existing 14 |
| **7 added — not blocking** | **generate an index from the headers.** A fact with no consumer cannot be kept true; the generated live-verdict page is what a successor actually wants, and a malformed header becomes visible |
