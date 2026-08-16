# Named identity and signature sorts — Q1a is the one to change

> **Ledger** — `[V]` Answers `IDENTITYANDSIGNATURES.md` (`REAUDIT54` findings 1 and
> 3). Named-type identity is **(declaring scope, name)** as a value, not the span
> (unique but not stable under edits); Q1b/Q1c/Q2 confirmed. Signature-sort binding
> stays in **step 1**.
> answers: IDENTITYANDSIGNATURES
> supersedes: not yet checked
> superseded by: not yet checked

**Q1a: not the span.** **Q1b, Q1c, Q2: yes, and Q2 for a stronger reason than
you gave.**

The finding is right and Q3's semantics are exactly as you read them: an opaque
named type unifies only with the same *declaration*. The question is what
"declaration" is named by, and a span is the one candidate the always-running
premise disqualifies.

Nothing here is measured — there is nothing measurable in it without your tree.

---

## Q1a — a span is a position, and positions move

The span is unique, and that is the only property you tested. The one it lacks is
**stability**, and this language cares about that more than most:

> Debug **is** development. The editor is always running, and source is edited
> continuously between checks.

Add a line above `type token;` and its offset changes. So its *identity* changes,
and the type it names becomes a different type — while nothing about it was
edited. Concretely:

- **the `(function, instantiation)` cache misses on every keystroke** that shifts
  a declaration, because the instantiation key contains a moved identity. That
  cache exists precisely to make an always-running checker affordable, and this
  would be the thing that makes it not;
- **incremental re-checking cannot tell that a type survived an edit**, which is
  the question it exists to answer.

And it undercuts your own escape clause. *"Two separate compilations of the same
source coincide, but nothing ever compares types across compilations"* — in a
session that re-checks after each edit, **comparing before-edit to after-edit is
exactly comparing across compilations.** It is not an exotic case; it is the
normal one.

### What to use instead

> **Identity = the declaring scope, plus the name.**

- **Unique by construction**, and by a rule you already have: two `type token;` in
  *one* scope is a duplicate declaration and is already refused. So (scope, name)
  cannot collide — the uniqueness is inherited from a rule rather than asserted.
- **It distinguishes your witness**: `function left`'s scope and `function
  right`'s scope are different scopes.
- **Stable under any edit that does not move the declaration between scopes** —
  which is the property the span lacks.
- **No new field**, same as the span: the declaring scope is known where
  `Declared` is created.

Two details worth building in from the start:

**It must be the *declaring* scope, not the containing one.** You note that
inherited declarations merge into a scope carrying their spans, so origin is
already tracked — carry the origin *scope* the same way, or an inherited type
would take on the identity of whoever inherited it.

**Not the `Declared` object's reference identity**, tempting as that is. A
re-parse builds new records, so reference identity breaks across exactly the edit
boundary this is meant to survive. (Scope, name) is a *value*, and that is what
makes it hold.

**The span stays** — for diagnostics, which is what it is good at.

## Q1b — defer the import reach: yes

Agreed, and (scope, name) makes the deferral cleaner than the span did. Two
same-named types in two files are already distinct because their declaring scopes
differ; what modules add is **which of them a third file sees**, which is a
visibility question and genuinely theirs. Nothing about doing identity now makes
that harder afterwards.

## Q1c — the lighter path, and it is not a compromise

Take it, and record *why*, because "lower blast radius" undersells it:

> **`Node.Name` is derivation identity — *are these two readings the same
> reading?* Semantic identity is a different question — *are these two types the
> same type?***

Conflating them is not just a wide change, it is a wrong one: the repair and
ambiguity machinery wants readings compared by shape, and a type wants comparing
by declaration. `Sort.Of` reading the declaring scope for a named type keeps the
two questions apart, which is the correct separation and happens also to be
cheap.

So promote onto `Node.Name` **only if a genuinely shared need appears** — not by
default when value-name identity arrives, because value-name identity may well
want the same separation.

## Q2 — step 1, and the reason is stronger than "small and observable"

Yes, step 1, now. But the argument is not that it is a present misclassification;
it is **what the misclassification does to the ledger**.

```
  function use (x => number)      }  same shape, same parameter sorts
  function use (x => (number))    }  -> a true DuplicateSignature, never expires
                                     -> reported as Overloaded, which EXPIRES
```

The expiry ledger is the mechanism that makes every approximation in this project
reversible. A declaration filed in the **expiring** bucket that belongs in the
**permanent** one means that when the overload expiry fires, **a genuine duplicate
silently becomes legal** — and two identical signatures can never be told apart at
a use site, so it lands as an unresolvable ambiguity or a silent pick.

That is not a cosmetic misfiling. It is **an approximation that expires into a
bug**, in the mechanism whose whole job is to stop that happening. Fix it where
the storage change lives, which is step 1.

And your framing of the fix is the right one and worth keeping in the commit:
*"the fix aligns the classifier with the ledger rather than fighting it."* Same
parameter sorts → permanent; different → expiring. The classifier and
`Test/Expiry.cs` then say one thing in one vocabulary.

## Summary

| | |
|---|---|
| **Q1a** | **not the span.** It is unique but not **stable** — an edit above a declaration changes it, and debug-is-development makes that the normal case, not an exotic one |
| the cost | the `(function, instantiation)` cache misses on every keystroke that shifts a declaration; incremental re-checking cannot tell a type survived an edit |
| instead | **(declaring scope, name)** — unique *by the duplicate rule you already have*, stable under edits, no new field |
| two details | carry the **declaring** scope through inheritance, not the containing one; and **not** the `Declared` object's reference identity, which a re-parse breaks |
| the span | keep it — for diagnostics |
| **Q1b** | **defer** — and (scope, name) already separates two files; modules add only visibility |
| **Q1c** | **lighter path**, and not as a compromise: `Node.Name` is *derivation* identity, a different question from *type* identity. Promote only on a shared need |
| **Q2** | **step 1**, and because the misfiling puts a permanent refusal in the expiring bucket — **an approximation that expires into a bug**, in the mechanism that exists to prevent exactly that |
