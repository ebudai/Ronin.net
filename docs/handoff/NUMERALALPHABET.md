# The source numeral — ASCII digits, one convention; cultures at runtime

> **Ledger** — `[V]` verdict, decided with the designer. A source numeric literal is ASCII `0-9` with `,` grouping and `.` decimal — the `12,345,678.99987` form the lexer already reads. Source carries ONE convention, which resolves the locale ambiguity by fiat; culture-aware parsing is a RUNTIME operation keyed on an explicit locale. The lexer becomes the authority for the digit alphabet.
> supersedes: none
> superseded by: none

**Decided in session with the designer**, from the REAUDIT64 finding-4 note: the
lexer admitted every `char.IsDigit` (Unicode decimal digits — `١` is one) while the
evaluator's invariant reader took only `0-9`. This settles which of the two is the
authority.

## The decision

- **Source alphabet: ASCII `0-9`.** No Unicode decimal digits in a literal. The lexer
  enforces it — `Numeric` is `0-9`, not `char.IsDigit`.
- **Source form: `12,345,678.99987`.** `,` groups the integer part, `.` is the decimal
  point. This is the form the lexer already reads (`7,000,876` → 7000876); nothing here
  changes it.
- **One convention in source, cultures at runtime.** A `number of (text)` /
  culture-aware parse is a runtime operation with an **explicit** locale argument — that
  is where `12.443,44929` (European) becomes a value.

## Why

- **A numeral's value is undecidable without a locale.** `12.443` is 12443 in a grouping
  culture and 12.443 in a decimal one; `1.23` could be grouped-by-two (India) or a
  decimal. Source code carries no locale per literal, so no lexer cleverness resolves it
  — it is the wrong layer for the decision. Choosing ONE convention is the resolution,
  and which characters carry it (`,`/`.`) is secondary to there being only one.
- **Unicode digits are a readability hazard, not a feature.** `char.IsDigit` admits ~660
  code points across scripts, invites mixing (`1٢3`) and confusables (a `5` that is
  fullwidth `５` or a lookalike). A numeral that reads as one value and is another is the
  `01-02-03` / one-spelling-two-readings family every ruling refuses — a direct
  contradiction of "reading a value tells you where it came from."
- **The multi-culture support the designer wanted is the RUNTIME parse, not the source
  alphabet.** A French user typing `1 234,56` into an app is runtime input in a stated
  locale; it has nothing to do with whether source literals accept `١`. Every serious
  language keeps source digits ASCII and pushes culture into a locale-keyed library.

## Implementation — for the numeric tower

The value work must touch `Evaluator.Value` regardless (DISCARDEDKINDSRULING §2), and
this rides with it:

- **The lexer becomes the authority.** `Lexicon.Numeric` lexes `0-9` (+ `,` grouping,
  `.` decimal), not `char.IsDigit`. After that, a `Numeric` token is always a well-formed
  ASCII numeral.
- **REAUDIT64 finding 4's guard is transitional.** `Evaluator.Value`'s
  `double.TryParse(...) ? number : Error` gives a Numeric the invariant reader cannot take
  a runtime Error rather than a throw — needed only while the lexer still admits Unicode
  digits. Once the lexer is ASCII-authoritative, no `Numeric` fails the parse, so that
  Error branch is dead and goes; the arm reads the value directly (as an exact rational,
  per the tower).
- **`date` is unaffected** — its own literal syntax, its own value work.

The runtime culture-aware parse (`number of (text) in (locale)`) is a later slice, named
here so the alphabet decision and the parse feature are not confused for one another.
