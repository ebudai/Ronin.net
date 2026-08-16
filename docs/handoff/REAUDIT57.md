# Re-audit 57 — Ruling incorporation and shared-container semantics

**Audited:** `a5bce23..e00ee38`, principally `76e3c67`, `645a93d`,
`e077fe9`, `74e89b8`, `d1588f8`, and `e00ee38`, against `REAUDIT56`,
the new `REAUDIT56RELAY` and `VARIABLEANDMODULE` ruling, and the governing
`SCOPEIDENTITYRULING`, `CONTAINERIDENTITYRULING`, `GENERICSII`,
`NAMEDIDENTITY`, and `CHECKERSCOPINGRULINGS` documents.

**Date:** 2026-08-15

## Result

**Not signed off. One high-severity and two medium-severity findings remain.**

All five direct `REAUDIT56` witnesses are repaired. Patterned non-functions are
findings rather than exceptions; ancillary declarations participate in overload-wide
uniqueness; a function's own body and ancillary types resolve in its signature and
are stored there; inference variables are privately minted and carry shaped
requirement records; and two pathless compilations no longer collapse into one null
module. The new designer ruling is clear and the implementation follows most of it.

The remaining high-severity issue is the other half of the same B decision. An
overload set is represented as one identity and checked as one uniqueness extent, but
its bodies are still built with separate declaration tables. A type in one body is
therefore unknown in another body or signature even though H-wide makes it visible
throughout their one container. Resolving inherited signatures separately in those
different tables can additionally emit contradictory permanent and temporary
overload diagnostics for the same pair.

The two ruled value objects also stop one step short of their contracts. A
`Requirement` record compares its operand tuple by list reference and retains the
caller's mutable list, so its set neither deduplicates semantic equals nor owns the
operand value stored in it. A pathless module's token is minted per compilation
rather than per document, so recompiling even the exact same `SourceText` changes all of its named
type identities; this deviation is honestly ledgered, but it is still the opposite
of `VARIABLEANDMODULE`'s explicit stable-document requirement and there is no API by
which a caller can supply the required handle.

---

## Findings

### 1. High — the B overload container has one identity but several visibility tables

`CONTAINERIDENTITYRULING` chooses B: an overload set is one named container.
`SCOPEIDENTITYRULING` chooses H-wide: a type belonging to a container is nameable
throughout it. The implementation now checks duplicate type names over the complete
body-plus-ancillary inventory (`Compiler/Compilation.cs:219-243,692-708`), but it
still calls `Scope` independently for each overload body
(`Compiler/Compilation.cs:201-210`). Each call builds a declaration table from only
that body's statements and ancillary scopes (`:133-145`). Nothing merges the one
container's declarations before its bodies and signatures are resolved.

The simplest observable result is a valid cross-body use being rejected:

```ronin
function use (x => number) {
    type token;
    return x;
}

function use (x => text) {
    var local => token;
    return x;
}
```

The audit probe produced `UnknownType` at the second body's `token`. Moving that use
into the second overload's signature did the same:

```ronin
function use (x => number) { type token; return x; }
function use (x => token)  { return x; }
```

This is not a request to reverse B. Under B these bodies deliberately share one type
namespace; the existing cross-body `Shadowed` check is already enforcing that fact
for duplicate declarations. Visibility must use the same extent as uniqueness.

There is a second consequence in signature classification. `Declarations.Of`
resolves and classifies every inherited overload set each time it builds a body's
table (`Compiler/Grammar/Declarations.cs:119-175`). `Compilation.Scope` later writes
that body's re-resolved signature back into the enclosing table
(`Compiler/Compilation.cs:176-192`), but the enclosing `Problems` were already
computed and cached. With:

```ronin
function use (x => token)   { type token; return x; }
function use (x => (token)) { return x; }
```

the current compiler emits all three of:

- `Overloaded` — the enclosing table classified the unresolved spellings as
  different;
- `DuplicateSignature` — the first body's table resolved both inherited signatures
  against its local `token` and classified them as equal;
- `UnknownType` — the second body cannot see that same container's `token`.

The first two diagnostics contradict the ledger: this pair cannot be both a
temporary type-selectable overload and a permanent duplicate.

Build one type-declaration table for the named overload container from **all** of its
bodies and ancillary transparent scopes, resolve every owning signature once against
that table, classify the set once, and then use that common type environment while
adding each body's own value bindings. Add body/body, body/signature, and
ancillary/signature visibility witnesses, plus the equivalent-spelling diagnostic
witness above asserting exactly `DuplicateSignature`, never `Overloaded` or
`UnknownType`.

### 2. Medium — `Requirement` is not a structural, owned set value

`VARIABLEANDMODULE` Q4b requires a collection of requirement records “deduped
whole,” each carrying the pattern, participating type tuple, and provenance. The new
record has those three fields, but it is a generated record over
`IReadOnlyList<Sort>` (`Compiler/Checking/Sort.cs:278-291`). Generated record equality
compares that interface value by its own equality—list reference identity—not by the
sorts in the tuple.

Two separately constructed but semantically identical requirements therefore both
survive the `HashSet`:

```csharp
var site = source.Span(0, 5);

variable.Requirements.Add(new Requirement(
    Pattern.Parse("print _"), [new Sort.Scalar("number")], site));
variable.Requirements.Add(new Requirement(
    Pattern.Parse("print _"), [new Sort.Scalar("number")], site));

Assert.Single(variable.Requirements); // fails: count is 2
```

The maintained test adds the **same record instance** twice, so it exercises the
`HashSet` rather than the record's semantic equality.

The record also retains the caller's collection. An array passed as `Operands` can be
mutated from `number` to `text` after construction, and the requirement immediately
reports `text`. That violates the project's ownership discipline and makes an
interface already stored in a set change under it.

Implement structural operand equality and hashing and copy the operand tuple at
construction. As with `Sort.Function` and `Container.Segments`, the public read-only
surface must front owned storage. Add independently constructed equal-record and
caller-array-mutation regressions; retain the existing different-operands control.
If propagated requirements need more than the current origin `Span`, shape that
provenance value before the constraint pass rather than widening it after consumers
exist.

### 3. Medium — pathless module identity belongs to a compilation, not its document

`VARIABLEANDMODULE` Q5b expressly rules that a buffer identity must belong to the
editor document, survive snapshots, and not be minted per `SourceText` or per
compilation. Current `Compilation` instead creates a private token in its own field
(`Compiler/Compilation.cs:43-48`) and uses it for the root whenever `Source.Path` is
null (`:113-121`). There is no overload or context parameter through which the
document owner can supply the stable token.

The exact same immutable source object therefore changes type identity when compiled
twice:

```csharp
var source = new SourceText("type token; var x => token;\n");
Sort first  = Assert.Single(Compilation.Of(source).Types).Type;
Sort second = Assert.Single(Compilation.Of(source).Types).Type;

Assert.Equal(first, second); // fails: two per-compilation Buffer tokens
```

The maintained test proves two **different** pathless compilations are different
modules, which closes `REAUDIT56`'s collision witness, but it does not prove stable
identity for one document. The new expiry row accurately names the approximation;
recording it prevents concealment but does not implement the ruling, and the present
API makes the successor impossible to provide without another refactor.

Let the compilation entry point accept a `ModuleIdentity` or stable document handle
from its owner. The one-shot compiler may mint a fresh buffer handle at its boundary;
the language server/document owner must reuse one across edits. Test both halves:
different buffer handles produce different named types, while the same handle across
new source snapshots and compilations preserves identity. Keep `Path` and `Buffer` as
distinct typed cases; that part of the ruling is correctly implemented.

---

## Disposition of `REAUDIT56`

| Prior finding | Reassessment |
|---|---|
| 1. patterned non-function terminates compilation | **Closed.** Datum and datatype patterns produce the dedicated `Parameterized` finding, while function patterns remain legal; the invalid cast is gone. |
| 2. H-wide type unavailable in its owning signature | **Direct witness closed.** Body-local and parameter-default types resolve in the owning function's parameter and return annotations, and the stored signature sorts are updated. Across several bodies of the one B container, the same visibility rule is still broken (finding 1). |
| 3. overload uniqueness omits ancillary declarations | **Closed.** `TypesOf` includes ancillary scopes recursively; ancillary/ancillary and body/ancillary collisions are maintained with source-order and negative controls. |
| 4. inference-variable requirement state diverges and value is incomplete | **Identity half closed.** `Variable` has a private constructor, a supply, reference equality, and a record with the ruled fields. The record's operand tuple is neither structural nor owned (finding 2). |
| 5. all pathless sources share one module | **Collision witness closed; stability ruling open.** Typed `Path`/`Buffer` roots distinguish pathless compilations, but the buffer token is per compilation rather than per document (finding 3). |

## What the implementation gets right

- Only a function may declare a pattern. Invalid datum and datatype shapes produce a
  source finding rather than reaching signature code or throwing.
- The original function-local signature witnesses now resolve against the function's
  own declarations, and their semantic sorts are written back onto the registered
  signature.
- Ancillary default delegates participate in overload-wide H uniqueness, including
  block-nested types and body/ancillary pairs.
- Inference variables can only be minted by `Variable.Supply`; equality is reference
  identity, so the former equal-but-independently-mutable state is unconstructible.
- `Requirement` now at least names the correct three semantic components and no
  solver machinery was prematurely added.
- `ModuleIdentity` is a genuine type with separate `Path` and `Buffer` cases.
  `Container` owns its segment sequence and compares the typed root plus segments
  structurally, preserving the “never parse a rendered identity” rule.
- Two distinct pathless compilations are distinct modules, while uses within one
  compilation share one identity.
- The per-compilation buffer approximation is explicitly ledgered with the stable
  document-handle successor rather than being presented as complete.

## Verification record

Temporary audit probes were removed before this report was written.

- Inspected the complete `a5bce23..e00ee38` production, test, diagnostic, ledger,
  and handoff diff, and read `REAUDIT56RELAY.md` and the untracked but governing
  `VARIABLEANDMODULE.md` completely.
- Cross-overload body visibility probe: **failed as finding 1 predicts** with
  `UnknownType` in the second body.
- Cross-overload signature visibility probe: **failed as finding 1 predicts** with
  `UnknownType` in the second signature; the body-specific re-resolution consequently
  leaves that stored parameter slot null.
- Equivalent local signature probe: **failed as finding 1 predicts** with both
  `Overloaded` and `DuplicateSignature`, plus `UnknownType`.
- Independently constructed equal-requirement probe: **failed as finding 2
  predicts**—both records remained in the set.
- Requirement ownership probe: **failed as finding 2 predicts**—mutating the caller's
  operand array changed the stored requirement from `number` to `text`.
- Same-pathless-document recompilation probe: **failed as finding 3 predicts**—the
  two named sorts compared unequal.
- `dotnet restore Ronin.sln --locked-mode`: passed.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror`:
  passed with zero warnings and zero errors.
- Full Debug suite: **1,278 passed, 0 failed, 0 skipped**.
- Exact Release coverage suite: **1,278 passed, 0 failed, 0 skipped**;
  **4,057/4,057 lines**, **2,831/2,831 branches**, and 100% methods.
- `dotnet list Ronin.sln package --vulnerable --include-transitive`: no known
  vulnerable direct or transitive packages in the configured source.
- Exact changed-file `dotnet format --verify-no-changes`: passed, formatted zero
  files, and emitted no diagnostics.
- `git diff --check`: passed before this report was added.
- No production or maintained test file was changed by this audit. The working tree
  already contained untracked `REAUDIT56.md` and `VARIABLEANDMODULE.md`; both were
  preserved. This report is the only new audit artifact from this pass.
