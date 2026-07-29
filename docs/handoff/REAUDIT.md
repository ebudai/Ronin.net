# Re-evaluation of the fresh audit

Audited at commit `7cebf9a` (`Guard a directory once, not twice`), after the
changes prompted by `FRESHAUDIT.md`.

The majority of the first audit's fixes are real. In particular, the parser
progress failure, dangling declarations, nested scopes, cascade legality,
resolver count overflow, graph atomicity, diagnostic attribution, ownership,
escape handling, and deep graph walks have all materially improved.

The compiler is not yet safe on malformed source, however. Error nodes embedded
in grammar composites are outside the new compilation walk. Two forms crash the
executable and at least three others compile cleanly. Unknown syntax also still
compiles cleanly. These are the same class of defect as the hand-built-data
substitutions from the first audit: a component test proves an error object was
built, but no production path proves that the compiler finds it and stops later
phases from consuming it.

No production source was changed as part of this re-evaluation.

## Release-blocking findings

### 1. Malformed declaration parameters crash the executable

Both of these terminate the process with exit code 134:

```ronin
function f (var +) {}
```

```ronin
var f (var +) => Number;
```

The stack is:

```text
NullReferenceException
  Identifier.Named                         Identifier.cs:86
  Identifier.TryPattern                    Identifier.cs:77
  Declarations.Declare                     Declarations.cs:111
  Declarations.Of
  Compilation.Scope
```

`Parameters.Parameter.Parse` accepts the
`Datum.ExpectedIdentifierError` returned by `Datum.Parameter`. That error is
wrapped as a parameter inside the declaration's `Identifier`.
`Compilation.Descendants` only walks statements and statement bodies, so it
does not see the error. Declaration building then assumes every parameter has
an identifier and dereferences `parameter.AsDatum.Identifier.Words`.

A null guard in `Identifier.Named` would prevent this one exception but would
not fix the compiler invariant. A malformed subtree must produce a finding and
must not enter declaration, resolution, or lowering. The durable fix is one
grammar-wide child traversal (or an explicit validation result from parsing)
covering every AST slot, followed by a phase gate for invalid declarations.

This needs executable-level regressions for both a function pattern and a datum
pattern. A parser-only assertion that an error node exists is not sufficient.

### 2. Errors embedded in values compile successfully

The same traversal gap is silent when later declaration code does not happen to
dereference the bad node:

| Source | Embedded error | Actual result |
|---|---|---|
| `var callback = (var +) => {};` | bad delegate parameter | exit 0, zero problems |
| `var value = { key = };` | `Association.ExpectedValueError` in a lookup | exit 0, zero problems |
| `var value = (key = );` | `Association.ExpectedValueError` in inputs | exit 0, zero problems |

The comment above `Compilation.Descendants` says an initializer, iterable, or
condition can never contain an error node. Lookup and input aggregates directly
disprove that statement, and `Test/Failure/Lookups.cs` currently asserts that a
malformed lookup successfully contains an `Association.ExpectedValueError`.
That test describes parser representation; it must not be mistaken for a
successful compilation contract.

This is likely to become a crash rather than merely silent acceptance as more
phases are connected. The compiler needs to visit all children of delegates,
lookups, inputs, lists, indices, associations, initializers, conditions,
iterables, return values, types, and identifiers—not only the slots that open a
lexical scope.

### 3. Catch-all `Unknown` syntax is still considered valid

This source reports one statement, zero problems, and exits 0:

```ronin
+;
```

`Unknown` stores the recovered tokens but does not implement `IError`, so even a
complete walk over statements would not report it. The old analyzer test that
would have classified it as unknown syntax is commented out, while
`Test/Failure/Unknowns.cs` only asserts the AST node type and
`Test/Integration/Progress.cs` only asserts termination and input consumption.

Recovery is not acceptance. `Unknown` should participate in the same structured
error contract as the other recovery nodes, with a source-to-finding and
executable exit-code regression.

### 4. The repository's CI test step is syntactically invalid

The workflow runs:

```text
/p:ThresholdType=line,branch
```

MSBuild treats the comma as a property separator and exits before running any
tests:

```text
MSBUILD : error MSB1006: Property is not valid.
Switch: branch
```

Encoding the comma makes the intended command work:

```text
/p:ThresholdType=line%2cbranch
```

At this commit, the corrected command passes all 515 tests and reports genuine
100% line, branch, and method coverage. The coverage target is therefore
currently real; the gate that is supposed to enforce it is not.

## Other correctness and diagnostics findings

### 5. One malformed function produces two findings for one mistake

`function f => {}` now fails, which fixes the dangling-return-type hole, but it
reports both:

```text
unexpected input: }
expected a type after '=>': function f => {
```

Recovery stops at the closing brace after consuming its opening brace, leaving
the close delimiter for `Module.UnexpectedInputError`. This contradicts the
diagnostic's promise that the rest of the statement was skipped so one mistake
is reported once.

Recovery needs delimiter awareness, or the error node needs to own the matching
closer it crossed while resynchronising. Add a compilation test that asserts the
finding count for each dangling form, not just parser completion.

### 6. An unreadable source file still terminates the whole project scan

Directory enumeration now handles `UnauthorizedAccessException`, and symlink
loops are correctly refused. File reading is a separate path:

```csharp
try
{
    text = File.ReadAllText(file.FullName);
}
catch (IOException unreadable)
```

On a discovered `.ron` file with mode `000`, `File.ReadAllText` throws
`UnauthorizedAccessException`, which escapes `Program.Report` and terminates the
process with exit code 134. Catch the same refusal set used by `Sources`, report
the file, and continue scanning.

The summary also combines unreadable directories and problem files in one
counter, so it can report more "files with problems" than files discovered.
Keep unreadable paths as a separate count or change the wording.

### 7. `Compilation.cs` contains a literal NUL byte

The delimiter in `Compilation.Identity` is an actual U+0000 byte in the source
file, not the escaped C# spelling `'\0'`. Consequently:

- `file Compiler/Compilation.cs` reports `data`;
- Git treats the new file as binary and cannot show a normal textual diff;
- ordinary search tools warn that it is binary.

The program compiles, but the central new compilation pipeline is harder to
review and merge. Replace the byte with an escaped source spelling, or preferably
replace the delimiter-built identity with a typed value.

### 8. Span bounds checking can be bypassed by integer overflow

`SourceText.Span` checks:

```csharp
offset + length > Text.Length
```

For a sufficiently large positive `length`, the addition wraps negative and an
invalid span is accepted. The safe condition is
`length > Text.Length - offset`, after `offset` has been validated.

This is not reachable from ordinary small source files, but it contradicts the
new invariant that malformed spans fail where they are built.

### 9. Diagnostic structure is hardened but remains runtime-shaped

The wildcard renderer, culture-sensitive counts, missing writer names, invalid
span checks, and lazy `Declarations.Problems` issues are fixed. The remaining
weak point is `Finding` itself: mutable free-form dictionaries and lists are
still its public internal contract, and each renderer indexes required roles by
string. A producer typo or later mutation therefore becomes a
`KeyNotFoundException` or a structurally invalid finding at report time.

The totality test proves that each enum member has one current producer and
message. It cannot prove that every future producer supplies that message's
roles. Typed payloads, or one immutable record per finding kind, remain the
clean way to make invalid findings unrepresentable.

## Incomplete integration and hand-built substitutes

### 10. `Compilation` still joins only parsing and declarations

`Program` calls `Compilation.Of`; `Compilation` lexes, parses, and builds
declarations. It does not call:

- `Resolver`;
- overload/type selection;
- effect analysis;
- `Cascades.Diagnose`;
- `Cascades.Writers`;
- `Initialisation`;
- lowering or `Evaluator`.

The executable's own comment correctly says those phases are not connected.
Accordingly, cascade, writer, initialisation, scope invocation, and most runtime
tests still construct the consumer's input by hand. `Findings.Examples` also
supplies effects/read sets for those three diagnostic kinds.

This is known ongoing implementation rather than a regression, but it remains
the largest source of false confidence. Each phase needs at least one
source-to-finding or source-to-runtime test as it is joined. A unit test of the
consumer should remain, but it cannot stand in for the producer/consumer
boundary.

The malformed-composite failures above are a concrete example: the parser tests
correctly prove error nodes are constructed, while production proves nobody
collects some of them.

## Performance and build hardening

### 11. Resolver allocation work is real but incomplete

The two implemented wins are substantial:

- binding-power levels are derived and reduced from 32 to roughly six;
- `Cell` collections allocate only on first offer.

The regression test measures about 25 MB for 149 lexemes, versus 158 MB before
those changes, with a 60 MB ceiling. That is a real and useful guard.

The resolver still allocates every cell in two full `(n+1)²` rectangular tables
and one `(n+1)² × levels` table. Invalid lower-triangle spans still get cells,
patterns are tested against every span rather than indexed by their first
anchor, and tables are recreated for every call. Triangular storage, first-word
pattern indexing, and pooling therefore remain worthwhile.

There is also still no cancellation or statement-token ceiling. One generated
or pasted long statement can consume the full cubic table shape. Per-statement
resolution limits the common case but not that denial-of-service case.

`Resolver.Match` also remains recursive by pattern segment. The current table
allocation is likely to exhaust practical inputs before that recursion alone
reaches the process stack, but that is not a safety boundary. Make the match
walk iterative or let the same explicit statement/pattern ceiling guard it
before resolution is connected to production.

### 12. Parser work is bounded, but the exponential brace path remains

Depth and total group-attempt budgets prevent stack overflow and unbounded
work. The test and comments explicitly document that three productions reparse
nested braces exponentially; `MaxGroups = 1_000_000` is a backstop over that
defect, not an algorithmic fix.

This matters most in `Workbench.Refresh`, which still synchronously completes
and resolves on every text change without cancellation, debounce, generation
checks, or a long-line limit. A bounded half-second-to-seconds parse is still a
visible editor freeze when paid per keystroke.

### 13. SDK selection is valid but not reproducibly pinned

`global.json` is now valid and the repository consistently builds with the
installed SDK. Its policy is still:

```json
"version": "10.0.100",
"rollForward": "latestFeature"
```

`latestFeature` permits a later installed feature band, while CI requests
`10.0.x`, which installs whatever 10.0 SDK is current. That is a valid
compatibility policy, not a feature-band pin. Use `latestPatch` with a matching
1xx setup constraint, or an exact version/disabled roll-forward if exact
reproducibility is intended. Otherwise change the comments so they do not claim
stronger pinning than the configuration provides.

### 14. Nullable, analyzer hardening, and formatting remain outstanding

All three projects still set `<Nullable>disable</Nullable>`. An opt-in
`AnalysisLevel=latest-all` warning-as-error build currently reports 135 unique
diagnostics. Much of that is low-value API-shape advice, but it also continues
to identify the multidimensional resolver storage and other maintainability
issues the ordinary build does not see.

`dotnet format --verify-no-changes` still fails on roughly 90 whitespace
locations plus naming diagnostics. The workflow intentionally declines to gate
it because the formatter disagrees with the project's alignment style. That is
a defensible style choice, but it means `.editorconfig` and
`EnforceCodeStyleInBuild` do not currently provide one command that proves the
tree conforms to its declared style.

Package locking and a benchmark project remain absent. The resolver allocation
ceiling is a useful targeted replacement for the most important missing
benchmark.

### 15. The numeric tower remains entirely outstanding

Runtime literal evaluation still parses every number as `double`. Large
integers lose precision, exact decimals and rationals do not exist, `fast
number` is not a distinct opt-in representation, and exactness-preserving roots
and transcendentals from `FAILUREMODES.md` are not implemented.

This re-evaluation treats that as known design work, not a failed attempt at the
audit fix.

## Remaining DRY failures

Two major duplications were genuinely removed or reduced:

- the second lexer is gone; resolver convenience input now uses the real lexer;
- parser recovery/progress is centralized in `Parser.Recover`.

The important remaining duplications are behavioral:

- operator identity is still split between `SymbolTable.Operators` and
  `Builtin.Operators`; a key-set test detects drift after it happens but cannot
  prevent precedence/implementation metadata from diverging;
- `Graph.Trigger.Previous` is a second implementation of `old`, as its own
  comment notes;
- effect/read/write data is still separately hand-supplied to cascade, writer,
  initialisation, and purity consumers because the shared effect analysis does
  not yet exist;
- the compiler has one scope-body traversal rather than one complete AST child
  traversal, which is why embedded errors are missed.

The last item should be addressed before adding more compilation phases; every
new phase otherwise risks growing another partial walk.

## Status of the original findings

| Original finding | Re-evaluation |
|---|---|
| 1 parser can loop forever | **Fixed.** Recovery advances the caller and the executable terminates. |
| 2 parser errors compile successfully | **Partial.** Statement-position `IError`s are reported; unknown and embedded errors are not. |
| 3 dangling `=>`, `=`, return types | **Fixed semantically.** All fail; the function form double-reports. |
| 4 nested diagnostics not connected | **Declarations fixed.** Later phases remain disconnected. |
| 5 cascade safety-rule bypass | **Fixed.** SCC legality catches the overlapping-ring case. |
| 6 resolver ambiguity overflow | **Fixed.** Group multiplication saturates at each step. |
| 7 graph-wide handling suppression | **Fixed.** Handling state is scoped to the adoption frame. |
| 8 failed duplicate `When` mutates graph | **Fixed.** Declaration succeeds before trigger metadata is installed. |
| 9 constants collide with nodes | **Fixed.** Uniqueness spans both stores. |
| 10 effect defects terminate session | **Fixed.** Writes are staged and faults retained. |
| 11 wrong cross-scope primary span | **Fixed for current single-source nesting.** Provenance determines the later declaration. |
| 12 hand-built ring-width test | **Fixed.** It now invokes the cascade rule on a seven-hop ring. |
| 13 diagnostic completeness | **Mostly fixed.** Free-form mutable payloads remain. |
| 14 caller-owned mutable collections | **Fixed.** Pattern, tree nodes, and blocks copy their inputs. |
| 15 deep valid programs overflow stack | **Mostly fixed.** Graph, cascade, initialisation, and parser paths are iterative or bounded; recursive resolver matching remains. Brace reparsing is capped but still exponential. |
| 16 text escapes not evaluated | **Fixed** for the two escapes the lexer currently admits. |
| 17 resolver allocation shape | **Partial with substantial measured wins.** |
| 18 synchronous unbounded workbench | **Outstanding.** |
| 19 SDK not pinned | **Valid configuration now; reproducibility claim remains partial.** |
| 20 nullable/strong analyzers disabled | **Outstanding.** |
| 21 formatting and CI absent | **Partial.** CI and `.editorconfig` exist; the coverage command is broken and formatting is not gated. |
| 22 traversal follows symlinks / access failures | **Symlinks and unreadable directories fixed; unreadable files still crash.** |

## Validation performed

At `7cebf9a`:

```text
dotnet build Ronin.sln --no-restore -c Release --warnaserror
  succeeded, 0 warnings

dotnet test ... -c Release
  515 passed

corrected coverage command
  line 100%, branch 100%, method 100%

workflow's literal coverage command
  MSB1006, invalid property switch "branch"

dotnet format Ronin.sln --verify-no-changes --no-restore
  failed

latest-all analyzer build with warnings as errors
  135 unique diagnostics
```

Executable regressions were run against real `.ron` files for parser progress,
dangling declarations, nested declarations, symlink traversal, unknown syntax,
malformed function/datum/delegate parameters, malformed lookup/input
associations, and unreadable files.

## Recommended order

1. Make malformed composite nodes visible to compilation and gate later phases
   on them; add the six executable regressions above.
2. Make `Unknown` a structured malformed finding.
3. Fix the CI property's escaped comma so the existing 100% result is enforced.
4. Catch unreadable source files without aborting the scan.
5. Make recovery delimiter-aware so dangling functions report once.
6. Remove the literal NUL and move finding identity/payloads toward typed values.
7. Continue the source-to-phase joins, adding one production integration test
   for every newly connected consumer.
8. Finish resolver limits/triangular storage, then nullable and the numeric
   tower on their planned tracks.

`FAILUREMODES.md` was not treated as addressed; its unresolved design decisions
remain outside the claims above.
