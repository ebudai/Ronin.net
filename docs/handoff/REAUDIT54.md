# Re-audit 54 — semantic Type-term foundation

**Audited:** `57f36e3..207409f`, principally `207409f`, against
`SEMANTICCHECKERSCOPING`, `CHECKERSCOPINGRULINGS`,
`BASERESOLUTIONRULING`, the expiry ledger, and the adjacent declaration and
resolution machinery.

**Date:** 2026-08-15

## Result

**Not signed off. Two high-severity and two medium-severity findings remain.**

The seven annotation-spellable shapes are translated cleanly, constructor and
function composition are structurally compared, owned function parameter lists
are retained, malformed single-type groups propagate as no sort, and the complete
maintained gate battery is green. The base-resolution deferral is also correctly
ruled and ledgered.

The claimed foundation is nevertheless not the ruled Type term. Opaque declared
types use their spelling as identity, so different declarations of the same name
become the same type. The confirmed `Action` and requirement-bearing `Variable`
cases are absent. Signatures still carry spelling only, which already
misclassifies semantically identical parameter types. Finally, a bounded-out type
annotation disappears from the semantic store without a diagnostic.

---

## Findings

### 1. High — opaque named types are identified by spelling, not declaration identity

The ruled term is `Named(SymbolId)`, and Q3 says an opaque named type unifies only
with **the same declaration**. The implementation instead constructs
`new Named(name.Words)` and defines equality and hashing solely from `Name`
(`Compiler/Checking/Sort.cs:65-70,184-197`). `Node.Name` and
`SymbolTable.Names` also carry only the spelling and kind, so declaration identity
has already been erased before `Sort.Of` runs.

That is observably wrong in legal sibling scopes. This audit probe compiled with
no findings:

```ronin
function left {
    type token;
    var x => token;
}
function right {
    type token;
    var y => token;
}
```

The compilation retained two `Sort.Named("token")` values, but
`Assert.NotEqual(named[0], named[1])` failed: they compare equal. The declarations
are distinct opaque types; neither is in the other's scope. Once unification,
overload filtering, or monomorphisation keys use this equality, values of one can
be accepted as the other and distinct instantiations can share a cache entry.

This cannot be repaired inside `Named` by changing its string comparison. Carry a
stable declaration identity from the symbol table through the resolved name into
the semantic type. Keep the spelling separately for presentation. Add at least a
sibling-scope regression, and preferably a cross-compilation/module identity test
before that boundary begins comparing types.

### 2. High — the confirmed `Action` and requirement-bearing `Variable` cases are missing

`SEMANTICCHECKERSCOPING` §3 defines nine cases, including `Action` and
`Variable(id)`. `CHECKERSCOPINGRULINGS` Q1 explicitly confirms `Action` as a real
case, specifies `nothing : Optional(Variable(fresh))`, and says that `Variable`
must be shaped now with room for its inferred requirement set. The ruling is
unusually direct about staging: it does not require the constraint machinery now,
but does require the case's shape now so construction sites are not rewritten
later. It then confirms that the scope and order are to be taken as written.

`Sort` contains only the seven shapes an annotation can spell. Its documentation
deliberately defers `Action` and `Variable` until the first pass that constructs
them (`Compiler/Checking/Sort.cs:28-35`), and there are no such nested cases in the
class. That substitutes a new staging decision for the designer's explicit one.
It also leaves step 2 without a representation for its first under-determined
values (`nothing` and `[]`) and without the action result against which value
positions are compared.

The coverage rationale does not force this omission. Direct equality/hash and
ownership tests make foundation cases reachable in exactly the same way the
current tests construct `Error`, `Function`, and `Named`; source construction
arrives in the consuming pass. Add `Action` and an identity-bearing `Variable`
whose contract already owns or otherwise accommodates the inferred requirement
set, with equality/hash/ownership tests. Do not represent either as null.

### 3. Medium — the unruled signature deferral preserves spelling-based type semantics

The programmer correctly surfaced this as a deliberate deferral, but it is not a
ruled one. `SEMANTICCHECKERSCOPING` §5 puts storage on annotations **and
signatures** in scope; §6 step 1 says “signatures carry `Type`s beside the
spelling”; `CHECKERSCOPINGRULINGS` §9 says to take that scope and order as written.
The designer's step 2 is initializer and return mismatch, not signature-sort
binding.

Today `Declarations.Signature.Types` is still a block of strings
(`Compiler/Grammar/Declarations.cs:503-511`), populated directly from written
annotations (`:355`). Duplicate classification length-prefixes those strings
rather than comparing resolved sorts. `Compilation.Types` is a separate flat
`(Span, Sort)` list with no annotation owner, declaration, signature position, or
modifier set (`Compiler/Compilation.cs:418-428`). It therefore does not make a
signature carry the resolved types “beside the spelling,” and does not yet provide
the ruled place beside the typed occurrence for `fast`.

This is already observable rather than merely a future integration preference:

```ronin
function use (x => number) { }
function use (x => (number)) { }
```

Both annotations resolve to the same `Sort.Scalar("number")`, but the compilation
reports one `Overloaded` finding and no `DuplicateSignature`, because the spellings
differ. Under equality unification these declarations are the same signature and
can never be selected by argument type. This distinction matters beyond wording:
the overload refusal is ledgered to expire, while a true duplicate must survive.

Bind each resolved parameter (and return) sort to its owning signature while
retaining the source spelling needed for presentation. Use the semantic sort for
duplicate classification and retain the owner/modifier relationship needed by the
later checker. If the designer intends to move all of this to step 2, record that as
a superseding ruling; the current governing order says step 1.

### 4. Medium — an over-limit written annotation vanishes without a type or finding

`Resolver.Resolve` returns `TooLong` beyond `Resolver.MaxLexemes`. The annotation
integration handles `Ambiguous`, `NoParse`, and a successful tree, but has no
`TooLong` branch (`Compiler/Compilation.cs:255-289`). The new maintained test then
asserts only that the type store is empty for 257 nested `optional` tokens
(`Test/Unit/Sorts.cs:140-144`); it does not assert a finding.

An audit probe asserted the required source-level behavior and failed:

```csharp
var chain = string.Concat(Enumerable.Repeat("optional ", Resolver.MaxLexemes + 1));
var compilation = Compilation.Of(
    new SourceText($"var z => {chain}number;\n", "probe.ron"));

Assert.NotEmpty(compilation.Findings); // failed; Findings and Types were both empty
```

This turns a resource ceiling into silent semantic erasure. In step 2 the written
annotation can look indistinguishable from an omitted one or simply be skipped,
which violates the governing requirement that an empty finding collection be the
negative-test failure condition.

Give `TooLong` a production diagnostic at the annotation span and assert both that
finding and the absence of a fabricated sort. The resolver ceiling itself is not
the issue; silence at its compilation boundary is.

---

## Disposition of the two declared deferrals

| Deferral | Reassessment |
|---|---|
| Base resolution | **Accepted.** `BASERESOLUTIONRULING` selects C; `Test/Expiry.cs` records the parser → type-operator table → `Bases`/`Unions` successor, provenance, and the condition that deferral is safe only while nothing consumes those collections. A production search found no consumer: only the collection declarations and ledger prose mention them. |
| Signature-sort binding | **Not accepted under the current ruling.** It is expressly part of step 1, no superseding ruling moves it, and the `number`/`(number)` probe demonstrates a present semantic-classification consequence. See finding 3. |

## What the implementation gets right

- `Scalar`, `Error`, `List`, `Optional`, `Lookup`, and `Function` have coherent
  structural equality and hashes, and `Function` owns a copy of its parameter
  list.
- Annotation resolution now retains successful semantic translations instead of
  discarding the resolver tree.
- Nested constructors, singleton grouping, zero/one/many-parameter function types,
  keyed and multi-part invalid positions, null propagation, cross-kind inequality,
  and equal-value hash behavior are covered.
- `fast` has not been smuggled into `Sort`; there remains exactly one number sort.
- The supersession edits correctly close the old aggregate miss fork, preserve one
  symbol table, strike the stale optional-modifier claim, and record the ruled but
  unbuilt expression ascription.
- The base-resolution ledger now states the actual parser dependency and the
  condition that makes the deferral safe.

## Verification record

Temporary audit probes were removed before this report was written.

- Inspected the complete `57f36e3..207409f` production, test, ledger, and handoff
  diff, plus adjacent symbol-table identity, node identity, declaration signature
  classification, annotation traversal, and scope construction.
- Opaque-identity probe: **failed as finding 1 predicts** — two legal sibling
  declarations retained two named sorts which compared equal.
- Semantic-signature probe: **failed as finding 3 predicts** — `number` versus
  `(number)` produced `Overloaded`, not `DuplicateSignature`.
- Annotation-ceiling probe: **failed as finding 4 predicts** — both findings and
  stored types were empty.
- `dotnet restore Ronin.sln --locked-mode`: passed.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror`:
  passed with zero warnings and zero errors.
- Full Debug suite: **1,261 passed, 0 failed, 0 skipped**.
- Exact Release coverage suite: **1,261 passed, 0 failed, 0 skipped**;
  **3,928/3,928 lines**, **2,727/2,727 branches**, and 100% methods.
- `dotnet list Ronin.sln package --vulnerable --include-transitive`: no known
  vulnerable direct or transitive packages in the configured source.
- Exact changed-file `dotnet format --verify-no-changes`: passed, formatted zero
  files, and emitted no diagnostics.
- `git diff --check`: passed before the report was added.
- No production or maintained test file was changed by this audit. The working
  tree was clean before this report was added; this report is the only audit
  artifact.
