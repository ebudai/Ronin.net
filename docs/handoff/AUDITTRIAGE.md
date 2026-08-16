# Audit triage

> **Ledger** — `[R]` Audit triage
> supersedes: none
> superseded by: none

Sorted into: **wrong** (the audit is mistaken), **ours** (needs a design
decision), and **theirs** (a code defect, no design input needed). Only the
first two sections need reading before work starts.

The audit is good. Most of it is real, several findings are ones I introduced,
and the section on why high coverage missed it all is the most valuable page in
it.

---

## WRONG — one finding, and the rule it questions is provably correct

### Finding 1: "the rule's definition and representation disagree"

They don't. The **example in `SCOPING.md` was wrong** — mine, corrected
separately — and the audit reasonably inferred a deeper mismatch from it.

`Pattern.Glue` excluding pre-hole literals is not an implementation shortcut,
it is the rule. R5 exists to stop a name **silently re-resolving** an existing
statement. Verified:

```
scope                                          statement              result
patterns send (_) / send (_) to (_)            send hello to alice    OK, 2 lookups
+ name «hello to alice»                                               «hello to alice» WINS
                                                                      silently, 2 beats 3

patterns compute (_) / compute total for (_)   compute total for      TIE -> ERROR
+ name «total for order»                       order                  caught loudly by R3
```

Absorbing **glue** wins silently at 2 vs 3 — that is the hazard, and R5 is the
only thing that catches it.

Absorbing an **anchor** word cannot do that. A name spanning an anchor word
stops the pattern matching at all, so producing any reading requires a *rival*
pattern — and the rival costs the same, so it ties, and R3 rejects it with both
readings named. Glue-only is therefore **exactly right**, not an
under-approximation.

Fix the test to use `send (_) to (_)`, which the programmer has already done.
Nothing in `Resolver.cs` needs to change.

---

## OURS — design decisions, in the order they block work

### 1. Overload resolution: count shapes, not declarations

The audit is right that this produces false ambiguity, but the fix is not only
"build the type filter". There is a conflation to remove first:

> **Two declarations sharing a pattern shape are ONE syntactic derivation.**

The derivation count exists to answer *"is this text parseable more than one
way?"*. Overloads are one way to parse and several things it could mean.
Inserting them as separate derivations makes R3's tie machinery fire on a
question it was never asked.

So: `Pattern → List<Declaration>`, one derivation regardless of list length,
and overload selection is a **later phase** operating on the single winning
reading. The phase order stands — enumerate, type-filter, rank by lookup, tie
is an error — with overload choice happening after all four, not inside them.

Until the type filter exists, a multi-declaration pattern should report
*"«area of (_)» has 3 declarations and type-directed selection is not
implemented"*, which is a missing feature, not an ambiguity.

### 2. A shadow-only step can miss triggers — my bug

Real, and mine. `reactive_events.py` has:

```python
while self.pending and rounds < self.cascade_limit:
```

Shadows advance at the start of a step, so a cell whose `old` value changed
dirties its dependents **with no pending write**. A `when` triggered off
`old x` can therefore be dirtied and never fire.

The loop condition is wrong. It should be: run at least one round, then
continue while *either* writes are pending **or** a trigger is dirty.

```python
first = True
while (first or self.pending or self.any_trigger_dirty()) and rounds < limit:
```

This also fixes a subtler case — a step with no writes at all still needs its
settle-and-fire phases, because shadow advancement is itself a change.

### 3. Multiple `when` bodies writing one cell

Not undefined — **already illegal**, by a rule we made and never connected to
`when`. Ownership says a var has exactly one writer. A `when` body's writes
count. Two whens writing one cell is a declaration-time error naming both,
exactly like two functions doing it.

The audit found the gap correctly; the rule to close it already exists.

### 4. Error propagation belongs in the graph, not in each operator

The doc promised a node with an error dependency becomes an error *without
running its body*. `lift()` gave that to builtins only, so casts, delegates,
extensions and trigger bodies escape it — which is what the audit found.

Move the guarantee down: **a read that returns an error short-circuits the
whole recompute.** The node adopts the error and the body never runs. That
makes the promise structural instead of per-operator.

`otherwise` is the sole exception and needs a non-short-circuiting read —
which it already is, by design, as "the only thing that catches".

### 5. Resolver cost: the answer is per-statement, and it is urgent

The audit undersells this. Measured (Python, so absolute numbers are inflated
perhaps 50×, but the shape is real):

```
  tokens        ms   typical of
       4      0.49   a short statement
      14      4.91   a long statement
      39     57.73   a very long statement
      99     901.43  a small FILE
     299   18344.41  a medium file
```

That is roughly n^2.5, not n². At file scale it is not slow, it is unusable.

**The resolver must never see a file.** The CFG skeleton layer already yields
statement spans — that is half of why the two-layer split exists — and a
statement is 5–30 tokens, where the cost is flat. Feeding it a file is a
category error, not a tuning problem.

Two cheap wins alongside:

- `MaxBindingPower + 2` allocates 32 levels per span. Only distinct operator
  binding powers plus one are reachable: six. A ~5× memory cut for a constant.
- Cells eagerly construct their collections. Most spans never receive an
  offer; allocate on first offer.

With per-statement scoping, the editor cancellation concern mostly evaporates
too — but keep cancellation anyway, because a pasted file still has to be
*split* into statements before anything is safe.

### 6. Ambiguity counts overflow — also my bug

`l.count * r.count` is unbounded and can wrap. The audit's fix is right and I
should have written it that way: **saturate at 2.** The only question ever
asked is unique-versus-ambiguous, so `min(2, a * b)` is both correct and
overflow-proof.

### 7. Duplicate declarations overwriting graph nodes

Already decided, not yet enforced: **shadowing is a declaration error**, and a
name declared twice in one scope is the same case. `Graph.Declare` replacing
silently is the graph layer not knowing about a rule made at the resolver
layer. Reject, naming both sites.

### 8. Trailing separators — the spec contradicts itself

The audit says the grammar forbids a trailing separator. The language guide's
own example has one:

```
options =
(
    turbo = true,
    greeting = "♪boo bee boo♫",
    heated seats = false,
)
```

So this is a decision, not a defect. **Allow trailing separators** — the guide
already does, they produce cleaner diffs, and they make generated code easier.
Forbid **omitted** separators, which is a genuine bug in the same finding:
`(a b)` must not parse.

### 9. Numeric semantics

Already designed, not yet built — exact by default, 64-bit, `fast number` as
opt-in, boundary at roots and transcendentals. Two things follow immediately
and cheaply, before any of that lands:

- **Division by zero is an error value, not infinity or NaN.** That is the
  error model, and it is a one-line change.
- Dates lexing but not evaluating should say so rather than fall through.

---

## THEIRS — code defects, no design input required

Everything else, and all of it looks right:

**Release-blocking:** the executable never awaiting its work and parsing every
filesystem entry; empty input returning null instead of a sentinel; keyword at
EOF indexing past the end; `Comment.Lex` treating an absolute index as a
relative length; aggregates accepting truncation and missing separators;
`Module.Parse` discarding trailing input; `while` bypassing `Scope.Parse`;
dangling `=>`/`=`/return types; runtime arity binding with `Zip`.

**Equality and mutability:** `Aggregate.GetHashCode` over list identity;
`Name.GetHashCode` over token objects rather than text; caller-owned mutable
collections retained as dictionary keys.

`Token.Append` incrementing `RunningIndex` by one rather than by the preceding
segment's length is the **same bug family** as the `AdvanceTo` sizing defect
already fixed — worth checking whether anything else derives a length from a
running-index delta.

**Build and dependencies:** enable nullable; bump the 2023-era test packages
carrying the two advisories; pin the SDK; add CI and a formatter gate;
`StringComparison.Ordinal` on prefix matching. Re-enable tiered compilation
unless there is a benchmark reason — it helps startup, which is the compiler's
whole life.

---

## The coverage section is the most important page

> *Tests commonly hand-construct token chains instead of passing source through
> the lexer.*

That is exactly the failure mode behind the `AdvanceTo` bug, the R5 test, and
the `Repeating` gap where "direct unit tests pass because they invoke
`Repeating.Parse` themselves". Three separate defects, one cause.

The remedy is a rule rather than more tests: **a test may hand-build tokens
only when the thing under test is token construction.** Everything above the
lexer takes source text and asserts on the result, including that the whole
input was consumed. That last clause alone would have caught findings 6, 7, 8
and 9.

I'd put that ahead of item 8 in their remediation order — it is what stops the
next batch, and it is cheap.
