# Re-audit 67 — nested returns are found, but words inside names are mistaken for returns

> **Ledger** — `[A]` Audit of `76d5dea..d908c25`, requested by
> `FORAUDIT67`. REAUDIT66's direct nested-return finding is repaired. **Not
> signed off:** one high-severity adjacent finding remains—the new flattened-token
> scan treats `return` inside a legal multi-word name as a value-return and can
> suppress `Unanswered`, leaving a written return promise with zero findings.
> supersedes: none
> superseded by: none

## Audit result

The requested depth repair works for the reported shapes. `Reading.Answering`
now sees direct, grouped, nested, and more-deeply-nested unresolved value returns;
the corresponding maintained regressions pass. Resolved nested returns still use
the ordinary `Called` tree walk, and bare nested returns still do not count as
value-carrying.

The replacement classifier is too broad, however. It scans every adjacent pair
in the outer reference's flattened lexeme stream and treats every `return`
followed by a non-closing token as a return call. The language reserves only the
whole spelling of a nullary supply; it does not globally prohibit that word from
larger names. `return` remains legal inside a multi-word user name, just as the
resolver explicitly documents for `true positive` and `stop word`.
Consequently, an unrelated unresolved outer call that contains such a name is
marked `Answering` and erases the missing-return finding.

## Finding 1 — high — `return` inside a legal multi-word name suppresses `Unanswered`

**Locations:** `Compiler/Compilation.cs:516-527`, where the flattened lexical
scan computes `Answering`; `Compiler/Compilation.cs:921-935`, where that flag
suppresses `Unanswered`; and `Compiler/Resolution/Resolver.cs:2008-2030`, which
defines whole-spelling reservation and expressly permits supplied words inside
larger names.

The new classifier is:

```csharp
var answering = lexemes.Zip(lexemes.Skip(1)).Any(pair =>
    pair.First.Text == anchor
    && pair.Second.Kind is not (LexemeKind.Close or LexemeKind.Separator or LexemeKind.Associates));
```

This loses both name boundaries and grammatical nesting. In particular, the
comment immediately above it—“`return` is reserved, so every occurrence is a
return”—does not match `SymbolTable.Whole`: a nullary entry reserves its own
complete spelling and nothing else. `customer return policy` is therefore a
legal user name, not a return call.

### Witness

```ronin
var customer return policy => number;
function send (x) { return x; }
function f => number { send (customer return policy) nope; return; }
```

The declared name inside the parentheses is legal. The enclosing reference is
unresolved because of the trailing `nope`, and the function has only a bare
return.

**Actual:** zero findings.

**Expected:** one `Unanswered` at `f`. No value leaves the body, so the written
`=> number` promise is unfulfilled. The unresolved outer call does not become a
value-return merely because one of its declared name's words happens to be
`return`.

The causal controls were executed as well:

- removing the trailing `nope`, so `send (customer return policy)` resolves,
  produces the expected `Unanswered`; this proves the multi-word declaration is
  accepted and its resolved tree contains no answer call;
- keeping the unresolved outer-call shape but changing the declared name to
  `customer policy` also produces `Unanswered`; and
- adding only the `return` word back makes the finding disappear.

This is the same class of silent contradiction as REAUDIT65 finding 2, not merely
an extra cascade diagnostic. Because unresolved references do not yet have their
own general finding, the witness is accepted with no diagnostic at all despite a
written return contract the body cannot satisfy.

### Repair direction and regression coverage

Keep the depth-aware behaviour, but derive answer intent from grammatical or
resolver structure that preserves sub-reference and name boundaries. Raw token
adjacency anywhere in the flattened outer span cannot distinguish a nested
`return (_)` from an ordinary word inside a declared multi-word name.

Maintain the current direct, grouped, nested, deeper-nested, resolved, and bare
return cases, and add at least:

- a declared multi-word name containing `return`, used in a resolved call;
- that name inside an otherwise unresolved outer call, beside a bare return;
- the equivalent unresolved outer call with a name not containing `return`; and
- additional accepted multi-word-name shapes containing `return`, so the result
  follows structure rather than token position.

## Disposition of REAUDIT66

| Prior finding | Reassessment |
|---|---|
| Unresolved nested value-return was missed | **Direct finding closed.** The requested nested and grouped witnesses now pass. The replacement raw scan introduces the distinct false-positive suppression above. |

## Verification performed

- Reviewed `FORAUDIT67` and the production/test diff for
  `76d5dea..d908c25`.
- Locked restore: clean.
- Debug and Release builds with `-warnaserror`: clean, zero warnings and errors.
- Maintained Release and Debug suites: `1332` passed, `0` failed in each.
- Release coverage gate: `100%` line and `100%` branch for `Ronin` and
  `Ronin.Server`.
- Changed-file `dotnet format --verify-no-changes`: clean.
- `git diff --check`: clean before this report.
- The failing witness and both causal controls were executed as temporary xUnit
  integration tests; all temporary probe files were removed after capture.

The requested repair is a material improvement and closes REAUDIT66's direct
case. Signoff should wait until unresolved-return classification preserves legal
multi-word names while retaining the new depth coverage.
