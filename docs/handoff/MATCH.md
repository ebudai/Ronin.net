# `match` as a prefixed lookup — the reuse is the good part, and one gap

> **Ledger** — `[R]` `match` as a prefixed lookup — the reuse is the good part, and one gap
> answered by: MATCH-RESULT
> supersedes: none
> superseded by: none

Your sketch:

```ronin
var x = match type of y
[
    number = 3,
    text = 7
] otherwise 0;
```

Grammatically sound, semantically almost free, and it makes exhaustiveness a
*typing* question rather than a separate analysis. One thing it does not reach.

---

## 1. The grammar is already verified

`match (_) [_]` is the same shape as `if (_) {_}` — a free hole followed by a
bracketed one — which `if_expression.py` measured:

```
match (_) [_]     glue = {}      reserves nothing
if    (_) {_}     glue = {}      reserves nothing

match y ( a )              OK   [match «y» «a»]
match type of y ( a )      OK   [match «type of y» «a»]
match a b ( a )            OK   [match «a b» «a»]
match a + b ( a )          OK   [match («a» + «b») «a»]
```

The discriminant is a free hole but the arms are not, so the bracket fixes where
it must stop. A multi-word discriminant is read whole; so is one containing an
operator. `match` is an anchor, so the construct costs **no reserved words**.

## 2. `otherwise` is not new syntax, and that is the best thing here

A lookup miss yields `nothing`. Postfix `otherwise` already catches `nothing`.
So `] otherwise 0` is not match syntax at all — it is the operator the language
already has, applied to an optional.

That is now the *third* use of one word: error recovery, the else-arm of `if`,
and the default of a match. One operator, one meaning — *when the left side
produced nothing, use this instead* — and no new grammar in any of the three.

## 3. Which makes exhaustiveness a typing question

This is the strongest argument for building it your way rather than as a
bespoke construct:

| the arms | the result | consequence |
|---|---|---|
| cover every case | `T` | `otherwise` is **unreachable** — a finding, not a silent no-op |
| miss a case | `optional T` | omitting `otherwise` is a **type error**, at the use site |

Exhaustiveness checking is usually a separate analysis with its own diagnostic.
Here it falls out of `optional` and the existing nothing-propagation, because the
type of a lookup indexed by a closed key set is exactly "the value, if the key is
covered". Nothing new is needed and the two directions are both caught.

## 4. `match` is sugar, and it is worth knowing that

A match is a lookup indexed by a discriminant. With `@`:

```ronin
[number = 3, text = 7] @ (type of y) otherwise 0
```

Same meaning. `match` reads far better and should exist for that reason alone —
but knowing it is sugar constrains the semantics usefully: matching inherits
lookup's rules for free, so a missing key yielding `nothing` and duplicate keys
being an error need no separate specification, and the two spellings can be
tested against each other.

**Order independence comes with it.** A lookup is unordered, so arms have no
fall-through and no first-match-wins. That is a real improvement on a C-style
switch and it should be stated as a property rather than left implicit.

## 5. The gap: payloads

ADTs are algebraic because cases carry data. Your example discriminates a case
with no payload — that is an enum, and a lookup handles it exactly. Destructuring
does not fit, because a lookup's value side has nowhere to put a **binding
occurrence**:

```ronin
match shape [
    circle = ???       // where does the radius go?
    rectangle = ???    // where do width and height go?
]
```

The fix uses machinery that already exists: **an arm is a delegate whose
parameters are the case's payload.**

```ronin
var area = match shape [
    circle    = (radius) => 3.14159 * radius * radius,
    rectangle = (width, height) => width * height,
    point     = 0
] otherwise 0;
```

And here is the payoff from a decision made for an unrelated reason: **reading a
zero-argument delegate invokes it**, so `point = 0` and `point = () => 0` are the
same thing. Constant arms and function arms unify with no special case, because
`DELEGATES.md` already settled that a delegate is read like any other name.

So the rule is one sentence: *an arm is applied to the case's payload; a case
with no payload takes a constant.*

## 6. Two things to settle before it is built

**a. `type of y` is not the same question as an ADT case, and conflating them
will hurt.** Matching on a *type* is runtime type dispatch over an open
universe — no set of arms is ever exhaustive, so the result is always
`optional T` and `otherwise` is always required. Matching on an ADT *case* is a
closed set, which is where §3's exhaustiveness lives. Both are useful and they
should look the same at the call site, but the compiler has to know which it is,
and only one of them can ever drop the `otherwise`.

If sum-type cases are themselves types, the two coincide and this is moot — but
that is a decision, not a given, and it is worth stating either way.

**b. Duplicate keys are a finding.** `[number = 3, number = 7]` should be
rejected at the literal, in the same family as the tie rule: two readings, no
basis to choose, so refuse rather than pick.

## 7. What I would not add

**Guards.** `match x [ n where n > 5 = … ]` does not fit a lookup, and it does
not need to — arms are expressions, so `if` inside an arm covers it:

```ronin
match kind [ number = if x > 5 { "big" } otherwise { "small" }, text = "words" ]
```

Adding guard syntax would put a second conditional mechanism beside `if` for no
new power. Worth deciding now, because guards are what every pattern-matching
design accretes and it is easier to decline once than to remove later.
