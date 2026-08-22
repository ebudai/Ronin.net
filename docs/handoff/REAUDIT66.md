# Re-audit 66 — all three direct findings close; unresolved nested returns remain misclassified

> **Ledger** — `[A]` Audit of `740cb18..76d5dea`, requested by
> `FORAUDIT66`. REAUDIT65's three findings are repaired. **Not signed off:** one
> adjacent medium-severity finding remains—the new unresolved-return flag inspects
> only the outer reference's first lexeme, although returns are legal and detected
> at any expression depth.
> supersedes: none
> superseded by: none

## Audit result

The requested repairs hold:

- a Unicode decimal digit rejected by `Numeric` is consumed by `Word`, so the
  full lexer remains exhaustive and terminates;
- unrelated unresolved statements no longer suppress `Unanswered`; and
- `Cell` now reports refusal, preventing rejected nullary declarations from
  filing a signature or resolver call.

All REAUDIT65 witnesses and the added controls pass. The maintained Debug,
Release, test, and coverage gates are clean.

One diagnostic inconsistency remains adjacent to finding 2. `Reading.Answering`
recognises only a reference that *begins* with `return`. That is enough for a
top-level `return nope`, but not for a value-return nested inside another call.
The resolved exit walk deliberately finds returns at every tree depth; the
unresolved classifier must cover the same structural positions.

## Finding 1 — medium — an unresolved value-return nested in another call is reported as if no value-return exists

**Locations:** `Compiler/Compilation.cs:500-528`, where `Answering` is computed;
`Compiler/Compilation.cs:911-925`, where it gates `Unanswered`; and
`Compiler/Compilation.cs:1120-1149`, whose resolved-tree walk establishes that
returns are recognised at any depth.

The new flag is computed from only the outer reference:

```csharp
answering: lexemes.Count > 1 && lexemes[0].Text is "return"
```

`Read` intentionally records only the outermost reference. If an inner
`return (_)` cannot resolve, the outer reference has no tree and no `Site`, but
its first lexeme belongs to the enclosing call rather than the return. The guard
therefore treats the body as having no attempted value-return.

### Witness

```ronin
function send (x) { return x; }
function f => number { send (return nope); }
```

**Actual:** one `Unanswered` at `f`.

**Expected under the repair's maintained cascade policy:** no `Unanswered`, the
same as direct `return nope`. The body syntactically contains `return (_)`; only
its value is unresolved. If unresolved value references later gain their own
finding, that finding belongs at `nope`, not a contradictory “no return carries a
value” message at the function.

This is not an exotic placement invented for the witness. `Called` explicitly
walks `tree.Whole` because `return` is a call and can occur at depth; its own
documentation uses nested calls as the reason. The unresolved path and resolved
path currently disagree about the same syntax.

### Repair direction and regression coverage

Record answer intent from the grammatical/reference structure at every expression
depth before resolution, rather than inferring it from the first flattened
lexeme of the outer reference. The anchor should also derive from the registered
`SymbolTable.Answer` pattern instead of copying the text `"return"`.

Maintain at least:

- direct unresolved `return nope`;
- grouped `(return nope)`;
- `send (return nope)` and a return nested more than one call deep;
- the corresponding resolved `send (return 5)` control;
- unrelated unresolved statements before, after, and inside a transparent block;
  and
- a body with no value-return, which must still report `Unanswered`.

The severity is medium because this is an inaccurate extra diagnostic on already
unresolved source, not a missed contradiction in otherwise clean source. It still
breaks the explicit cascade policy and makes diagnostics depend on expression
nesting.

## Disposition of REAUDIT65

| Prior finding | Reassessment |
|---|---|
| 1. Unicode digit caused lexer non-progress | **Closed.** `Word` and `Numeric` share the ASCII digit authority; full-lexer, adjacent-token, mixed-word, and compilation witnesses terminate. |
| 2. any unresolved reading suppressed `Unanswered` | **Direct finding closed.** Unrelated unresolved text no longer suppresses. An unresolved value-return inside an enclosing call is missed by the new shallow classifier (finding above). |
| 3. refused nullary declaration was installed | **Closed.** `Cell` returns false on refusal and `Declare` exits before signature/nullary filing. Supplied literals, prior data, both declaration orders, and valid nullaries are maintained. |

## Verification performed

- Reviewed the production diff for `740cb18..76d5dea` and `FORAUDIT66`.
- Debug and Release builds with `-warnaserror`: clean, zero warnings and errors.
- Maintained Release suite: `1332` passed, `0` failed.
- Release coverage gate: `100%` line and `100%` branch for `Ronin` and
  `Ronin.Server` using the workflow's `%2C`-escaped threshold argument.
- `git diff --check`: clean before this report.
- The remaining witness was executed as a temporary xUnit integration test; the
  probe was removed after capture.

The repair range is substantially improved and all three requested findings are
closed. Signoff should wait for the unresolved nested-return classifier to match
the compiler's depth-aware return semantics.
