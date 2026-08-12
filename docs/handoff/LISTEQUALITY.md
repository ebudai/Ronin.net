# Finding 6 — raise it. The missing function is not a cutoff, it is `is`

The finding is correct and the probe is the right one. But **"low / pessimization
rather than a wrong value" is wrong as of this week**, and that changes which of
the three recommended routes are available.

There is also a second half nobody has looked at: lookup equality is not the
same function as list equality, and the obvious implementation gets one of them
wrong.

---

## 1. The severity

`IS-AND-EQUALITY.md` settled two things days ago:

- **`is` on a list is value equality** — one operator, and for anything with
  identity, identity *is* its equality;
- **lists and lookups are values; instances are entities.**

So the comparison the cutoff needs and the comparison the language operator
needs are the same function. With reference equality in place:

```
  under reference equality   «[1,2] is [1,2]»  ->  False
  under structural equality  «[1,2] is [1,2]»  ->  True
```

If the evaluator reaches `Graph.cs:1404-1409`'s comparison — or any comparison
built the same way — then `is` returns a **wrong answer** on lists, which is a
correctness bug, not a pessimization. If it does not reach it, then there are
**two** equality implementations in the runtime and they disagree, which is
worse in a different way and should be found before either is extended.

That is the first thing to check, and it decides the severity.

Reproduced (`list_equality.py` §1), confirming the finding as written:

```
  reference equality (today)    downstream evaluations after 3 ticks: 3
  structural equality           downstream evaluations after 3 ticks: 0
```

## 2. It removes one of the three routes

> *"if full comparison is deliberately too expensive … explicitly exempt them
> from cutoff and remove the false O(n) claim"*

**Not available as a whole answer.** Structural equality has to exist regardless,
because `is` requires it. Once it exists, exempting cutoff is a *performance
choice about when to call a function you already have* — a different and much
smaller decision, and one that should be made on a measurement rather than on
the fear of O(n).

## 3. The half that is missing: lookups are unordered

`MATCH.md` §4 states it as a semantic property — *"a lookup is unordered, so arms
have no fall-through and no first-match-wins"* — and that implies an equality:

```
  elementwise (list rules)   [a=1, b=2] == [b=2, a=1]  ->  False
  as unordered associations                            ->  True
```

Two lookups with the same associations written in a different order **are the
same lookup**. So:

> **List equality is order-sensitive. Lookup equality is not. They are two
> functions.**

Reusing the list comparison for lookups is the obvious move and it is the wrong
one. The symptom is quiet: a lookup-valued cell that never cuts off because
someone wrote the arms in a different order somewhere, plus `is` returning false
for two lookups a reader would call identical. Worth settling in the same change
— the finding says "before joining more collection-valued cells", and lookups
are already one.

Duplicate keys interact here too: `MATCH.md` §6b left "duplicate keys in a
lookup literal are a finding" open. If that lands, lookup equality gets simpler,
because the association set is guaranteed to be a set.

## 4. When comparison pays — arithmetic, not judgement

```
  with cutoff:     compare(n) + (1 - hit) x downstream
  without:         downstream
  so cutoff pays when   compare(n) < hit x downstream
```

`compare(n)` has **early exit**, so it costs n only when the lists are equal —
which is exactly the case where the saving is banked. When they differ it
usually exits at the first element.

```
   list length   hit rate   downstream   cutoff pays?
            10       0.97           50            yes
           100       0.97         5000            yes
          1000       0.97        50000            yes
            10       0.05           50             no
          1000       0.97          500             no
        100000       0.97         1000             no
```

The 0.97 is not invented — `FAILUREMODES.md` §2 measured 97% of recomputes
producing an unchanged value in a mouse-move scenario. At that hit rate,
comparison loses only when the list is long **and** the downstream is small,
which is a narrow band.

**So: structural comparison with early exit, unconditionally, as the default.**

On the digest option — it is the right fix for the band where comparison loses,
and it is cheap *because lists are immutable values*, so the hash is computed
once at construction and never invalidated. But note it buys **nothing for the
probe's own case**: a flat list literal rebuilt every round pays O(n) to hash
exactly as it would to compare. Hashing wins on **nested** structures, where
child hashes are reused, and on long-lived lists compared repeatedly. Build it
when a measurement asks for it.

## 5. The route I would rule out

> *"represent immutable lists with stable identity"*

Interning gives O(1) equality forever and should still be refused, for a reason
outside this finding:

- it needs a **global table**, which in an always-running environment is never
  collected and grows for the life of the session;
- and that table is a **synchronisation point**. `THREADING.md` deliberately
  designed the parallel section to have none — thread-local arrays, a spin
  pool, no shared mutable structure in the hot path. A global intern table cuts
  straight across that, and contention on it is worst exactly where the work is
  most parallel.

The O(1) is real. The price is a shared mutable structure in a design whose
threading story depends on not having one.

## 6. Two process notes

**a. This is the third comment-outlives-its-evidence in one audit round.** The
`O(n)` array-equality comment here, `MaxGroups`' brace-specific explanation in
finding 8, and my own "one parse and one decision" sentence in the spec. Same
shape every time: a claim about a runtime property, written where nothing tests
it. The remedy that would have caught all three is the same one — **a comment
that asserts a runtime property needs a test, or it must be phrased as intent
rather than fact.**

**b. "Pin downstream evaluation count, not merely the returned elements" should
be a standing rule, not this test's fix.** Every reactive defect found in this
project has been invisible in the value and visible in the count — the
accumulation leak, the removal-cache finding, and this one, three for three. A
graph test that asserts only on values is measuring the one thing that has never
been wrong.

## 7. Summary

| | |
|---|---|
| the finding | **correct**, reproduced |
| severity | **raise** — `is` on lists is value equality as of this week, so this is a wrong answer or two disagreeing implementations. Check which |
| "exempt lists from cutoff" | **not available whole** — structural equality must exist for `is` regardless |
| lookup equality | **missing from the finding** — unordered, so a different function; do not reuse the list comparison |
| default | structural comparison with early exit, unconditionally; digest later, on a measurement |
| interning | **refuse** — a global table is a synchronisation point in a design built to have none |
| the O(n) comment | third of three this round; the fix is the general one, not a reword |
| evaluation-count assertions | make it standing policy for graph tests |

Probe: `list_equality.py`.
