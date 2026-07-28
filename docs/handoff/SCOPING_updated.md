# Scoping, and two smaller items

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
outer scope:   var hello to alice          (a legal name today)
inner scope:   function send (message) to (recipient)   <- «to» follows a hole
```

The inner pattern is what breaks the outer name, so **reject the inner
declaration**, not the outer name. Error at the mistake, consistent with every
other diagnostic. Message should name both.

> **Correction.** An earlier draft of this file used `compute total for (_)`
> against `total for order` here. That example does not fire, and it is worth
> understanding why before writing a test against it. Glue is the literal
> segments *after the anchor run* — the words before the first hole. Every word
> of `compute total for (_)` precedes its hole, so its anchor is
> `compute total for` and its glue set is **empty**. `total for order` beside
> it is perfectly legal. The rule needs a word that follows a hole, which is
> what `send (_) to (_)` has.
>
> ```
> apply (_) smoothed (_)     anchor=apply              glue={smoothed}
> compute total for (_)      anchor=compute total for  glue={}
> send (_) to (_)            anchor=send               glue={to}
> ```

### Injected names and duplicate R5 complaints

A shadow is a multi-word name, so R5 examines it too, and `hello to alice`
produces two complaints for one mistake — the second naming `old hello to
alice`, which nobody can rename.

**Do not suppress R5 on injected names.** A shadow can fail where its source
passes, and that case is only reachable through the shadow:

```
smoothed         (declared, one word)  -> R5 never looks at it
old smoothed     (injected, two words) -> REJECTED on glue «smoothed»
                                          from «apply (_) smoothed (_)»
```

R5 only examines multi-word names, so a single-word declaration is never
checked — but its two-word shadow is. Blanket suppression would hide a real
conflict.

**Suppress only when the source name also fails.** Then it is genuinely one
mistake with one fix, and the second message adds nothing:

| source | shadow | report |
|---|---|---|
| fails | fails | source only — shadow is a duplicate |
| passes | fails | shadow, phrased against what *can* be changed |
| fails | passes | source only |

In the second row the message must name the two things the programmer
controls, not the generated name:

```
«old smoothed», injected by «let smoothed», collides with pattern glue
«smoothed» from «apply (_) smoothed (_)».
Rename «smoothed», or respell the pattern.
```

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

## A note on this document

Two of the errors this turn trace to the same cause: **this file shipped as
prose while every other design note shipped as runnable code.** The R5 example
was never executed, so it was never checked, and it propagated into a test —
where it passed vacuously, because an example that cannot fire also cannot
fail.

Rules with a mechanical definition should ship with the mechanism attached.
The glue table above is three lines of output; had it been generated rather
than written, the error would have been impossible.

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
