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

| run | units | policy | resolutions | ties |
|---|---|---|---|---|
| single pattern | ≤ 3 | blanket | 5,052,672 | **0** |
| single pattern | ≤ 3 | refined | 7,375,104 | **0** |
| pattern pairs | ≤ 2 | blanket | 10,776,192 | **0** |
| pattern pairs | ≤ 2 | refined | 21,927,552 | **0** |
| pattern pairs | ≤ 3 | blanket | 96,985,728 | **0** |
| pattern pairs | ≤ 3 | refined | 197,347,968 | **0** |

**The pair runs exist at two statement widths and the figures are not
interchangeable.** `fuzz_pairs.py` takes an optional unit count and defaults to
3; the ≤ 2 rows are what fit the time budget here, the ≤ 3 rows are the
programmer's reproduction at the default. Same script, same shape, wider space,
same answer — but quoting one as the other is exactly the error this document
was written to stop, so both are listed with their width.

Totals: **45,131,520** across the four narrower runs; **294,333,696** across the
two ≤ 3-unit pair runs. No tie anywhere, and no tie present under `refined`
that `blanket` was preventing.

### The pairs baseline — added, and it cross-validates the extension

`fuzz_pairs.py` now takes a third policy, `strict`: **plain holes only, blanket
reservation** — the original harness's configuration, run through the new
machinery. It is a better baseline than a third glue policy would have been,
because it answers a question nobody had asked: *did adding `{_}` and `<_>`
change the thing we were measuring before?*

```
strict   pairs 67   resolutions 4,083,840   ties 0   29.9s
```

67 kept pairs is exactly the original's 91 generated minus 24 rejected by R6.
Same structure, same answer, more statements (bracketed units are admitted here
and were not there). If that row ever moves, the extension changed the baseline
and every figure above it is suspect.

**On who should have added it:** the programmer declined to, on the grounds
that it is the script that verifies his work and he should not be the one
changing it. That is the right call and worth stating as a standing rule — the
thing under test does not get to modify its own oracle. The inverse is fine:
if a harness is wrong, say so and I will fix it.

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

- ~~**leading-BHOLE** — whether R6 should admit it is open and untested.~~
  **Settled in `LEADING-HOLES.md`:** admit a leading `{_}`, keep refusing a
  leading `<_>` and `(_)`. 10,490,112 resolutions / 0 ties for `{_}`;
  22,731,264 / 1,134 ties once `<_>` is admitted.
- ~~pattern pairs at 3-unit statements.~~ **Done** — the two ≤ 3-unit rows in
  the table above, run by the programmer at 286s and 571s. The claim that it
  exceeded the time budget was true of this machine only, and it stopped being
  true before this document was revised.
- names longer than two words. **Still open**, and now the only one.

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
for the resolution layer — `fuzz_brackets.py`, `fuzz_pairs.py` and
`fuzz_leading.py` in the handoff folder, and `fuzz_verify.py`, the original
harness behind the 2,382,240 figure, which was not in the folder and is shipped
with this revision — and none for the statement layer. It would be small:

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

## 6. Sequencing — **SUPERSEDED, all of it shipped**

> This section is kept only so that a copy of the document found on its own is
> not read as current guidance. Every item below is done as of `4829af8`, and
> the one condition it imposed was lifted by `LEADING-HOLES.md`.

The original text made `THOLE` wait on the leading-`BHOLE` question. That
question is settled, and it turned out to be **independent** of `THOLE` rather
than a precondition for it: leading position is governed by determinacy of
*identity*, interior position by determinacy of *extent*, which is why one is
unsafe and the other is not. The condition should not have been imposed.

| step | status |
|---|---|
| 1. Protected injection words (`old`, `index`, `of`) | done |
| 2. Statement-shape enumeration test | done — 84 generated programs |
| 3. Rename `patterns.txt` → `stdlib-proposal.txt` | done |
| 4. Base-1 in the spec | done |
| 5. `THOLE` rule change unreserving `in` | done — `## RESERVED (0)` |

`ZERO-GLUE.md` step 1 — adopt the shapes — remains correctly blocked on there
being a stdlib to respell.
