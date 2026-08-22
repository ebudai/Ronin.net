# Re-audit 72 — every separator is visible, but generated escapes collide with source backslashes

> **Ledger** — `[A]` Audit of `947ce04..ed49e6c`, requested by
> `FORAUDIT72`. REAUDIT71's Unicode line-separator finding is repaired across
> LF, CR, CRLF, NEL, LS, and PS; semantic rendering and spans remain intact.
> **Not fully signed off:** one adjacent low-severity presentation finding
> remains—the display encoding does not escape its own backslash introducer, so
> distinct source text can produce identical quoted diagnostics.
> supersedes: none
> superseded by: none

## Audit result

The requested character-semantic repair works. `Visible` now converts LF and CR
to the familiar `\n`/`\r`, converts every other control character to
`\uXXXX`, and explicitly covers Unicode LINE SEPARATOR and PARAGRAPH SEPARATOR.
The six maintained rows emit exactly one `Unresolved`, contain no raw line
boundary in `Diagnostics.Report`, retain recognizable content, and preserve the
complete original source span.

`Reading.Words` and `Lexemes.Render()` remain semantic; only the finding's copy
is transformed. The unresolved-return ruling and all earlier diagnostic cases
remain unchanged and passing.

The visible notation is not self-escaping, however. Backslash is emitted
unchanged even though it introduces every generated representation. An actual
line boundary and source containing the corresponding backslash notation are
therefore quoted identically.

## Finding 1 — low — actual LF and source `\n` collapse to the same `Unresolved` text

**Location:** `Compiler/Compilation.cs:236-252`, where `Visible` generates
backslash-prefixed escapes but passes an existing backslash through the default
branch unchanged.

### Witness

Compare these two unresolved references:

```text
var y => number = send "hello<actual LF>world" nope;
var y => number = send "hello\nworld" nope;
```

The first literal contains an actual line feed. The second contains the two
characters backslash and `n`. Both are legal text tokens at the lexical stage
and both enclosing references resolve to `NoParse`.

**Actual for both:**

```text
Unresolved.Words == "send \"hello\\nworld\" nope"
```

Their `Message` values are consequently identical as well. The executed paired
control failed its `Assert.NotEqual` at the `Words` comparison; `Message`
interpolates that same equal field. The same collision family exists for source
text such as `\r` and `\u0085` versus the corresponding generated visible forms.

**Expected:** the diagnostic representation is unambiguous: a generated escape
for a carried control/separator is distinguishable from literal backslash text
that was already present in the source.

The severity is low because both programs correctly fail with one
`Unresolved`, neither message contains a raw line boundary, and each primary
span still identifies the exact source. The defect is limited to the fidelity
of the quoted reference.

### Repair direction and regression coverage

Escape the escape introducer as part of the same single-pass display encoding,
or use another injective visible representation. If backslash remains the
introducer, it must be handled before/default-apart from generated sequences so
an existing backslash cannot impersonate one.

Add paired regressions that compare:

- actual LF against literal backslash-plus-`n`;
- actual CR against literal backslash-plus-`r`; and
- actual NEL/LS/PS against literal `\uXXXX` text.

For each pair, assert both reports remain one physical line, their displayed
`Words`/messages differ, their visible content is recognizable, and each
primary span retains its original source.

## Disposition of REAUDIT71

| Prior item | Reassessment |
|---|---|
| NEL (`U+0085`) remained raw | **Closed.** It renders as `\u0085`. |
| LINE SEPARATOR (`U+2028`) remained raw | **Closed.** It renders as `\u2028`. |
| PARAGRAPH SEPARATOR (`U+2029`) remained raw | **Closed.** It renders as `\u2029`. |
| LF, CR, and CRLF controls | **Closed and maintained.** |
| One-line output invariant | **Closed for every admitted line boundary.** The remaining finding concerns escape fidelity, not physical line breaking. |

## Verification performed

- Reviewed `FORAUDIT72` and the production/test diff for
  `947ce04..ed49e6c`.
- Locked restore: clean.
- Debug and Release builds with `-warnaserror`: clean, zero warnings and errors.
- Maintained Release and Debug suites: `1340` passed, `0` failed in each.
- Release coverage gate: `100%` line and `100%` branch for `Ronin` and
  `Ronin.Server`.
- Changed-file `dotnet format --verify-no-changes`: clean.
- `git diff --check`: clean before this report.
- The actual-LF versus literal-`\n` collision was executed as a temporary xUnit
  integration test and reproduced exactly; the probe file was removed after
  capture.

The Unicode line-boundary repair is complete and correct. Full signoff should
wait until its visible encoding also preserves distinctions from backslash text
already present in source.
