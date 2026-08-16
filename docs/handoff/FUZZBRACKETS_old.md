# The probe extension, run — and the two questions answered

> **Ledger** — `[R]` The probe extension, run — and the two questions answered
> supersedes: none
> superseded by: none

The programmer is right that steps 2 and 3 were my side. Done. Files:
`bracket_probe.py`, `fuzz_brackets.py`, `fuzz_pairs.py`.

---

## 1. Result

Two new hole kinds, so the probe can express what `ZERO-GLUE.md` describes:

```
HOLE    (_)     an unbracketed argument                    -- as before
BHOLE   {_}     an argument that must be bracketed         -- mechanism 3
THOLE   <_>     a hole pinned to exactly one word token    -- the loop variable
```

and two reservation policies:

```
blanket   every glue word is reserved                              R5 as verified
refined   glue immediately after a BHOLE or THOLE is not reserved  the proposal
```

| run | policy | resolutions | ties |
|---|---|---|---|
| single pattern, statements ≤ 3 units | blanket | 5,052,672 | **0** |
| single pattern, statements ≤ 3 units | refined | 7,375,104 | **0** |
| pattern pairs, statements ≤ 2 units | blanket | 10,776,192 | **0** |
| pattern pairs, statements ≤ 2 units | refined | 21,927,552 | **0** |

**45,131,520 resolutions, no tie under either policy, and no tie present under
`refined` that `blanket` was preventing.**

Note `refined` runs *more* resolutions than `blanket` from the same generator —
that is the point. Fewer reserved words means more legal name sets, so the
refined policy explores a strictly larger space and still finds nothing.

### What that does not establish

The fuzzer counts **ties**. R5's actual purpose is preventing **silent
capture**, and the original 2,382,240-resolution run measured ties too — worth
saying out loud, since nobody had. The no-capture property does not come from
the fuzzer at either policy; it comes from a one-line invariant in the
tokenizer:

```python
if all(k == WORD for k, _ in t[i:j]):      # only word-only spans can be names
```

A name cannot contain a bracket, so it cannot straddle a `BHOLE`. And a `THOLE`
fixes the split point at anchor-length + 1, so no name can start earlier and run
into the glue. Both are structural. The fuzzer's job was to check that
unreserving those words does not open a *tie* somewhere unexpected, and it
does not.

### The loop, specifically

`for each <_> in (_)` has glue `{in}` under blanket and `{}` under refined:

```
for each bank in banks                  OK   [for each «bank» in «banks»]
for each bank in in                     OK   [for each «bank» in «in»]
for each in in in                       OK   [for each «in» in «in»]
for each order in transit in banks      OK   [for each «order» in «transit in banks»]
for each bank in in banks               OK   [for each «bank» in «in banks»]
```

All unique, with `in` **unreserved** and names containing `in` legal. The last
two are the ones that would have been ambiguous with a free-growing hole.

So the pinned declaring hole does buy back `in`, and `Glue.Registry` would print
`## RESERVED (0)`.

### Still not verified

- **leading-BHOLE** — bracket-delimited infix is now expressible, because a
  `BHOLE` must consume `(` and so is not left-recursive, unlike a leading
  `HOLE`. Whether R6 should admit it is open and untested.
- pattern pairs at 3-unit statements (2 units only above; the 3-unit pair run
  exceeds the time budget and wants a proper machine).
- names longer than two words.

---

## 2. Base 0 or base 1 — decided: **1**

`index of bank` is 1 on the first iteration. `item (_) in (_)` must agree.

The reasons are specific to this language rather than by analogy: there is no
pointer arithmetic and no C legacy to stay consistent with; `item 1 in banks`
meaning the first item is what the prose says; and exact-numbers-by-default
already rejected "match what the machine does" as a design principle. This is
the same call.

**The rule that matters more than the number:** *one convention, everywhere the
words `index` or `item` appear.* If something genuinely machine-facing ever
needs 0-based counting — a byte offset into a buffer, an interop boundary — it
is called `offset`, not `index`, and the difference is documented at both. Two
conventions under similar names is the actual disaster; which end they start
from is a detail.

---

## 3. `item (_) of (_)` — make it a rule now, not a note

The programmer is right that it isn't blocking today, and right that it becomes
real the moment a collections module exists. The fix is not to remember: **put
`of` in the protected set now.**

The protected set is the dual of the glue registry:

> **glue words may not be names; injection words may not be glue.**

Injection words today: `old`, `index`, `of` — the words the compiler uses to
build `old X` and `index of X`. No pattern may use any of them as a non-leading
segment, rejected at the *pattern's* declaration:

> «item (_) of (_)» may not use «of» as glue: «of» forms the injected name
> «index of (loop variable)». Respell the pattern — «item (_) in (_)».

That converts a future trap into a present rule, and it fires once at the
offending pattern instead of once per loop. Same shape as the `old`-as-glue
hole. `Glue.Registry` should print both lists.

---

## 4. The block-separator bug — the important thing in that commit

```
function f { if x { return 1; } return 2; }     did not compile
```

Two statements where the first is a block is most programs, and 582 tests at
100% line and branch did not notice.

**That is not a gap in the tests, it is a property of the metric.** Coverage
measures which lines ran, not which input *shapes* were formed. A grammar's
failure modes live in combinations of constructs, and every block in every test
happened to be single-statement or block-final — a habit, invisible in a
coverage report, that no amount of line coverage would reveal.

The remedy for a grammar is generative, not incremental. There is already one
for the resolution layer — that is what `fuzz_verify.py` is — and none for the
statement layer. It would be small:

> Enumerate every sequence of 1–3 elements drawn from {simple statement, braced
> statement, braced statement containing a braced statement}, in a block, and
> assert each compiles. About 40 programs, generated in a loop.

That catches this bug on the first run and catches the next one before an
audit does. Worth doing while the lesson is fresh.

---

## 5. Two registries — keep them separate, rename mine

The programmer is right not to overwrite `patterns.txt` with compiler output.
They answer different questions:

| | |
|---|---|
| `Glue.Registry` → `docs/reserved-words.txt` | what the compiler actually reserves. Cannot drift. The one a test gates. |
| `patterns.txt` | a design study of an aspirational stdlib, and the analysis that goes with it |

They should not merge — but mine should be renamed so nobody mistakes it for
stale output. `stdlib-proposal.txt` with a header saying it is a proposal, not
a description. When a stdlib exists, its patterns get declared and the proposal
is consumed rather than merged.

---

## 6. Sequencing from here

Step 1 of `ZERO-GLUE.md` — adopt the shapes — remains correctly blocked on there
being a stdlib to respell. Steps 2 and 3 are now done. Step 4, the rule change,
is unblocked for **`BHOLE`**; for **`THOLE`** I would want the leading-BHOLE
question settled first, since it is the one place the extension revealed
genuinely new territory rather than confirming an argument.

Concretely, in order:

1. Protected injection words (`old`, `index`, `of`) — independent of everything
   above, closes two known traps.
2. The statement-shape enumeration test.
3. Rename `patterns.txt`.
4. Base-1, written into the spec.
5. Only then, the `THOLE` rule change that unreserves `in`.
