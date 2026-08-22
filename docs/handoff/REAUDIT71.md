# Re-audit 71 — CR/LF are visible, but legal Unicode line separators still escape the seam

> **Ledger** — `[A]` Audit of `46c02b8..947ce04`, requested by
> `FORAUDIT71`. REAUDIT70's LF witness is repaired, semantic rendering is
> preserved, and the primary span is unchanged. **Not fully signed off:** one
> adjacent low-severity presentation finding remains—`Visible` handles only CR
> and LF, while text literals also admit Unicode NEL, line separator, and
> paragraph separator characters into the nominally one-line diagnostic.
> supersedes: none
> superseded by: none

## Audit result

The requested separation is correct. `Reading.Words` remains the semantic
lexeme rendering, including token-carried content, while only the text passed to
`Unresolved` is made display-safe. The direct multiline-literal witness now
renders its LF as the two visible characters `\n`; the literal remains
recognizable, the report contains no CR/LF, and `Primary` still spans the
original multiline reference.

The corrected `Reading.Words` documentation accurately distinguishes trivia
line breaks from line breaks carried by a token. The semantic
`UNRESOLVEDRETURNRULING` paths remain untouched and pass their maintained
coverage.

The display seam is narrower than the lexer alphabet, however. `Text.Lex`
accepts every character until an unescaped closing quote. That includes the
Unicode characters conventionally used as NEL, line separator, and paragraph
separator. `Visible` replaces only `\r` and `\n`, so these other legal token
characters remain literal separators in `Finding.Message` and downstream
diagnostic output.

## Finding 1 — low — Unicode line-separator characters remain literal in `Unresolved`

**Locations:** `Compiler/Compilation.cs:236-242`, where `Visible` replaces only
CR and LF; `Compiler/Lexicon/Literal.cs:179-197`, where text tokens admit the
characters; and `Compiler/Diagnostics/Finding.cs:963-970`, where the result is
interpolated into the diagnostic.

The current seam is:

```csharp
private static string Visible(string words) => words.Replace("\r", "\\r").Replace("\n", "\\n");
```

### Witness

For each of `U+0085` (NEXT LINE), `U+2028` (LINE SEPARATOR), and `U+2029`
(PARAGRAPH SEPARATOR), compile an unresolved reference whose text literal
contains the character:

```text
var y => number = send "hello<U+2028>world" nope;
```

All three sources lex successfully and each emits exactly one `Unresolved`.

**Actual:** `Diagnostics.Report(finding)` still contains the corresponding raw
separator (at position 29 in the executed witnesses).

**Expected:** every character capable of representing a line boundary is shown
visibly in a diagnostic promised to occupy one physical line. None remains as a
literal separator in the report string.

The severity is low because resolution, finding count, source span, and build
failure are correct. This affects only presentation and line-oriented consumers,
at the Unicode boundary immediately adjacent to the repaired CR/LF case.

### Repair direction and regression coverage

Define the display invariant by character semantics rather than by the two most
common encodings. The diagnostic-safe renderer should handle the complete set
of line-boundary characters the lexer permits, while leaving
`Reading.Words`/`Lexemes.Render()` unchanged. A shared diagnostic escaping seam
would also make the rule reusable by other findings that quote token content.

Maintain the LF test and add a table covering at least:

- LF (`U+000A`), CR (`U+000D`), and CRLF;
- NEL (`U+0085`);
- LINE SEPARATOR (`U+2028`); and
- PARAGRAPH SEPARATOR (`U+2029`).

For every row, assert exactly one `Unresolved`, no raw separator in
`Diagnostics.Report`, recognizable visible content, and an unchanged primary
span over the original source.

## Disposition of REAUDIT70

| Prior item | Reassessment |
|---|---|
| LF inside a multiline text literal split `Unresolved` | **Closed.** It is displayed visibly as `\n`. |
| Semantic rendering must remain unchanged | **Closed.** Escaping occurs only when constructing the finding. |
| Primary span must cover original multiline source | **Closed.** The maintained source-slice assertion passes. |
| One-line diagnostic invariant | **Direct CR/LF cases closed, incomplete for the legal Unicode separators above.** |

## Verification performed

- Reviewed `FORAUDIT71` and the production/test diff for
  `46c02b8..947ce04`.
- Locked restore: clean.
- Debug and Release builds with `-warnaserror`: clean, zero warnings and errors.
- Maintained Release and Debug suites: `1335` passed, `0` failed in each.
- Release coverage gate: `100%` line and `100%` branch for `Ronin` and
  `Ronin.Server`.
- Changed-file `dotnet format --verify-no-changes`: clean.
- `git diff --check`: clean before this report.
- NEL, LINE SEPARATOR, and PARAGRAPH SEPARATOR were executed as temporary xUnit
  integration probes; all three failed the one-line assertion as described.
  The probe file was removed after capture.

The reported CR/LF defect is repaired cleanly. Full signoff should wait until
the diagnostic-safe rendering seam covers every legal line-separator character,
not only CR and LF.
