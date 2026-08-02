# Fresh audit 3 — scope-heading boundary

**Audited:** the implementation changes `d620303` through `6d22dd9`, against
the previous signed-off implementation at `253d010`

**Date:** 2026-08-02

## Result

**One high-severity correctness finding.  No sign-off.**

The new boundary is correct for the five scope headings to which it is applied,
but the same grammatical join exists in two declaration headings and was not
included.  A typed function can still lose its body to its return type, and an
algebraic type can silently attach its empty body to its base reference.

The maintained suite and all repository gates pass; focused full-parser probes
are what exposed the omission.

## Finding

### 1. `Heading` covers scopes but misses the function-return and type-algebra headings

**Severity: high — documented declarations are rejected, and one ordinary form
is silently given the wrong tree**

`Reference.Component.Parse` now refuses an opening brace only while
`Parser.Heading` is true (`Compiler/Grammar/Reference.cs:189-205`).  The flag is
set around:

- an iterating collection (`Compiler/Grammar/Scope.cs:123-134`);
- a `when changing` target (`Scope.cs:218-231`); and
- an `if`, `while`, or `when` condition (`Scope.cs:356-369`).

That fixes the reported scopes.  It is not the complete set of references
immediately followed by a definition:

- `Function.Parse` parses its declared return type at
  `Compiler/Grammar/Function.cs:36-49`, then asks for the definition at lines
  52-54; and
- `Type.Parse` parses its algebra at `Compiler/Grammar/Type.cs:35-45`, then asks
  for the member definition at line 48.

Neither path sets `Heading`.  Their reference therefore remains allowed to
consume the following brace as an anonymous argument, which is the exact
failure mechanism the new flag removes from scopes.

#### Full-parser witnesses

These two documented function declarations both produce one `Malformed`
finding whose reason is `expected definition`:

```ronin
function f => Number { 1 }
function f => Number {}
```

In both cases the return-type reference consumes the braced value and leaves no
function body.  Existing typed-function tests do not expose it because their
bodies contain a terminated statement such as `return 1;`; the semicolon makes
the speculative braced anonymous value fail, incidentally leaving the brace for
the function parser.  This is the same test-shape blind spot that hid the scope
bug.

The type form is silent:

```ronin
type T = Base {}
```

It produces **no finding**.  The algebra is parsed as the reference `Base {}`
(with the empty brace as its anonymous argument), and `Type.Members` is null
instead of an empty definition.  `Type.Parse` permits a missing member
definition, so there is no later failure to reveal the theft.

Both forms are authoritative syntax: the spec describes a function datatype
and a type algebra as references followed by bodies
(`docs/spec/grammatical-structure.md:73-83`), and the guide spells them as
`function identifier [=> type] { statements }` and
`type identifier [= algebra] { members }`.

#### Recommendation

Apply the same save/set/restore boundary while parsing:

1. a function's declared return type; and
2. a type's algebra.

The concept is no longer specific to `Scope`, so either make `Heading` mean a
general “a definition brace follows this reference” context or centralize the
reference-before-definition operation.  Keeping a hand-maintained list of
scope classes is what made the two declaration joins easy to omit.

Add real `Compilation.Of` regressions for at least:

- typed functions with `{}`, `{ 1 }`, and `{ 1; }` bodies;
- `type T = Base {}` with assertions that the definition is non-null and the
  algebra did not absorb the brace;
- a derived type with members; and
- bracketed braced arguments in the return type/algebra, proving that the
  documented escape hatch still works there just as it does in a conditional.

## What did pass independent probing

Temporary full-parser probes, removed before this report, exercised all five
implemented scope headings with indexers, nested input blocks, braced arguments
inside brackets, operators, trivia, and delegates with both empty and named
signatures.  Those forms all produced the intended scope tree.

The spelling `if applies x => { 1 } { 2 }` was also investigated and is **not a
finding**.  The prefix `applies x => { 1 }` is itself the documented bare
delegate with the multi-word parameter `applies x`; parentheses are what make
`x => { 1 }` an argument to the reference `applies`.  The explicitly grouped
form parses correctly.

The `6d22dd9` save-and-restore change is also correct.  Aggregate parsing
temporarily clears the context and restores it on success, so nested bracketed
values do not accidentally end the surrounding heading.

## Verification

- Focused scope-heading expansion: 28 passed before the temporary probes were
  removed.
- Focused declaration witnesses: both typed functions failed with `expected
  definition`; the algebraic type incorrectly passed without a finding.
- Temporary probes removed and the worktree restored before repository gates.
- Debug: **883 passed**, zero failed, zero skipped.
- Locked restore: passed.
- Exact Release solution build with `-warnaserror`: zero warnings and zero
  errors.
- Exact Release test/coverage gate: **883 passed**, with **100% line, branch,
  and method coverage**.
- `git diff --check 253d010..6d22dd9`: clean.

The documented hand-aligned `dotnet format` whitespace differences remain
settled project style and are not a finding.  Formatting was not used as a gate.
