# Date literals — keep the four-digit minimum, remove the four-digit maximum

**The year needs four digits or more.** The `TODO` is half right: there *is* a
width bug in `Date.Lex`, but it is at the **top**, not the bottom. And the
specification already says so — this is a case of the implementation being
narrower than the written rule, not of the rule needing to change.

---

## §0 — what the spec already says

`docs/spec/lexical-structure.md`, temporal:

```
  date values
      (four digits | a number >= 1000) `-` two digits `-` two digits
```

So the spec's rule is **four digits or more**. `Date.Lex` implements **exactly
four** (`Length` is a `const int` of 10, and the year is four unrolled digit
tests). The guide says a year runs `0 .. 2^57` — eighteen digits. So today,
*almost every legal year is unwritable*: `123456-01-01` does not lex.

That is the defect the `TODO` should be pointed at. Widening downward instead
would take the implementation *past* the spec in the other direction.

## §1 — why the minimum stays: the year field is the only thing labelling itself

Month and day are two digits in every candidate rule. So a four-digit-minimum
year is the one field a reader can identify **by looking at it**:

```
  2026-08-16     one field is four wide -> that field is the year
  0005-01-01     one field is four wide -> that field is the year
  5-01-01        nothing distinguishes the fields
  01-02-03       nothing distinguishes the fields
```

`01-02-03` is the case that decides it. Under a one-or-more rule that is a legal
Ronin date meaning **the second of January, year 1** — and essentially no reader
alive would guess that. It is the third of February 2001 to most of Europe and
the second of January 2003 to most of America. One spelling, three readings, no
cue. That is the hazard this language refuses everywhere else, and it arrives
here for free the moment the year stops being four wide.

Note what the rule is *not* doing: it is not saying year 5 is illegitimate. It is
saying the **literal** must be self-identifying. Year 5 is written `0005-01-01`,
which is the ISO spelling, costs three keystrokes, and appears in real source
approximately never.

**So the minimum costs no expressiveness at all** — every legal year remains
writable. The maximum costs a great deal. That asymmetry is the whole ruling.

## §2 — and it collides with subtraction, in the direction that matters

`-` is an arithmetic operator at binding power 10 (`Runtime/Values.cs`), and
`Symbolic.Parse` hands a bare `-` to the resolver, so an unspaced
`digits-digits-digits` is a well-formed subtraction chain today. Widening the
year takes strings away from arithmetic:

```
  5-10-20     today: 5 - 10 - 20 = -25.   Under a one-or-more rule: a date.
  5-13-01     today: arithmetic.          Under a one-or-more rule: a date.
```

Nothing warns. A number silently becomes a date.

I am not claiming a four-digit year eliminates the collision — `2026-10-31` is
also a legal subtraction. The claim is the one the comma rule already makes, in
`Numeric` and `Separator`:

> «1,234» is one number and «1, 234» is two of something — **which is how the two
> are already written by hand, so the rule makes the reader right rather than
> asking them to learn anything.**

That is the exact test to apply. `2026-08-16` unspaced is how a date is already
written by hand and is not how anyone writes subtraction. `5-10-20` is the
reverse. The four-digit rule keeps the reader right; the one-digit rule makes the
reader wrong on the commoner construct.

## §3 — `5-1-1` is a larger change than the `TODO`, and it is already refused

Worth separating, because the message asked for it as an aside. Relaxing the
*year* leaves `5-1-1` as arithmetic; making `5-1-1` a date requires relaxing
month and day too. That runs into two things already written down:

- the spec says **two digits** for both, unconditionally; and
- `Test/Failure/Dates.cs` maintains `"1984-01-4"` → *not a literal*, named
  "too short".

So one-digit months and days are a deliberate existing refusal, not an
oversight. Leave it refused. Fixed-width month and day also keep the property
that dates sort correctly as text, which is worth having and is silently lost
the moment a field can be one digit.

## §4 — state the year in **digits**, not as "a number"

One trap in the spec's own wording. If the year is lexed as a ***number***, the
digit-separator rule comes with it, and `1,234-01-01` becomes a date with a
comma in it. Rewrite the rule as:

```
  date  ::=  digit{4,} `-` digit{2} `-` digit{2}
```

Four **or more** digits, no separators, no sign. A year is `0 .. 2^57` and never
negative, so a leading `-` is never part of a date — which also means a `-`
immediately before a date literal is always an operator, with no case analysis.

Take the longest digit run for the year, so `12345-01-01` is year 12345 rather
than a failed match that falls back to `12345 - 01 - 01`.

## §5 — a sub-ruling the widths make urgent: shape lexes, range validates

`Date.Lex` checks digit *positions* and never checks that the month is 1..12 or
the day 1..31, so `2026-99-99` lexes as a date today. Whatever the widths, decide
this deliberately, because the wrong answer reintroduces the same hazard:

> **The shape decides what the token is. The range decides whether it is valid.**
> `2026-13-01` is a **date literal with a finding**, not a subtraction.

If an out-of-range field instead fell back to arithmetic, then `2026-12-01` would
be a date and `2026-13-01` a number — the same shape, a different type, and the
only cue is whether the middle field exceeds twelve. That is `01-02-03` again in
a subtler costume. A literal must not change *kind* based on its own value.

## §6 — one thing to keep in view

The spec plans ***date*** followed by ***time*** and a *timezone acronym*. If an
offset is ever admitted instead of an acronym, `-` returns as a discriminator
problem (`2026-08-16T12:30-05:00`), and fixed field widths are the only thing
that keeps that lexable in one token. Another reason not to spend the widths now.

## Summary

| | |
|---|---|
| the ruling | **four digits or more** for the year; month and day stay exactly two |
| the real bug | the **maximum**, not the minimum — the type admits years to 2^57 and the lexer accepts exactly four digits, so almost every legal year is unwritable |
| already written | the spec says *(four digits \| a number >= 1000)*. The implementation is narrower than the spec; the spec is right |
| why the minimum | the year is the **only self-labelling field**. `01-02-03` is a legal date meaning *2 January, year 1* under a one-digit rule, and no reader would guess it |
| what it costs | **nothing** — year 5 is `0005-01-01`, the ISO spelling. Every legal year stays writable |
| the collision | `5-10-20` is arithmetic today and becomes a date. The comma rule's test — *is this how it is already written by hand?* — answers for four digits and against one |
| `5-1-1` | a larger change than the TODO: it needs one-digit months and days, which the spec refuses and `Test/Failure/Dates.cs` maintains as *"too short"*. Leave it refused |
| spell the rule | in **digits**, not "a number" — otherwise digit grouping leaks in and `1,234-01-01` is a date |
| sub-ruling | **shape lexes, range validates.** `2026-13-01` is a date literal with a finding, never a subtraction — a literal must not change kind based on its own value |
| the near miss | where a date is expected and `5-01-01` appears, the finding should **offer the padded form**, not just report a type mismatch |
