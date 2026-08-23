# Re-audit 73 — self-escaping diagnostic rendering is complete; signed off

> **Ledger** — `[A]` Audit of `ed49e6c..c928691`, requested by
> `FORAUDIT73`. REAUDIT72's lossy-display finding is closed: the escape
> introducer is doubled, carried controls/separators remain distinguishable from
> source spelling their escapes, and the representation stays one-line and
> span-preserving. **Signed off with no findings.**
> answered by: FINDINGCOMPOSITION
> supersedes: none
> superseded by: none

## Audit result

The repair is correct. `Visible` handles backslash before its generated escape
forms, so the display encoding is self-escaping:

- an actual LF renders as `\n`, while source backslash-plus-`n` renders as
  `\\n`;
- an actual control such as NEL renders as `\u0085`, while source spelling
  those six characters renders as `\\u0085`; and
- repeated backslashes remain distinct because each input backslash contributes
  exactly one doubled pair.

The mapping is single-pass and injective over the handled UTF-16 characters:
every generated control/separator representation begins with backslash, and an
input backslash has its own distinct doubled representation. Semantic
`Reading.Words` and `Lexemes.Render()` remain unchanged; only the copy quoted by
`Unresolved` is display-safe. Primary spans continue to cover the exact original
source.

The maintained pairs cover LF, CR, NEL, LINE SEPARATOR, and PARAGRAPH SEPARATOR.
Additional temporary controls for NUL, TAB, and ESC exercised the general
`char.IsControl` branch: each rendered visibly, differed from literal
backslash-`uXXXX` source, and retained the original character in its primary
source span.

## Disposition of REAUDIT72

| Prior item | Reassessment |
|---|---|
| Actual LF and literal `\n` quoted identically | **Closed.** Backslash is doubled, so the displayed references differ. |
| CR and literal `\r` collision family | **Closed and maintained.** |
| NEL/LS/PS and literal `\uXXXX` collision family | **Closed and maintained.** |
| Reports must remain one physical line | **Closed.** No raw admitted line boundary survives. |
| Semantic rendering and primary spans | **Unchanged and verified.** |

## Concurrent unresolved-return amendment

`docs/handoff/UNRESOLVEDRETURNAMENDMENT.md` appeared untracked while this audit
was running and is outside the committed FORAUDIT73 range. Its one immediately
audit-relevant correction—unresolved readings in a nested delegate must not
suppress the enclosing function's `Unanswered`—is already honored by the
implementation's callable-owner partition.

The amendment's exact shape was executed:

```ronin
function f => number {
    var callback = () => { nope };
    return;
}
```

It produces both correct findings at their separate callable levels:
`Unanswered` for `f` and `Unresolved` for the delegate. The amendment's nested
return dead-code proposal and `otherwise` strictness question explicitly ask to
be raised as separate future items; they are not changes requested by
FORAUDIT73 and do not block this signoff.

## Verification performed

- Reviewed `FORAUDIT73` and the production/test diff for
  `ed49e6c..c928691`.
- Locked restore: clean.
- Debug and Release builds with `-warnaserror`: clean, zero warnings and errors.
- Maintained Release and Debug suites: `1345` passed, `0` failed in each.
- Release coverage gate: `100%` line and `100%` branch for `Ronin` and
  `Ronin.Server`.
- Changed-file `dotnet format --verify-no-changes`: clean.
- `git diff --check`: clean before this report.
- Focused maintained collision/line-boundary tests passed.
- Temporary NUL/TAB/ESC and nested-delegate probes passed and were removed.

FORAUDIT73 is signed off. The unresolved-return classification and its
diagnostic-rendering tail are closed; the separate language/design work named
by the concurrent amendment remains outside this repair range.
