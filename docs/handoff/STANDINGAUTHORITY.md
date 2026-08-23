# Three answers, and a standing rule so the next three do not need asking

> **Ledger** — `[V]` Answers the three open items from Pass 1, strikes the `-RESULT`
> answer edges in favour of a commit stamp, and gives a standing rule for what needs
> a ruling and what does not.
> answers: the Pass 1 completion report
> supersedes: none
> superseded by: LEDGERRATIFICATION (§3.1, §3.3 part)

**Pass 1 is better than what I asked for.** Five calls in it were yours and all
five were right — worth naming individually, because "be more confident" is
useless advice and "these specific judgments were correct" is not.

---

## §1 — what you got right without being told

- **All-`[R]` shell as the default.** That is the ruling applied in the direction
  that makes a wrong answer cheap. Exactly right.
- **Answer edges filled only from explicit on-disk statements.** The ruling
  demanded citation-not-tone for `[V]`; you extended the same evidence standard to
  a field I had not applied it to. That is the rule generalised correctly, not
  followed literally.
- **`superseded by: none` on every answered memo, 130→113.** That inference is one
  step past what `ANSWEREDEDGE` actually wrote down, and it is sound: a memo whose
  ruling incorporates it has been checked, so `not yet checked` would be false.
- **Flagging the `SEMANTICCHECKERSCOPING` §2 conflicts instead of resolving them
  silently.** A conflict quietly resolved is a conflict that reappears.
- **`--check` gating drift.** I asked for a generator that verifies reciprocity.
  You made it a *gate*, which is the difference between a report and an
  enforcement, and it is the whole reason the ledger will still be true in six
  months.

## §2 — the standing rule

> **Ask when a wrong answer cannot be recovered by the next reader. Otherwise
> decide, record the reason, and carry on.**

The `[V]` gate exists because a wrong `[V]` **is** unrecoverable in effect: it
asserts binding authority that was not granted, and a later reader acts on it
before anyone notices. That is a narrow property, and almost nothing else has it.

Things that are **yours**, without asking:

- anything the generator can check — a wrong edge is a failing `--check`, not a
  silent defect;
- anything whose default is safe — `[R]` costs a promotion later, and that is all;
- anything reversible by a later edit with no reader misled in between;
- **the order you do things in.**

Things that need a ruling:

- promoting a document to `[V]`;
- a change to what the language *means* — where valid source becomes invalid, or
  an answer changes;
- refusing a capability, as opposed to refusing a spelling;
- a decision that **sets a precedent** — where the answer is a rule others will
  cite, even when the instance itself is reversible. The instance is small; the
  rule it establishes is not, and a rule established silently is one nobody can
  cite (`FINDINGCOMPOSITION` §5).

That list is short on purpose. If a decision is not on it, it is yours.

### And the concrete instance, which is the useful part

**Pass 2 does not depend on the ratification.** A supersession edge records which
document struck which claim; whether either document is a verdict or a
recommendation does not enter into it. The two are fully independent, and pausing
one on the other cost a session for no reason.

That is the general shape of the problem, and it is worth checking before
stopping: *does the thing I am about to wait on actually gate the thing I am
about to stop doing?* Here it did not, and it was checkable in a minute.

## §3 — the three items

**1. Ratification.** Send `LEDGERVERDICTS.md` through and I will work it. The
criterion I will apply, so you can predict the outcome:

> `[V]` **iff the document records a decision that was ruled and relayed.** A
> ruling written in answer to a consultation is `[V]`. A decision the owner made
> is `[V]`. Analysis, probes, consultation packages, proposals and backgrounders
> are `[R]`. A document written and withdrawn before relay is not `[V]` — that is
> a supersession-side fact.

**On the ⚠ conflicts: the citation wins, and `SEMANTICCHECKERSCOPING` §2 gets
corrected.** §2's inline marks were *reconstructed from voice* — they are the
guesswork the header scheme exists to replace. Letting them govern would launder
the original re-derivation into the new mechanism and we would have built the
ledger to preserve the thing it was built to end. Where a citation-backed row
disagrees with §2, §2 is wrong.

**2. The two unwired pairs.** `TYPEHALFDECISIONS` ↔ `TYPEHALFRULINGS` is a pair —
wire it. `MODIFIERNAMES` ↔ `MODIFIERNAMES-RESULT` is item 3's category, so see
below. And by §2 this class of call is yours from now on: an answer edge you can
check against the two documents does not need me.

**3. The `-RESULT` edges: strike them.** `answered by X` has a defined meaning —
**X governs.** A measurement does not govern a design question; it is the evidence
a ruling stood on. Wiring it as an answer edge sends a reader looking for a verdict
to a table of numbers, which is the same category error `ANSWEREDEDGE` just fixed
one level in.

Do not add a `measured by` field for it. The relation has no consumer worth a
paired edge, and the schema should stop growing. **What that genre actually needs
is a different field:**

> A `-RESULT` document is a claim about the tree **at a commit**, exactly like an
> audit finding, and it goes stale when the code moves rather than when a document
> overturns it. Give it `measured at: <commit>` and nothing else.

That is the field with a real consumer — anyone deciding whether the number is
still true — and it is the discipline I imposed on myself after quoting a figure
that was accurate when measured and wrong when I cited it.

## Summary

| | |
|---|---|
| Pass 1 | **better than specified.** Five unasked calls, all correct — the all-`[R]` shell, citation-only edges, the 130→113 inference, flagging the §2 conflicts, and making the generator a **gate** rather than a report |
| **the standing rule** | **ask when a wrong answer cannot be recovered by the next reader; otherwise decide, record why, carry on** |
| yours without asking | anything the generator checks, anything defaulting safe, anything reversible with no reader misled in between, **and the order of work** |
| needs a ruling | `[V]` promotion; a change to what the language **means**; refusing a **capability** rather than a spelling. That is the whole list |
| the instance | **Pass 2 never depended on the ratification** — a supersession edge does not care whether either document binds. Check whether the blocker is real before stopping; here it took a minute |
| **1 ratify** | send `LEDGERVERDICTS.md`. Criterion: `[V]` **iff ruled and relayed**; analysis, probes, packages and proposals are `[R]` |
| the ⚠ conflicts | **the citation wins.** §2's inline marks were reconstructed from voice — letting them govern would launder the guesswork the ledger exists to replace. §2 gets corrected |
| **2 pairs** | `TYPEHALFDECISIONS` ↔ `TYPEHALFRULINGS`: **wire it.** And this class is yours from now on |
| **3 `-RESULT`** | **strike the answer edges.** `answered by` means *X governs*, and a measurement does not govern |
| instead | **no new field.** Give that genre `measured at: <commit>` — a measurement is a claim about the tree at a point in time, and staleness is the only thing a reader needs to judge it |
