# Overloads: alternatives, not a second pass — and mostly not needed for the library

> **Ledger** — `[R]` recommendation. §4's open question ("is there an expression-level
> type ascription?") is answered by `FIVERULINGS` §3 — see the note in §4. The
> declaration-site refusal and its use-site successor live in `Test/Expiry.cs`.

Three answers, in order of how much they change what you do next.

1. **Overloads are resolver alternatives**, and the type filter is one pass. But
   the granularity is neither of the two you offered: one derivation per
   **shape**, carrying a **candidate set** of declarations. `Call.Alike` keeps
   comparing the shape. Tuesday's work is not a fork to back out; it is missing a
   field.
2. **Tag it — but tag the successor, not just the expiry.** An entry that says
   "expires" produces a rewrite. An entry that says *what it becomes* produces a
   deletion. And what is currently called `Overloaded` is **two rules**, only one
   of which expires.
3. **Do not write `print number (_)` / `print text (_)` into the standard
   library.** The pressure you are feeling is largely a missing generic, not a
   missing overload — and if the workaround ships in the library, landing the
   type checker becomes a library migration rather than a compiler change.

---

## 1. Why a separate pass is not merely a second mechanism

You framed the choice as *alternatives filtered by types* versus *a selection
step after resolution*. The second option has a cost that is not obvious from
that framing, and it is the reason to reject it.

If overload selection runs **after** a reading is chosen, then while readings are
being eliminated an overloaded pattern **has no single parameter type**. The
resolver cannot eliminate on it. Not at ambiguous overload sites — *at every call
to an overloaded pattern anywhere*.

```
  run                        A separate    C candidate set   agree?
  q total of items               UNIQUE             UNIQUE   yes
                             callee not overloaded -- baseline

  show total of items       2 AMBIGUOUS             UNIQUE   NO
                             show «total of items»       ill-typed
                             show (total of «items»)     the real reading

  render total of items     2 AMBIGUOUS        2 AMBIGUOUS   yes
                             both readings admissible -- genuinely ambiguous
```

The middle row is the finding. Under a separate pass, `show total of items` is
reported as an **ambiguity between two readings only one of which type-checks** —
and then overload resolution, which would have settled it, never runs, because it
runs after a reading is chosen and no reading was chosen.

So the cost of separating the passes is not a second mechanism. It is **a hole in
the first one**, sized in proportion to how much of the library is overloaded.
For the library that motivated your question, that is most of it. This is the
argument that decides it.

## 2. Why splitting derivations is right about *when* and wrong about *granularity*

Your instinct — make `Call.Alike` compare the declaration — gets the right
answers. It pays for them:

```
  call sites in the run                   B split      C set   ratio
  1 site, 2 structural readings                 5          2       2x
  2 sites, 2 structural readings               13          2       6x
  3 sites, 2 structural readings               35          2      18x
  4 sites, 2 structural readings               97          2      48x
  5 sites, 2 structural readings              275          2     138x
```

The overload arities multiply at every call site in the run, whether or not
anything is ambiguous. We have kept derivation counts near-linear by refusing
juxtaposition; this would spend that back on a library that is overloaded by
design.

And it spends it for nothing, because **overloads are not structural**. Two
declarations of one shape span the same tokens and build the same tree. The only
thing that differs is which declaration the node binds to — which is a field on
the node, not a different node.

## 3. The shape it wants: one derivation, a candidate set

```
  resolve      a Call node carries  Shape  +  Candidates : set of declarations
               (today: Shape + the single declaration)

  type filter  narrow Candidates to those admitting the argument type(s)
                 |Candidates| = 0   the derivation is DEAD
                                    -> ordinary reading elimination, unchanged
                 |Candidates| = 1   resolved
                 |Candidates| > 1   an OVERLOAD ambiguity  (see §4)
```

One pass, two granularities, and they compose in the right direction: an empty
candidate set kills a *derivation*, which is exactly the elimination the resolver
already does. That is what makes the middle row of §1 come out right.

`Call.Alike` compares the shape — unchanged. What changes is that binding a call
produces a set rather than a single declaration, and that the type filter can now
narrow that set. If the set is a singleton everywhere, which it is today, nothing
about current behaviour moves.

**Where I cannot see:** whether `Cell.Alternatives` and the current binding step
can carry a set without disturbing anything downstream, and whether declaration
lookup currently returns at-most-one by construction. Both are questions for you,
not assertions from me.

## 4. The half of this that is genuinely undecided

`|Candidates| > 1` after typing is an ambiguity — and **it has no bracket
repair**. Brackets group; they do not classify. `show (x)` does not select
between two `show (_)` declarations, exactly as `old (is valid)` could not select
a name.

By the rule we have been applying everywhere — *a construct needs a bracketable
form to survive ambiguity-as-error* — same-shape overloading needs **a second
repair vocabulary**. The natural one is a use-site type ascription: whatever
`x => text` looks like as an *expression* rather than a declaration.

So the real prerequisite for overloading is not the type checker. It is:

> **Does Ronin have an expression-level type ascription?** If not, same-shape
> overloading admits unrepairable errors and should not land — the same test that
> killed the injected `old X` and the juxtaposed scope.

> **Answered — yes, it is ruled in.** `FIVERULINGS` §3 rules `(x => text)` an
> expression-level ascription: a check and never a coercion, binding loosest, costing
> no reserved word because `=>` is a symbol and symbols cannot be captured by names.
> It is the repair that makes same-shape overloading admissible. What remains is not
> the design question but *building* it, deferred together with the overload expiry —
> `CHECKERSCOPINGRULINGS` Q7, and the ledger row in `Test/Expiry.cs`.

That is a designer question and I would rather raise it now than have it found
during the type work. It is also cheap to answer, and it is the kind of answer
that changes whether overloading is a feature or a trap.

## 5. The tagging — yes, and what the entry should say

Fifteen minutes, do it, with two corrections to the shape of the entry.

**First: `Overloaded` is two rules wearing one name.**

| refusal | expires? |
|---|---|
| same shape, **same** parameter types | **never** — that is a genuine duplicate declaration |
| same shape, **different** parameter types | **yes** — this is the approximation |

If they share a diagnostic and a ledger entry, landing the type checker means
picking the two apart under time pressure. Split them now; it is a message string
and a branch.

**Second: an expiry entry needs a successor.** `DONT-DO-THAT.md` §5 says ship the
approximation with its expiry written down — the tests point showed why that is
not enough. `Overloaded` does not *disappear* when types land: the refusal moves
from declaration time to the use site, and needs a diagnostic that does not exist
yet (§4). An entry that records only "expires" schedules a surprise.

So the ledger format wants a third column:

```
  rule            approximates                       becomes
  Overloaded      type-directed selection            a use-site overload
    (differing      at the call site                   ambiguity, repaired by
     types)                                            ascription -- NOT a
                                                       deletion
  self-ambiguity  a name may not have another        the same rule with
                    reading of the same TYPE in        "of the same type"
                    the same position                  restored
```

Both of ours are *narrowings*, not deletions. If the ledger cannot say that, it
will read as a checklist of things to delete and half of it will be wrong.

## 6. The part I would push back on: the library does not need this as much as it looks

`print number (_)` and `print text (_)` is indeed not a language anyone wants to
write in. But before treating that as the overloading requirement, check whether
it is an overloading problem at all.

`print (_)` with the parameter type omitted **is already generic** — omission
≡ `=> ?`, per `GENERICS-II.md`. A generic `print` has one declaration, one
inferred constraint (*the argument has a `to text`*), and that constraint is its
exported interface, per `GENERICS.md`. The per-type behaviour lives in each type's
own `to text` declaration. That is one declaration, no overload set, no candidate
sets, no ascription, no `Call.Alike` question — and it is the answer Rust and
Haskell reach for the same function.

Which reframes the priority. Overloading is needed where **implementations
differ for reasons the constraint cannot express**, not merely where types
differ. `print`, `max of`, `sort` — all constraint-shaped. I would want to see
the list of library functions that genuinely resist the generic route before
overloading is scheduled at all, because my guess is it is short and none of it
is in the first hundred functions you need.

**And regardless of that: do not ship the workaround into the library.** Names in
the standard library are the reservation surface for the self-ambiguity rule, and
they end up in every document and every user's fingers. `print number (_)` landing
now means that when overloading (or the generic) arrives you have a *library
migration* with deprecations, not a compiler change. Prefer a `print (_)` that is
monomorphic or generic-and-incomplete over a `print number (_)` that is complete
and wrong. An unfinished library function is a gap; a shipped bad name is a debt.

## 7. On ordering

Your concession is right and I would not push further. Tooling does not need
types; the library needs both; they are parallel. The one addition: the library
is also what *generates* the type requirements, so a thin vertical slice of it —
five or six functions taken all the way through — will tell you more about what
the type checker must do than designing the type checker will. `print` is a good
first one precisely because §6 is unresolved and building it settles it.

## 8. Summary

| | |
|---|---|
| overloads as a separate post-resolution pass | **no** — measured: it disables reading elimination at every overloaded call and manufactures ambiguities |
| overloads as split derivations | right answers, wrong granularity — 138× at five call sites |
| **one derivation per shape, candidate set of declarations** | **this** — one type-filter pass, empty set feeds ordinary elimination |
| `Call.Alike` | **unchanged**, compares the shape. Not a fork; a missing field |
| the real prerequisite | **an expression-level type ascription** — without it, overload ambiguity is unrepairable |
| tag `Overloaded` | yes — but **split it in two**, only the differing-types half expires |
| ledger format | needs a **successor** column; both our entries are narrowings, not deletions |
| `print number (_)` in the library | **don't** — it is a missing generic, and a shipped name is a migration |

Probe: `overload_alternatives.py`.
