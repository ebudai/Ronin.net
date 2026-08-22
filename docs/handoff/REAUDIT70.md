# Re-audit 70 — trivia is canonicalized, but multiline literal content still breaks the diagnostic line

> **Ledger** — `[A]` Audit of `350bb56..46c02b8`, requested by
> `FORAUDIT70`. REAUDIT69's raw-whitespace witness is repaired and its span is
> preserved. **Not fully signed off:** one adjacent low-severity presentation
> finding remains—`Lexemes.Render()` removes trivia, but a legal text literal
> can itself contain a newline, which still enters the one-line `Unresolved`
> diagnostic.
> supersedes: none
> superseded by: none

## Audit result

The requested repair works for the reported defect. `Read` renders the lexemes
it already resolved and stores that canonical form on `Reading`; `Unresolveds`
uses it instead of slicing the source. The maintained witness now produces
`Words == "nope more"`, `Diagnostics.Report` has one physical line, and the
primary span still covers all 13 original characters across both source lines.

Single-word and bracketed composite controls remain correct. The semantic
`UNRESOLVEDRETURNRULING` implementation is unchanged, and all earlier
return-classification cases continue to pass.

Canonical lexeme rendering is not sufficient to guarantee a one-line
diagnostic, however. It removes trivia between tokens, but appends each token's
canonical text verbatim. Ronin explicitly permits multiline text literals, so a
newline belonging to a `Text` token survives `Render()` and is interpolated
into the finding message.

## Finding 1 — low — a multiline text literal still injects a newline into `Unresolved`

**Locations:** `Compiler/Compilation.cs:540-545`, where `lexemes.Render()` is
stored as `Reading.Words`; `Compiler/Resolution/Lexemes.cs:100-120`, where each
lexeme's text is appended verbatim; `Compiler/Lexicon/Literal.cs:179-197`, which
admits newlines inside a text token; and
`Compiler/Diagnostics/Finding.cs:963-970`, where `Words` is quoted in the
message.

### Witness

```ronin
var y => number = send "hello
world" nope;
```

The complete initializer is one unresolved reference. Its inter-token spaces
are canonicalized, but the newline inside `"hello\nworld"` belongs to the
literal token and is therefore retained.

**Actual build rendering:**

```text
Player.ron:1:19: «send "hello
world" nope» does not resolve: nothing in scope reads these words as a value or a call.
```

**Expected:** the diagnostic remains one physical line. The literal's line
break should have a visible, diagnostic-safe representation rather than an
actual CR or LF in `Finding.Message`/`Diagnostics.Report`.

This is low severity because the correct `Unresolved` is emitted at the correct
full reference span, compilation fails as intended, and no semantic result is
changed. It is the same line-oriented presentation failure at a token-content
boundary the current regression does not exercise.

### Repair direction and regression coverage

Separate semantic canonical rendering from diagnostic-safe rendering. A
newline inside a literal is not trivia and may legitimately remain in the
canonical lexeme value; the diagnostic layer must nevertheless render control
characters visibly. Avoid silently changing the meaning of `Lexemes.Render()`
for its other consumers merely to satisfy one output format.

Add an unresolved-reference regression containing a multiline text literal and
assert:

- exactly one `Unresolved` is emitted;
- `Diagnostics.Report` contains neither `\r` nor `\n` from token content;
- the displayed form still makes the literal boundary/content recognizable;
  and
- `Primary` continues to span the complete original multiline reference.

The `Reading.Words` comment should also stop claiming that lexeme rendering
removes every line break: it removes trivia line breaks, not line breaks carried
by tokens.

## Disposition of REAUDIT69

| Prior item | Reassessment |
|---|---|
| Raw source whitespace entered `Unresolved.Words` | **Closed.** Inter-token whitespace and indentation are removed by canonical lexeme rendering. |
| Primary span must remain the original reference | **Closed.** The maintained 13-character span assertion passes. |
| One-line diagnostic goal | **Direct witness closed, but incomplete at the legal multiline-literal boundary described above.** |

## Verification performed

- Reviewed `FORAUDIT70` and the production/test diff for
  `350bb56..46c02b8`.
- Locked restore: clean.
- Debug and Release builds with `-warnaserror`: clean, zero warnings and errors.
- Maintained Release and Debug suites: `1334` passed, `0` failed in each.
- Release coverage gate: `100%` line and `100%` branch for `Ronin` and
  `Ronin.Server`.
- Changed-file `dotnet format --verify-no-changes`: clean.
- `git diff --check`: clean before this report.
- The multiline-literal witness was executed as a temporary xUnit integration
  test and failed because `Diagnostics.Report` contained the literal's newline;
  the probe file was removed after capture.

The reported raw-trivia defect is repaired cleanly. Full signoff should wait
until diagnostic rendering also handles line breaks that are part of legal
token content.
