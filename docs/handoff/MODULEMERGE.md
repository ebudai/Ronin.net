# §1 — the hole is real, the fix is right, and the confinement claim is not

> **Ledger** — `[V]` §1 — the hole is real, the fix is right, and the confinement claim is not
> supersedes: none
> superseded by: none

Three parts. The compiled-scope decision is correct and I would take it now, for
a reason he does not give that makes it more urgent than he says. The claim that
it confines the conflict to *the importer's new statements* is **false** —
measured. And the rule he leaves in place at the import boundary is the wrong
one there, for a reason that is also measured: it over-refuses by 87%, and
across a module boundary over-refusal has no repair.

---

## 1. Compiled-scope resolution: agreed, and it is more urgent than stated

> A module's own statements resolve against the scope it was compiled in.

Right, and the argument he gives — separate compilation, importing cannot
change what A meant — is the standard one. The Ronin-specific argument is
stronger and belongs in the decision record:

**The environment is always running. Debug *is* development.** Imports are not
a build-time event here; they are an edit made against a live graph with live
`let` nodes and live `when` arms. Without compiled-scope resolution, adding an
import in the IDE re-resolves the source of modules that are *currently
executing*. That is not a diagnostic problem, it is a "the running program
changed underneath you" problem, and there is no message that makes it
acceptable.

So: yes, and I would put it in `docs/spec/` as a semantics rule rather than an
implementation note.

## 2. It does not confine the conflict to new statements — measured

`module_merge.py` §1. Module A exports `send (_) to (_)` and `send (_)`; module
B exports the name `hello to alice`; the importer already had `hello`, `alice`,
and this line:

```
    send hello to alice

  before «import B»:  OK   send «hello» to «alice»
  after  «import B»:  OK   send «hello to alice»
```

`Resolved` both times. The statement predates the import and its meaning
changed anyway, because the importer's own code lives in the scope the import
joins.

Compiled-scope resolution confines the hazard **to one module**. That is a
genuine and worthwhile guarantee — it is the difference between one file to
inspect and a whole program — but it is much weaker than "new statements only",
and the rest of §1's mitigation is sized against the stronger claim.

## 3. The blanket rule is right inside a module and wrong at the boundary

This is the part I would change. R5 and R6b are **declaration-time blanket
rules**: they refuse a name for *containing* glue, or for *beginning with* an
anchor run, without asking whether a rival reading actually exists. That is
deliberate — `GLUE-AS-WHOLE-NAMES.md` §1 already rejected the alternative,
because legality that depends on which unrelated names happen to be declared is
legality nobody can predict.

The trade that justifies it is that **the repair is a rename**, and inside one
module you own both sides.

At an import boundary you own neither. So the question becomes: how much of
what the blanket rule refuses is actually dangerous?

```
  send (_) to (_) | send (_)      refused  56   dangerous   8   over-refused  86%
  print (_)                       refused  20   dangerous   6   over-refused  70%
  sum of (_) | (_) otherwise (_)  refused  47   dangerous   2   over-refused  96%

  TOTAL                           refused 123   dangerous  16   over-refused  87%
```

Examples of what it refuses that can never capture anything: `hello to`,
`send to`, `print print`, `x otherwise`.

**87% of the library pairs a blanket import-time check would reject could have
coexisted**, and the importer cannot rename either side. That is not a
diagnostic cost, it is a dependency-hell cost: two innocent libraries that
cannot appear in the same program, with the resolution being "wait for one of
them to rename an exported symbol."

## 4. What to put at the boundary instead

> **An import may not change the reading of any statement already in the
> importing module.**

Formally: for each import `I`, resolve the importing module against the full
merged table and against the table minus `I`. Any statement that parses both
ways but parses **differently** is an error attributable to `I`. `NO PARSE →
parses` is an extension and is fine — that is the same monotonicity test the
`nothing found` withdrawal turned on.

Four things recommend it:

**It is not a new instrument.** It is `name_capture.py`'s sweep pointed at one
module's real statements instead of a generated universe. The resolver is
deterministic and already computes exactly what is compared.

**It is exactly as strict as the danger.** By construction it flags every
capture and nothing else — none of the 87%.

**The diagnostic is the best in the language.** It names the import, the
statement, and both readings:

```
  «import B» changes the meaning of line 47.
      was:  send «hello» to «alice»
      now:  send «hello to alice»   (name «hello to alice» from B)
```

**The cost is affordable precisely because the environment is always running.**
n imports, n+1 resolutions, recomputed only when the import list changes, in the
background. Order-independent, because each import is checked against
*everything else*, not incrementally.

`module_merge.py` §3 runs it: `import B` is rejected with the line and both
readings; `import C` (name `alice greeting`, which R5 also refuses) is accepted,
because nothing changes.

### And it restores the glue economy per module

Worth stating separately, because it is the largest consequence and neither of
us has written it down. Under a blanket import-time check, a library that
spells `send (_) to (_)` **spends `to` for every program that imports it** — the
glue registry becomes global and the cost of a connective compounds with every
dependency. Under compiled-scope plus the differential check, `to` is spent in
A's scope only. A library can afford a connective without taxing its users.

That changes how expensive glue words are, and it is the difference between a
registry that can grow and one that cannot.

## 5. On the qualification tool

Agreed as the **repair**, with one caveat about how it fires.

Automatic demotion — "a conflicting symbol becomes reachable only qualified" —
has to choose a side. In `send (_) to (_)` versus `hello to alice`, demoting the
pattern and demoting the name are equally defensible, and the language's
standing principle is *ties are compile errors, never a silent pick*. Demoting
both is symmetric but awkward, because a multi-word pattern has no natural
qualified spelling — `A.send x to y` is not a form the grammar has.

So the qualification should be **written by the importer**, not inferred:

```
import A;
import B renaming «hello to alice» as «greeting»;
```

Explicit, local to the one place a human can fix it, and it leaves the
diagnostic doing the work of finding it.

## 6. The collision he did not list, and it is the one that will bite

§1 is about two *different* symbols that conflict under a spelling rule. The
more likely failure is two modules exporting the **same** symbol:

```
  module A exports   print (_)
  module B exports   print (_)      -> duplicate on merge
```

No spelling rule prevents this and no differential check helps — it is a
duplicate declaration under no-shadowing, and it fails for every importer of
both. `print`, `sort`, `count of`, `first of`, `item (_) of (_)` are exactly the
symbols two libraries pick independently.

This needs the qualification escape hatch whether or not §1's R5 case is ever
hit in the wild, and I would size the import syntax against *this* case rather
than against §1's — it is more common, and if it works for duplicates it works
for conflicts.

## 7. Summary

| | |
|---|---|
| the hole is real and unpredicted | **agreed** |
| compiled-scope resolution | **agreed, take it now** — and the live-environment argument is the stronger one |
| "confines the conflict to new statements" | **false** — measured; it confines it to one module |
| blanket R5/R6b at the import boundary | **no** — 87% over-refusal with no rename available |
| differential check instead | **recommended** — same instrument, exactly as strict as the danger, best diagnostic in the language |
| glue economy | becomes **per-module**, which is the largest consequence of getting this right |
| explicit qualification/renaming | **agreed**, but importer-written rather than inferred |
| same-symbol duplicates | **missing from §1**, more likely, and it should drive the import syntax |

Ordering unchanged: §1 first, for his reason and mine.

Probe: `module_merge.py`.

---

One note outside §1, because it is the sharpest thing in the document: his
observation under "Order I would take them" item 4 — that `() => …` already
makes a cell hold a deferred computation, so **§6's immunity is already lost**
— is correct and it means §6 is not an immunity to preserve but a decision to
make against existing programs. That deserves its own round.
