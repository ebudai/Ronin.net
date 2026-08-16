# The two open items — decided, with a tool for the second

> **Ledger** — `[R]` The two open items — decided, with a tool for the second
> supersedes: not yet checked
> superseded by: not yet checked

Both closed below. The second one produced a finding that changes the first,
which is the argument for building it now rather than later.

---

# A. §7b — the loop index

**Decision: inject `index of «loop variable»`.** One name, per loop,
unconditionally, derived from the name the author chose.

```
for each bank in banks
    print bank
    print index of bank
```

## Why not a bare `index`

Because there is no shadowing. A bare injected `index` collides with any
user's own `var index`, and `index` is a name people reach for constantly. Every
loop in the file would be fighting it, and the fix — rename your variable
because the loop wanted the word — is exactly the diagnostic we agreed never to
produce.

## Why derived from the loop variable

Three properties fall out for free:

- **Nested loops don't collide.** `index of bank` and `index of branch` coexist;
  a bare `index` would need shadowing rules to nest, and we don't have any.
- **The author controls it.** If the injected name collides with something, the
  fix is renaming the loop variable — a local, obvious edit at the site of the
  problem.
- **It reads as prose**, which is the whole point of paying for this grammar.

## It is subject to R5, like every injected name

`SCOPING.md` already settled this for `old x`: injected names are examined by
R5, are *not* suppressed, and the diagnostic names the declaration that caused
the injection rather than the generated name. Same here — primary span on the
loop variable, related span on the pattern that made the word glue.

> «index of bank», injected by the loop over «banks», collides with pattern
> glue «of» from «item (_) of (_)». Rename the loop variable.

## The catch — and it is live today

That example message is not hypothetical. **`of` is currently a glue word**,
because of one pattern:

```
item (_) of (_)        anchor = «item»        glue = {of}
```

Meanwhile six patterns use `of` for free, because in them it precedes the first
hole:

```
sum of (_)   count of (_)   average of (_)   first of (_)   last of (_)   length of (_)
```

So a single badly-shaped pattern would reserve `of` for every program that
imports `collections` — and would make the injected loop index illegal in all
of them.

**Fix: respell `item (_) of (_)` as `item (_) in (_)`.** `in` is already
reserved by `for each (_) in (_)`, so reusing it costs *nothing new*, and
`item 3 in banks` reads at least as well as `item 3 of banks`.

This is the first instance of a rule worth adopting generally — see §B.

## Base 0 or base 1

This must be decided together with `item (_) in (_)`, not separately, and the
two must agree. My recommendation is **1-based**, for reasons specific to this
language rather than by analogy:

- There is no pointer arithmetic and no C legacy to stay consistent with.
- `item 1 in banks` meaning the first item is what the prose says it means, and
  prose is the thing being optimised for.
- Exact-numbers-by-default already rejected "match what the machine does" as a
  design principle. This is the same call.

The cost is that anyone arriving from another language gets bitten exactly
once. Your call; I only insist the two agree and that it be written down.

## Don't inject a family

`is first`, `is last`, `count so far` are all tempting and all unnecessary —
`index of bank` plus the existing `count of banks` expresses each of them, and
each extra injected name multiplies the collision surface.

## Tests

| # | case | expect |
|---|---|---|
| 1 | `for each bank in banks` then read `index of bank` | resolves; advances per iteration |
| 2 | nested loops | `index of bank` and `index of branch` independent |
| 3 | user declares `var index of bank` in the same scope | collision error, both sites named |
| 4 | injected name fails R5 | diagnostic names the **loop variable**, not the injected name |
| 5 | loop variable renamed | injected name follows |
| 6 | `index of bank` read outside the loop | not in scope |

---

# B. §6 — the glue registry

**It is not documentation. It is load-bearing data, and it is generated.**

Three things need it and none of them can be satisfied by prose:

1. **R5's diagnostic** must name the pattern *and the module* that reserved the
   word. Without a registry the message can only say "that word is reserved",
   which tells the programmer nothing about what to do.
2. **Adding a stdlib pattern is a breaking change** — names that were legal stop
   being legal in every program that imports the module. Nothing currently
   notices.
3. **Nobody can see the cost of a pattern before committing to it.** The `of`
   finding above took thirty seconds once the registry existed and had gone
   unnoticed across every design document before that.

## Shipped: `glue.py`, `patterns.txt`, `GLUE-REGISTRY.txt`

```
python3 glue.py                            regenerate
python3 glue.py --check GLUE-REGISTRY.txt  CI gate; exit 1 + unified diff
```

`patterns.txt` is a seed containing the patterns that appear across the design
documents plus the obvious control-flow ones. **Replace it by having the
compiler emit it**, so the registry can never drift from the language.

The `--check` failure is the point of the whole exercise — it turns "we
silently broke everyone's names" into a reviewable diff:

```
GLUE REGISTRY OUT OF DATE.
  collections
    by           sort (_) by (_)                    collections
+   by           group (_) by (_) into (_)          collections
+   into         group (_) by (_) into (_)          collections
```

## What the seed already shows

```
patterns declared        24
reserve nothing          11        <- 46% cost zero
distinct reserved words  13
always reserved           4        (in, otherwise, then, times)
```

Four words are reserved **everywhere, with no escape**, because they come from
the prelude. `otherwise` is already a keyword, so it is free. `then` is rarely
inside a name. `in` we decided to pay for. **`times` is the one that is not
carrying its weight** — it costs "response times", "wait times", "boot times",
"build times" so that `repeat 5 times` can read nicely, and `repeat (_) times`
is the only pattern that uses it.

The registry also flags the two structural cases:

- **Reserved by a single pattern** — 11 of the 13 words. Each is one respelling
  away from being free again. This is the list to work down.
- **Postfix patterns** — `(_) rounded` has a *leading* hole, so its anchor run
  is empty and **every word in it is glue**. Postfix is the most expensive
  shape there is. Prefer `rounded (_)`, or make it a symbol.

## Three rules that follow

**1. Put the words first.** A pattern whose words all precede its first hole
reserves nothing. Eleven of twenty-four already do. This should be the default
shape, and interleaving should require a reason.

**2. Reuse glue that is already reserved.** `item (_) in (_)` costs nothing
because `in` is already paid for; `item (_) of (_)` costs `of` for everyone.
Once a word is in the registry, further uses are free — so the registry is also
a menu of the connectives you may spend freely.

**3. Protect the common connectives.** The dual of the reserved list: a small
set of words that stdlib patterns may use **only in anchor position, never as
glue**, because they appear inside ordinary English names constantly.

```
of   for   the   and   a   to   with   from   at   by   on   in*
```

`in` is starred because we have already spent it deliberately. The rest should
require a design decision at the same level as the loop syntax did — which
means `sort (_) by (_)`, `join (_) with (_)`, `split (_) on (_)` and
`send (_) to (_)` all want a second look. Anchor-first respellings exist for
each: `sort by (_) in (_)`, `join with (_) the (_)`… some read worse, and that
is the trade to make consciously rather than by accident.

## The prelude / import split is the module-composition hazard, made visible

The registry separates *always reserved* from *reserved on import*. That split
is exactly the hazard already flagged — importing a module can retroactively
invalidate your names — and having it in a file means the import diagnostic can
say which module cost you the word, and the language can honestly document
"these four words are reserved everywhere; everything else you only pay for
when you import it."

That is a much better story than a single flat reserved-word list, and it is
only tellable because the registry distinguishes them.

## Adoption

1. Emit `patterns.txt` from the compiler's pattern declarations.
2. Check `GLUE-REGISTRY.txt` into the repo.
3. Add `glue.py --check` to CI.
4. Feed the registry into R5's diagnostic so it can name the causing pattern
   and module.
5. Work the single-pattern list: start with `item (_) of (_)` → `item (_) in (_)`,
   because it is blocking §A.
