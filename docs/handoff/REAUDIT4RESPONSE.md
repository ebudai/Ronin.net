# REAUDIT4 — designer decisions

> **Ledger** — `[V]` REAUDIT4 — designer decisions
> supersedes: not yet checked
> superseded by: not yet checked

Three items need me. Two are yes, one is a reversal of my own advice, and the
reversal is the reason the other two got hard.

---

## 0. What I got wrong

`LOOP-SYNTAX.md` §7a said:

> Banning [single-word `in`] too is one line, makes the rule easier to explain
> ("`in` is reserved"), and costs almost nothing.

That was wrong in the way that matters: I said *what* to enforce and never said
*where*, and "one line" was true only of the rule, not of the mechanism. Moving
`in` into the lexer is what produced

- finding 1's bug class (token identity depending on whether punctuation
  follows),
- finding 2's regression (`var ready if needed` stops compiling),
- and the loss of the typed R5 diagnostic on checklist case 5.

The programmer implemented what I asked for. The consequences are mine.

**And the rule has no safety content at all.** I checked, rather than assuming:

```
for each bank in in              OK  3   for each «bank» in «in»
for each in in in                OK  3   for each «in» in «in»
for each bank in count of in     OK  4   for each «bank» in count of «in»
```

Every one resolves uniquely. A single-word name `in` cannot capture anything —
capture needs a *multi-word* name straddling a hole, which is R5's job and R5
already does it. So banning single-word `in` is a **legibility** rule, not a
correctness rule. That is the whole reason it must not be paid for in the
lexer.

---

## Decision A — `in` is enforced in the symbol table, not the lexer

**Reverse it.** Take `in` out of `Lexicon.Keyword`. Enforce it as an ordinary
declaration check: *a name may not be exactly a glue word of an in-scope
pattern*, producing a typed finding that names the pattern.

What this buys:

| | |
|---|---|
| checklist case 5 | gets its typed R5 diagnostic back — `for each in flight order in orders` names the loop variable with the loop pattern as a related span, instead of `expected name` |
| finding 2 | the `in`-at-every-position special case disappears entirely; `Name.Parse` narrows to the auditor's recommendation with nothing added |
| finding 1 | stops being a *policy hole* for `in` (it remains a real bug for the other keywords — see below) |
| R5/R6 | stay the single mechanism for name/pattern conflicts, rather than one of two that must agree |

**The decisive argument is reversibility.** We have said explicitly that
`in` is likely to stop being reserved: if the pinned-declaring-hole refinement
in `ZERO-GLUE.md` survives the fuzzer, `for each bank in banks` becomes safe
with nothing reserved at all. A symbol-table rule is deleted in an afternoon. A
lexer keyword is a tokenizer change with every downstream assumption hanging off
it. **Do not spend a lexical reservation on a rule we are actively trying to
remove.**

---

## Decision B — finding 3: accept, with two conditions

**Accept.** `for (_)` stays a legal user pattern. The checklist item was mine
and it was over-conservative — and I said so in the same document that demanded
it:

> I went looking for a statement where `for (_)` could actually swallow a loop
> header, and **there isn't one** … R6's rejection here is *conservative*, not
> load-bearing.

`loop_syntax.py` §7 is that evidence. The programmer's refinement lands exactly
where my own probe said the boundary was. Test 8 should be rewritten to assert
`for (_)` **is** accepted, with a comment pointing at that probe.

I also accept the framing: R6 defined over lexer-token segments is coherent,
and `"for"` is genuinely not a segment prefix of `"for each"` under that model.

Two conditions attached, neither large:

**B1 — normalise whitespace inside multi-word keywords.** `for  each` and
`for<TAB>each` must be the same keyword. Right now user names are
whitespace-insensitive word sequences and builtin anchors are not, which means
one construct in the language behaves differently from every other for reasons
invisible on screen. That is a bug report we will get exactly once from every
user, and it is a `Split`-and-rejoin away.

*Budai confirms `for<any non-zero whitespace>each` was always the intent, so
this is a defect against the design, not a change to it. The keyword spelling
currently contains a literal single space.*

**B2 — R6 must still be enforced between *user* patterns.** The case the fuzzer
found — `b (_)` beside `b b (_)`, three lookups either way, no name involved —
has nothing to do with builtins and is not fixed by anything in this audit.
Confirm there is a test for it at token level.

---

## Findings 1 and 2 — no designer decision needed, both are straight fixes

**Finding 1.** The auditor is right and the recommendation is right: a keyword
boundary must use the same continuation rule as `Word.Lex`. Build the
punctuation matrix as specified. Note that Decision A removes `in` from that
matrix but not the bug — `if`, `while`, `when`, `function` have it too, and
`var if=>Number` compiling is the same defect.

**Finding 2.** Also right. Restore position sensitivity: production-announcing
keywords rejected at the first word only, other keywords legal thereafter. With
Decision A there is no `in` clause to add — the rule reverts exactly to what
`48a75d6` intended, and `var ready if needed => Number` compiles again.

---

## The general principle worth writing down

**Every word moved into `Lexicon.Keyword` is a word removed from R5 and R6's
jurisdiction.** The lexer reserves unconditionally, produces untyped
"malformed input" messages, and cannot be scoped or imported. R5 and R6 are
scoped, typed, and can name the pattern responsible.

For a language whose error messages are the teaching mechanism, the direction of
travel should be *fewer* lexical keywords over time, not more.

**Where the line falls — this is the rule, and it has no exceptions:**

| kind | may it be lexical? | why |
|---|---|---|
| **anchor** — words before the first hole | yes | R5 never had jurisdiction; anchors reserve nothing, because no name can straddle a word matched before any hole opens |
| **glue** — words after a hole | **never** | glue is exactly what R5 governs; lexicalising one bypasses the rule, loses the typed message, and makes it unscopable and un-importable |
| **infix** — a pattern with a leading hole | not a pattern at all | R6 already rejects these; infix belongs to the symbol layer (R7). See `R6-AND-INFIX.md` |

So `for each`, `if`, `while`, `when`, `function` may stay lexical. `in` and
`then` must not. `otherwise` is the third row, not an exception — it is an infix
form and infix forms live in the parser.

**There is a floor, and it is set by diagnosis rather than by parsing.**
Statement introducers earn their keep because the compiler must know a statement
is an `if` *before* resolution to say "the `if` has no body" instead of "no
reading". Driving builtins all the way into the prelude as ordinary patterns
would make that message worse, which is the one thing we are least willing to
trade. "Fewer keywords" is the direction; zero is not the target.

I had written here that 539 tests at 100% coverage was not worth disturbing over
an architectural preference. Budai's correction, which I accept: 100% coverage is
the instrument that makes the refactor safe rather than the thing being
protected — pulling a mechanism out shows up immediately as newly-dead code and
as tests that stop being exercised, which says what the mechanism was actually
doing rather than what we thought. So the constraint is not appetite for
refactoring. It is the diagnosis floor above.

---

## Summary

| item | decision |
|---|---|
| 1. keyword boundary | fix as recommended; no designer input needed |
| 2. anywhere-in-name restriction | narrow as recommended; the `in` clause disappears under A |
| 3. one-token `for each` / R6 refinement | **accept**; checklist item 8 was my error, rewrite test 8 to assert acceptance |
| B1 whitespace normalisation | required |
| B2 user-pattern R6 test | confirm it exists |
| §7a outright `in` reservation | **withdrawn** — enforce at declaration, not in the lexer |
| case 5 diagnostic | must be the typed R5 finding again; that is the point of A |
| `Progress.cs` stale comments | agreed, and they should also stop describing the loop decision as open |
| explicit no-leading-hole rule + finding | **new**, from `R6-AND-INFIX.md`; R6 bans infix only emergently today |
| R6 comparison of two leading-hole patterns | **new**; currently unchecked, unreachable once the rule above lands |

Read `R6-AND-INFIX.md` alongside this. It settles `otherwise` without a special
case, and it carries one correction that affects how the grammar work is quoted:
the 2,382,240-resolution zero-tie result covers **anchor-first word patterns
with no brackets only** — `gen_patterns()` never emits a leading hole. The
number stands; its scope was never stated.
