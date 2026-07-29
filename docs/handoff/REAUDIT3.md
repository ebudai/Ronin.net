# Third re-evaluation

Audited at commit `cdd0f1c` (`Make a bound refuse instead of terminate, and share one operator`).

The concurrency and operator-identity fixes are complete, and a genuinely over-wide source pattern is now refused with a typed finding instead of terminating compilation. The loop syntax contract remains open. Exercising that contract through the whole parser exposed a more general routing defect: several valid keyword-led constructs are silently parsed as data declarations whenever `=>` is followed by a reference.

## 1. Full statement routing silently changes typed functions and arrow-bodied scopes into data declarations

**Severity: high**

The individual productions work when called directly, but they do not necessarily win through the real source path:

- `Statement.Parse` tries `Member.Parse` before `Scope.Parse`.
- `Member.Parse` tries `Datum.Parse` before `Function.Parse`.
- `Datum.Parse` accepts an identifier with no mutability when it has a type.
- `Identifier`/`Name` accepts any `Word`, and every `Keyword` is a `Word`.

Consequently, a keyword-led construct containing `=> <reference>` can satisfy the datum production first. Confirmed through `Compilation.Of`:

| Source | Actual outer statement(s) |
| --- | --- |
| `function f => Number { return 1; }` | `Datum`, then `Scope.Basic` |
| `if ready => result;` | `Datum` |
| `while ready => result;` | `Datum` |
| `when ready => result;` | `Datum` |
| `when changing ready => result;` | `Datum` |
| `iterate banks => bank { return bank; }` | `Datum`, then `Scope.Basic` |

All compile with zero findings. The same conditional with a numeric result, `if ready => 1;`, does become `Scope.Conditional<If>` because a number cannot be mistaken for a type. Thus a scope's AST shape changes according to whether its result happens to look like a type reference.

The command-line executable likewise accepted a file containing a typed function and a reference-valued conditional, reporting three statements and no problems where the source contains two constructs.

This is the hand-built-data problem in another form. The positive function and loop tests call `Function.Parse` or `Scope.Iterating.Parse` directly, frequently with hand-built token chains. That selects the intended production before the test begins and cannot detect a competing production winning in `Statement.Parse`.

**Recommendation:** add source-to-`Module` positive conformance tests for every keyword-led declaration and scope, asserting the outer AST type, important fields, statement count, and complete consumption. Then make keyword ownership or production precedence explicit so an unmutated datum cannot steal a construct announced by another keyword. Include both block and `=>` bodies, and both literal and reference-valued bodies.

This defect occurs before resolution. Joining the resolver would diagnose some names, but it would not repair the wrong AST.

## 2. The `for each` versus `iterate` language contract remains unresolved

**Severity: high contract defect; unchanged from the previous re-audit**

The change to `Progress.Survivable` correctly documents that the test asserts survival and consumption only. It does not resolve the contradiction:

- The specification and introduction document `for each <name> in <expression>`.
- The lexer recognizes `iterate`.
- `Scope.Iterating` implements an `iterate <iterable> => <name> <definition>` shape.
- `for each bank in banks { ... }` still becomes `Member.Unresolved` followed by `Scope.Basic`, with no findings.

The implemented spelling also has the routing problem in finding 1. `iterate banks => bank { ... }` does not reach `Scope.Iterating`. The form used by the direct unit test, `iterate banks => var bank { ... }`, does reach it from source, but records the current identifier as the literal two-word name `var bank`; the unit test only checks that an iterable and body exist.

An explicit language decision is still required: implement the documented form, or make `iterate` canonical and update the specification and examples. The first test after that decision should assert a `Scope.Iterating` AST and its iterable/current/body fields through `Module.Parse`.

As the programmer noted, syntax and name resolution are separate milestones. Correcting the syntax should produce the correct loop AST now. It will not make unresolved `bank` or `banks` findings appear until the resolver is joined to `Compilation`, and this re-evaluation does not expect it to.

## 3. The pattern-width fix rejects a wide plain name as though it were a pattern

**Severity: medium**

`Identifier.TryPattern` now records `Width` and returns false for either of two reasons:

1. The identifier has no holes and is therefore a plain name.
2. It has more than `Pattern.MaxSegments`.

`Declarations.Declare` distinguishes those cases using only `Width`. A plain name wider than 128 words is therefore reported as `PatternTooWide`, even though it is not a pattern and will never enter the recursive pattern matcher.

Confirmed through `Compilation.Of`:

- A 128-word `var` name compiles cleanly.
- A 129-word `var` name produces `PatternTooWide`, saying “a pattern may have at most 128.”

The lexer and resolver statement ceilings still admit this source, and no general identifier-width rule is documented.

**Recommendation:** preserve whether the identifier contains holes, or return a result that distinguishes `PlainName`, `Pattern`, and `PatternTooWide`. Apply `Pattern.MaxSegments` only to pattern declarations. If the language wants a separate maximum name width, define and diagnose that as its own rule rather than borrowing the pattern-recursion bound.

## 4. Mutable operator entries still do not enforce their stated invariants

**Severity: medium for the internal extension seam**

The important semantic split is fixed: `Node.Operation` now carries the exact `Operator` chosen by resolution and `Evaluator` applies it. Added and replaced operators evaluated as expected in the original reproductions.

The remaining hardening recommendation from the previous report was not implemented. `SymbolTable.Operators` is intentionally mutable because scopes and resolver tests may add operators, while `Operator` and `Resolver` assume without enforcing that:

- binding power is between 0 and 30;
- `Apply` is non-null;
- the symbol can be produced by the lexer.

Confirmed failures:

- Binding powers `-1`, `31`, and `int.MaxValue` all cause raw `IndexOutOfRangeException` while constructing `Resolver`.
- A null implementation resolves successfully and then causes `NullReferenceException` in `Evaluator`.

This does not affect the current built-in table or source language, which has no user-defined operators. It does make the supported mutable extension path fail far from the invalid insertion, and contradicts the `Operator` comment that an implementation is required.

**Recommendation:** validate entries at a controlled insertion boundary, or validate the complete table in `Resolver` and the `Operator` constructor with specific argument exceptions. A controlled API can also reject multi-character symbols until the lexer supports them.

## Prior findings now closed

- A source pattern with 129 words and holes emits `PatternTooWide`; it no longer throws `ArgumentException`.
- The reflective member cache uses `ConcurrentDictionary.GetOrAdd`. The original cold/cleared-cache parallel stress no longer corrupts the collection.
- An operator added to a scope evaluates with its supplied implementation.
- Replacing a built-in operator in a scope changes the evaluated meaning, proving the evaluator uses the resolver's selected object.

## Validation

- Release: 526 tests passed.
- Debug: 526 tests passed.
- Release coverage: 100% line, branch, and method.
- Release and Debug builds completed without warnings.
- The original public-path reproductions for all four previous findings were rerun.
- Focused source-path tests used `Compilation.Of` and the command-line executable, not direct production calls.

The documented hand-aligned formatting policy remains settled and is not a finding. `dotnet format` was not used as a gate.

## Still outside this change

The resolver is not yet joined to the public `Compilation` pipeline. Accordingly, unresolved names—including names inside a correctly parsed future loop—will remain silent until that integration is completed. The numeric tower, nullable analysis, workbench/parser performance work, semantic-pipeline integration, and `FAILUREMODES.md` also remain outstanding as previously recorded.

## Recommended order

1. Fix full-source production routing and add positive `Module.Parse` shape tests.
2. Decide and implement the canonical iteration syntax, using those full-source tests.
3. Separate plain-name and over-wide-pattern outcomes.
4. Put validation around the mutable operator extension seam.
