# Fresh audit 21 — the type-checker slice

**Audited:** `a41bde4..c797b5f`, the type-half handoff, its four rulings, and the
three implementation commits for kind-aware resolution, source annotation
diagnostics, function-type arrows, and `fast number`.

**Date:** 2026-08-14

## Result

**No sign-off. The type checker is not mostly implemented.** What has landed is
a useful and substantially sound **type-annotation resolver**: it can decide
whether annotation text names a type, report unknown types, preserve
type/value kinds in one symbol table, and expose the ruled arrow ambiguities.
It does not yet construct, retain, infer, or unify semantic types, and therefore
does not check a program's values against its annotations.

The distinction is observable through the ordinary source path. All of these
programs compile with no finding:

```ronin
var x => number = "text";
function f => number { return "text"; }
var xs => list of number = ["text"];
var m => lookup text => number = [];
```

The last line also still constructs the empty square group as a list rather than
the expected lookup. A final bare value is not sugared into a return either:

```ronin
function f => number { 1; }
```

That source is clean, but the tree contains the bare literal and no synthetic
return site. This is the known `TAILSUGAR.md` dependency, not an implementation
of it.

Inside the narrower resolver slice, I found four additional semantic defects:
`fast` is absent from the registry the ruling expressly requires; the generic
modifier grammar accepts it in unrelated or nonsensical positions; a valid
outermost grouped type is rejected by source capture; and a keyed type group is
prematurely diagnosed as an unknown type instead of being carried to the
checker as ruled. The prescribed changed-file formatting gate also fails,
although its whitespace violations predate this slice.

The maintained build health is otherwise excellent: locked restore, the
warning-as-error Release build, all 1,249 Debug and Release tests, exact 100%
line/branch/method coverage, and the direct plus transitive package
vulnerability audit all pass. The findings are omitted semantic relationships
and source domains, not uncovered production lines.

---

## Findings

### 1. Annotation resolutions are discarded; no semantic type checking occurs

**Severity: high completion blocker — annotations currently reject unknown
spellings but do not constrain any value, function, call, or aggregate. This is
a known mid-slice boundary, not a regression.**

`Compilation.Annotations` creates a type-kind `Resolver`, resolves each
`Type.Unresolved`, and emits only `Ambiguous` or `UnknownType`
(`Compiler/Compilation.cs:255-281`). A successful `Resolution` is not retained
or converted into a semantic type. `Grammar.Type` remains an unresolved
reference (`Compiler/Grammar/Type.cs:136-157`), and the repository contains no
semantic type representation, type-checking pass, unifier, expected-type
carrier, or type-mismatch finding.

The nearby exit analysis states the boundary directly: return inference “waits
for a type to unify into” (`Compiler/Compilation.cs:297-301`). Function
signatures similarly retain annotation **spellings** for duplicate/overload
classification, not resolved types (`Compiler/Grammar/Declarations.cs:189-191,
503-511`).

Temporary source probes produced:

| source | required behavior | current result |
|---|---|---|
| `var x => number = "text";` | initializer/type mismatch | clean |
| `function f => number { return "text"; }` | return/type mismatch | clean |
| `var xs => list of number = ["text"];` | element/type mismatch | clean |
| `var m => lookup text => number = [];` | expected-type `Lookup.Empty` | clean; resolver still makes a list |
| `function f => number { 1; }` | final value is a return site | clean; no sugar or inferred answer |

Consequently the following expressly ruled work is still open:

- initializer, return, operator, call, and assignment checking;
- type inference and equality unification, including the `error` bottom type;
- list-element and lookup-key/value unification from `EAGGREGATES2.md` §7;
- outward-in expected typing of `[]` from §5;
- type-directed overload candidate elimination and expiry of the temporary
  declaration-site refusal;
- function return inference, recursion/base-case solving, and tail sugar;
- constructor arity and group multiplicity checking; and
- resolution/checking of type-definition bases, already recorded in the expiry
  ledger.

**Recommendation:** describe this delivery as the annotation-resolution
foundation, not as a mostly complete checker. The next pass needs a semantic
type term that successful annotation resolution returns and stores, followed by
expression inference/unification against declared and expected types. Maintain
source-level negative tests for each row above; an empty finding collection is
the failure condition. Then make tail sugar and overload expiry consumers of
that same typed tree rather than separate text-based passes.

### 2. `fast` is a lexer keyword but is absent from the name-rule registry

**Severity: high — this violates the explicit `TYPEHALFRULINGS.md` §1 /
`FIVERULINGS.md` §0 invariant and permits the silent capture the ruling named.**

The implementation comment says `fast` is a keyword “so no name may contain the
word” and cites the table requirement (`Compiler/Lexicon/Modifier.cs:13-27`).
The code adds only a `Modifier` token (`Modifier.cs:29-37`). It adds no descriptor,
symbol, shape, or reserved entry to `SymbolTable.Supplies`
(`Compiler/Resolution/Resolver.cs:1896-1961`). `SymbolTable.Whole` and every
generated registry therefore remain unaware of it (`Resolver.cs:1982-1985`).

`Name.Parse` explicitly allows a `Modifier` to lead a name
(`Compiler/Grammar/Name.cs:70-83`), and declaration refusal consults only
`SymbolTable.Whole` and names already in scope
(`Compiler/Grammar/Declarations.cs:276-302`). The complete source witness is
clean:

```ronin
var fast number => number = 1;
function f => number { return fast number; }
```

The declaration enters the value table, and the nested `return (_)` call can use
it as its argument. This is not just a disconnected declaration accepted by the
parser.

The omissions are visible in the safeguards too:

- `fast` is not in the supposedly exhaustive `Boundaries.Keywords` data
  (`Test/Unit/Boundaries.cs:29-36`), so punctuation/EOF boundary coverage did not
  expand with the keyword;
- the new keyword test checks only `"fast number"` followed by whitespace
  (`Test/Unit/Keywords.cs:94-103`); and
- neither `docs/reference.md` nor `docs/reserved-words.txt` contains `fast`,
  because both derive from the descriptor/symbol registry rather than the lexer.

**Recommendation:** put `fast` in the single registry the name rules and
generated documentation consume, using a reserved/non-resolving entry if the
current value/type kinds cannot honestly describe it. Refuse declarations whose
relevant name position contains it, and maintain both the direct declaration
and nested-use witness. Add `Fast.keyword` to the exhaustive keyword-boundary
census so the next keyword cannot repeat this split.

### 3. The generic modifier path accepts `fast` everywhere and validates nothing

**Severity: medium — invalid representation requests compile cleanly and are
silently ignored.**

`Modifiers.Parse` consumes any number of every `Modifier` without position,
compatibility, or duplicate checks (`Compiler/Grammar/Modifiers.cs:27-34`). That
same parser is shared by annotation sites, type declarations, basic/applicative
scopes, loops, and conditional scopes. For example,
`Conditional<T>.Parse` accepts modifiers before `if`, `while`, and `when`
(`Compiler/Grammar/Scope.cs:330-355`).

Every one of these source probes compiled cleanly:

```ronin
var x => fast truth;
var x => fast list of number;
var x => fast fast number;
fast if true { }
fast while true { }
fast when true { }
fast type box;
```

Only the positive `var pace => fast number` case is maintained
(`Test/Integration/TypeAnnotations.cs:44-59`). The annotation walk strips the
modifier and resolves the remaining type, so it cannot distinguish `fast
number` from `fast truth`; no later checker consumes `Fast`. On scopes and type
declarations it is simply stored and ignored.

`fast` is a representation qualification of a `number` occurrence, including
numeric-producing signatures described by the numeric rulings. It is not a
generic execution/scope modifier, not applicable to another type constructor,
and not meaningful twice.

**Recommendation:** represent and validate `fast` at the annotated type
occurrence rather than making it globally interchangeable with execution and
visibility modifiers. Reject a non-`number` target, duplicate `fast`, and every
non-type position. Maintain the positive datum/signature cases beside a table of
the negative sources above.

### 4. Source parsing rejects an outermost grouped type that the type resolver admits

**Severity: medium — grouping is ruled as load-bearing in type position, but a
whole grouped annotation cannot reach the resolver.**

Type mode is established only around `Reference.Parse`
(`Compiler/Grammar/Type.cs:147-156`). The shared reference parser treats a single
anonymous temporary as “not a reference” and returns it through the value-only
`alone` channel (`Compiler/Grammar/Reference.cs:87-109`). `Type.Unresolved` does
not consume that channel. Thus these valid grouped annotations fail during
parsing:

```ronin
var x => (number);
var callback => (text => number);
function use (callback => (text => number)) { return; }
```

The first two produce `Malformed: expected a type after '=>'`; the parameter
case destabilises its enclosing parse and produces `expected definition`.
This is a parser/resolver reachability split: direct type resolution already
admits round grouping, and source tests prove it only when the group fills a
larger constructor hole (`Test/Unit/TypeResolution.cs:67-75`).

**Recommendation:** give type capture a route for a lone round `Temporary` to
become a type-group reference/tree, while continuing to reject lone anonymous
values in value-reference parsing. Maintain source-path tests for `(number)`, a
fully grouped function type, and a grouped function type in a parameter.

### 5. Keyed round groups are rejected by resolution instead of deferred to checking

**Severity: medium diagnostic/phase defect — this contradicts the explicit
“admit grouping; defer arity and multiplicity” ruling.**

`TYPEHALFDECISIONS.md` §3 names `optional (a = b)` as the keyed-group example to
carry through resolution and reject later by constructor multiplicity. The
current group resolver searches for `=` only when the brackets are a square
collection:

```csharp
var associates = collection ? Associating(lexemes, start, end) : [];
```

(`Compiler/Resolution/Resolver.cs:380-418`). A round group therefore leaves `=`
inside a span no expression can consume and produces `NoParse`.

The source witness removes unknown component names from the equation:

```ronin
type a;
type b;
var x => optional (a = b);
```

Actual result: `UnknownType` for the entire `optional (a = b)`, including the
misleading remedy to declare that whole spelling as a type. Required result for
this slice: resolution succeeds and preserves the keyed/multiplicity structure;
the semantic checker then reports why `optional (_)` cannot take it.

**Recommendation:** let type-mode round groups preserve keyed entries as a
kind-correct group without treating them as a runtime lookup value. Add the
declared-`a`/declared-`b` source test now, with the expected result moving from
clean resolution to the checker-specific multiplicity finding when finding 1 is
implemented.

### 6. The prescribed changed-file formatting gate is not green

**Severity: low process finding — the semantic build is clean, but the handoff's
“every commit passes all of it” gate statement is currently false.**

The exact `dotnet format Ronin.sln --verify-no-changes --include <changed files>`
check exits 2. It reports whitespace errors in changed files including
`Compiler/Grammar/Type.cs`, `Compiler/Parser.cs`, and `Test/Unit/Admission.cs`,
plus the documented pre-existing `IDE1006` warnings.

Blame shows the reported whitespace lines predate this type slice (some date to
2023), so this is not evidence that the new logic was formatted incorrectly.
It does mean a touched-file gate cannot be reported as passing and cannot
protect this change set until the old whitespace in those files is normalised or
the gate is scoped to changed hunks by an explicit mechanism.

**Recommendation:** normalise the formatter's whitespace-only changes in a
separate commit, then rerun the same changed-file command. Do not weaken the
semantic warning-as-error build, which already passes.

---

## What the implementation gets right

The delivered foundation is worth keeping. In particular:

- type and value symbols share one table and are filtered by `SymbolKind`, as
  ruled;
- supplied scalar types and `optional`, `list of`, and arrow-spelled `lookup`
  constructors resolve through the same DP as values;
- type mode suppresses value literals, value operators, `old`, and square-list
  readings while retaining round grouping;
- the parser's `Typing` flag admits `=>` only during type reference capture, so
  value delegates and the value grammar are not widened;
- the function arrow is non-associative: a bare two-arrow chain yields two
  readings, and `lookup text => number => truth` yields all three ruled readings
  with bracket repairs;
- annotations in parameters, returns, nested function bodies, and type member
  scopes are reached with the correct scope table and are not reported twice;
  and
- unknown type diagnostics point at the annotation site, while declared opaque
  types are immediately usable.

Those are real compiler capabilities. They are the resolution half that a type
checker can consume; the central issue is that no consumer exists yet.

## Adversarial verification

Temporary test files were added only to execute probes and were removed before
the report was written.

| probe | result |
|---|---|
| scalar initializer conflicts with declared type | **failed:** clean |
| explicit function return conflicts with declared type | **failed:** clean |
| list element conflicts with declared element type | **failed:** clean |
| empty list under expected lookup type | **failed:** clean; still list-kind |
| final value under declared return type | **failed:** clean; no tail return |
| declared and nested-used `fast number` value name | **failed:** clean |
| `fast` on wrong types, duplicated, and on scopes/type declarations | **failed:** all clean |
| outermost `(number)` and `(text => number)` annotations | **failed:** malformed |
| declared names inside `optional (a = b)` | **failed:** `UnknownType` |
| supplied/declared types, nested constructors, source arrow types | passed |
| non-associative bare arrow chain | passed: two readings |
| two-arrow lookup/function chain | passed: three readings |

## Verification record

- Inspected the complete `a41bde4..c797b5f` production/test diff and adjacent
  parser, resolver, symbol registry, declaration, scope, diagnostic, aggregate,
  exit, runtime, and expiry code.
- Read the modern type rulings and dependencies, including
  `TYPEVOCABULARY.md`, `TYPECHECKERHANDOFF.md`, `TYPEHALFDECISIONS.md`,
  `TYPEHALFRULINGS.md`, `TYPEHALFARROW.md`, `ARROWASSOCIATIVITY.md`,
  `TAILSUGAR.md`, `EAGGREGATES2.md`, and `REAUDIT47RULING.md`.
- `dotnet restore Ronin.sln --locked-mode`: passed.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror`:
  passed with zero warnings and zero errors.
- Full Debug suite: **1,249 passed, 0 failed, 0 skipped**.
- Exact Release coverage suite: **1,249 passed, 0 failed, 0 skipped**;
  **3,829/3,829 lines**, **2,631/2,631 branches**, and 100% methods.
- `dotnet list Ronin.sln package --vulnerable --include-transitive`: no known
  vulnerable direct or transitive packages in the configured source.
- Changed-file `dotnet format --verify-no-changes`: **failed**, finding 6.
- `git diff --check`: passed.
- `git diff -- Compiler Test`: empty after probe isolation. No production or
  maintained test file was changed by this audit.

The working tree was clean before this report was added. This report is the only
audit artifact.
