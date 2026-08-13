# The function type's arrow — two things the ruling could not see, both measured

**From:** the successor. **State:** the foundation of the type half is landed and
green — type names, `list of (_)`, `optional (_)` annotate and resolve, an unknown
type is a finding at its site, 100% line/branch/method, clean `-warnaserror`
build. What is left of step 4 is §4 of `TYPE-HALF-RULINGS`: fold in the function
type `(_) => (_)` so `var m => text => number` resolves and
`lookup text => number => truth` is the three-way ambiguity `LOOKUP-ARROW` §2
requires.

The ruling said of §4: *"there is less weight than you feared… variance: none;
spelling: `=>`; arity: a group §3 already admits."* Two of those are right. But
building it turned up two things the ruling did not have in view, and I probed
both rather than press on — the working rule here is that a five-minute probe
before the code has caught a wrong premise every time this session.

Neither is a reason not to do §4. Both change what doing it means.

---

## 1. The parser cannot put `=>` inside a reference — so no arrow type parses at all today

`Symbolic.Parse` — the thing that lets a symbol stand between the names of a
reference — is written `current.Token is not (Symbol and not Punctuation)`.
`Arrow` **is** `Punctuation`. So `=>` is refused as a reference component, and:

```
  var g => lookup text => number;
  Player.ron:1:22: unexpected input. «=> number» could not be read …
```

The reference stops at `lookup text`, and the second `=>` falls out of the
statement. **This is pre-existing** — nothing I changed touches the parser — and
it means the `lookup (_) => (_)` type, which has been in the resolver's
vocabulary since the prelude landed, has **never been writable through the
parser.** The resolver reads it fine (its own tests lex a string directly and
resolve it); no source can reach it.

So §4 is not only "add a function type". It is "make the arrow a thing a type
reference may contain", which the lookup type needs too. That is a change on the
**value side of the grammar**, and it is not free of consequence:

**`Component.Parse` special-cases the arrow already, in the other direction.** It
reads a name, and *refuses* to take it as a component when an arrow follows —

```csharp
if (Name.Parse(ref ahead) is Name leading && ahead.Token is not Arrow) return leading;
```

— because `x =>` is the start of a delegate `x => { … }`, and the name is that
delegate's parameter, not a component of a reference. Allowing `=>` to be an
ordinary reference symbol collides with exactly this: after the change, `x => {…}`
and the expression-level ascription `(x => text)` that `FIVE-RULINGS` §3 rules in
both have to keep parsing the way they do, while `text => number` and
`lookup text => number` newly parse as references. The arrow now does four jobs
in the parser rather than three, and three of them are on the value side.

**What I need ruled:** how the arrow enters a reference. Options, roughly:

- **(a) Arrow becomes an ordinary reference symbol** (drop the `not Punctuation`
  exclusion for it, keep it for brackets and `;`). Simplest, and it puts the
  delegate/ascription lookahead under load — the `ahead.Token is not Arrow` guard
  and `Temporary.Parse`'s delegate attempt now have to be shown to still win where
  they should. I can do this and pin it with tests, but whether the arrow should
  be a plain reference symbol at all is a grammar decision, not the type
  checker's to make quietly.
- **(b) A narrower rule** — the arrow is a reference symbol only where a delegate
  cannot begin (no braces follow its right side). More surgical, more moving
  parts, and "where a delegate cannot begin" is the kind of context-sensitive
  test we have twice rejected elsewhere.

I lean (a), with the delegate and ascription cases nailed as maintained tests
before anything else. But it is a value-grammar change and I would rather have it
ruled than assume it.

---

## 2. The three-way ambiguity is real but sits on a knife-edge — and it fights currying

`LOOKUP-ARROW` §3 says, emphatically, *do not give the arrow a binding power that
resolves the ambiguity* — the three readings differ by **which constructor claims
which arrow**, not by tightness. I took that seriously and measured what a plain
arrow **operator** in the resolver actually produces, sweeping every binding
power and both associativities, with the `lookup (_) => (_)` pattern present so
its arrow-segment competes with the operator. The input is
`lookup text => number => truth`; `two` is the unambiguous
`lookup text => number` for control.

```
  arrow          lookup text => number       lookup text => number => truth
  bp 1–6  (either)   Resolved, unique         Ambiguous, 2 readings   (missing «lookup text => (number => truth)»)
  bp 7  right-assoc  Resolved, unique         Ambiguous, 2 readings   (missing «(lookup text => number) => truth»)
  bp 7  LEFT-assoc   Resolved, unique         Ambiguous, 3 readings   ← all of §2, exactly
  bp 8–11 (either)   Resolved, unique         Ambiguous, 2 readings   (missing «(lookup text => number) => truth»)
```

At **bp = 7, left-associative** — and nowhere else — the resolver yields exactly
`LOOKUP-ARROW` §2's three, confirmed by node type, not just rendering:

```
  [Call]      lookup text => (number => truth)      a table of callbacks        §2 reading 1
  [Call]      lookup (text => number) => truth      a table keyed by functions  §2 reading 3
  [Operation] (lookup text => number) => truth      a function taking a table   §2 reading 2
```

`7` is not a magic number: it is `PatternBindingPower`, the level a pattern's
trailing argument parses at. The arrow binding **exactly there** is what lets the
lookup's own trailing hole and the outer arrow compete for the second `=>` —
which is the whole of why there are three readings. So this is consistent with
§3: the setting **produces** the ambiguity, it does not resolve it, and the common
shape `lookup text => number` stays unique throughout.

**But it is a knife-edge, and it has a cost §3 did not price.** Left-associative
at bp 7 also decides the *bare* chain, the one with no lookup:

```
  text => number => truth      →  (text => number) => truth        left-associative
```

That is **not** the curried reading. Everywhere function types chain, `a => b => c`
means `a => (b => c)` — a function returning a function — and this makes it "a
function *taking* a function returning truth". Right-associative would give the
curried reading, but bp 7 **right** drops §2's reading 2 (the table-as-parameter
one). So the single setting that makes the *lookup* ambiguity complete makes the
*bare* chain associate the unusual way.

Three ways to read that, and it is yours to pick:

- **Bare chains associate left, and that is fine** — Ronin functions are not
  curried (they take parameter blocks, not one argument at a time), so `a => b => c`
  need not mean the curried thing, and left is as defensible as right. Then bp 7
  left is simply the answer and §2 lands with it.
- **Bare multi-arrow chains are themselves ambiguous** and must be bracketed —
  which is the honest reading if `a => b => c` genuinely has two meanings a reader
  cannot tell apart, and is of a piece with the language's whole stance. That is
  not what a single associative operator produces, though; it needs the arrow
  modelled as non-associating, which is more than a binding power.
- **The arrow is not a resolver operator at all** but a dedicated binary type
  constructor with its own rule, if the operator machinery's associativity is the
  wrong tool for a choice that is about constructors rather than tightness — which
  is precisely what §3 warned the binding power was.

I can build any of the three. I will not pick among them, because the difference
is what `a => b => c` *means* to a reader, which is the language's call and not
the checker's.

---

## 3. What I propose, pending your answers

- **Land the foundation now.** It is complete and correct for every arrow-free
  type, fully covered, and it closes the `Compilation` comment it was written to
  close. The lookup and function types are additive on top of it and gate nothing
  that is already done.
- **Hold §4** for two rulings: **§1** — how the arrow enters a reference (I lean
  (a), arrow as an ordinary reference symbol, delegate/ascription pinned first);
  and **§2** — how `a => b => c` associates, which decides whether bp 7 left is the
  answer or the arrow wants a mechanism of its own.
- Everything else in `TYPE-HALF-RULINGS` is done or in flight: the modifier
  `fast number` is deferred as you allowed; the base-resolution gap is in the
  expiry ledger with its successor named; the diagnostic states "nothing declares
  it" and leaves the "declared, not imported" case for when modules exist.

The measurements are `Symbolic.Parse:36` for §1 and the sweep above for §2 (a
throwaway resolver probe, deleted; I can restore it as a maintained test the day
the mechanism is chosen).

---

## 4. Summary

| | |
|---|---|
| the arrow in a reference | **the parser refuses it** — `Symbolic` excludes all punctuation, so no arrow type parses today, lookup included. A value-grammar change, and it loads the delegate/ascription lookahead. **Rule how it enters** |
| the three-way ambiguity | **reachable, but only at bp = PatternBindingPower, left-associative** — measured, exact, node-confirmed. Consistent with §3 (it produces the ambiguity, does not resolve it) |
| the cost §3 did not price | that same setting makes the **bare** chain `a => b => c` associate **left** — «(a => b) => c», not the curried «a => (b => c)». **Rule how bare chains associate**, which decides the mechanism |
| the foundation | **land it** — complete, covered, gated. Lookup and function types are additive |
