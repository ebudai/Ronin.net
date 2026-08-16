# Ambiguity as an error — direction, with a per-rule ledger

> **Ledger** — `[V]` Ambiguity as an error — direction, with a per-rule ledger
> supersedes: INJECTEDDEDUP
> superseded by: none

**Supersedes the version sent earlier today.** That one had a terse verdict
table and no reasoning per rule, which is what Budai asked about — and writing
the reasoning out turned up a **third** error, which changes the scope again and
this time simplifies it.

Budai has approved the shape: **ambiguity becomes an error, and the error offers
the bracketings, selectably.** This is the spec.

---

## 1. Three corrections, all mine

**1a. "0.00% ambiguous with the rules in force" did not measure the rules.**
That arm generated names avoiding glue and anchor words *entirely* — far
stricter than R5′ (interior only) or R6b (leading only). `to uppercase`,
`is valid`, `by sum` are legal under the rules and were excluded. Honest
numbers:

```
  all four in force   0.03%      R6b only   0.16%      none   3.47%
```

**1b. Brackets group; they do not classify.** "Bracket it" is not a complete
fix. `send send price` with a name `send price` has two readings, and
`send (send price)` is ambiguous *inside the bracket* in the same way. That
reading cannot be expressed at all.

**1c. The per-rule test that found 1b tested nothing for one of the rules.** It
reported *"R5′ interior — ALL READINGS EXPRESSIBLE"* on **zero** cases: the
generator used pattern glue, and `Infixes(names)` is about **operator** words,
which it never produced. Zero cases proves nothing. Retested properly, operator
words behave like R6b, not like glue.

## 2. Per-rule ledger

| | `Infixes(names)` | `Glue(names)` | `Refining(names)` | `Shadowing(names)` |
|---|---|---|---|---|
| **is** | R5′ operator words | R5′ pattern glue + all-glue | R7b | R6b |
| **refused** | a name containing an operator word interiorly, or wholly of them | a name containing pattern glue interiorly, or wholly of it | a name beginning with a refining word (`a`, `an`, `not`) | a name beginning with a pattern's whole word content |
| **existed to** | stop `x is y` as a name re-reading the comparison | stop `a to b` as a name re-reading `send a to b` | stop `a number` colliding with the `is a` type test | stop `print job` as a name re-reading the call |
| **verdict** | **KEEP** (as part of §3) | **DELETE** | **DELETE** | **KEEP** (as part of §3) |
| **because** | the name's own span `x is y` also reads as a comparison — **unrepairable** | the span `a to b` reads only as the name; the ambiguity is elsewhere and a bracket reaches it | same — `a number` is the only reading of itself | the span `send price` also reads as a call — **unrepairable** |
| **what happens now to what it caught** | unchanged: still refused at declaration | `send a to b` becomes an ambiguity error; `send (a to b)` and `send (a) to (b)` each select a reading | `x is a number` becomes an ambiguity error; `x is (a number)` and `x is a (number)` each select one | unchanged: still refused at declaration |

Measured:

```
  rule                                 self-ambiguous   repairable
  Glue(names) -- interior pattern glue          False         True
  Glue(names) -- all glue                       False    (no ambiguity at all)
  Infixes(names) -- operator word                True        False
  Shadowing/R6b -- leading pattern words         True        False
  Refining(names) -- leading article            False    (no ambiguity at all)

  [PASS] unrepairable exactly when the name's own span is ambiguous
```

**Untouched:** the four pattern-side rules, and `Anchors(sound)` (R6), which is
still parked with the leading-holes question.

## 3. The two keepers are one rule

`Infixes(names)` and `Shadowing(names)` are not two rules. They are the two ways
a name's own span can read as something else — spanning an infix operator, and
beginning a pattern's words. So:

> **A name may be declared only if its own token span has no other reading.**

**Budai's read of this is right and is stronger than "a new rule": plain
duplicate declaration is its one-word case.** `var price` twice is refused
because the span `price` already reads — same check, smaller arity. So this is
not a rule added beside "symbol already declared"; it is that rule, stated over
spans instead of over identifiers.

The diagnostic follows the same way, picking its wording from *what* the other
reading is:

```
  another name     «price» is already declared, at line 12
  a pattern call   «send price» already reads as «send «price»»
```

**One correction to how to implement it.** The *exact* form — resolve the name's
own tokens against the current table — is **order-dependent**:

```
  «sum of squares»   without «squares» declared   0 other readings   legal
                     with    «squares» declared   1 other reading    self-ambiguous
```

Legal today, self-ambiguous tomorrow, and the convention would refuse the
declaration arriving second — which here means refusing `var squares` over a
collision its author never saw. Same worst-shape diagnostic R7b's conditionality
ran into.

So implement it **pessimistically**: assume any word run *could* be a name. That
is order-independent, and it measures out as **exactly** `Shadowing(names)` +
`Infixes(names)`:

```
  candidate           pessimistic   R6b or Infixes
  «send price»               True             True
  «sum of squares»           True             True
  «x is y»                   True             True
  «a to b»                  False            False
  «a number»                False            False
  [PASS] identical over the sample
```

**Which means the code does not shrink.** The win is the spec sentence, the
subsumption of duplicate declaration, and a diagnostic that states its own
reason — *the name would be unwriteable, because bracketing cannot select it* —
rather than "begins with a pattern's word content". Worth saying plainly,
because "one rule replaces two" reads like a deletion and is not one.

And it is still the reason the other two go: their names **are** the only reading
of themselves, so everything they caught is reachable by a bracket.

## 3a. Injected names — in scope, and who owns the diagnostic

**The rule applies to written and injected names alike. There is no
`InjectedBy` exemption.** Without that stated, the existing exemption survives
the rewrite and `index of is valid` — an injected span containing an operator
word — stays legal. Measured: it is self-ambiguous under every configuration
tested, and it is exactly the case an exemption would let through.

**Diagnostic ownership falls out of one test rather than being asserted.** An
injected name is a fixed prefix plus a hole filled by a user name, so its
collision is either **universal** over that hole or **particular** to one
filling. Substitute a fresh, otherwise-unused word and re-run the check:

```
  index of (_) exists
      «index of qqq» self-ambiguous: True    -> UNIVERSAL
          «index of bank»          True
          «index of is valid»      True
          «index of bank account»  True

  index of bank (_) exists
      «index of qqq» self-ambiguous: False   -> particular
          «index of bank»          False
          «index of is valid»      True
          «index of bank account»  True
```

**But that is only half the ownership rule**, and the half I left out is the
commonest case: when a name and its shadow **both** offend, removing the
`InjectedBy` exemption doubles the report — two messages, one repair between
them. `SCOPING.md` already settled that half for R5 and it generalises
unchanged. And a **particular** collision has to distinguish what the rival is,
which the first version of this table lost: I collapsed *"by declaration order"*
into *"against the source"* and thereby dropped a case. Four rows:

| source name | injected name | collision | rival | report |
|---|---|---|---|---|
| **fails** | either | — | — | **source only** — the shadow's failure is a consequence, not a second mistake |
| passes | **fails** | particular | a **built-in operator** | **the originating name.** The operator cannot be respelled, so the name is the only actionable party |
| passes | **fails** | particular | a **pattern** | **the later of the two declarations.** Both are actionable — respelling the pattern works as well as renaming the variable — so the standing convention decides |
| passes | **fails** | **universal** | a pattern | **the pattern, once**, deduplicated across every injection site — no filling of the hole avoids it, so no rename helps |

The auditor's case is row 3 and it is real:

```ronin
var accounts => Number;
for each (bank account) in accounts {
    function index of bank (x => Number) { return x; }
    return index of bank account;
}
```

Particular — renaming `bank account` fixes it — but the pattern was declared
**after** the loop variable, and respelling the pattern fixes it too. Blaming
the source there contradicts `SCOPING.md`'s convention and points at the
declaration that was already correct when it was written.

One invariant behind all four, which is what the tests should assert:

> **One mistake, one diagnostic, against the thing the author can change — and
> when both parties can change it, the later declaration.**

Actionability first, declaration order as the tie-break. That subsumes the
convention rather than competing with it, and it explains rows 2 and 4 without
naming them as exceptions: in row 2 only one party is actionable, in row 4 only
one party is, and the tie-break never runs.

*(If neither party is actionable — a prelude injection colliding with a built-in
pattern — that is a defect in the prelude, not a diagnostic for a user. It should
be impossible by construction and worth a build-time assertion rather than a
message.)*

Emitting one per loop instead produces a message about a name nobody wrote, once
per variable in scope — which `GLUE-AS-WHOLE-NAMES.md` §2 called the worst
diagnostic outcome in the language, and which is what the auditor is guarding
against.

**Sequencing: removing the exemption is blocked on `REAUDIT46` findings 2–3.**
Row 1 is that dedup machinery; without it the exemption's removal makes the
diagnostics worse, not better. §3a cannot land alone. That is a change to 46's
status — it now has a dependent — rather than a change to this design.

Safe to leave in the meantime, and worth saying why rather than treating it as
expedient: under the **old** design an exempted injected name was a real hazard,
because minimum lookup would silently pick a reading. Under
**ambiguity-as-error** any span with two readings errors at the use site
whatever is in the table, so the exemption can produce a **confusing message**
but not a **wrong reading**. Diagnostics debt, not soundness debt.

`REAUDIT46` findings 2 and 3 stay live and are not superseded by this design.

## 4. Minimum lookup goes, in the same commit

His Precision 2 stands, with a sharper reason: with §3 in force, every remaining
ambiguity is repairable, so turning ambiguity into an error creates no unwritable
programs. Without §3 it would.

The intermediate state — rules gone, cost filter kept — is strictly worse than
either branch, so `Resolver.cs:655` and the two deletions are one change.

**Deleting the cost filter does not make the resolver enumerate.** The chart
holds *cheapest, and how many derivations achieve it*; it becomes *how many, and
what they are*. Same table, same asymptotics, no parse tree materialised. The
renderings the diagnostic needs are already in `Cell.derivs`.

## 5. The ambiguity diagnostic — the new work

Budai's requirement: the bracketings are **in the error**, and **selectable**
(click or tab). Hover-shows-implicit-brackets is a **separate, unrelated
feature** and is not part of this.

**The payload is data, not prose** — a message string cannot be clicked:

```
  ambiguity {
    span, shown, total
    readings : [ { rendering, insertions : [ {at, text} ], rank } ]
  }
```

`insertions` is the **minimal** set of bracket insertions that makes that
reading unique. Minimal because a suggestion that brackets everything is correct
and useless.

**Ranking — and this is where the cost model goes.** Order by fewest lookups,
most likely first. That is the function being deleted from `Resolver.cs:655`,
moved from deciding to presenting. Put this sentence beside it:

> **Cost may order the suggestions. It may never choose among them.** The moment
> it chooses, every silent capture we are removing comes back, looking like a
> feature.

**Cap the list** at about five, and report the total. A large count is itself
information — the names are fighting the grammar — and the message should say so
rather than list twelve equally bad options.

```
  «send a to b» has two readings.

    1  send (a) to (b)      send «a» to «b»
    2  send (a to b)        send the value of «a to b»

  Pick one.
```

## 6. The property test that must never regress

> **Every reading of an ambiguous statement is selectable by some bracketing.**

Load-bearing now rather than nice-to-have: it is the difference between
"bracket it" being a fix and a dead end. It must be a **property test over
generated statements** — §1b and §1c were both found by generation and neither
would have been found by fixtures.

**Demonstrated, exhaustively** (`injected_and_repair.py` §2). The name set keeps
everything `Glue(names)` used to refuse — interior glue `a to b`, the all-glue
name `to to` — so the property is tested on exactly what the deletion admits;
`send a` and `a is b` are refused by §3 as they should be:

```
  every statement of length 2..6 : 19525 candidates
  ambiguous                      :    24
  with an unreachable reading    :     0
  [PASS]
```

Exhaustive rather than sampled on purpose. A sampled run of the same check found
only **two** ambiguous statements, and two is not a property test — the auditor
is right that the earlier `repair_complete.py` was cited while reporting FAIL,
which is the claim-outlives-its-evidence shape this project keeps finding. That
script stays in the tree as the pre-rule baseline and should be read as the
*motivation* for §3, not as its proof.

If a future pattern shape breaks the property, the symptom is a program nobody
can write, and nothing else in the suite will notice.

## 7. The deleted rules become lint

`Glue(names)` and `Refining(names)` stop being errors. Ship them the same week
as **lint** — same predicates, same generator, warn instead of refuse,
switchable off:

```
  avoid a glue word in the middle of a name
  avoid a name made only of glue words
  avoid starting a name with an article
```

Three reasons over waiting for an idiom to emerge: the idiom exists on day one;
lint can be **measured against real code** once there is real code, which the
rules never could; and if the ambiguity rate is worse in practice than 0.16%,
the lint is already the thing that would become a rule again.

That last one matters for sequencing — **re-adding a name refusal later breaks
programs that used those names.** Same closing window as the glue registry.

## 8. Order of work

1. Replace `Infixes(names)` + `Shadowing(names)` with the §3 self-ambiguity
   check, delete `Glue(names)` and `Refining(names)`, and delete
   `Resolver.cs:655` — **one commit**, so the suite never sits in the
   intermediate state. **The `InjectedBy` exemption stays put at this step.**
2. The registry's name-reservation sections and their goldens.
3. The structured ambiguity diagnostic — ranked, capped, minimal insertions.
4. The repair-completeness property test.
5. **`REAUDIT46` findings 2–3** — the dedup machinery and blame ownership, built
   to §3a's four-row table.
6. **Then** remove the `InjectedBy` exemption, which at that point adds no
   messages because row 1 suppresses them. Not before: doing it earlier doubles
   the report and makes the diagnostics worse.
7. The two deleted rules re-shipped as lint.

Not in scope: R6, the pattern-side rules, hover-shows-implicit-brackets.

Probes: `injected_and_repair.py` (§3a's universal/particular test, and §6's
exhaustive property run), `self_ambiguous.py` (§1c and §3),
`already_declared.py` (§3's duplicate-declaration subsumption and the
pessimistic-form equivalence), `which_rules.py` (§1a and §1b, and the degenerate
arm that §1c corrects), `repair_complete.py` (pre-rule baseline — reports FAIL
by design, read as motivation not proof), `rules_or_brackets.py` (superseded,
kept with its error).
