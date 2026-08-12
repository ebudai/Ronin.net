# Re-audit 42 — `FRESHAUDIT5` finding 9

**Audited:** `1e7e9b5`, including the uncommitted authoritative-document alignment

**Date:** 2026-08-04

## Result

**No sign-off. Finding 9 is substantially incorporated, but it is not closed.**

The large repairs are real: the grammatical structure now teaches square
collections, one collection production, `@` indexing, stable instance handles,
and the actual remaining type-`when` join; the diagnostic XML comments are
attached to the right findings and malformed XML is gated; and the repaired
spec contents links all resolve in both directions.

One documentation finding remains, with three concrete remnants. The edited
terms and introduction now promise lookup indexing and indexed assignment even
though the formal indexing section and runtime implement list reads only. A
communications-style document under the authoritative `docs/spec/` directory
still says that choosing `@` is an open decision. Finally, the exact stale
curly-collection test examples named by the original finding remain, including
one executable fixture that still passes while no longer exercising a lookup.

No production-code regression was found in this documentation-only
incorporation. The prior `REAUDIT41` allocation sign-off remains unaffected.

## Finding

### 1. The authoritative sweep still contains three versions of the removed model

**Severity: documentation — the source of truth contradicts itself and one
maintained test no longer exercises the path its comment claims**

#### 1a. The replacement text promises lookup behavior that does not exist

The edited terms define indexing as returning a value from a “list or lookup,”
given a position or key (`docs/spec/terms-and-definitions.md:29-30`). The edited
introduction then uses:

```ronin
distressed banks @ (banks @ 1) = starting balance
```

as an indexed lookup write (`docs/spec/introduction.md:26-36`).

That disagrees with all three maintained implementation surfaces:

- the formal production is only `list @ position`
  (`docs/spec/grammatical-structure.md:587-594`);
- the runtime accepts only `(List, double)` and otherwise says that `@` indexes
  a list (`Compiler/Runtime/Values.cs:211-229`);
- the indexing tests explicitly record that a lookup has no runtime value yet
  and that `[a = 1]` does not resolve (`Test/Unit/Indexing.cs:393-399`).

A temporary real resolver/evaluator probe confirmed the boundary: `[1 = 2] @ 1`
could not produce a resolver tree. The introduction statement itself passes the
current compilation diagnostic pass only because that pass does not join source
statements to resolution/execution; that is not evidence that the shown lookup
write works.

This is worse than leaving the old brackets untouched: the replacement now
looks deliberately aligned with the new operator while teaching an unsupported
operand and an unsupported write use.

**Recommendation:** decide which contract is intended. If finding 9 is only
document alignment to what is built, restrict the definition to list/position
and replace the introduction line with an implemented list read, or label the
lookup-write example as planned and non-executable. If lookup reads and indexed
writes are language requirements, they need resolver, runtime, assignment, and
real-source regression coverage before these sentences can describe the current
language.

#### 1b. An authoritative file still says `@` has not been chosen

`docs/spec/NOTHINGANDINDEXING.md:58-92` calls `@` “a lean, not a decision,” says
the choice still needs a call, and instructs the reader to send an older handoff
document. This directly conflicts with the formal §4.7.7, the implementation,
and the rest of the edited spec, all of which treat `@` as settled.

The file reads like designer/programmer correspondence, but its location is
`docs/spec/`, which the project identifies as authoritative; unlike genuine
handoff correspondence, a reader has no location-based reason to discount it.

**Recommendation:** update it with the settled decision and its supersession, or
move/archive it with non-authoritative handoff material. Leaving contradictory
adjudication in the source-of-truth directory keeps the exact proposal-became-
fact ambiguity that the document warns about.

#### 1c. The syntax-directed test/example sweep is incomplete

The two parser class examples called out by the original finding still show the
removed curly spellings while their token fixtures use square brackets:

- `Test/Unit/Lists.cs:71-88` says `var x = { 5, 2, test }`;
- `Test/Unit/Lookups.cs:40-56` says `var x = { "stuff" = 4 }`.

More importantly, `Test/Integration/Compilations.cs:157-174` still supplies
`var value = { key = };` and says it exercises an error “in a lookup.” Braces
now open a block only, so the test remains green by finding the malformed
association inside a block. It no longer tests the reflective diagnostic walk
through a square collection/lookup—the kind of rewritten-input false assurance
that the bracket migration was specifically meant to audit.

Other maintained `StatementShapes` cases do reach malformed associations under
square collections, so this does not create a current unguarded production bug.
It does mean the original finding's explicit test/comment cleanup is not done
and this named regression is asserting a different path from the one it
documents.

**Recommendation:** change the two comments to square literals and the
integration fixture to `var value = [ key = ];`. The latter should retain the
same `Malformed` assertion while once again proving that the diagnostic walk
finds an error held by a collection.

## Status of the original finding 9

| Original surface | Result |
|---|---|
| heading rule and dormant heading explanation | **Fixed.** The spec and historical implementation comments distinguish old braced lists from current blocks. |
| reference and anonymous-value sections | **Fixed.** Bracketed indexer suffixes are historical explanation only; current indexing is `@`. |
| lexical names and spec contents | **Fixed.** Brackets are collections, the deleted indexer entry is gone, and maintained link tests pass. |
| introduction indexing example | **Not fixed.** The spelling changed, but it now promises an unsupported lookup write; finding 1a. |
| instance identity | **Fixed.** Stable `(type, slot, generation)` handles are distinguished from transient dense indices. |
| type-scope `when` explanation | **Fixed.** It names the missing join rather than the already-built instance model. |
| diagnostic XML comments | **Fixed and gated.** The summaries attach correctly and malformed XML is a build error. |
| list/lookup examples and test comments | **Not fixed.** Two stale comments and one stale executable fixture remain; finding 1c. |
| consistency of authoritative spec material | **Not fixed.** `NOTHINGANDINDEXING.md` still describes `@` as undecided; finding 1b. |

## What was rechecked without another finding

- Every edited README target and anchor resolves, and every numbered grammar
  section appears in the contents.
- `grammatical-analysis.md` now describes the single collection production and
  its post-parse kind decision rather than ordered list/lookup alternatives.
- `[` and `]` are consistently called collection delimiters in the lexical
  structure and lexical-analysis ordering.
- The obsolete `ordinal` anonymous-value/category definition is removed.
- Current curly-list references in the parser, aggregate, scope-heading tests,
  and formal grammar are explicitly historical explanations of the removed
  syntax, not current syntax claims.
- Uses of “indexer” for a C# object indexer or as shorthand for the indexing
  operation do not reintroduce the removed Ronin bracket-suffix production.

## Verification

The temporary lookup-indexing probe was removed before the maintained gates.

```text
focused SpecLinks, Lists, Lookups, and diagnostic-walk tests
  passed — 12 tests

dotnet restore --locked-mode
  passed

dotnet build --no-restore --configuration Release -warnaserror
  passed — 0 warnings, 0 errors

dotnet test --no-build --configuration Release
  /p:CollectCoverage=true
  /p:CoverletOutputFormat=cobertura
  /p:Threshold=100
  /p:ThresholdType=line%2Cbranch
  /p:ThresholdStat=total
  passed — 1024 tests, 100% line, 100% branch, 100% method

git diff --check
  passed
```

The owner-authorized warning suppressions and settled hand-aligned formatter
output are outside this round and are not findings. Existing uncommitted
documentation, handoff reports, and probe scripts were preserved.
