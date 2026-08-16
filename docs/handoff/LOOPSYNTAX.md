# Loop syntax — resolved

> **Ledger** — `[V]` Loop syntax — resolved
> supersedes: not yet checked
> superseded by: not yet checked

**Decision: `for each bank in banks`.** The documented spelling wins. Ship it.

Your instinct that it "should only resolve to one thing" is correct, and it is
correct for a stronger reason than uniqueness-by-luck: **R5 already makes it
unique by construction.** But the reasoning matters, because the programmer's
caution was well-founded — the failure mode he was avoiding is real, it is
worse than he thought, and R5 is the only thing standing between you and it.

Everything below is from `loop_syntax.py`, which runs against `dp_resolver.py`
and passes 7/7. Run it rather than trusting this file.

---

## 1. The loop is a pattern, and its glue is `in`

```
for each (_) in (_)      anchor run = «for each»      glue = {in}
```

Anchor run is the words before the first hole. Glue is the words after it.
That distinction is the whole of this document.

---

## 2. Why the caution was justified: the hazard is silent, not a tie

The instinct was "a name with `in` in it makes the split point ambiguous." The
mechanism is right. The consequence is worse than ambiguity, because **the
competing readings do not tie — one is strictly cheaper, so nothing complains.**

```
for each order in transit in count of banks
```

with an innocent variable named `transit in count of banks` declared somewhere
else, for some other purpose, possibly in another file:

```
R5 off, name present   OK, 3 lookups   for each «order» in «transit in count of banks»
R5 off, name absent    OK, 4 lookups   for each «order in transit» in count of «banks»
```

No tie. No error. **A different program.** This is exactly the
`send hello to alice` case that forced R5 into existence — a longer name always
costs fewer lookups than the call it swallows, so minimum-lookup actively
prefers the swallowing reading.

Note that the loop variable is a *declaring* hole, so it resolves no matter
what is in scope. That makes the competing reading stronger, not weaker: the
`for each «order in transit» in …` reading is always available, and only loses
because the other one got cheaper.

## 3. Why R5 closes it structurally

> **R5** — A multi-word name may not contain any word appearing after the first
> hole of any in-scope pattern.

With glue `{in}`, both competitors are rejected at their declaration sites:

| candidate | fate |
|---|---|
| `transit in count of banks` | rejected — multi-word name containing `in` |
| `order in transit` | rejected — and it is a *loop variable*, so the error lands on the loop |

The statement then has **no reading at all** (`NO PARSE`), which is the right
outcome: it was never writable.

And this generalises. Under R5 the only `in` that can appear in a loop header
is the pattern's own glue, because

- the loop variable is a declared name, and multi-word ones cannot contain `in`;
- any name inside the collection expression is under the same rule;
- the only pattern with `in` after a hole is `for each (_) in (_)` itself, and
  it needs its own `for each` anchor to appear.

One `in` → one split point → one reading. There is no competing reading to
tie-break. That is why `iterate banks => bank` is not needed: it was buying, at
the cost of readability, protection that R5 already provides.

**But this is conditional.** It holds only if R5 is genuinely enforced,
including on loop variables. If R5 is not implemented yet, `for each … in …`
is not safe to ship, and the audit finding should stay open until it is.

---

## 4. What it costs: `in` becomes reserved inside multi-word names

This is the real price and it should be paid with open eyes. The moment
`for each (_) in (_)` is a builtin — i.e. in scope everywhere, always — these
become illegal declarations:

```
in flight order       in progress tasks      opt in list
logged in user        in memory cache        sign in token
built in defaults     in stock items         check in time
```

All have decent renames (`pending tasks`, `current user`, `default settings`),
and R5's diagnostic lands at the declaration with both sites named. But the
shape of the cost is worth stating: **every program pays it, forever, so that
one pattern reads well.**

I think that is the right trade — readability is the language's first
principle, and `for each bank in banks` is the readable spelling by a wide
margin — but it is a trade, not a free win.

**Single-word `in` is still legal.** R5 only examines multi-word names, so
`var in => number` passes. Whether that is desirable is a separate call; see
§7.

---

## 5. R6 and the word `for`

R6 (leading runs prefix-free) rejects `for (_)` beside `for each (_) in (_)`,
because `for` is a prefix of `for each`. Implement that as stated — blanket,
cheap, checked once at scope entry.

But record the finding, so it does not get mis-remembered: I went looking for a
statement where `for (_)` could actually swallow a loop header, and **there
isn't one**, because swallowing needs a name spanning `… in …` and R5 already
banned those. R6's rejection here is *conservative*, not load-bearing. So
`for each` does not foreclose a future `for (_)` on ambiguity grounds — only on
R6's current strictness, which could be refined if that spelling is ever
wanted.

---

## 6. The general rule this is an instance of, which matters more than the loop

**Glue words are reserved words. The standard library's glue set is the
language's reserved-word list, and it grows every time a pattern with a word
after a hole is added — retroactively invalidating user names**, which is
already noted as the module-composition hazard.

The lever is that **a pattern whose words all precede its first hole has an
empty glue set and reserves nothing:**

```
sum of (_)                anchor = «sum of»              glue = {}
count of (_)              anchor = «count of»            glue = {}
compute total for (_)     anchor = «compute total for»   glue = {}
for each (_) in (_)       anchor = «for each»            glue = {in}
send (_) to (_)           anchor = «send»                glue = {to}
repeat (_) times          anchor = «repeat»              glue = {times}
```

So the stdlib design rule: **put the words first.** Reach for word glue only
where readability genuinely demands the interleaving, and treat each instance
as a deliberate reserved-word decision with a review attached, not a style
choice. `repeat (_) times` costs you `times` — "response times", "wait times",
"boot times" — for an ergonomic gain that is much smaller than the loop's. That
one is worth re-spelling.

A running list of glue words, in one place, with the pattern that caused each
one, belongs in the repo. It is the reserved-word list, and right now nothing
writes it down.

---

## 7. Open calls for you

**a. Reserve `in` outright, or only inside multi-word names?**
R5 as written permits single-word `var in`. Banning it too is one line, makes
the rule easier to explain ("`in` is reserved"), and costs almost nothing since
nobody names a variable `in` on purpose. I lean toward banning it, but it is
your call and either is defensible.

**b. Loop index.** `for each bank at index i in banks` would add `at` and
`index` to the reserved set — expensive. Cheaper, and consistent with the
`old x` precedent: **inject `index` as a name inside the loop body.** Single
word, so R5 never examines it, and no new glue. The cost is the same one `old`
has — it collides with a user's own `index` under no-shadowing, and the
diagnostic must name the loop rather than the generated name.

**c. `for each` returns nothing / is a statement.** Keep it that way. If a loop
were expression-valued you could nest two headers and get two `in`s in one
statement. R5 still saves you (the outer variable would have to span an `in`),
but the reasoning gets subtle for no benefit.

---

## 8. Implementation checklist

1. Spell the pattern `for each (_) in (_)`; delete `iterate (_) => (_)`.
2. **Enter the loop variable into the symbol table as a declaration, and run R5
   on it.** This is the one that is easy to miss — a loop variable is a
   declaration site like any other, and if it skips the R5 check the whole
   argument above collapses.
3. R5's glue set must be computed from *all* in-scope patterns including
   builtins, so `for each` contributes `in` in every scope.
4. R6 check at scope entry rejects any other pattern whose leading run is a
   prefix of `for each`, or vice versa.
5. Diagnostic for a loop variable that fails R5 — primary span on the variable,
   related span on the pattern that made the word glue:

   > «order in transit» cannot be a name: «in» is pattern glue from
   > «for each (_) in (_)». Rename the loop variable.

### Tests

| # | case | expect |
|---|---|---|
| 1 | `for each bank in banks` | resolves, unique |
| 2 | `for each open order in banks` | multi-word loop variable, no `in`, fine |
| 3 | `for each bank in count of banks` | unique; collection is a pattern call |
| 4 | `var in flight order => number` | R5 rejection at the declaration |
| 5 | `for each in flight order in orders` | R5 rejection **on the loop variable**, span on the variable |
| 6 | `var transit in banks => list` then any loop | R5 rejection at the var, not at the loop |
| 7 | `var in => number` | passes today — pin whichever answer §7a picks |
| 8 | pattern `for (_)` added to scope | R6 rejection at scope entry, both patterns named |
| 9 | `for each order in transit in count of banks` | `NO PARSE`, and the message should be about the unresolvable collection, not a tie |

Test 9 is the regression guard for this whole document. If it ever starts
resolving, R5 has been weakened somewhere.
