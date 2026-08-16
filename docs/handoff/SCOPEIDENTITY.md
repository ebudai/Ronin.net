# Scope identity for a named type — the anonymous-scope case, to pin or overrule

> **Ledger** — `[R]` Implementing `NAMEDIDENTITY`'s «(declaring scope, name)». One
> thing that ruling did not pin turns on a type-scoping semantics I should not decide
> alone: what identifies an ANONYMOUS scope as a stable value. My default is A below;
> you may prefer to overrule.
> answered by: SCOPEIDENTITYRULING
> supersedes: none
> superseded by: none

**From:** the successor, at `1ae081e`.

`NAMEDIDENTITY` rules a named type's identity to be «(declaring scope, name)» — a
value, stable under edits, and explicitly **not** the span, because a span moves
when a line is added above the declaration. That is clean for a scope with a name:
a function or a type is identified by its name, which survives a line added above
it. But the identity has to be a value for **every** scope a type can be declared
in, and I probed that some of them have no name.

## What the tree does today (probed at `1ae081e`)

A `type X;` resolves in every scope, and types are **block-scoped**:

| declared in | result |
|---|---|
| a module, a function body, a type body | a distinct type |
| an anonymous block, a loop, a delegate, a «when» | a distinct type |
| the SAME scope, twice | «Shadowed» — the existing duplicate rule refuses it |
| two SIBLING blocks, same name | **two distinct types**, and clean |

The last row is the one that forces the question. `function f { { type token; … } {
type token; … } }` declares two **distinct** opaque types, clean because the two
blocks are two scopes. So the identity must tell them apart — and a block has no
name to tell them apart *with*.

## Why the nameless scopes are the hard case, and what it costs

A named scope's token is its name, stable when a sibling is added above it. An
anonymous scope's only distinguisher is **positional** — its ordinal among sibling
scopes — and an ordinal shifts when a sibling block is inserted above it. That is
the one edit that breaks it, and the cost is bounded: the «(function,
instantiation)» cache *misses* for that type's instantiations after such an edit —
a recompute, never a wrong answer. It is a weaker guarantee than a name gives, on a
rarer edit than the line-addition that disqualified the span.

## The options

**A — mixed path (my default).** The scope token is the name for a named scope and
the sibling ordinal for an anonymous one. It distinguishes every scope, including
the two sibling blocks above. No language change; block-scoped types stay
block-scoped. The anonymous ordinal is stable under a line-addition and shifts
under a sibling-block insertion — the bounded cache-miss cost above.

**H — the scope is the nearest named container.** Redefine a type's "scope" as its
nearest enclosing **named** container — module, type, or function — and extend the
«Shadowed» rule so a type name is unique within that container, across its anonymous
sub-scopes. Then «(container, name)» is fully stable (the container is named) and
unique **by the rule** — the framing you used for why «(scope, name)» cannot
collide. The cost is a semantics change: two same-named types in sibling blocks of
one function become «Shadowed», where today they are two distinct types. Type
naming becomes container-scoped, not block-scoped.

**E — refuse a type in an anonymous scope.** Allow «type X;» only in a module, a
type body, or a function body, and refuse it in a block, loop, delegate, or «when».
Every remaining scope is named, so the identity is fully stable and the simplest of
the three. The cost is a new refusal for source that compiles today.

**C — rejected, recorded so it is not re-proposed.** Treat anonymous scopes as
transparent — a type belongs to the nearest named scope, no extra rule. This
reintroduces exactly the finding-1 bug one level in: the two sibling-block
«token»s collide though they are distinct types.

## Recommendation, and why it is yours

A is the no-change default and is always **correct**; its only weakness is a cache
miss on a rare edit. But be aware of what A gives up: it meets your stability bar
less than fully, and its anonymous ordinal is *asserted* rather than
*unique-by-a-rule* — and stability and unique-by-a-rule are the two properties you
chose «(scope, name)» **for**. H satisfies both, at the price of making type naming
container-scoped. E sidesteps the question by keeping types out of nameless scopes
entirely.

Which is right is a decision about what a type's **scope** is — the block it sits
in, or the named thing that contains it — and whether a type may live in an
anonymous scope at all. That is a language-semantics call, not an implementation
detail, so I am bringing it to you rather than taking A silently. If it helps
weight it: I have not seen a fixture that declares a type inside a block or a loop,
so E and H cost little in practice and A's weakness is rarely exercised — which
also means A's cache miss is rare, so all three are cheap and the choice is about
what you want the language to *mean*.

## What I need

Approve A, or rule H, E, or another shape. Finding 3 — a signature carrying its
resolved sorts, so «use (x => number)» and «use (x => (number))» are one
`DuplicateSignature` — is independent of this, and I will build it in parallel
whichever way you rule here.
