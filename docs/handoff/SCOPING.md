# Scoping, and two smaller items

> **Ledger** — `[V]` Scoping, and two smaller items
> supersedes: none
> superseded by: SCOPING_updated

## The decision: what an inner scope sees

**Inward yes, outward no, and no shadowing.**

1. **An inner scope sees every declaration of every enclosing scope, at any
   position.** The declaration pre-pass already makes order irrelevant within a
   scope; nesting inherits that, so use-before-declaration works across levels
   too.

2. **Nothing flows outward.** An inner declaration is invisible to siblings and
   to the parent. This matters more here than in most languages, because a
   pattern declaration is a *grammar production* — if inner declarations
   escaped, a nested function would change the grammar of its siblings' bodies
   and scopes would have to be resolved inside-out. They resolve outside-in.

3. **Shadowing is a declaration error.** An inner name identical to an
   enclosing one is rejected where the inner one is written.

That third point is the load-bearing one, and it buys more than it costs.

### Why no shadowing

The readability argument first: the language's premise is that reading a value
tells you where it came from. If `total` means different things at different
nesting depths, that question needs a "which one?" every time — and unlike the
minimum-lookup ties, the compiler can't even flag it, because both readings are
legal.

The implementation argument second, and it's substantial: **with shadowing
banned, a scope's symbol table is a flat merge of its enclosing chain, not a
chain to walk.** The resolver already does per-position lookups against a name
set; a flat table keeps that a single hash probe instead of a walk up N levels
per probe. Build the merged table once when entering the scope, reject
collisions as you merge, and the DP never knows nesting exists.

The cost is that a parameter can't be called `name` inside a type that has a
`var name`. That's a rename, and the error names both sites.

### R5 and R6 are checked against the merged table

Both scope-wide rules — pattern glue reserved against multi-word names, anchor
runs prefix-free — apply to the *merged* set, which means an inner declaration
can invalidate an outer one:

```
outer scope:   var total for order       (a legal name today)
inner scope:   function compute total for (order)     <- makes «for» glue
```

The inner pattern is what breaks the outer name, so **reject the inner
declaration**, not the outer name. Error at the mistake, consistent with every
other diagnostic. Message should name both.

### Lexical scope is not ownership scope

Worth stating explicitly before it gets conflated. Ownership is about
*concurrent evaluation units* — a type instance, a reactive node — not about
every `{}`. A function nested in a type shares that instance's ownership and
may write its members freely. The single-writer rule is between nodes, not
between braces.

---

## Defaulted parameters

Right call to decline the pattern rather than emit a block with a null. A wrong
block binds arguments to wrong names silently, which is the worst available
outcome.

Two notes for the fix:

**It's a parser ordering bug, not a grammar gap.** The skeleton already has
`parameter: modifier* [kind] name [returns type_ref] ["=" body]`. The `= 3` is
being claimed by the association parser before the parameter parser sees it, so
the alternatives need reordering inside a parameter block rather than a new
production.

**And it changes the arity check you just added.** The guide says a parameter
with an initialiser, or marked `optional`, need not be supplied. So a block's
arity *for binding* is its **mandatory** count, not its total:

```
draw (shape) at (x, y = 0)
```

is legal with a single argument in the second block. Validating one block per
hole stays right; validating group size against total parameters would reject
this. Count mandatory, and let defaults fill the rest at the call site.

---

## The `AdvanceTo` bug

Good find, and the instinct that the test was asserting the bug is the part
worth keeping.

It's the second time a test has encoded an implementation detail rather than a
behaviour. The pattern is worth naming: tests over the token layer should
assert **semantic** properties — the joined text, the word count, what
`Name.Words` returns — rather than **representational** ones like an array
length, because the representation is precisely what's most likely to be wrong.
`Tokens.Length == 7` could only ever have been derived from running the code.
