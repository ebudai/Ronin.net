# Re-audit 69 — ruling (A) closes the return heuristic cycle; multiline diagnostics need canonical words

> **Ledger** — `[A]` Audit of `d908c25..350bb56`, requested by
> `FORAUDIT69`, against `UNRESOLVEDRETURNRULING`. The ruling is implemented and
> the REAUDIT65–68 return-classification series is closed. **Not fully signed
> off:** one adjacent low-severity presentation finding remains—the new
> `Unresolved` message copies raw source whitespace, so a multiline reference
> breaks the compiler's one-line diagnostic rendering.
> supersedes: none
> superseded by: none

## Audit result

The structural change is sound. Every value reading whose resolution is
`NoParse` now emits one `Unresolved` at the outer reference span. A function
body containing any such reading suppresses `Unanswered` on that same
`NoParse` condition. `Reading.Answering` and its token classifier are deleted,
and the resolved `Called` walk remains unchanged.

This correctly makes all formerly irreconcilable shapes one policy:

- direct, parenthesized, unparenthesized, and deeper unresolved returns;
- unresolved statements containing a legal medial `return` name;
- unrelated unresolved text beside a bare return; and
- an unresolved tail-position reference under the currently pre-sugar check.

Each produces exactly one `Unresolved` and never a simultaneous `Unanswered`.
Resolved return calls continue to be found at every depth, while a resolved
multi-word name containing `return` remains a `Node.Name` and still permits
`Unanswered` when the body carries no value. A separate probe confirmed the
same mutual exclusion for an unresolved reference inside a transparent nested
`if`, not only the direct-body rows maintained in the suite.

The four verification questions are answered consistently with the request:

- **VER-1:** `Unanswered` currently sees pre-tail-sugar syntax;
  `function f => number { 5 }` is `Unanswered`, while `{ nope }` is
  `Unresolved` because it is independently `NoParse`.
- **VER-2:** `NAMEVSANCHOR` constrains declaration admission and preserves the
  exact resolved tree walk; it neither narrows the ruling's `NoParse` scope nor
  requires retaining the deleted heuristic.
- **VER-3:** no consumer of `Reading.Answering` remained. The property,
  constructor argument, producer, and sole `Unanswered` consumer are gone;
  `AnsweringReaction` is a distinct finding and unaffected.
- **VER-4:** the maintained silence expectations affected by the general
  finding were updated. The complete suite passes with the new diagnostic.

## Finding 1 — low — raw source newlines are embedded in `Unresolved` build messages

**Locations:** `Compiler/Compilation.cs:229-235`, where the message text is
created with `Source.Text.Substring`; `Compiler/Diagnostics/Finding.cs:963-970`,
where that raw slice is interpolated; and
`Compiler/Diagnostics/Finding.cs:1031-1042`, whose report format reserves
newlines for labelled related locations.

`Read` already resolved a canonical lexeme sequence, but `Unresolveds` discards
that rendering and extracts the original character span:

```csharp
reading.Span.Source.Text.Substring(reading.Span.Offset, reading.Span.Length)
```

Whitespace is trivia to the language, so an unresolved reference may legally
cross a line. The raw slice then turns one diagnostic into multiple physical
output lines.

### Witness

```ronin
var y => number = nope
    more;
```

**Actual `Words`:** `"nope\n    more"`.

**Actual build rendering:**

```text
Player.ron:1:19: «nope
    more» does not resolve: nothing in scope reads these words as a value or a call.
```

The continuation looks like an independent unlocated output line. It also
quotes indentation rather than the “words” the finding and ruling say it
carries. Comments or other trivia inside a reference have the same underlying
problem.

**Expected:** retain the full reference as `Primary`, but render its content
canonically, for example `«nope more»`, as `UnknownType`, `Unanswered`, parse
errors, and other word-bearing findings already do through `Lexemes.Render()`.

This is low severity because resolution, finding identity, span, suppression,
and build failure are all correct. Only the human/line-oriented rendering is
malformed.

### Repair direction and regression coverage

Carry the canonical rendering produced from the reading's lexemes, or otherwise
render those lexemes rather than slicing source characters. Do not change the
primary span: it correctly covers the original multiline reference.

Add a regression with an unresolved reference split across lines and assert:

- `Words` is canonical and contains no trivia;
- `Diagnostics.Report` contains no source-derived newline; and
- `Primary` still spans the complete original reference.

## Disposition of REAUDIT68 and the ruling

| Item | Reassessment |
|---|---|
| Unparenthesized unresolved nested return produced `Unanswered` | **Closed by the ruled policy change.** It now produces exactly one `Unresolved`; no token-position classification remains. |
| REAUDIT67 medial-name collision | **Closed.** Both unresolved name-containing statements and the resolved-name control follow the correct paths. |
| REAUDIT65 unrelated-unresolved expectation | **Intentionally superseded by ruling §6.** The unresolved statement now reports first and suppresses `Unanswered`. |
| `Answering` heuristic family | **Closed and deleted.** No replacement lexical heuristic was introduced. |

## Verification performed

- Read `FORAUDIT69`, `UNRESOLVEDRETURNRULING`, `NAMEVSANCHOR`, its measured
  result, `TAILSUGAR`, and `RETURNANDLITERALS`; reviewed the implementation and
  test diff for `d908c25..350bb56`.
- Locked restore: clean.
- Debug and Release builds with `-warnaserror`: clean, zero warnings and errors.
- Maintained Release and Debug suites: `1333` passed, `0` failed in each.
- Release coverage gate: `100%` line and `100%` branch for `Ronin` and
  `Ronin.Server`.
- Changed-file `dotnet format --verify-no-changes`: clean.
- `git diff --check`: clean before this report.
- The nested-transparent-scope control passed. The multiline-rendering witness
  failed as described. Both were executed as temporary xUnit integration tests,
  and the probe file was removed after capture.

Ruling (A) resolves the semantic problem cleanly and ends the contradictory
token-heuristic cycle. Full signoff should wait only for canonical rendering of
the new finding's quoted reference.
