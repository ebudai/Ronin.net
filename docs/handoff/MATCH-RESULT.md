# `match` — §2 already works, §1 is verified for a hole kind that cannot be declared

> **Ledger** — `[R]` `match` — §2 already works, §1 is verified for a hole kind that cannot be declared
> measured at: 4882635

Answering `MATCH.md` and `match_shape.py`. Nothing implemented; this is what the
compiler says about the claims, measured.

## 1. §2 holds, and it is already built

`otherwise` composes after a pattern call exactly as §2 needs, with the pattern
`match (_)` declared:

```
match y otherwise 0     Resolved   (match «y» otherwise 0)
```

The fallback guards the **call's result**, not its last argument. That is worth
naming because it was wrong three weeks of work ago and was `FRESHAUDIT4`
finding 3: `otherwise` sat above the pattern binding power, so `parse input
otherwise standby` read as `parse («input» otherwise «standby»)` — a fallback on
the argument, then the call made with it, silently. It binds below calls now, so
§2's third use of the word costs nothing and needed nothing.

§2 is the strongest claim in the document and it is the one already true.

## 2. §1 is verified for something the language cannot declare

`match_shape.py` models the arms as `BHOLE`, a bracketed hole, and `Pattern`
does not have that kind:

```
Pattern.Parse("match _")          ->  match (_)
Pattern.Parse("match _ [_]")      ->  ArgumentException: not words and «(_)» holes
Pattern.Parse("match _ {_}")      ->  ArgumentException: not words and «(_)» holes
Pattern.Parse("match (_) [_]")    ->  ArgumentException: not words and «(_)» holes
```

A declaration is words and free holes. `EMPTYBRACKETS.md` says so and names the
gap in its own last line — *"Only the braced and pinned kinds still need the
design call"* — and this is that gap, arriving as a prerequisite rather than a
refinement.

**`if (_) {_}` did not need it.** `if` is a keyword production in the parser, so
the brace is grammar and never a hole in a declared pattern. That is why the
`if` shape could be measured and then built while the hole kind stayed
undesigned. `match` as described is a *pattern*, so it cannot borrow that.

So §1's "the grammar is already verified" is true of the shape and not of the
spelling. Two ways forward, and they are different projects:

- **design the bracketed hole kind**, which is the open `EMPTYBRACKETS` item and
  buys `match` plus every other construct that wants a delimited argument; or
- **make `match` a keyword production**, which is the route `if` took and works
  today — a heading ends at the brace that opens its body, so `match y { … }`
  would parse with no new hole kind and no reserved words.

The second is cheap and available now. It costs `match` as a word, which the
first does not, and it makes `match` grammar rather than a library pattern —
which may be the wrong trade for something the document rightly calls sugar.

## 3. The arms are written in a syntax the language does not have

Every example writes the arms as `[ number = 3, text = 7 ]`. Measured:

```
var x = { number = 3, text = 7 };    parses          — Lookup, §4.6.4
var x = [ number = 3, text = 7 ];    Malformed
var x = [ 1, 2 ];                    parses          — Indexer, §4.6.5
```

A lookup is **braced** and an indexer is **bracketed values with no `=`**. §4's
`[number = 3, text = 7] @ (type of y)` has the same problem. The argument that a
match *is* a lookup is right and is the best structural point in the document —
it just has to be spelled `{ }`.

`match_shape.py` cannot catch this: `BHOLE` is an abstract bracketed hole and
the probe never distinguishes `[` from `{`. Worth a note in the script, because
the shape result is real and the spelling it appears to endorse is not.

## 4. §3 and §5 are blocked on things that do not exist

**§3, exhaustiveness as typing.** There is no type checker. `optional` is a
modifier that parses, is stored on the declaration, and is read by nothing;
there is no `Finding` for a type error and no phase that could raise one. The
argument is good and the mechanism is absent, so "nothing new is needed" is true
only relative to a type system that has not been started.

**§5, an arm is a delegate.** The payoff rests on reading a zero-argument
delegate invoking it. The spec says so (§4.8.2); I have not verified the
implementation, and it is worth checking before the unification is relied on,
because "constant arms and function arms unify with no special case" is exactly
the kind of claim that is true in the spec and absent in the runtime.

## 5. §7, declining guards, is right and worth taking now

Deciding once not to have guards is cheaper than removing them, and the
replacement — `if` inside an arm — is real as soon as `if` is an expression.
That is `IFASEXPRESSION.md` §4, still unbuilt, and it is now load-bearing for
two documents rather than one.

## 6. Ordering, if it helps

§2 needs nothing. §3 needs a type system. §5 needs a check. §1 needs one of two
decisions. Of everything here the cheapest real progress is `if` as an
expression, which §7 depends on, `IFASEXPRESSION` §4 specifies, and
`BRACEDECISION.md` is said to settle — and that document is still not in the
folder.
