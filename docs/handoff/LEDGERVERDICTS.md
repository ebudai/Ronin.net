# Pass 1 — the `[V]` ratification checklist

> **Ledger** — `[R]` Proposes the `[V]` promotions for Pass 1: 75 documents the sweep
> left `[R]` that read as settled designer verdicts. Nothing is promoted until the
> designer ratifies — `LEDGERRULING` §2, one-directional. Grouped by area (a proxy
> for the relay events), evidence per row, conflicts with `SEMANTICCHECKERSCOPING`
> §2 flagged ⚠.
> supersedes: none
> superseded by: not yet checked

## How to read this

The shell sweep marked every design document `[R]` — the safe default. This is the
proposal for which of them are actually **verdicts** (`[V]`), each with an evidence
quote. Per `LEDGERRULING` §2 the split is **one-directional**: you only ever promote
`[R]→[V]`, and a wrong `[V]` cannot be here because each row carries the citation.
**Ratify by the group** — strike any row that should stay `[R]`, and I promote the
rest.

Five are already `[V]` and not relisted: `BASERESOLUTIONRULING`,
`CHECKERSCOPINGRULINGS`, `CONTAINERIDENTITYRULING`, `NAMEDIDENTITY`,
`SCOPEIDENTITYRULING`.

**⚠ Look here first.** `SEMANTICCHECKERSCOPING` §2 — your own confirmed
classification of the type-checker source documents — marks several of the docs
below `[R]` on purpose: the *rule they provide the checker* is a recommendation even
where the prose sounds decided. Where this proposal and §2 disagree, **§2 governs
unless you say otherwise.** Those rows are flagged ⚠§2[R].

---

## Group A — types, inference, generics, aggregates

- **TYPEHALFRULINGS** — "Verdicts: modifier, annotations only, your line in §3 as written" (§2 [V])
- **TYPEVOCABULARY** — "Lowercase, case-sensitive. Yes to type patterns … one table, not two" (§2 [V])
- **FIVERULINGS** — "Five rulings — and the one law that decides three of them" (§2 [V])
- **REAUDIT47RULING** — "the design question … is answered here" (§2 [V])
- **INSTANCEBINDING** — "Decided: one cell per declared member, holding N values" (§2 [V])
- **VARIABLEANDMODULE** — "Q4a: enforced object uniqueness … Q5b: the module-identity type"
- **NUMERICANDWRITERS** — "Implement `Cascades.Writers(...)` as a pure function over supplied sets"
- **LISTREPRESENTATION** — "Confirming the design edge … not a runtime-representation choice"
- **ERRORASVALUE** — "you are right … Take the easy way"  ⚠§2[R]
- **ISANDEQUALITY** — "Short version: adopt it"  ⚠§2[R]
- **RETURNANDLITERALS** — "I am just ruling the literals here … your §1 is right, your §2 is right"  ⚠§2[R]
- **RECURSIVERETURN** — "you were right … Right call"  ⚠§2[R]
- **MONOMORPHANDRETURN** — "He is right, and the residue is even smaller than he says"  ⚠§2[R]
- **GENERICSII** — "I agree with all three"; monomorphisation `[forced]`  ⚠§2[R]
- **TAILSUGAR** — "Both taken … ruled, and no marker"  ⚠§2[V/R]
- **OVERLOADS** — "Overloads are resolver alternatives … the type filter is one pass"  ⚠§2[R]

## Group B — syntax: arrows, holes, brackets, postfix, operators, glue

- **ARROWASSOCIATIVITY** — "§2 does not need an associativity chosen — it needs one refused"
- **ARROWSEGMENT** — "take option (a) … the finding is right"
- **EMPTYBRACKETS** — title "reject it"; "Ill-formed — and working out why produces a small piece of syntax"
- **LEADINGFREEHOLES** — "that is the recommendation taken verbatim, so it is settled"
- **LEADINGHOLES** — title "settled"; "The reproducibility hit is fair, and taken"
- **POSTFIXPATTERNS** — title "admit them"; section "## 1. Verdict"
- **POSTFIXDIAGNOSIS** — "a narrower rule that solves §3(d) and §8(a)"
- **OPERATORNOTPATTERN** — "Both corrections accepted … I would take it"
- **GLUEASWHOLENAMES** — title "accept, with one rule added"; "the removal is right"
- **GLUERULESPLIT** — "Yes, R5′ narrows the pattern-glue rule too … His shape 1 is right"
- **ZEROGLUE** — "your instinct was right … the correct move for the correct reason"
- **R7BCONDITION** — "then one correction and one rule" (delivers both for R7b)
- **R7BTRIM** — "keep `not` in the generated set … start A now"
- **ONELAW** — "Agreed on all three of his points"
- **SIMPLERRULES** — title "Yes"; "yes on the algorithm, with one disagreement retained"
- **NAMEVSANCHOR** — "The retraction stands and the test behind it is the correct one"
- **UNDERSCORE** — title "yes, a stand-in"; "Correcting before anyone implements from it"
- **OLDASWHAT** — "Getting it out of the flat name table is the right move"

## Group C — reactive, control flow: when / wait / stop / chains / loops

- **CHAINACTIVATIONS** — "Budai's instinct is right — the trouble is the one-activation rule"
- **WHENANDWAIT** — "Handoff for the programmer. Design decisions, agreed"
- **WHENTYPESCOPE** — title "he is right, §1 is wrong, and §4 is wrong too"
- **WAITSEMANTICS** — title "decided: proceed"; "Level, not edge. His implementation is correct"
- **STOPALL** — title "good idea, and it catches a bug I was about to ship"
- **STOPANDLADDER** — "Both answered … All three taken"
- **LADDERRETRACTION** — "Taken. `STOP-AND-LADDER.md` §2 is withdrawn"
- **FOURANSWERS** — "The rule: `when` bodies are unordered relative to each other …"
- **DIRECTIONPACKET** — "Chain segments must be one writer — confirmed"
- **INSTANCESDIRECTION** — "That number is an artifact of an unfair baseline"
- **TIMETOLIVE** — title "`time to live` — yes, it can be legal"
- **LOOPSYNTAX** — "Decision: `for each bank in banks`. The documented spelling wins. Ship it"
- **LOOPINDEXANDGLUE** — "Decision: inject `index of «loop variable»`"
- **ROUNDLIMIT** — "He is right on all of it"

## Group D — aggregates, lookups, match, ambiguity, `if`

- **AGGREGATEPARSE** — "The recommendation is right and I would build it as written"
- **AMBIGUITYASERROR** — "Budai has approved the shape … This is the spec"
- **MATCHNAMED** — title "decided: `match` is for inline arms, `@` for named tables"
- **LOOKUPARROWRULED** — "Ruled. Zero reserved words, prose-shaped"
- **LOOKUPEQUALITY** — "Straight answer to the question … Here it is"
- **ARTICLECONDITIONAL** — title "Yes it can be conditional"
- **IFASEXPRESSION** — title "`if` as an expression — yes, and it is cheaper than what it replaces"
- **Q2SETTLED** — "Q2 is settled below, against my own argument"

## Group E — resolver, findings, identity, corrections, misc

- **DELEGATES** — "Accepting the fix … The narrowing is correct"
- **DESCRIPTORSLICE** — "`Injection` as the precedent is the right read"
- **DONTDOTHAT** — "You are right, and it is worth saying how consistently"
- **FINDINGSANDSPANS** — "Both answers are yes-to-what-you-proposed"
- **MODULEMERGE** — "The compiled-scope decision is correct and I would take it now"
- **REFERENCESTRUCTURE** — "His sentences are better than mine … Ruling on what happens when the compile-time filter lands"
- **REMOVALADJUDICATION** — "Verdict: the finding is correct and the severity is right"
- **SHRINKTAGGING** — "yes to 1, no to 2 for the common case, yes to 3"
- **SLICINGRESPONSE** — title "Three corrections accepted"; "All three corrections stand"
- **SWEEPITEMS** — "`<` and `>` are not syntax, and never were … Yes — both"
- **INTERPRETERDECISIONS** — "Answers to the three blockers … they pin every decision below"
- **OPENDECISIONS** — "His per-scope implementation is right and my framing was wrong"
- **REAUDIT4RESPONSE** — title "REAUDIT4 — designer decisions"; "Two are yes, one is a reversal of my own advice"
- **DEFERRALCREDIT-UNOBSERVABLE** — "The offer is withdrawn, the counter stays"
- **DATELITERALS** — title "keep the four-digit minimum, remove the four-digit maximum" (already implemented)
- **LEFTASSOCIATIVEWORDS** — "Withdraw `WHYSYMBOLINFIX.md` … Budai's proposal works"

## Group F — process / meta

- **constants** — "yes for constants, this is perfect … enforce dependency order, cycles banned"
- **SCOPING** — "## The decision: what an inner scope sees … Inward yes, outward no, and no shadowing"
- **SCOPING_updated** — same decision as `SCOPING` (one supersedes the other — a Pass 2 edge)

---

## Two probable pairs the sweep left unlinked (not answer-edged yet)

These read as a memo→ruling pair but carry **no** explicit "answers/answered by"
statement, so I did not wire the edge. Confirm and I will:

- **TYPEHALFDECISIONS** (memo, "four things to rule") ↔ **TYPEHALFRULINGS** (ruling, "four rulings") — TYPEHALFRULINGS opens "the memo is the right shape" but never names it.
- **MODIFIERNAMES** (memo) ↔ **MODIFIERNAMES-RESULT** — the result says "You asked me to run the §1 check" but names no doc.

## Not proposed for `[V]` (stay `[R]`)

The memos that ask (`BASERESOLUTION`, `CONTAINERIDENTITY`, `IDENTITYANDSIGNATURES`,
`SCOPEIDENTITY`, `SEMANTICCHECKERSCOPING`, `FASTRESERVATION`, `TYPEHALFARROW`,
`TYPEHALFDECISIONS`), the measurement/verification responses (the `-RESULT` docs,
`ACCUMULATIONBOUND`, `BRACENEST-MEASURED`, `QUEUEDEPTH`, `REACTIVEPERFORMANCE`), the
surveys/indexes (`HANDOFF`, `README`, `TYPECHECKERHANDOFF`, `DELTA`, `BUILTANDFOUND`,
`NEEDFROMDESIGN`, `FAILUREMODES`, `NOTHING-ANALYSIS`, `AUDITTRIAGE`), the two
supersession notices, the design memos that raise without settling (`EAGGREGATES`,
`EAGGREGATES2`, `GENERICS`), the probe triplicate (`FUZZBRACKETS*`), and the
unsettled proposals (`BRACEDECISION`, `DEFERRALCREDIT`, `MATCH`, `MODIFIERNAMES`,
`LISTEQUALITY`, `WAITACTIVATIONS`, `REAUDIT56RELAY`).

Two I could argue either way and left `[R]`: **R6ANDINFIX** (verdict-sounding title,
but a fuzz-checked finding report) and **WASITTOOSIMPLE** (endorses the algorithm,
but measurement-dominant with "two real criticisms survive").
