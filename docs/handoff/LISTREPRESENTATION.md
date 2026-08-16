# Finding 3 — the invariant is mine, and the `ImmutableArray` question is a language question

> **Ledger** — `[V]` Finding 3 — the invariant is mine, and the `ImmutableArray` question is a language question
> supersedes: not yet checked
> superseded by: not yet checked

Confirming the design edge the programmer asked about, with one answer that
changes the framing: **this is not a runtime-representation choice.** List and
lookup have to be distinguishable types with *different* equalities, so the
sealed types are required by the language, not preferred by the implementation.

Also: normalise on entry, yes — but two of the three obvious ways to implement
"normalise" preserve the defect exactly.

---

## 0. The invariant came from me, and that is the process lesson

`IS-AND-EQUALITY.md` §1 asserted *"lists and lookups are values; instances are
entities"* and built the whole equality table on it. I stated it without asking
what would enforce it. The audit's sentence — *"the immutability invariant is
stated, not enforced"* — is exactly right and it points at my document.

Worth a standing rule, since this is the fourth of the same shape in one audit
round: **a design document that states an invariant must name what enforces
it.** Not "lists are values" but "lists are values, enforced by X." If nothing
can be named, the invariant is a wish and should be labelled one.

## 1. `ImmutableArray<object>` vs a sealed type — decided by the language

The programmer's technical answer is right: `ImmutableArray` closes the
mutation half and the cycle half, with the `default(ImmutableArray<T>)` trap
and the casting caveat. I would add the argument that I think settles it, and
it is not about ergonomics:

**A list and a lookup must be different runtime types.**

- `LIST-EQUALITY.md` §3: list equality is **order-sensitive**, lookup equality
  is **not**. Two functions. With `ImmutableArray<object>` for both they are the
  same CLR type and equality cannot dispatch — you need a tag beside the value,
  and a tag that can be wrong is a third representation bug waiting.
- `[1, 2] is [a = 1]` must be **false by type**, not by comparing contents.
- `x is a list` and `x is a lookup` are language operators as of this week
  (`IS-AND-EQUALITY.md` §9). They need a runtime type to answer from.
  `ImmutableArray<object>` cannot distinguish the two.

So the sealed types are load-bearing for `is`, for type tests, and for cutoff
correctness. That they also give somewhere to hang a digest, and dodge the
`default` trap, is a bonus rather than the reason.

On the `default(ImmutableArray<T>)` trap specifically — it is a second bottom
value in the runtime, with different behaviour from `nothing`, in a language
that deliberately has exactly one. That alone is worth avoiding.

## 2. Normalise on entry — yes, and "normalise" has three meanings, two broken

`list_freeze.py`:

**WRAP — a read-only view over the caller's array.** Satisfies the audit's
"storage cannot be recovered by casting" and fixes nothing, because the caller
never gave up its reference:

```
  normalise mode wrap    host wrote 2   ->  graph reads 2
  normalise mode deep    host wrote 2   ->  graph reads 1
```

This is the dangerous one, because it looks like the fix and it makes things
*worse* than today: the invariant is now asserted while still false.

**SHALLOW — copy the top level only.** Same defect one level down:

```
  normalise mode shallow  host wrote 99 into the inner list  ->  graph reads 99
  normalise mode deep                                        ->  graph reads 1
```

And nested lists are not exotic — they arrive with `match` arms and any grouped
data.

**DEEP — copy recursively, bottom-up.** The only one that holds.

So the answer to the programmer's question: **`Graph.Var("xs", new object[]{…})`
stays legal, and normalisation is a deep copy.** Convenience at the boundary is
preserved; the cost is one O(n) copy at entry, paid by hosts and tests, which is
the right place to pay it.

## 3. Where the cycle guard goes — the boundary, not `Same`

Deep normalisation needs its own cycle detection, and that is a feature rather
than an extra cost, because it is the **only place a cycle can be reported
usefully**:

```
  deep normalise:                       REFUSED -- a list contains itself
  Same on two self-containing lists:    RecursionError
                                        (= StackOverflowException, unrecoverable)
```

Both stop the crash. Only one can say what went wrong: at the boundary the value
has a name and a host call site, so the message is *«xs» contains itself* and it
points at the caller. Inside `Same` the two values are anonymous and the only
honest message is "too deep".

**And keep a cheap depth cap in `Same` anyway.** "`Same` can never see a cycle"
is precisely the class of invariant this round keeps finding unenforced. The cap
is one integer and it turns an unrecoverable process death into a `Fault`.

## 4. What `Read` hands back — the compatible-looking option is the bug again

The programmer flagged this as the part he wanted said out loud. Three choices:

| | | |
|---|---|---|
| **a** | the immutable type | honest; host code changes once |
| **b** | a defensive `object[]` copy | host code unchanged — **and a host that mutates what it got back sees nothing happen.** That is the same silent-mutation defect moved to the read side, plus an O(n) allocation per read |
| **c** | a read-only interface | host sees `IReadOnlyList<object>`; mutation does not compile or throws loudly |

**(b) is the trap**, and it is the option "keep the boundary convenient" pulls
toward. Between (a) and (c) either is defensible. I would take **(c)**: it keeps
the host boundary as convenient as it can honestly be, and it refuses mutation
out loud rather than absorbing it.

Note the asymmetry is correct and worth stating in the API docs: **entry accepts
a mutable array and copies it; exit hands back something that cannot be
mutated.** A host reading its own array back and finding it unchanged is
surprising once; a host mutating a returned array and having it silently ignored
is surprising forever.

## 5. Bulk access without leaking the array

The audit asks that indexing and destructuring consume a read-only interface.
One addition, because the invariant will otherwise be one `internal` accessor
away from being lost the first time something needs to be fast:

For bulk paths — a vectorised sum, a copy into a column — expose
`ReadOnlySpan<object>` rather than the backing array. A span **cannot be stored
in a field**, so the compiler enforces non-retention. That gives the fast path
without an accessor that hands out storage, and it is checked rather than
promised.

## 6. Empty is a singleton — free, and not the interning I refused

`[]` is the commonest list in any program. One cached static instance gives it
O(1) equality:

```
  EMPTY is EMPTY   True
```

`LIST-EQUALITY.md` §5 refused interning because a global table is a
synchronisation point in a threading design built to have none. A single static
is not a table: no lookup, no growth, no contention. The distinction is worth
keeping straight so the singleton does not get refused by association.

## 7. Summary

| | |
|---|---|
| the finding | **upheld**, and the invariant it names is mine |
| `ImmutableArray` vs sealed type | **sealed** — required by the language: list and lookup need different types and different equalities, and `x is a list` needs one to answer from |
| `Graph.Var(…, object[])` stays legal | **yes** — normalise on entry |
| what "normalise" means | **deep copy**. Wrap is not freeze; shallow is the same bug one level down |
| cycle guard | in the **normaliser**, where the value has a name — plus a cheap depth cap in `Same`, because the invariant should not be the only thing standing between us and a process death |
| what `Read` returns | a read-only interface. **Not** a defensive `object[]` copy — that is the same defect on the read side |
| bulk access | `ReadOnlySpan<object>`, which cannot be retained, rather than an internal accessor |
| empty list | a singleton static — free O(1), and not the interning that was refused |
| process | a design document that states an invariant must name what enforces it |

Probe: `list_freeze.py` — wrap vs shallow vs deep, the two cycle failure modes
side by side, and the singleton.
