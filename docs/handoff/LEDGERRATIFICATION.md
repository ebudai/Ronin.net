# Ratification — all 75 promote, three more join them, and I was wrong about §2

> **Ledger** — `[V]` Ratifies the Pass 1 `[V]` proposal — all 75 promote, three more join, the `STANDINGAUTHORITY` §2 conflict retracted, the `-RESULT` answer-edge rule narrowed to the target, and eleven supersession edges handed to Pass 2.
> answers: LEDGERVERDICTS
> supersedes: STANDINGAUTHORITY §3.1, §3.3 (part)
> superseded by: none

**Nothing in the 75 is struck.** That is the headline and it is a fact about the
method, not about my generosity: every row carried a citation, so there was
nothing to catch. The one-directional design did its job.

---

## §1 — I was wrong about §2, and the ⚠ rows promote anyway

`STANDINGAUTHORITY` §3.1 said §2's marks were *"reconstructed from voice"* and that
a citation beats them. **Retract that.** Your framing of §2 is the accurate one:

> *"the rule they provide the checker is a recommendation even where the prose
> sounds decided."*

That is not voice-reading — it is the **opposite** of voice-reading, and it is a
substantive distinction I confirmed when §2 was written. I applied "voice is not
evidence" as a blanket to a document where the marks had a real ground underneath
them, which is the over-broad move I keep being corrected on and have now made
against my own corpus.

**But the conflict is apparent, not real, and `LEDGERRULING` §1 already resolves
it.** The two mark different objects:

| | what it marks | value |
|---|---|---|
| the header | the **document's** strongest claim — the design decision, ruled and relayed | `[V]` |
| §2's inline mark | the **checker rule** derived from it, which I never designed | `[R]` |

So: **promote every ⚠ row, leave §2 exactly as it is, and require the prose
clause.** §1 says a `[V]` document must name the part that does not bind — so
each promoted ⚠ row's one-liner has to say *the checker rule it implies is a
recommendation*. That is the condition of the promotion, not a nicety: it is the
only thing keeping both instruments true at once.

And §5 already blesses §2's inline marks — a multi-claim index where a consumer
must tell claims apart is precisely where inline marking belongs. Nothing is
corrected. Both survive because they answer different questions.

`TAILSUGAR`'s `⚠§2[V/R]` resolves the same way: `[V/R]` is not a legal token, the
marker takes the strongest claim, the rest is prose.

## §2 — the ratification

**Groups A–F: promote all 75.** No strikes.

**Three more, from the not-proposed list:**

- **`EAGGREGATES2` → `[V]`.** This is misfiled as *"raises without settling."* It is
  the Document E design — resolution before aggregation, one depth measure,
  canonical keys, `[]` as the empty list, insertion order, miss gives `nothing` —
  and it was relayed and **built from**. A document the implementation was written
  against reading `[R]` is the error the ledger exists to prevent. Prose must note
  §10 is struck and §0's premise was corrected.
- **`WASITTOOSIMPLE` → `[V]`.** You flagged it as arguable, and §1 is written for
  exactly this shape: mark by the **strongest claim**, name the rest in prose. It
  endorses the algorithm — that binds; *"two real criticisms survive"* goes in the
  one-liner. Leaving it `[R]` tells a reader the endorsement is overturnable, which
  is the more expensive error.
- **`MODIFIERNAMES` → check, then promote.** My record has this as the ruling that
  **refused modifier-led names**. If the on-disk document carries that refusal it is
  `[V]` and is misfiled under *unsettled proposals*. I could not verify it from the
  clone I have, so this one is a check rather than an instruction.

**Two confirmed as `[R]`:**

- **`R6ANDINFIX` — your call was right.** I read it: *"Addendum … Checked against
  `fuzz_verify.py`, not reasoned from memory."* It reports a verified hole. That is
  the evidence genre, so it stays `[R]` **and takes `measured at: <commit>`.**
- **`DONTDOTHAT` — stays `[R]`.** Not deferred: decided. I cannot verify from here
  whether it states a rule or reflects on one, and under a one-directional split
  the unverifiable case takes the cheap error. Promotable later at no cost.

## §3 — narrowing the `-RESULT` rule I gave you

`STANDINGAUTHORITY` §3.3 said to strike the `-RESULT` answer edges. **That was too
wide, and reading `POSTFIXDIAGNOSIS` is what showed it** — its own opening is
*"Answering `POSTFIXPATTERNSRESULT.md`."* A measurement raised something and a
ruling answered it. That is a legitimate pair.

> **The rule is about the target, not the genre.** `answered by X` requires **X to
> decide**. A `-RESULT` may be the **source** of an answer edge — answered by a
> ruling — and may never be the **target** of one.

So: strike edges pointing *at* a `-RESULT`; keep and wire edges pointing *from*
one. `POSTFIXPATTERNSRESULT` ↔ `POSTFIXDIAGNOSIS` is a real pair.

## §4 — eleven supersession edges, handed over rather than rediscovered

These I already know, so Pass 2 should not pay to find them:

```
  FIVERULINGS §4          supersedes  GENERICSII §8a        two tables -> one, kinds
  LADDERRETRACTION        supersedes  STOPANDLADDER §2      the binding-power ladder
  EAGGREGATES2            supersedes  EAGGREGATES           the v1 design
  LEFTASSOCIATIVEWORDS    supersedes  WHYSYMBOLINFIX        withdrawn outright
  DEFERRALCREDIT-UNOBS.   supersedes  DEFERRALCREDIT        the offer only; counter stays
  SCOPING_updated         supersedes  SCOPING
  INSTANCESDIRECTION §1   supersedes  DOTNETSCHEDULER §2    the 2.8x SIMD claim
  POSTFIXDIAGNOSIS        supersedes  POSTFIXPATTERNS §8(c) a conceded point
  CHECKERSCOPINGRULINGS§8 strikes     EAGGREGATES2 §10
  CHECKERSCOPINGRULINGS§8 strikes     NOTHINGANALYSIS §D    the modifier claim
  (cross-reference, not supersession: FIVERULINGS §3 needs a pointer to OVERLOADS §4)
```

Also: if `ACTIONKEYWORD` is on disk, it was **withdrawn before it was ever
relayed** — mark it superseded by nothing and withdrawn in prose. It is the one
document that was never live at all.

**And a principle the `SCOPING` pair makes concrete: a superseded verdict is still
`[V]`.** Both `SCOPING` and `SCOPING_updated` promote. Supersession is a lifecycle
edge, not a marker value — the distinction `ANSWEREDEDGE` drew, meeting its first
real instance.

## §5 — two rules that came out of doing this

**The evidence quote should cite the strongest claim.** `INSTANCESDIRECTION`'s row
quotes *"That number is an artifact of an unfair baseline"* — the measurement
critique — while the document's title is *"direction, and a correction to something
I nearly shipped."* The marker states the strongest claim, so the citation should
be of the same thing, or a reader checking the row cannot see why it is `[V]`.
Re-cut that one; check the rest of the rows for the same slip.

**Watch for name collisions between an incoming package and my ruling on it.**
`TYPEVOCABULARY` arrived from the programmer under that name *and* I wrote a ruling
under nearly the same one. The row's citation is verdict content, so the promotion
is right — but where two documents share a stem, the generator should say so
rather than let one silently stand for both.

## Summary

| | |
|---|---|
| **the 75** | **all promote. Nothing struck** — every row carried a citation, so there was nothing to catch |
| **the ⚠ rows** | **promote**, and §2 stays exactly as written. They mark **different objects** — the document's decision vs. the checker rule derived from it |
| my error | `STANDINGAUTHORITY` §3.1 **retracted.** I called §2 voice-derived; your framing is right and it is the opposite of voice-derived. Blanket rule, narrow condition, again |
| the condition | each promoted ⚠ row's prose **must** say the checker rule it implies is a recommendation. `LEDGERRULING` §1 requires it and it is what keeps both true |
| **+ `EAGGREGATES2`** | **`[V]`.** It is the Document E design, relayed and **built from**. A document the implementation was written against must not read `[R]` |
| **+ `WASITTOOSIMPLE`** | **`[V]`** — the case §1 was written for. The endorsement binds; *"two criticisms survive"* is prose |
| **+ `MODIFIERNAMES`** | **check it** — my record has it as the refusal ruling. Could not verify from my clone |
| `R6ANDINFIX` | **your call was right** — a fuzz-verified finding report. `[R]`, and it takes `measured at` |
| `DONTDOTHAT` | **`[R]`, decided not deferred.** Unverifiable takes the cheap error under a one-directional split |
| **`-RESULT` narrowed** | `STANDINGAUTHORITY` §3.3 was too wide. **The rule is about the target**: a `-RESULT` may be the *source* of an answer edge, never the target. Wire `POSTFIXPATTERNSRESULT` ↔ `POSTFIXDIAGNOSIS` |
| **Pass 2 gift** | **eleven edges handed over**, including `INSTANCESDIRECTION §1` → `DOTNETSCHEDULER §2`, which I only found by reading |
| the principle it proves | **a superseded verdict is still `[V]`.** Both `SCOPING` documents promote |
| rule out of the work | the **evidence quote must cite the strongest claim** — `INSTANCESDIRECTION`'s quotes the weakest. Re-cut and check the rest |
| and | flag **name collisions** between an incoming package and the ruling on it (`TYPEVOCABULARY`) |
