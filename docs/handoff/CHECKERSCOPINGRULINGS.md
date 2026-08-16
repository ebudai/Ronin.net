# The semantic checker — Q1–Q7, and why four of them were already answered

> **Ledger** — `[V]` Answers `SEMANTICCHECKERSCOPING` Q1–Q7 and governs the
> semantic-checker build. Confirms `REAUDIT47RULING` §5 (Q2), `FIVERULINGS` §4 (Q4),
> `REFERENCESTRUCTURE` §5 (Q5), `FIVERULINGS` §5 (Q6), `FIVERULINGS` §3 (Q7).
> supersedes: EAGGREGATES2 §10, GENERICSII §8a, NOTHING-ANALYSIS §D
> superseded by: not yet checked

Your §2 reconstruction is accurate and I am confirming it rather than correcting
it. §1 is right and important: **this is a monomorphising inference engine, not a
value-against-annotation check**, and the seam between "foundation" and "generics"
is not clean because they share the inference variable. Staging it anyway is
correct.

**Q1 confirm with three additions. Q2 closed. Q3 confirm, with a correction to the
reasoning that matters more than the answer. Q4, Q5, Q6 and Q7 are already ruled**
— and that they did not reach you is the most useful thing in this document. §8.

Nothing here is measured; there is nothing measurable in it.

---

## Q1 — the type term: confirmed, plus two additions and one prohibition

The shape is right, and both of the choices you singled out are right for reasons
worth recording:

**`Error` as a case, not a per-type flag** — because a flag *is* the union we
refused. `T`-with-an-error-bit is `T | Error` wearing a different hat, and
`ERRORASVALUE` §1 measured why that partitions nothing. A bottom is a case.

**`Action` as a case, not a null return** — because `FIVERULINGS` §2b needs *"the
action type differs from every value type"* to be a **comparison**. As a null it
becomes a null-check scattered across every site that asks, and one of them will
forget.

**`Named` opaque this pass** — yes, see Q3.

### Addition 1: the literal `nothing` has no type in this term

`nothing` is a value (`NOTHINGANDINDEXING` §1.1) that inhabits `optional T`.
Under §3 as written there is nothing to give the expression `nothing`, and step 2
needs it the first time anyone writes `var x => optional number = nothing`.

> **`nothing : Optional(Variable(fresh))`** — the empty optional at an unknown
> inner type, resolved by unification exactly as `[] : List(Variable(fresh))` is.

No new case, no second asymmetry. It is the same under-determined-literal shape as
`[]` and `return empty list`, which is a good sign it is the right answer.

### Addition 2: `Variable` needs room for a requirement set

You scoped **inferred** constraints in and declared ones out, which is right. But
`GENERICSII`'s *"the inferred requirement-set is the interface, checked at the
call boundary before entering the body"* means a `Variable` accumulates
requirements as the body uses it. `Variable(id)` bare has nowhere to put them.

Not asking you to build the constraint machinery now — asking you to shape the
case so it can grow one without a rewrite of every construction site.

### The prohibition: `fast` must not appear in `Type`

This is the one that would quietly undo a ruling. `TYPEHALFRULINGS` §1 chose the
modifier spelling *precisely so the checker never sees two number types*. If
`fast` reaches the type term — as a case, a flag, a variant of `Scalar`, anything
— you have the seventh type back by the side door, and every unification site has
to decide whether `fast number` unifies with `number`.

> **`fast` is an attribute of the annotated occurrence, stored beside the type,
> never inside it.** The checker's `Type` has exactly one number.

Step 6's target check reads the occurrence's type *and* its modifier set as two
things. That is what makes `fast truth` reportable without `fast` ever being one.

## Q2 — closed

`REAUDIT47RULING` §5 settles the miss (`nothing`, `m @ k : optional V`, optionals
nest, list index out of range stays `Error`), and `ERRORASVALUE` settles the
larger fork behind it — the easy way, a named one-directional bottom, Errors equal
by kind and reason. `EAGGREGATES2` §10 predates both. **Your reading is right.**

But do not leave §10 as *"read as superseded."* **Strike it**, and put the
supersession in the file. A live sentence saying *"the one thing I still want
before the type checker"* in a document the checker is built from will cost
someone else the same half hour it cost you. See §8.

## Q3 — confirmed, and the tension you found is not one

Build opaque `Named`, unifying only with itself. But the reasoning wants
correcting, because getting it wrong shapes the code:

> **A strong alias *is* an opaque `Named`.** It does not pull apart from it.

`UNITSRESEARCH` §6 defines a strong alias as *"same representation, different
type, no implicit conversion either way."* A type that does not implicitly convert
to its base **does not unify with its base** — that is the whole point of it. So
`money` never unifies with `number`, and `Named` unifying only with itself is not
an approximation of aliasing; it **is** aliasing.

What is deferred is narrower than you have it: the **declaration syntax** for
naming a base (`type money = number`), and **representation sharing**, which is a
storage matter and not a unification one.

Why the distinction matters: if you carry "aliases will later unify with their
base" as a pending idea, you will leave room for a subtype relation that must
never exist. There is nothing to leave room for. Defer the syntax; the semantics
are already what you are building.

## Q4 — one table. `GENERICSII` §8a is withdrawn, not open

Not a live option. I wrote §8a and then withdrew it explicitly in `FIVERULINGS`
§4, for three reasons — `type of x` puts a type in a value position; every name
rule would otherwise run twice and fail silently; and the prize was measured at
0.0072%.

And the strongest evidence arrived after that ruling, in work you have already
built on: `LOOKUPARROWRULED` §1 — `m => lookup text => number` resolves **uniquely**
because the kind filter admits only the reading where ascription's left operand is
a *name* and the arrow's operands are *types*. **With two tables the commonest
annotation in the language would not resolve.**

`GENERICSII` §8a should be struck the same way §10 should.

## Q5 — already answered, and the answer is: it is runtime, and that is fine

This was closed two handoffs ago and did not travel. From your predecessor:
*"`Scope.Invoke` is RUNTIME — the answer that did not reach you."* And I ruled on
it in `REFERENCESTRUCTURE` §5:

- **the narrowing belongs in the compile-time filter** — so overload and call
  ambiguity is compile-time, reported with repairs, which is your recommendation;
- **the runtime check stays as an invariant assertion**, because if it ever fires
  the compile-time filter has a bug and that is the cheapest place to detect it;
- **its message is addressed to the compiler's authors**, not to a user, because
  no user can reach it. *"A check that cannot fire from source is an assertion,
  and an assertion's message is addressed to the compiler's authors."*

So: build the narrowing compile-time. Leave the runtime check. Nothing is open.

## Q6 — constructor, definitively — and something needs verifying

`FIVERULINGS` §5 ruled `optional (_)` a pattern, and your predecessor reported it
**built**: *"`optional (_)` is a pattern; the modifier keyword, its lexer class and
its token factory are gone."*

So if `NOTHINGANALYSIS` §D says `optional` is parsed and stored as a modifier, one
of two things is true, and **which one is a question about the tree that only you
can answer**: either that document is stale, or something reintroduced it.

- Stale → strike the section.
- Reintroduced → it is dead storage and removing it is the fix, exactly as you say.

The ruling either way: **the constructor reading is the only one. The checker
reads nothing else.**

## Q7 — it exists; what is deferred is building it

Not "is there an expression-level ascription" — `FIVERULINGS` §3 ruled one **in**:
`(x => Text)`, a check and never a coercion, binding loosest, costing nothing
because `=>` is a symbol and symbols cannot be captured by names. It is the
repair that made same-shape overloading admissible in the first place.

So: **defer the build, with the overload expiry, and let the ledger row carry
both** — which is your recommendation, with the correction that the design
question is closed rather than open. Worth having in the ledger row itself, or the
next reader asks Q7 again.

## 8. Four of seven — and the remedy

Q4, Q5, Q6 and Q7 were all ruled, and none of the rulings reached you. You read
everything and cited carefully; this is not a reading failure. It is that **the
superseded sentences are still live in the documents you read.**

That is the same defect as the stale megabyte figure and the `[a = 1]` premise,
one level up: *a fact with no consumer cannot be kept true* — and a design memo's
paragraph has no consumer at all.

Two things worth doing, and the second is the one that lasts:

**A supersession pass.** Strike `EAGGREGATES2` §10, `GENERICSII` §8a, and
`NOTHINGANALYSIS` §D's modifier claim (subject to Q6's verification); add the
`FIVERULINGS` §3 pointer to `OVERLOADS` §4. Four edits.

**Put your `[V]`/`[R]` marking in the documents.** You reconstructed the
verdict-versus-recommendation distinction from voice, which is careful reading and
should not have been necessary. A one-line header on each memo — *what this
decides, what supersedes it, what it supersedes* — is the consumer those facts
currently lack. It is the descriptor slice's principle applied to prose, and the
next successor should inherit it rather than re-derive it.

## 9. §5's scope, §6's order, and the two flagged consequences

**Scope and order: take them as written.** The gating-commit-with-negative-tests
shape is right, and *"an empty finding collection is the failure condition"* is the
sentence that makes it real — it is the direct answer to the check that reports
PASS over zero cases.

**Both flagged consequences are real.** The storage one is recorded
(`ERRORASVALUE` §5) and is a runtime-representation decision to take deliberately
before a profiler forces it. **Mid-session monomorphisation is genuinely
undesigned** — a call at a new argument type instantiating during a run is a
requirement of the always-running premise that no document has addressed, and it
interacts with the `(function, instantiation)` cache you are about to build. Flag
it in the ledger now; it is the next design item after this pass, and I would
rather write it before the cache hardens than after.

## 10. Summary

| | |
|---|---|
| §1 — inference engine, no clean seam | **right**, and staging it anyway is correct |
| §2 reconstruction | **accurate**; build to it |
| **Q1** | **confirmed.** `Error` as a case because a flag *is* the union we refused; `Action` as a case because §2b needs a comparison |
| Q1 addition | **`nothing : Optional(Variable(fresh))`** — the term has no type for it today |
| Q1 addition | **`Variable` needs room for a requirement set** — inferred constraints are in scope |
| Q1 **prohibition** | **`fast` never enters `Type`.** It is an attribute of the occurrence. Otherwise the seventh number type returns by the side door |
| **Q2** | **closed.** And **strike §10** rather than reading it as superseded |
| **Q3** | **confirmed** — but a strong alias **is** opaque `Named`, not an approximation of one. Only the declaration syntax and representation sharing are deferred. Leave no room for subtyping |
| **Q4** | **already ruled — one table.** §8a was withdrawn in `FIVERULINGS` §4. With two tables, `m => lookup text => number` would not resolve |
| **Q5** | **already answered** — `Scope.Invoke` is runtime; narrowing goes compile-time; the runtime check stays as an assertion addressed to maintainers |
| **Q6** | **constructor.** Verify whether the modifier storage still exists — stale doc or regression, and only you can tell |
| **Q7** | **exists** (`FIVERULINGS` §3). Defer the build with the overload expiry; put "ruled, unbuilt" in the ledger row |
| **the finding** | four of seven were ruled and did not travel. **Supersession pass**, and put the `[V]`/`[R]` marking *in* the documents |
| §5/§6 | take as written |
| mid-session monomorphisation | **genuinely undesigned.** Ledger it now; write it before the `(function, instantiation)` cache hardens |
