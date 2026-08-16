# `function ping ()` — reject it, and the reason gives you `(_)` for free

> **Ledger** — `[R]` `function ping ()` — reject it, and the reason gives you `(_)` for free
> supersedes: not yet checked
> superseded by: not yet checked

REAUDIT9 finding 3. Budai's read is that it is either ill-formed or already
defined. **Ill-formed** — and working out why produces a small piece of syntax
that fixes half of the `Pattern.Parse` round-trip problem at the same time.

---

## Why not "already defined"

Treating `function ping () { … }` as a second spelling of `function ping { … }`
looks harmless and is not, for one reason that outweighs the others:

**It would make the call site surprising.** Someone who writes `function ping ()`
is importing a C habit, and the next thing that habit produces is `ping()` at
the call site — which cannot work, because `ping` is a plain name and is called
`ping`. Accepting the declaration buys one moment of familiarity and sets up a
worse surprise immediately after. Rejecting it puts the correction at the
declaration, where the author still has the model in mind.

The lesser reasons: a language that trades writability for readability gains
nothing from a second spelling of the same declaration; and it would establish
that empty brackets are erasable, which invites `send () to ()` and a rule
nobody wants to write.

## Why it is ill-formed, precisely

In Ronin a bracket in a declaration marks **a hole**, not a parameter list:

```
declare   send (message) to (recipient)
call      send x to y
```

`(message)` is one hole with one name. So `()` is *a hole with no name* —
zero-width, referring to nothing. It is not "an empty parameter list", because
**Ronin has no parameter lists.** That is the whole content of the finding, and
it is what the diagnostic should say:

> «()» is an empty hole. Ronin has no parameter lists — a bracket in a
> declaration marks one argument. A function with no arguments is declared
> «function ping { … }».

The message has to explain the model, not just report a syntax error, because
the author's mistake is a wrong model rather than a typo.

---

## What falls out: `(_)` should be legal, and is not the same thing

There *is* a real use for an unnamed hole — an argument the body does not need:

```
send (_) to (recipient)
```

That is different from `()`. It is a hole with a name the author has declined to
use, which is `_` — the same convention as elsewhere, and legal as an ordinary
identifier.

So the rule is three-way and each case is distinct:

| form | meaning |
|---|---|
| `(name)` | a hole, referable as `name` |
| `(_)` | a hole, deliberately unreferenced |
| `()` | ill-formed |

**And this closes half of the last finding.** `SWEEP-ITEMS.md` §1 asked for
`Pattern.Parse` to round-trip or reject the registry's own rendering. The
registry renders a free hole as `(_)` — which, with this rule, becomes genuine
source syntax rather than notation. So free-hole patterns round-trip with
nothing invented. Only the braced and pinned kinds still need the design call.

---

## The theme, which is now three findings long

Three in a row have the same shape:

| | accepted | built |
|---|---|---|
| `Pattern.Parse("… <_> …")` | yes | a different pattern that renders identically |
| `return 1 return 2` | yes | something other than two statements |
| `function ping ()` | yes | `ping (_)`, which no call satisfies |

Each time the compiler accepted the text and constructed something other than
what the author wrote, with no finding. That is not three coincidences, it is a
missing invariant, and it is worth stating as one:

> **Every declaration either means what it says or produces a finding.** There
> is no third outcome in which it means something else quietly.

The `Pattern.Parse` property test is one instance. The right generalisation is
to apply the same test to every declaration form the compiler can render:
declare it, render it, re-parse it, and assert the result is equal or a finding.
That would have caught all three of these before an audit did.
