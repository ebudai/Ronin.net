# The two sweep items — one is my fault, one is a bug the reference settles

> **Ledger** — `[R]` The two sweep items — one is my fault, one is a bug the reference settles
> supersedes: not yet checked
> superseded by: not yet checked

---

# 0. First: `<` and `>` are **not** syntax, and never were

Budai asked, which is itself the finding.

`<_>` is **my display notation**, invented in `bracket_probe.py` so that
`pat_str` could show a pinned hole distinctly from a free one:

```python
'(_)' if s is HOLE else '{_}' if s is BHOLE else '<_>' if s is THOLE else s
```

Nothing in Ronin uses angle brackets. A loop variable is a bare word, and a
multi-word one is bracketed with the ordinary `( )`:

```
for each bank in banks
for each (in flight order) in orders
```

That is the whole of it. **But the notation has escaped** — it is in
`reserved-words.txt`, `Pattern.Parse` is being asked to read it, and the
designer had to ask whether it was a language feature. I flagged this shape two
messages ago as a thing to watch and then did not act on it; it has since
become a real question in two places.

**Immediate fix, independent of any design call:** the registry must not render
internal notation as though it were source. Either render the user-facing
declaration form, or render hole kinds as a separate column with words —
`free`, `braced`, `pinned` — that nobody could mistake for something to copy.

---

# 1. `Pattern.Parse` — the defect is the silence, not the missing feature

Reading `<_>` and `(_)` as literal word segments produces a different pattern
that **renders identically**. That is the same shape as the `Numeric.Lex` bug
from months ago: *the token text and the text the scanner approved were
different strings, silently.* It was the worst bug in that sweep and this is its
twin.

So the fix is not "teach `Parse` the notation". It is:

> **`Pattern.Parse` must round-trip or reject.** Anything it cannot represent
> is a finding, never a silently different pattern.

And the test is a property, not a case:

```
for every pattern the registry can emit:
    Parse(Render(p)) == p        or    Parse(Render(p)) produces a finding
```

That test is correct under either answer to the design question below, so it can
go in first.

## The design call it forces: should braced and pinned holes be user-declarable?

**Yes — both.** The governing principle is already stated: *user declarations
are grammar productions*. If the standard library can declare a hole kind that
users cannot, builtins become privileged and that principle is decoration. It
matters concretely too: braced holes are the **free** shape, so restricting them
to the stdlib would mean every user-declared construct pays reserved words that
builtins do not.

### Syntax: let the declaration mirror the call site

The rule to hold onto is that **a declaration shows what the call looks like,
with argument names in place of arguments.** That is already true of the free
hole:

```
declare   send (message) to (recipient)
call      send x to y
```

So the braced hole needs no new notation at all — it is the bracket that will
appear at the call site:

```
declare   if (condition) {body}
call      if x { … }
```

The pinned hole is the one that needs thought, because at the call site there
are no brackets — `for each bank in banks`. Its defining property is not really
"one token"; it is **that the argument is a binding occurrence rather than a
value**, and one-token-unless-bracketed is the rule for new names that we
already settled. So it wants a marker for *declaration*, not a bracket:

```
declare   for each (new item) in (collection)
call      for each bank in banks
          for each (in flight order) in orders
```

Then two independent choices compose: the bracket style picks free-vs-braced,
and a `new` marker inside picks binding-vs-value. Nothing new is invented; both
halves already exist.

**Confidence, stated plainly:** the round-trip requirement above is measured —
the reference resolver and the probe both agree on what the hole kinds mean.
The *declaration syntax* is reasoning only. My probes model resolution, not
declaration parsing, so nothing here has been run. Treat §1's syntax as a
proposal to argue with, and §1's round-trip test as settled.

---

# 2. `return 1 return 2` — the reference rejects it, so this is a bug

Not a design question. I ran it both ways:

```
dp_resolver     return 1 return 2      NO PARSE
dp_resolver     return a return b      NO PARSE
probe atom      return 1 return 2      NO PARSE
probe expr      return 1 return 2      NO PARSE
```

Both engines, both trailing-argument policies — including `expr`, the permissive
one we settled on, where an unbracketed trailing argument reaches as far as a
full expression. It still cannot reach across a second `return`, because there
is no juxtaposition rule that would let `1 return 2` be an expression.

So the C# accepting it is a divergence from the reference, and the reference is
right. Worth knowing what it is building before fixing it — his instinct to stop
short was correct — but the verdict is not in doubt.

### The principle to write into the spec while fixing it

> **Statement boundaries are structural, not resolved.** A block is split into
> elements on `;` and on `}` *before* resolution. The resolver is then handed one
> element and either resolves it or fails.

If the resolver can accept two statements' worth of text as one element, then
minimum-lookup is doing statement-splitting implicitly — and that is a much
worse property than the specific bug, because it means the number of statements
in a program depends on what names are in scope.

Note that `var a => Number var b => Number;` failing is *not* evidence the rule
holds. Declarations fail for their own reasons. The `return` case is the one
that tests it, and it failed.

One check worth adding while in there: `return return 1` **does** resolve, in
both engines, at 2 lookups. That is correct — `return` takes an expression and a
`return` expression is one — but it is worth a deliberate test either way, since
under `IF-AS-EXPRESSION.md` more statements are becoming expressions and the
question of which ones nest will come up again.
