# Fresh audit

Audited at commit `2b741d0` (`Convert the cascade and initialisation rules to
findings`).

The isolated components are generally improving, but their joins are still
unsafe. The real executable can hang, silently accept malformed source, and
omit diagnostics that unit tests prove only through hand-connected paths.

No source changes were made as part of the audit.

## Release-blocking findings

### 1. Parser errors can cause a permanent loop

`var +;` never completes. The actual executable was still running when killed
by a timeout.

`Datum.Parse` (`Compiler/Grammar/Datum.cs:50`) consumes the offending token
through its local parser while leaving `current` unchanged. `Module.Parse`
receives a non-null error statement, adds it, then starts again at the same
`var`.

Every successful parse, including recovery nodes, needs a hard invariant that
it advances the caller. Ideally the parser framework should enforce this rather
than relying on every production.

### 2. Most parser errors are treated as successful compilation

`function ;` exits 0 and reports one statement and zero problems.

Parser errors implement `IError` (`Compiler/Error.cs:7`), but they are also
`Statement` subclasses, so `Module.Parse` accepts them.
`Program.Report` (`Compiler/Program.cs:98`) recognizes only
`Module.UnexpectedInputError`; it never traverses statements for other
`IError` objects.

This affects malformed imports, exports, functions, data, iterators, reactive
scopes, and conditionals. Unknown recovery nodes are also never surfaced as
problems.

### 3. Finding 9's outstanding syntax cases are confirmed

The executable accepts all of these with exit code 0:

```ronin
var x => = 1;
function f => {}
type T = ;
```

The permissive paths are:

- `Compiler/Grammar/Datum.cs:63`
- `Compiler/Grammar/Function.cs:36`
- `Compiler/Grammar/Type.cs:32`
- `Compiler/Grammar/Scope.cs:174`

A consumed `=>` or `=` must commit the parser to either a value/type or a
structured error. It cannot remain optional afterward.

### 4. Nested diagnostics are not connected to production

This file exits successfully with zero problems:

```ronin
type Box {
    var x => Number;
    var x => Number;
}
```

`Program` calls `Declarations.Of` only on the module's outer statements
(`Compiler/Program.cs:104`). It never recursively visits function, type, or
scope bodies.

The nested-scope tests pass because `ScopeBuilding.Nested`
(`Test/Unit/ScopeBuilding.cs:129`) parses two independent strings and manually
supplies the enclosing `Declarations`. That validates merging but not compiler
traversal.

The same production gap presently affects resolution, overload selection,
cascade diagnostics, writer diagnostics, initialisation order, and evaluation.
Those components exist, but no source-to-runtime path invokes them.

### 5. Cascade detection can allow an undeclared feedback ring

`Cascades.Cycles` (`Compiler/Runtime/Cascades.cs:70`) is a standard DFS
back-edge collector, not an enumeration of "every ring" as its contract states.

The audit constructed:

- feedback A -> B and C
- feedback B -> A
- non-feedback C -> B

The DFS finds and filters the allowed A-B-A cycle, settles B, and then misses
A-C-B-A. The result is zero findings even though C did not opt into feedback.

This is an actual safety-rule bypass. Use SCC analysis for legality: an SCC is
cyclic if it has multiple members or a self-edge, and it is allowed only when
every participant meets the feedback rule. Enumerating every elementary cycle
is unnecessary and potentially exponential.

### 6. Ambiguity overflow is only partially fixed

Most resolver products saturate, but `Resolver.Group`
(`Compiler/Resolution/Resolver.cs:169`) still performs raw multiplication:

```csharp
count *= part.Count;
```

It saturates only after the entire group. A group containing 64 independently
ambiguous parts wraps the count to zero and returns `Resolved`.

Saturate each multiplication, exactly as `Match` and `Expression` already do.

### 7. `Handling` suppresses failures beyond the expression it protects

`Graph.Handling` (`Compiler/Runtime/Graph.cs:240`) uses one graph-wide integer.
While `otherwise` is handling one read, any dirty nested `let` recomputed
during that read also has adoption disabled.

The audit confirmed that a nested body can read and ignore an `Error`, return
`42`, and make the outer handler observe `42` rather than the error or
fallback.

Handling must be scoped to the current evaluation/adoption frame, not globally
across the graph.

### 8. A failed duplicate `When` mutates the live graph

`Graph.When` (`Compiler/Runtime/Graph.cs:130`) writes `whens[name]` before
`Let` checks whether the node already exists.

A duplicate declaration throws, but the original trigger node remains paired
with the new body and mode. The audit confirmed that firing the original
condition afterward executes the rejected body.

Validate all preconditions first, or roll back atomically.

### 9. Constants can overwrite or mask graph nodes

`Graph.Constant` (`Compiler/Runtime/Graph.cs:199`) checks neither `constants`
nor `nodes`. Meanwhile `Read` checks constants first.

Confirmed behavior:

```text
Var("x", 1)
Constant("x", 2)
Read("x") == 2
```

The original node and its edges still exist but are unreachable by name.
Declaring a node after a constant has the same masking problem. Declaration
uniqueness must span both stores.

### 10. Effect-body defects can still terminate the always-running session

`let` bodies are converted to `Fault`, but `Graph.Fire`
(`Compiler/Runtime/Graph.cs:404`) calls effectful bodies directly. Any
exception escapes `Step`, possibly after earlier writes have been queued.

If "always-running" applies to the runtime rather than only pure nodes, effect
bodies need a fault boundary and a defined policy for writes queued before the
failure.

## Diagnostics findings

### 11. Cross-scope findings point at the wrong declaration

`Rules.Glue` (`Compiler/Diagnostics/Rules.cs:90`) always makes the name
primary. If an outer name is legal until an inner pattern is declared, it still
blames the outer file, even though the message says the later declaration
gives way.

The audit's nested R5 harness reported `outer.ron` as primary. The corresponding
test at `Test/Unit/ScopeBuilding.cs:217` checks the kind and symbols but never
checks `Primary`.

R6 has the same structural flaw. `Anchors`
(`Compiler/Diagnostics/Rules.cs:52`) always makes the longer anchor primary,
regardless of which declaration is new.

Rule input needs provenance such as scope depth or declaration ownership, not
merely `(Pattern, Span)`.

### 12. The "ring width" test is another hand-built substitute

`AReportStaysReadableAtTheWidthOfACascadeRing`
(`Test/Unit/Findings.cs:119`):

- creates an `Overloaded` finding, not a cascade finding;
- does not invoke `Cascades.Diagnose`;
- does not contain a ring message;
- does not measure width or wrapping;
- only counts related-span lines.

It cannot detect a regression in cascade-ring readability.

### 13. Diagnostic completeness is fragile

Additional issues:

- `Rules.Offender` returns the first conflicting pattern, so fixing that pattern
  can reveal another previously hidden conflict.
- `Diagnostics.Render` uses `_` as the `InitialisationRing` case. A future enum
  value will silently receive a plausible but false initialisation-cycle
  message.
- Finding symbols are free-form dictionary keys. A missing or renamed role
  becomes a runtime `KeyNotFoundException`.
- `ManyWriters` retains writer spans but drops their names from the rendered
  message.
- Counts use culture-sensitive `ToString()`.
- `SourceText` does not validate span bounds, so malformed spans produce
  misleading locations rather than failing near their origin.
- `Declarations.Problems` is lazy and recomputes the scope rules on every
  enumeration.

A typed finding payload or one record per finding kind would remove several of
these failure modes.

## Runtime and data-integrity findings

### 14. Caller-owned mutable collections remain embedded in keys and trees

`Pattern` (`Compiler/Resolution/Resolver.cs:425`) retains the caller's segment
list and is then used as a dictionary key by runtime scopes. Mutating the list
changes its hash. The audit confirmed that an existing declaration becomes
unreachable.

The same ownership problem affects:

- `Node.Group.Parts` (`Compiler/Resolution/Node.cs:58`)
- `Node.Call.Arguments` (`Compiler/Resolution/Node.cs:78`)
- `Declaration.Blocks` (`Compiler/Runtime/Scope.cs:30`)

Copy these at construction, preferably into immutable arrays.

### 15. Deep valid programs can overflow the process stack

These paths are recursive without depth guards:

- resolver pattern matching;
- `Graph.MarkDirty`;
- dependency settling and recomputation;
- cascade DFS;
- initialisation ordering.

A sufficiently deep dependency chain or adversarial statement can terminate
the process with `StackOverflowException`, which cannot normally be recovered.
Graph traversals should be iterative; parsing and resolution need an explicit
complexity/depth budget.

### 16. Text literal evaluation does not implement escapes

`Evaluator.Value` (`Compiler/Runtime/Evaluator.cs:98`) simply removes the
surrounding quotes. The lexer recognizes escaped delimiters, but runtime values
retain escape syntax literally.

Escaped quotes, backslashes, and any future `\n`-style escapes therefore do not
produce the value the source spelling implies.

## Numeric tower

The numeric tower remains entirely outstanding:

- every numeric literal becomes `double`;
- integers above `2^53` silently lose precision;
- exact decimals and rationals are absent;
- `fast number` is not a distinct opt-in representation;
- exactness-preserving roots and transcendentals from `FAILUREMODES.md` are
  absent;
- operator behavior and overload/type integration assume doubles.

Division-by-zero and unsupported-date behavior are genuinely fixed, but they
sit on the provisional double-only evaluator.

## Performance and pessimizations

### 17. The resolver still allocates the full cubic table shape eagerly

For `n` lexemes, `Resolver.Resolve`
(`Compiler/Resolution/Resolver.cs:64`) allocates:

- two `(n+1)^2` `Cell` tables;
- one `(n+1)^2 * 32` `Cell` table;
- a `List` and `Dictionary` eagerly inside every `Cell`.

At 100 tokens this is roughly 346,000 cells and nearly 700,000 empty
collections before useful work begins. At 300 tokens it reaches millions.

The acknowledged wins remain available:

- index only reachable binding powers, roughly six instead of 32;
- allocate cell collections on first offer;
- use triangular span storage;
- pre-index patterns by first anchor word;
- avoid repeated `string.Join` over every word span;
- add cancellation and a statement-token ceiling;
- consider pooling tables for repeated editor calls.

Per-statement resolution limits the normal case but does not solve a pasted or
generated single-statement denial of service.

### 18. Completion and the workbench do unbounded synchronous work

`Workbench.Refresh` (`Scratch/Workbench.cs:121`) runs completion and resolves
every non-empty line on each text change, on the UI thread. There is no
cancellation, debounce, generation check, or long-line limit.

Completion also rescans every name and pattern for suffixes on every request.
A prefix/trie index would avoid repeated splitting and matching.

There are no benchmarks or allocation regression tests, so the expected
resolver wins are currently unguarded.

## Build, CLI, and operational findings

### 19. The SDK is not pinned

`global.json` specifies `8.0.0`, which is not a valid SDK feature-band version.
`dotnet --info` reports the file as invalid, and the project actually built
with SDK `10.0.110`.

`rollForward: latestMajor` would also defeat reproducibility after correcting
the version. Use an installed feature band such as `8.0.4xx` and a deliberate
roll-forward policy.

### 20. Nullable and stronger analyzers remain disabled

Nullable is disabled in all three projects. An opt-in `latest-all` analyzer
build completed with 123 warnings, including:

- multidimensional resolver arrays;
- culture-sensitive diagnostics;
- public/internal surface issues;
- null argument handling;
- completion dispatch opportunities;
- the intentionally broad runtime catch.

The ordinary warning-as-error build passes because most of those analyzers are
not enabled.

### 21. Formatting and continuous verification are absent

`dotnet format --verify-no-changes` fails across many source and test files.
There is no:

- CI configuration;
- `.editorconfig`;
- shared `Directory.Build.props`;
- formatter gate;
- package lock;
- benchmark project.

Package vulnerability checks are clean, so the dependency-update portion of
the prior audit is real.

### 22. Directory traversal follows symbolic links

The audit pointed the executable at a directory containing a symlink back to
itself. It compiled the same source 41 times through increasingly long paths
before filesystem loop handling stopped traversal.

`Program.Sources` (`Compiler/Program.cs:64`) should reject reparse-point or
symlink directories, or track visited filesystem identities. Enumeration
exceptions and `UnauthorizedAccessException` are also not covered by the
current `IOException` catch.

## DRY and duplicated-path failures

The significant DRY failures are behavioral rather than cosmetic:

- `Lexeme.Split` is a second lexer. It diverges on comma numbers, text, dates,
  comments, and punctuation.
- Parser errors repeat advancement and recovery logic across many nested error
  classes. One inconsistent implementation already causes the infinite loop.
- Operator identity is duplicated between `SymbolTable.Operators` and
  `Builtin.Operators`. A test notices key drift but cannot prevent semantic or
  precedence drift.
- Test token builders reproduce lexer output instead of exercising the lexer.
- Nested declaration traversal is reproduced by test helpers rather than owned
  by a production compilation pipeline.
- Cascade, writer, and initialisation tests supply effect and read sets because
  the effect-analysis path does not exist.

These should be consolidated around one compilation pipeline that produces
structured phase outputs and diagnostics.

## Why the test suite did not catch this

The current result is:

```text
450 tests passed
line coverage:   100%     (1611 / 1611)
branch coverage: 99.91%   (1113 / 1114)
```

That confidence is misleading because:

- `Program` is excluded from coverage.
- The parser "integration" suite still hand-builds token chains.
- Resolver tests often use `Lexeme.Split`, not the lexer.
- Nested scope tests manually connect two independent parses.
- Runtime tests manually build graph nodes, scopes, patterns, and resolved
  trees.
- Cascade and initialisation tests hand-build the data that future effect
  analysis must produce.
- The ring-width test hand-builds the wrong finding kind.

The highest-value additions are executable-level tests with timeouts, parser
progress properties, full-input-consumption assertions, and
source-to-finding tests for every diagnostic kind.

## Recheck of the first audit

### Confirmed fixed

- empty-input sentinel and EOF lexer failures;
- comment offset handling;
- aggregate closure and separator strictness;
- trailing module input;
- repeating-scope dispatch;
- token offsets and equality/hash contracts;
- runtime arity checking;
- equal-write and recompute cutoff;
- division by zero and unsupported-date reporting;
- package vulnerabilities;
- synchronous and deterministic executable traversal;
- removal of unsafe and tiered-compilation pessimizations.

### Partial or not real

- Ambiguity saturation: one unchecked product remains.
- Duplicate graph declarations: nodes are checked, constants are not, and
  `When` corrupts state before checking.
- Scope diagnostics: rules exist, but nested traversal and later-declaration
  attribution do not.
- Parser strictness: dangling syntax and general `IError` reporting remain.
- SDK pinning: the file is invalid.
- Integration-test discipline: several new tests use source, but the old bypass
  paths remain dominant.

### Still outstanding as expected

- dangling syntax and return types;
- numeric tower;
- resolver allocation work;
- nullable.

## `FAILUREMODES.md`

`FAILUREMODES.md` remains mostly actionable.

The cutoff described there now exists in code, so that section is stale.
The following remain unresolved or unimplemented:

- module-composition and compiled-scope semantics;
- live-edit node lifetime and removal;
- outward-in typing;
- exactness behavior;
- the higher-order-cell prohibition as a language constraint;
- graph-based reactive explanations.

## Recommended implementation order

1. Enforce parser progress and unify the parser error contract.
2. Build one recursive production compilation pipeline.
3. Replace cascade legality checking with SCC analysis.
4. Fix the remaining resolver count overflow.
5. Make graph declarations transactional and handling frame-local.
6. Add declaration provenance to diagnostics.
7. Copy mutable construction inputs.
8. Enable nullable and build hardening.
9. Implement the resolver allocation wins and benchmarks.
