# Re-audit 23 — `FRESHAUDIT3` incorporation

**Audited:** `6d22dd9` through `1c0404e`

**Date:** 2026-08-02

## Result

**Sign-off for the `FRESHAUDIT3` incorporation.  No findings.**

The function-return and type-algebra boundaries now use the same mechanism as
the five scope headings, the original rejection and silent-tree witnesses are
fixed through the real compilation path, and the regressions assert the
structural property the silent case previously violated.

This is a sign-off for the heading-boundary incorporation, not a claim that the
language's previously disclosed unfinished features are now complete.

## What was rechecked

### One definition for the boundary

`Heading.Of` names the actual grammatical relation: a reference that a
definition follows.  It saves the incoming parser state, enables the boundary
while the reference production runs, and restores the incoming state before it
returns.

Every current reference-before-definition join now goes through it:

- a function's declared return type;
- a type's algebra;
- a `for each` collection;
- a `when changing` target; and
- the shared conditional production used by `if`, `while`, and `when`.

The refactoring also removed the three open-coded save/set/restore sequences
from `Scope.cs`.  `Aggregate` retains its complementary rule: bracketed content
temporarily parses outside the surrounding heading and restores that context
after its closer.  The two operations therefore compose rather than allowing a
nested argument to end its caller's heading.

### Original witnesses

The maintained full-parser tests now accept all of:

```ronin
function f => Number { 1 }
function f => Number {}
function f => Number { 1; }
function f => Number { return 1; }
type T = Base {}
type T = Base { var a => Number; }
```

The important type assertion goes beyond “no findings”:
`type T = Base {}` has a non-null, empty `Members` definition.  The old silent
tree—brace absorbed into the algebra and `Members == null`—can no longer make
the test pass.

The declaration tests also preserve the designed escape hatch:

```ronin
function f => Takes ({ 1 }) {}
type T = Base ({ 1 }) {}
```

Thus the repair stops the definition's brace without banning a braced argument
that is explicitly nested inside brackets.

### Independent adversarial probes

Temporary `Compilation.Of` probes, removed before the repository gates,
exercised:

- operator-bearing return types and algebras (`A and B`);
- a function's arrow-expression body after a declared return type;
- multiply nested input blocks containing braced arguments;
- a zero-argument delegate with a braced body inside a return-type argument;
- comments immediately before a definition brace;
- a type algebra terminated with `;`; and
- malformed missing return types/algebras, each recovering to exactly one
  finding.

All passed.  No state leak, changed recovery boundary, or additional
reference-before-definition join was found.

## Programmer-disclosed pre-existing observations

The commit records two shapes encountered during the repair and correctly does
not present them as regressions from it:

- `var x;` has neither an explicit type nor an initializer from which to infer
  one.  The guide says all data is typed by one of those two routes, so its
  rejection is not a heading finding.  The compact optional notation in the
  spec could be made more explicit, but the implementation and guide agree on
  the operative rule.
- A comma-separated algebra is not syntax in the current spec or guide; algebra
  is a reference and the guide names `and`/`or`.  The unused `Bases` and `Unions`
  fields do not turn the historical comma shape into a requirement of this
  incorporation.

Neither observation changes this sign-off.

## Verification

- Expanded heading probe: **34 passed** before the temporary rows were removed.
- Temporary probes removed and the worktree returned clean before repository
  gates.
- Debug: **892 passed**, zero failed, zero skipped.
- Locked restore: passed.
- Exact Release solution build with `-warnaserror`: zero warnings and zero
  errors.
- Exact Release test/coverage gate: **892 passed**, with **100% line, branch,
  and method coverage**.
- `git diff --check 6d22dd9..1c0404e`: clean.

The documented hand-aligned `dotnet format` whitespace differences remain
settled project style and are not a finding.  Formatting was not used as a gate.
