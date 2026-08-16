# Lookup equality — no, I raised it and did not specify it. Here it is

> **Ledger** — `[R]` Lookup equality — no, I raised it and did not specify it. Here it is
> supersedes: not yet checked
> superseded by: not yet checked

Straight answer to the question: **`LIST-EQUALITY.md` §3 flagged that lookup
equality is a different function and said "settle it in the same change". That
raised it and left it open.** He is right to push.

Specifying it turns up two things I had not looked at, one of which **revises
what I told him last time.**

---

## 1. Order-insensitive equality plus order-sensitive iteration breaks cutoff

`MATCH.md` §4 makes a lookup unordered, so equality must ignore written order.
But *iteration* has to do something, and if it preserves construction order the
two decisions collide:

```
  «[a=1,b=2]» is «[b=2,a=1]»     -> True        (correct: unordered)
  iteration order of the first   -> [a, b]
  iteration order of the second  -> [b, a]
```

So a lookup-valued `let` that recomputes from `[a, b]` to `[b, a]` is
**unchanged** by equality — cutoff fires, nothing downstream re-runs — while a
downstream `for each` would have produced a different order. **Cutoff has
suppressed an observable change**, which is a wrong answer, not a
pessimization.

That is not a cutoff bug. It is the price of an equality that ignores something
the program can still see. Python lives with exactly this (dicts compare
equal regardless of insertion order, iterate in insertion order) and gets away
with it because Python has no cutoff. We do.

The three ways out:

| | |
|---|---|
| iteration order becomes a function of content, not history | equal lookups iterate identically — cutoff sound |
| equality includes order | contradicts `MATCH.md` §4 |
| lookups exempt from cutoff | gives up the optimisation on exactly the values `match` produces |

## 2. Canonicalise at construction — and it collapses two functions into one

Sort the associations at construction, by a defined total order over key kinds:

```
  «[a=1,b=2]» is «[b=2,a=1]»     -> True
  iteration order of the first   -> [a, b]
  iteration order of the second  -> [a, b]
```

**This revises what I sent last time.** "Lookup equality is a different
function" is true only if you compare as written. Canonicalise at construction
and lookup equality is the **list** comparison applied to a canonical form —
one function, less code, and strictly safer than two.

It also gives, for free:

- a sound cutoff, because equal lookups are indistinguishable downstream;
- a well-defined digest, if a measurement ever asks for one;
- deterministic iteration, which the replayable/round model wants anyway;
- `@` can binary-search the canonical form rather than needing a side table.

**What it costs**, stated plainly: one O(n log n) sort per lookup — paid once,
because lookups are immutable; a total order over key kinds, which needs a case
per runtime type; and the written order is not recoverable. For a map that is
not a loss, and `match` arms were unordered by design.

If the sort ever shows up in a measurement, the fallback is to keep insertion
order *and* exempt lookups from cutoff — but that trades a real optimisation
for a construction-time cost, which is the wrong direction.

## 3. Blocking dependency: duplicate keys must be refused first

`MATCH.md` §6b left *"duplicate keys in a lookup literal are a finding"* open.
**It cannot stay open**, because lookup equality is not well defined until it is
closed:

> with duplicates admitted, is `[a = 1, a = 2]` equal to `[a = 2, a = 1]`?

Either answer is defensible, which is the definition of a coin toss. Refuse
duplicates and a lookup is a genuine map — *same keys, same value at each key* —
and the canonical order is unique.

So this is a **prerequisite**, not a related item. It should land before or with
the equality work, not after.

## 4. `@` and equality must share one key relation

This one is probably live today and is worth checking before anything else in
this document:

```
  «table @ [1,2]» with a structural        key comparer -> 'x'
  «table @ [1,2]» with a reference/default key comparer -> nothing
```

A lookup backed by a hash table with the host's default comparer takes the
second row: a structural key that **`is`** a key in the table is **not found**
by `@`. That is finding 6 one level down, and it applies to any list-valued or
lookup-valued key.

The rule for the spec, and it is one sentence:

> `@` finds the association whose key **`is`** the index.

One relation, used by indexing, by equality, and by the duplicate-key check. If
a hash accelerates it, the hash must be a function of the same structural
content — otherwise the acceleration silently disagrees with the language, and
the disagreement is invisible until someone uses a compound key.

## 5. Summary

| | |
|---|---|
| was it addressed | **no** — flagged in `LIST-EQUALITY.md` §3, not specified. This does it |
| iteration vs equality | order-insensitive equality + insertion-order iteration makes **cutoff unsound** — it hides an observable change |
| the fix | **canonicalise at construction** (sort by a total order on keys) |
| consequence | lookup equality becomes the **list** comparison on a canonical form — one function, revising what I said last time |
| duplicate keys | **blocking prerequisite** — equality is undefined until `MATCH.md` §6b is closed. Refuse them |
| `@` | must use the same key relation as `is`; a default-comparer hash table is a live bug for compound keys — **check this first**, it is cheap and probably real |
| cost | one O(n log n) sort per lookup, paid once; a total order over key kinds; written order not recoverable |

Probe: `lookup_equality.py`.
