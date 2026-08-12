# R7b's condition — check one thing first, then one correction and one rule

Three answers, in the order that unblocks him fastest.

---

## 1. Generate the pattern half first — it may be empty today

R7b's pattern half comes from pairs where one pattern is another with words
inserted at the **start of a hole**:

```
  sum of (_)   ->   sum of all (_)
```

`sum of all (_)` is **my example, not a stdlib pattern.** Before deciding any
conditionality, generate the set from the real pattern table. If no such pair
exists today the set is **empty**, the generator is still correct and still
worth shipping, and the whole question defers itself at no cost.

That is the cheapest possible answer and it should be checked before the rest of
this document matters.

## 2. If it is not empty: the condition is broader than "a declared name"

His phrasing — *conditional on the remainder being a declared name, which is what
makes the second reading exist* — is right about what it covers and misses a
case. What makes the second reading exist is the remainder being a **parseable
argument**, and a declared name is only one way to be one.

Covered correctly:

```
  sum of all things   without «all things»   OK  2   sum of all «things»
  sum of all things   with «all things»      TIE -> ERROR
  (with «things» undeclared)                 NO PARSE -> OK   harmless
```

Missed — the remainder is a **pattern call**:

```
  sum of all count of items   without the name           OK 3  sum of all count of «items»
  sum of all count of items   with «all count of items»  OK 2  sum of «all count of items»
```

`count of items` is not a declared name, so his condition does not fire — and
this one is not even a tie. The name is **cheaper**, so it wins **silently**.

Note the name is admitted by R5′ legitimately: `of` precedes the hole in both
`sum of (_)` and `count of (_)`, so it is anchor, not glue, and there is nothing
interior.

> **Restated:** refused when the **remainder resolves as an expression** in the
> namespace the refined hole expects.

That is a resolve of the remainder span, not a symbol-table lookup. And it
covers both halves in one sentence: value language for `sum of all (_)`, type
table for `(_) is a (_)`.

## 3. Which words get conditional and which get blanket — the rule is table volatility

The reason the article and the pattern half should be treated differently is not
arbitrary, and it is worth stating as the rule rather than as two decisions:

> **Condition against a stable table. Go blanket against a volatile one.**

| word | remainder resolves in | table | verdict |
|---|---|---|---|
| `a`, `an` | the **type** table | small, mostly declared up front | **conditional** |
| `not` | value language | volatile | **blanket** (also Budai's call, also R6b) |
| pattern-half words (`all`, …) | the **value** language | large and growing all session | **blanket** |

The cost of conditional is not the check — it is the **re-check**. Conditional
legality depends on the table, so a later declaration invalidates an earlier
name:

```ronin
var all things = ...     // legal: «things» is not declared
var things = ...         // now «sum of all things» has two readings
```

`SCOPING.md`'s convention refuses the declaration that arrives **second** —
which here means refusing `var things`, a far more natural name than `all
things`, with a message about a variable the author may not own. That is the
same worst-shape diagnostic `GLUE-AS-WHOLE-NAMES.md` §2 flagged.

Against the **type** table that barely happens: types are few and declared
early, so the re-check rarely fires and `a number` colliding with type `number`
is a message that explains itself. Against the **value** language it fires all
day.

## 4. So, concretely, for what he is building now

1. **Generate** the pattern half from the pattern table. If empty, ship the
   generator and stop — §2 and §3 become documentation for when it is not.
2. If non-empty, use **blanket** for pattern-half words, and record the reason
   as table volatility rather than as a preference.
3. Keep `a`/`an` **conditional** against the type table, with the condition
   phrased as *the remainder resolves as a type*, not *the remainder is a
   declared type name* — same correction as §2, smaller blast radius.
4. `not` stays **blanket**, unchanged.

And the scheduling note that makes this safe: **narrowing a refusal is
backward-compatible.** Blanket now costs nothing later, and the eventual right
answer for all of these is the differential check from `TIME-TO-LIVE.md` §3 —
no declaration refused, the ambiguous *statement* errors, repaired by a bracket.
That is the same instrument as the import check and should be built once.

Probe: `r7b_condition.py`.
