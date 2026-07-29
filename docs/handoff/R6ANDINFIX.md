# `otherwise` needs no special case — and R6 has a hole

Addendum to `REAUDIT4-RESPONSE.md`. Checked against `fuzz_verify.py`, not
reasoned from memory.

## `otherwise` cannot be a pattern, so the question doesn't arise

`(_) otherwise (_)` has a **leading hole**, so its anchor run is empty. R6 as
implemented compares anchor runs for prefixes, and the empty tuple is a prefix
of every non-empty one:

```
infix alone            R6 prefix-free: True
infix + one anchored   R6 prefix-free: False   clash = ((), ('send',))
```

So a word-level infix pattern is rejected in any scope containing any anchored
pattern — i.e. every real scope. **R6 already bans word infix and postfix.**
That is consistent with R7: infix belongs to the symbol layer, where an
operator is recognisable without the symbol table.

So `otherwise` is not an exception to "glue must never be lexical". It is not
glue, because it is not a pattern. It is an infix form, infix forms live in the
parser, and that is where it already is. The principle stays exception-free:

- **anchors** — may be lexical; R5 never had jurisdiction there anyway
- **glue** — never lexical; R5 owns it, with typed scoped findings
- **infix** — not a word pattern at all; symbol layer or parser form

## Two defects this exposes

### 1. R6 bans infix emergently, via a comparison with the empty tuple

The ban is correct but accidental, and the diagnostic will be incomprehensible
— it will say an anchor run is a prefix of another anchor run, when the actual
problem is that the pattern begins with a hole.

**Make it an explicit rule with its own finding**, checked before the
prefix comparison:

> A pattern may not begin with a hole. `(_) rounded` is infix; word patterns
> must lead with their name. Spell it `rounded (_)`, or declare a symbolic
> operator.

### 2. R6 does *not* compare two leading-hole patterns with each other

```
infix + postfix        R6 prefix-free: True
```

Both have empty anchor runs, and `len(r1) < len(r2)` is false for two empty
tuples, so neither is checked against the other. If the explicit rule in (1)
lands, this becomes unreachable — but it should be unreachable *by rule*, not
by an accident of a `<` comparison. Worth a test either way.

## The verification gap, stated plainly

`gen_patterns()` in `fuzz_verify.py` only ever emits patterns that start with
an anchor word:

```python
for anchor in ['a', 'b']:
    pats.add((anchor, HOLE))
    ...
```

So the headline result — 2,382,240 resolutions, 0 ties — is verified **over
anchor-first word patterns with no brackets**. It says nothing about:

- leading-hole patterns (moot once rule (1) exists, but currently only
  *emergently* excluded, and never tested);
- bracket-delimited holes, which is what `ZERO-GLUE.md` mechanism 3 needs.

Neither was flagged when the number was reported. It should be quoted with its
scope attached from now on.

## My own seed file is wrong

`patterns.txt` contains `numbers | _ rounded`, which is illegal under R6. I
listed it as "the most expensive shape" when it is in fact a forbidden one.
Corrected to `rounded (_)`.
