# Consultation — headering the five relayed Slice-1 documents

> **Ledger** — `[R]` Consultation — headering the five relayed Slice-1 documents
> supersedes: none
> superseded by: none

You relayed the five binding documents `SLICEONETYPINGS` §0 named — `NUMBERRUNTIME`,
`SCALARANDPROMOTION`, `EXACTNESSISAVALUE`, `RUNAWAYWATCHDOG`, `TEXTDESIGN`. All five
are now in the folder. `TEXTDESIGN` carried a `[R]` header (I fixed its one format
bug — a combined `supersedes | superseded by` line). **The other four have no ledger
header at all**, so the generator lists them under "awaiting a header," and headering
them needs two things that are yours to decide, not mine to assume:

1. the **marker** — `[V]` promotion is on the standing "needs a ruling" list; and
2. the **supersession edges** — each doc "corrects the last document," but they cite
   the predecessor *descriptively*, so the chain is yours to pin, not mine to guess.

Only `TEXTDESIGN`'s content feeds Slice 1 (the `@`-on-`text` answer), which I have.
The other four are numeric-tower rulings for deferred work; this is about getting them
into the corpus correctly so the drift `SLICEONETYPINGS` §0 flagged is closed.

---

## Q1 — the markers

My read from each document's own language, for you to confirm or correct:

| doc | proposed | why |
|---|---|---|
| `EXACTNESSISAVALUE` | **`[V]`** | a ruling — "exactness belongs on the value, not the type"; `SLICEONETYPINGS` §1 cites it as binding |
| `SCALARANDPROMOTION` | **`[V]`** | "**Accepted:** scalar only … silent promotion …" + a correction (§5) |
| `RUNAWAYWATCHDOG` | **`[V]`** | "**Three rulings.** §2 … §3 … §4 …" |
| `NUMBERRUNTIME` | **`[R]`** | reads as a recommendation — "the library is the last question … nearly forced" |

`TEXTDESIGN` stays `[R]` (self-marked). Confirm the four, and I header them.

## Q2 — the internal chain: what is "the last document" for each?

Two of them explicitly correct a predecessor's numbered section, so if that predecessor
is a **headed corpus doc** it gets a `supersedes` edge (and a reciprocal `superseded by`);
if "the last document" was **conversational** (an unrelayed turn), the edge is `none`.

- **`EXACTNESSISAVALUE`** — "**§2 of the last document was wrong** … a rule that says
  roots return `fast number` throws away exactness." *Which document proposed
  roots-return-`fast number` in its §2?* Candidates in the corpus that touch
  `fast number` / roots: `FASTRESERVATION`, `DISCARDEDKINDSRULING`, `NUMERICANDWRITERS`
  — or one of the other four relayed here.
- **`SCALARANDPROMOTION`** — "an argument I made in **the last document** … the
  always-running IDE *mitigates* silent promotion … **I had it the wrong way round.**"
  *Which document made the always-running-IDE-mitigates-promotion argument?*
- **`RUNAWAYWATCHDOG`** and **`NUMBERRUNTIME`** — I read as `supersedes: none` (the first
  extends the `Graph.Draining` *mechanism*, not a doc; the second is a fresh
  recommendation). Confirm.

For each, I need either a **doc `§`** (I'll add the supersession pair) or **"conversational
→ none."**

## Q3 — do any of these overturn an existing `[V]` numeric ruling?

The sharper version of Q2, and the one that matters for corpus consistency: if this
dialogue **corrects or narrows a ruling already in force** — e.g. anything in
`DISCARDEDKINDSRULING` `[V]`, `NUMERALALPHABET` `[V]`, `NUMERICANDWRITERS` `[V]`, or
`FASTRESERVATION` `[R]` — that existing doc needs a `superseded by` edge back, or the
ledger will show the new ruling as authoritative while the old one still reads as live
(the exact drift the `OPENDECISIONS §3` pass just fixed). Name any and I wire both sides.

---

## What I'll do with the answers

Header all four (marker + one-line summary + the edges you give), add every reciprocal
edge, regenerate and `--check` the ledger, and commit the five documents into the corpus
with `TEXTDESIGN`'s fix. After that the "awaiting a header" list is empty and Slice 1 is
fully unblocked on the doc side (its `@`-on-`text` content is already in hand).

## Summary

| # | question | what I need |
|---|---|---|
| Q1 | markers for the four unheadered docs | confirm `[V]`/`[V]`/`[V]`/`[R]` above |
| Q2 | what "the last document" is for `EXACTNESSISAVALUE` and `SCALARANDPROMOTION` | a doc `§`, or "conversational → none"; and confirm the other two are `none` |
| Q3 | do any overturn a live `[V]` numeric ruling | name it, so I add the reciprocal `superseded by` |
