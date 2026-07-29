# Second re-audit

Audited at commit `bfd2c1c` (`Make a finding that cannot be rendered impossible to build`).

This audit rechecked the previous audit and re-audit findings through the public compilation path and the command-line executable, then inspected the newer diagnostics, resolver, operator, and CI changes. The earlier fixes are real: the previously reproduced malformed-input crashes and silent successes now produce findings, unreadable files are counted without crashing, the CI coverage command is corrected, package locking is in place, and both Debug and Release test runs retain complete coverage.

Four current findings remain. The first three can produce incorrect or fatal behavior today through public interfaces. The fourth is a semantic split hidden behind what appears to be a unified operator definition.

## 1. Pattern-width refusal escapes as an unhandled exception

**Severity: high**

`Pattern` enforces its 128-segment ceiling by throwing `ArgumentException`:

- `Compiler/Resolution/Resolver.cs`, `Pattern` constructor
- reached from `Identifier.TryPattern`
- reached from declaration collection during `Compilation.Of`

That is appropriate as an invariant guard for direct internal construction, but the source path does not validate or translate it into a `Finding`. A syntactically valid declaration with 129 words and holes therefore terminates compilation instead of being rejected diagnostically.

Reproduction through the public compilation API:

```csharp
var source = "function " + string.Concat(
    Enumerable.Repeat("word ", 128)) + "(x) {}";

Compilation.Of(new Source("wide.ron", source));
```

Observed result:

```text
ArgumentException: a word pattern may have at most 128 words and holes
```

This is especially important because the ceiling was introduced as a denial-of-service bound. Inputs beyond that bound must be safely refused; they must not turn the bound itself into a fatal path.

**Recommendation:** validate the segment count in the source/declaration path and emit a typed finding such as `PatternTooWide`. Continue to gate later phases after that finding. Retain the constructor guard as a last-resort invariant if desired. Add an end-to-end source test at 128 and 129 segments.

## 2. The documented `for each` construct silently compiles as unrelated statements

**Severity: high**

The language specification and implementation disagree:

- `docs/spec/grammatical-structure.md` §4.5.4 documents a `for each ... in ...` loop.
- `docs/spec/introduction.md` uses that syntax in an example.
- The lexer recognizes `iterate`, not `for`.
- The implemented grammar expects `iterate <Datum> => <Name> <Definition>`.

As a result, the documented example:

```ronin
for each bank in banks
{
    return bank;
}
```

exits successfully with no findings. It is not parsed as a loop; it becomes ordinary statements and unresolved names. This is currently masked by the fact that resolution is not connected to the public compilation pipeline.

The existing malformed-input test cases for `for ;` and `for each ;` also expose a test-asserting-a-bug pattern: they are labelled malformed, but the test only verifies parser progress/input consumption. Both inputs currently exit successfully with zero findings.

**Recommendation:** make an explicit language decision and enforce it end to end:

1. Implement the documented `for each ... in ...` form, or
2. Make `iterate ... => ...` canonical and update the specification, examples, and malformed-input corpus.

Whichever syntax is selected, add positive AST-shape tests, negative diagnostic tests, and an executable-level test. Do not use parser progress alone as the acceptance criterion.

Adjacent documentation drift should be corrected at the same time: some guide material refers to `.ronin`, while source discovery currently uses `.ron`.

## 3. The reflective diagnostic walk corrupts its static cache under cold parallel compilation

**Severity: high for the compiler API and future parallel execution**

The new recursive error walk caches reflected members in a process-wide mutable `Dictionary<Type, PropertyInfo[]>`:

```csharp
private static readonly Dictionary<Type, PropertyInfo[]> members = [];
```

`Members(Type)` performs an unsynchronized check followed by assignment. Concurrent first use can mutate the dictionary from multiple threads and corrupt it.

A fresh-process stress test calling `Compilation.Of(...)` inside `Parallel.For` reproduced the failure in five out of five runs. Observed inner exceptions included:

```text
Operations that change non-concurrent collections must have exclusive access.
A concurrent update was performed on this collection and corrupted its state.
```

and, on some runs:

```text
IndexOutOfRangeException
```

The current CLI happens to compile sequentially, so it avoids this path. The public compiler API, IDE use, tests, or planned parallel file compilation do not have that protection.

**Recommendation:** use `ConcurrentDictionary<Type, PropertyInfo[]>` with `GetOrAdd`, or construct an immutable/precomputed cache. Add a cold-start parallel regression test; a warmed cache will not reliably reproduce the defect.

## 4. Operator definitions are still copied and re-resolved through different registries

**Severity: medium**

The new `Operator` record usefully puts binding power and implementation together, but resolution and evaluation do not consume the same registry entry:

- `SymbolTable.Operators` is initialized as a mutable copy of `Runtime.Builtin.Operators`.
- The resolver consults the `SymbolTable` copy.
- `Node.Operation` stores only the operator symbol.
- The evaluator looks the symbol up again in the global `Builtin.Operators`.

This makes the apparent single source of truth diverge as soon as a symbol table is modified. Two direct reproductions demonstrate both directions:

- Adding `^` with a valid implementation to `SymbolTable.Operators` allows resolution, but evaluation returns `error(«^» has no implementation)`.
- Replacing the copied `+` implementation with one returning `999` still evaluates `2 + 3` as `5`, because evaluation ignores the definition selected by resolution.

The existing identity test only proves that the initial copied values refer to the same operator objects. It does not prove that resolution and evaluation share a registry or resolved meaning.

**Recommendation:** choose one model explicitly:

- If operators are fixed built-ins, expose one immutable shared registry and do not copy it into mutable symbol tables.
- If operators can be scoped or extended, carry the resolved `Operator` (or its implementation) in `Node.Operation` and have evaluation use it directly.

If mutation remains supported, validate binding powers and implementations at insertion time so invalid custom entries cannot become indexing or null-call failures.

## Previously reported fixes rechecked

The following changes were independently exercised or inspected and are no longer findings:

- Malformed prefix operators, delegate parameters, lookup/input arguments, function parameters, datum patterns, and dangling declarations now yield findings instead of crashing or succeeding silently.
- The dangling-function case no longer emits the previous duplicate cascade.
- File discovery reports unreadable files separately and exits unsuccessfully without an unhandled exception.
- Source spans no longer overflow at large offsets.
- The NUL sentinel ambiguity has been removed.
- Typed findings replaced the newly introduced free-form diagnostic strings.
- The CI coverage-property separator is encoded correctly.
- Package locks and locked CI restore are present.
- The SDK statement now accurately describes a minimum/floor rather than a pin.
- Resolver storage and lookup have received substantial, measurable allocation and search improvements.

Debug and Release each passed all 524 tests with 100% line, branch, and method coverage. The Release solution build completed with zero warnings. Locked restore also succeeded; an initial `NU1900` was caused by the audit environment being unable to reach NuGet's vulnerability endpoint, not by the repository or lock files.

## Settled policy: formatter output is not a finding

`dotnet format` still reports approximately 90 whitespace differences caused by intentionally hand-aligned continuations. The project workflow documents this style and does not gate it. This is a settled project policy and is **not** an audit finding. It was not counted among the issues above and should not be raised again unless the project changes its formatting policy.

## Known work still outstanding

These are retained from the earlier audit or explicitly described as ongoing; they are not discoveries attributed to the latest changes:

- The public `Compilation` pipeline still stops after parsing/declaration diagnostics. Resolution, cascades, writers, initialization, and evaluation are not connected end to end. Until they are, unresolved names and several semantic errors can still compile successfully.
- `Resolution.TooLong` and other resolver failures still need production-quality typed diagnostics when that phase is connected.
- The numeric tower remains represented by `double`, with the previously documented correctness consequences.
- Nullable analysis remains disabled, and the strict/latest analyzer pass still has a substantial backlog.
- The brace parser remains bounded rather than algorithmically corrected.
- The workbench still performs synchronous whole-document work on edits without cancellation or debounce.
- Effect analysis and some trigger/history construction remain hand-built representations rather than one authoritative path.
- `FAILUREMODES.md` has not yet been addressed.

## Recommended order

1. Convert the pattern-width ceiling into a source diagnostic.
2. Resolve the `for each` versus `iterate` language contract before more tests or examples depend on either spelling.
3. Make the reflective member cache concurrency-safe before parallel compilation is enabled anywhere.
4. Decide whether operators are immutable globals or resolved/scoped values, then remove the copied-registry split.
5. Continue connecting the semantic pipeline; that connection is what will expose currently silent unresolved-name cases such as the loop example.
