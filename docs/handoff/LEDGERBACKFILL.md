# The ledger backfill — six decisions before a hundred headers get written once

> **Ledger** — `[R]` Asks the designer to ratify the `[V]`/`[R]` backfill that
> `CHECKERSCOPINGRULINGS` §8 instructed: the marker vocabulary, who may stamp `[V]`,
> the header's scope, the corpus boundary, granularity, and how a header says
> *superseded*. Doubles as a backgrounder for the owner.
> supersedes: none
> superseded by: not yet checked

## §0 — the ask, in one paragraph

The designer has already decided that these memos should carry a status header —
`CHECKERSCOPINGRULINGS.md` §8: *"Put your `[V]`/`[R]` marking in the documents … the
next successor should inherit it rather than re-derive it."* Fourteen of ~122
design documents carry that header today; the rest do not. This is **not** a memo
asking whether to backfill the other hundred-odd. It is a memo asking the six
questions a backfill of that size has to answer *before* it is executed, so it is
done once and to a fixed convention rather than improvised a hundred times and
re-litigated. Each question below carries a recommendation; a single ruling
unblocks the whole mechanical pass. **No documents are changed by this memo.**

---

## §1 — what the ledger is (for the owner)

Two markers, reconstructed by a successor in `SEMANTICCHECKERSCOPING.md` §2 and
turned into an instruction by the designer in `CHECKERSCOPINGRULINGS.md` §8:

| marker | means | who it belongs to |
|---|---|---|
| `[V]` | **verdict** — a binding decision. The design *is* this. | the designer |
| `[R]` | **recommendation** — a proposal, not yet binding. | a successor / auditor |

The distinction is not decorative. A checker rule sourced from a `[V]` is settled;
the same rule sourced from an `[R]` is a proposal a later document may overturn.
Today that status is carried only by **voice** — whether a document "speaks as a
verdict" (uses *forced / decided / taken*) or "speaks as a recommendation." The
successor who wrote §2 reconstructed the whole verdict-vs-recommendation split by
reading tone across dozens of files, and the designer's reply names that as the
problem: *"careful reading … should not have been necessary."* The header is the
fix — the fact stated once, on the document, instead of re-derived by every reader.

The house form of the header is a leading blockquote, seen at the top of the
fourteen marked files:

```
> **Ledger** — `[V]` verdict. Answers `CONTAINERIDENTITY.md`: B, the overload set
> is one container … §3 structural identity, the string render-only. Finding 2
> lands with the refactor.
```

The designer specified its **content** as three facts: *what this decides, what
supersedes it, what it supersedes.*

## §2 — current state

| | |
|---|---|
| tracked handoff `.md` | **210** |
| — audit / review reports (`REAUDIT*`, `AUDIT*`, `FRESHAUDIT*`, `CODEREVIEW`) | 88, in their own `**Audited:** / ## Result` format |
| — design memos, rulings, analyses, advisories | ~122 |
| — of those, carrying a `[V]`/`[R]` Ledger header today | **14** |
| tracked `.py` tooling scripts | 69, no prose to mark |
| marker tokens actually in use | `[R]` ×20, `[V]` ×17, and two one-offs: `[V/R]` ×1, `[R, but the checker rule]` ×1 |

So the backfill's real size is roughly **~108 design documents** (122 minus the 14
done), *if* the 88 audit reports are excluded — which is itself one of the
questions below.

---

## §3 — the six decisions

### 1. The marker vocabulary — fix it at two, qualifiers as prose

The scheme is two symbols, but the corpus already sprouts `[V/R]` and
`[R, but the checker rule]`, and the prose leans on `[forced]`, `[decided]`,
`[taken]` as voice-words that push an `[R]` toward `[V]`. A hundred more documents
is exactly where a token zoo grows, and a growing token set defeats the point — a
consumer that once again needs interpretation.

**Recommend:** canonical set of exactly **two** bracket tokens, `[V]` and `[R]`.
Every shade — *forced*, *partial*, *the-checker-rule-inside-a-recommendation* —
lives in the trailing prose of the header, never as a new token. `[V/R]` becomes
`[V]` where the verdict part governs, with the recommendation part spelled out in
words.

### 2. Authority — a successor proposes, the designer ratifies the `[V]`s

This is the one that actually needs you. Marking a document `[V]` **asserts it is
binding** — and no one but the designer can certify that. A successor guessing
`[V]` from voice is precisely the re-derivation §8 wanted to end, now committed to
disk under an authority it doesn't have. A successor *can* freely mark its own
memos `[R]`; that claims nothing.

**Recommend:** split the labor by authority. The successor does a mechanical pass
proposing a marker for every document (from voice plus the known ruling record) and
draws up the list; the designer **ratifies the `[V]` set** — a checklist, not
prose. The `[R]`s need no ratification. This is the smallest irreducibly-designer
step, and it keeps the pass from silently promoting recommendations to verdicts.

### 3. Header scope — two passes, and don't block the first on the second

The designer asked for three facts (decides / superseded-by / supersedes). The
marker and the one-line *what it decides* are cheap. The **supersession edges** are
archaeology across 210 files, and the designer already scoped a specific
*"supersession pass"* in §8 (strike `EAGGREGATES2` §10, `GENERICSII` §8a,
`NOTHINGANALYSIS` §D's modifier claim; add the `FIVERULINGS` §3 pointer to
`OVERLOADS` §4 — "four edits", plus whatever a full walk surfaces).

**Recommend:** two passes. **Pass 1** (mechanical, cheap): marker + one-line
*what it decides / asks* on every design document. **Pass 2** (deliberate, lasting):
the supersession edges, done as the §8 supersession pass. Ship Pass 1 without
waiting on Pass 2 — a header that states status is already most of the value.

### 4. Corpus boundary — memos get a header; audit reports keep theirs

The 210 files are three genres. Design memos and rulings want the Ledger. The 88
audit reports already open with `**Audited:** / **Date:** / ## Result` and a
*"Not signed off"* line — that *is* their ledger, in a different form, and an audit
report is arguably neither a verdict nor a recommendation but a **finding**. The 69
`.py` scripts have no prose to mark.

**Recommend:** Ledger header on **design memos and rulings only**; leave audit
reports in their native format untouched; skip scripts. **One sub-question for
you:** should audit reports *also* carry a `[V]`/`[R]`? I read them as a third
genre that should stay out — but if a report's *recommendation* deserves the same
status a memo's does, that argues for including them, and I'd rather you rule it
than I guess.

### 5. Granularity — header-level, not inline per claim

`SEMANTICCHECKERSCOPING.md` marks individual bullet points `[V]`/`[R]`, because it
is an index of many independent rulings. Most documents make one decision; a few
make many. §8 asks for *"a one-line header on each memo"* — header-level.

**Recommend:** header-level for the backfill. Inline per-claim marks stay optional
and are worth it only in the multi-claim index documents that already have them.
Don't inline-mark a hundred single-decision memos.

### 6. Superseded documents — say it in the header prose, not a third token

Some documents predate the rulings that overturned them — `NOTHING-ANALYSIS.md`'s
own header admits it *"predates several rulings it raises."* A backfilled header
has to be able to say *no longer live.*

**Recommend:** the header prose carries it — `[R]`, superseded by `X` — reusing
the marker's third fact (*what supersedes it*) rather than minting a `[superseded]`
token. Consistent with decision 1: status shades are words, not brackets.

---

## §4 — what a ruling unblocks, and what it costs

| | |
|---|---|
| Pass 1 | marker + one-line *decides/asks*, on ~108 design documents. Mechanical once decisions 1–6 are set. Bounded — a focused sitting, not a project |
| the designer's part | ratify one checklist: which of the proposed markers are `[V]`. Everything else is convention you set once here |
| Pass 2 | the supersession edges — the §8 supersession pass. Real archaeology; deliberate; the part that lasts. Separable and later |
| if nothing is ruled | the backfill either stalls or gets improvised per-document and re-litigated — the exact re-derivation §8 set out to end |

## §5 — scope of this memo

This is the consultation package, not the backfill. I have changed no documents and
assigned no markers. On a ruling over the six decisions, Pass 1 is a mechanical
sitting I can bring back as a single reviewable change (the proposed-marker list for
your `[V]` ratification, plus the headers), and Pass 2 can follow on its own.

## Summary

| | |
|---|---|
| the instruction | already given — `CHECKERSCOPINGRULINGS.md` §8, *"put the `[V]`/`[R]` marking in the documents"* |
| the state | 14 of ~122 design documents marked; ~108 to go; audit reports and scripts aside |
| **1 vocabulary** | fix at two tokens `[V]`/`[R]`; every shade is prose, never a new bracket |
| **2 authority** | successor proposes all markers; **designer ratifies the `[V]` set** — the one irreducibly-designer step |
| **3 scope** | two passes; marker + one-liner now, supersession edges later; don't block the first on the second |
| **4 boundary** | headers on memos/rulings; audit reports keep their native format — *sub-question: do reports carry a `[V]`/`[R]` too?* |
| **5 granularity** | header-level, not inline per claim |
| **6 superseded** | said in the header prose, not a `[superseded]` token |
| what I need | a ruling on the six, and the `[V]` ratification when Pass 1 comes back |
