# Finding 8 — endorsed, and the false spec sentence is mine

> **Ledger** — `[V]` Finding 8 — endorsed, and the false spec sentence is mine
> supersedes: not yet checked
> superseded by: not yet checked

The recommendation is right and I would build it as written, with four
additions. But the severity is understated in one place, and the provenance of
the false claim needs stating because it is the failure class this project keeps
finding.

---

## 1. "One parse and one decision" is my sentence

`grammatical-structure.md:499-507` did not invent it. It is `BRACE-DECISION.md`
§3:

> `[` then opens a list or a lookup, and those are separated by whether the first
> element is an assignment — a discriminator *inside* the first element, so
> **still one parse and one decision**, with only `[ ]` needing a stated default.

That was a **design conclusion about what the grammar makes possible**, and it
went into the spec as an **authoritative statement about what the compiler
does**. The two are not the same sentence and nothing marked the transition.
Same shape as the left-recursion comment and `@`: a proposal became a fact
because a later document quoted it without the hedge.

So the doc fix is not "correct a stale line", it is: the spec should say what
the design *requires*, and the regression test should be what makes it true.

## 2. `[ ]` is worse than "needs a stated default" — today it is a silent pick

This is the part I would raise the severity on. Under ordered alternatives the
empty case is not undecided; it is **decided by the order of two `Parse`
calls**:

```
  [ ]        ORDERED  lookup
             SINGLE   list
```

Both alternatives succeed on empty input, so `Temporary.Parse` returns whichever
it tried first. That is the language's cardinal rule broken in the code — *ties
are compile errors, never a silent pick* — and it is broken not by a subtle
interaction but by an ordering.

`BRACE-DECISION.md` §3 already decided the answer (**empty is a list**; an empty
lookup takes a type annotation or a marker). So "pin the empty-list decision at
the same layer" is not a new decision to make — it is an existing one to
implement, and until it is, `[ ]` means whatever the try-order says.

**Caveat, since I cannot read the code:** if `Lookup.Parse` requires at least one
association, `[ ]` falls through to a list by a guard rather than by intent —
still a decision living in parser structure rather than in the spec, and still
worth moving. Worth checking which of the two it is.

## 3. Why it doubles — and the invariant the fix depends on

The exponential is not "two alternatives are tried". It is that **an
association's key is itself a value**, so *is this an association?* cannot be
answered without parsing the key — and a nested `[` inside the key re-enters
`Temporary` and tries both again.

`aggregate_parse.py` reproduces exactly that structure:

```
   depth      ORDERED     SINGLE      ratio
       1            2          1        2.0
       5           62          5       12.4
      10         2046         10      204.6
      14        32766         14     2340.4
```

`2^(d+1) − 2` against `d`. That is the reported curve, and at depth 10 it is the
~2000 element attempts that become ~600 ms.

Which gives the reason the recommended fix works, and it is worth writing down
as a **constraint rather than an observation**:

> `=` inside `[ ]` is only ever an association separator. It is never an
> expression operator.

That is what makes "parse the element once, then look for `=`" a decision rather
than a guess. If `=` ever becomes an expression-level operator, the fix silently
stops working and the exponential comes back through a door nobody is watching.
It should sit in the spec beside the collection production, not in a comment.

## 4. "Decide kind from the first element" needs one more word: *symmetrically*

Read literally, deciding from the first element and bailing at the first
mismatch reproduces an asymmetric diagnostic — the same mistake reported two
different ways depending on which order the programmer typed it:

```
  [ a = 1 , 2 ]   ORDERED  parse failure (no reason available)
                  SINGLE   mixed aggregate: element 1 is an association,
                                            element 2 is a value
  [ 2 , a = 1 ]   ORDERED  parse failure (no reason available)
                  SINGLE   mixed aggregate: element 1 is a value,
                                            element 2 is an association
```

So: parse every element, **then** compare kinds. One message for one mistake, in
both orders, naming both positions. It costs nothing — the elements are already
parsed — and it is the whole reason the mixed case becomes diagnosable at all.
Under ordered alternatives it is not merely a worse message, it is
structurally unavailable: each alternative fails for its own reason and the
caller only sees the last one.

## 5. On the regression test

Agreed on a work count rather than a timer, and two refinements:

**Assert a ratio, not an absolute.** An absolute count bakes in today's
implementation and will be edited to fit the first time it drifts.

```
  work(depth 20) / work(depth 10)   <  3       linear
                                    ~ 1000     exponential
```

Two depths, one comparison, machine-independent, and it fails loudly on
reintroduction rather than on a refactor.

**Name the test after the spec section.** The recurring failure here is a claim
outliving its evidence. If the test is named for
`grammatical-structure.md` §"one parse and one decision", then the sentence and
the thing that makes it true are findable from each other, and the next person
to weaken the parser finds out what they are contradicting.

## 6. On keeping `MaxGroups`

Agreed, and I would go slightly further on the comment. Once the collection
production is single-parse, `MaxGroups` no longer has a *known* thing it is
protecting against — which is precisely the argument for keeping it and the
argument for rewriting the comment honestly:

> A defensive ceiling on total group attempts. No current production is known
> to be super-linear; this exists because that has been false twice — first
> for brace nests, then for square nests — and the failure mode is a hang
> rather than a wrong answer.

A ceiling whose comment names a fixed bug reads as dead code and gets deleted.
One that names its own history does not.

## 7. Summary

| | |
|---|---|
| the finding | **endorsed** |
| the false spec sentence | **mine** — a design conclusion quoted as an implementation property. The doc fix is to split those two claims, not to reword one |
| `[ ]` | **raise it** — not "needs a default"; today it is decided by try-order, which is a silent pick. The design decision (empty = list) already exists and needs implementing |
| why it doubles | an association's key is a value, so the kind test needs a parse. Add the enabling invariant — `=` inside `[ ]` is only ever an association separator — to the spec |
| "decide from the first element" | add **symmetrically**: compare kinds after parsing all elements, or the mixed diagnostic stays order-dependent |
| the regression | ratio across two depths, not an absolute; name the test after the spec section |
| `MaxGroups` | keep, and let the comment name its history rather than one bug |

Probe: `aggregate_parse.py` — both parsers over the same toy grammar, the work
curve, and the mixed-aggregate messages.
