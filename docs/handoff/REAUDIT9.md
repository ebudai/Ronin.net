# Ninth re-evaluation — parameter declarations and the incomplete scope walk

Audited at commit `f824a81` (`Close REAUDIT8`), against the previous audited
commit `7fd6dc1`.

This review reproduced the three `REAUDIT8` findings through their real
boundaries, inspected the new identifier/readback and binding-invocation code,
and then followed every source-reachable parameter and nested-scope path that
the fixes depend on.

Sign-off is withheld. The three reported failures are repaired for the cases
they addressed, but the new claim that writability is an invariant of every
declaration stops at parameter blocks. Parameters are flattened to strings
without ever becoming declarations in their body scope, delegate bodies are
absent from the declaration walk, and an empty parameter block installs a dead
pattern. The width crash is fixed, but the width guard still does not prevent
the full readback work it was meant to precede.

## 1. Parameter declarations bypass writability, identity, and scope rules

**Severity: high — distinct source declarations collapse to one runtime
binding, and parameter references will resolve against the wrong scope**

`Declarations.Declare` validates the identifier of the member being declared.
The identifiers of parameters inside that member are never passed through the
same path. `Identifier.TryPattern` immediately flattens each one through:

```csharp
holes.Add([.. component.AsParameters.Select(Named)]);
```

and `Named` returns only:

```csharp
parameter.AsDatum.Identifier.Words
```

That rendering has already discarded canonical word boundaries, source span,
and every property needed by `Unwritable`, `Refused`, R5, and the no-shadowing
rule.

Both of these compile with zero findings:

```ronin
function compute
    (ready part /* gap */ of world => Number)
{
    return 1;
}

var callback =
    (ready part /* gap */ of world) => { return 1; };
```

The first is a function parameter and the second a delegate parameter. Each
declares the four canonical words:

```text
ready | part | of | world
```

but its stored block key is `ready part of world`, whose readback contains the
three words:

```text
ready | part of | world
```

The loss is observable at runtime. This source also produces zero findings:

```ronin
function compare
    (ready part /* gap */ of world => Number,
     ready part of world => Number)
{
    return 1;
}
```

The two distinct parameter word sequences become this one block:

```text
["ready part of world", "ready part of world"]
```

A focused probe registered the source-derived pattern and block as a runtime
`Declaration`, resolved `compare (1, 2)`, and invoked it. `Scope.Invoke` wrote
both values into `Dictionary<string, object>` under the same key. The body
received one entry, with the first argument silently overwritten by `2`.

Parameters also never become declarations in their function body scope.
`Compilation.Bodies` enters a function body with no bindings:

```csharp
case Grammar.Function { Definition.Statements: { } body }:
    yield return new Body(body, null);
```

Consequently this direct example from the settled scoping design produces no
`Shadowed` finding:

```ronin
type Box {
    var name => Number;
    function read (name => Number) { return name; }
}
```

When body resolution is joined, `name` will be present only as the type member.
The parameter either cannot be resolved as a parameter or, worse, the reference
will silently read the member. This is not only deferred resolution: the
declaration pass already promises to enforce no shadowing across the merged
scope and currently omits the declaration that should trigger it.

**Recommendation:** preserve parameter `Identifier` objects until declaration
validation and scope construction are complete. Apply writability, reserved
prefix, collision, R5/R6, and canonical identity checks to every function and
delegate parameter before producing runtime block names. Bind valid parameters
into their body scope just as loop variables are bound now. As a final runtime
invariant, make `Declaration` reject null, empty where unsupported, and
duplicate parameter names instead of allowing `Scope.Invoke` to overwrite
them.

Add the two unwritable sources above, the canonical-collision call, duplicate
ordinary parameters, and the `Box.name` shadowing example. Assert both findings
and the exact source spans.

## 2. Delegate bodies are absent from declaration-scope traversal

**Severity: high — all declaration diagnostics disappear inside a
source-reachable scope**

`Compilation.Errors` deliberately uses a reflective tree walk because an
explicit switch had missed nested grammar slots. The declaration-scope walk
immediately below it is still an explicit switch over only:

- function bodies;
- type bodies;
- loop bodies; and
- statements that are themselves a `Scope`.

A `Delegate` is a `Value`, and its `Definition` can sit in a datum initializer,
an input, a list, a lookup, or another delegate. None of those positions is a
direct case in `Compilation.Bodies`.

This compiles with zero findings:

```ronin
var callback = (x) => {
    var duplicate => Number;
    var duplicate => Number;
};
```

The same duplicate in a function or ordinary block produces `Shadowed`.
Delegate bodies likewise skip overload, R5, R6, injected-name, and nested-scope
checks. Malformed syntax inside the same body *is* found by the reflective error
walk, which makes the split especially surprising: syntax diagnostics work,
declaration diagnostics silently do not.

Adding `case Delegate` to the existing switch is insufficient because the
delegate is normally nested inside another syntax node rather than being the
statement passed to `Bodies`.

**Recommendation:** make scope discovery complete over grammar-declared
members, using the same completeness principle as the error walk while
retaining explicit ownership rules for which node opens which scope. At
minimum, recursively discover every `Delegate.Definition` inside values before
descending the declarations in its statements. Add delegates at top level and
nested in datum initializers, lists, lookups, inputs, and other delegates, with
duplicate-name and R5/R6 regressions.

## 3. An empty parameter block installs a pattern no source call can satisfy

**Severity: medium — accepted declaration is semantically dead**

`Parameters.Parse` permits an empty aggregate. `Identifier.TryPattern` treats
every parameter block as one hole regardless of its parameter count, so:

```ronin
function ping () { return 1; }
```

compiles with no findings and installs:

```text
pattern: ping (_)
blocks:  [[]]
```

The resolver cannot resolve `ping ()`: `Resolver.Group` rejects an empty group
because there is no subexpression, so the result is `NoParse`. `ping 1` can
resolve but the runtime then rejects the argument because the corresponding
block binds zero parameters. The declaration has no source spelling that
invokes it as written.

This needs one explicit language decision, but neither answer admits the
current representation:

- if `()` is a zero-arity call marker, preserve and resolve that marker rather
  than turning it into a value hole;
- if a zero-arity function is called as the plain name `ping`, reject the empty
  block or erase it consistently from the declared shape.

Add declaration, resolution, and full invocation tests for the chosen spelling.

## 4. The width guard still runs after full readback, over a second hand-built
shape

**Severity: medium pessimization and DRY failure — the hostile-input bound does
not bound the work before rejection**

The fatal diagnostic path from `REAUDIT8` is fixed: `Reads` no longer constructs
a `Pattern`, and `Declarations.Declare` chooses `PatternTooWide` before it builds
an unwritable-name finding.

`Identifier.TryPattern`, however, has already done the expensive work:

```csharp
pattern = BeginsWithHole
       || Writable is false
       || segments.Count > Pattern.MaxSegments
        ? null
        : new Pattern(segments);
```

For every non-leading over-width pattern, `Writable` first:

1. rebuilds the shape through the `Shaped` property;
2. joins the complete shape into a new string;
3. lexes that string into another complete lexeme list; and
4. compares both sequences.

Only afterward does `segments.Count > MaxSegments` refuse it. The comment in
`Declarations` that readback “is not even reached” is therefore false: it is
reached inside `TryPattern` before control returns to `Declarations`.

There are also two implementations of the shape decomposition. `TryPattern`
builds `segments` by walking `Components`, while `Shaped` independently walks
the same components with a different LINQ expression. The comment on `Shaped`
calls it “the one decomposition”, but construction, width, writability, and
diagnostic readback can still acquire separate opinions if either walk changes.
This is the same class of hand-built substitute the earlier audits were asked
to hunt.

**Recommendation:** build the segment sequence once. Check leading-hole and
width from that sequence before any rendering or re-lexing, then pass the same
sequence to writability and construction. If diagnostics need it later, return
an immutable analysis result rather than recording mutable side properties on
`Identifier`.

## The three `REAUDIT8` repairs

1. **Top-level and loop identifier writability: direct repair passes, parameter
   boundary incomplete.** Data, constants, types, plain functions, patterns,
   and bracketed loop variables now produce `UnwritableName`; valid spellings
   remain accepted. Finding 1 is the nested declaration boundary.
2. **Over-width unwritable crash: correctness repair passes.** Widths around the
   128-segment boundary no longer throw, and diagnostic readback is
   non-constructing. Finding 4 is the remaining allocation/order half.
3. **Binding invocation: passes.** A registered declaring call receives an
   `Evaluator.Binding` and runs; ordinary arguments remain eager; evaluating a
   binding outside a declaring call remains an error. A focused full-call probe
   also passed `for each (open order) in banks`, preserving the bracketed
   multi-word name as `open order`.

Renderer totality and the `FindingKind` rename also pass: every finding kind has
one example, the golden report covers the renamed `UnwritableName`, and no stale
`PatternUnwritable` reference remains.

## Regression-test note

`ADeclarationIsRefusedNeverFatalAtAnyWidth` succeeds at its primary purpose:
any source-triggered exception fails the test. It does not assert that each
invalid row produces a finding—`Assert.All` passes on an empty collection—and
it is not the requested Cartesian matrix of exact widths 128/129/130 against
zero/one/two interrupted composite keywords. Existing tests separately protect
ordinary over-width and ordinary unwritable declarations, so this is not a
fifth implementation finding, but the combined boundary should be made
non-vacuous while this code is being touched.

## Validation

- Locked restore succeeded without changing lock files.
- Debug: 692 tests passed, zero skipped.
- Release: 692 tests passed, zero skipped.
- Exact non-incremental Release build with `-warnaserror`: zero warnings and
  zero errors.
- Release coverage: 100% line, branch, and method.
- `fuzz_verify.py`: 2,382,240 resolutions, 91 pattern pairs, 24 R6 refusals,
  zero ties.
- `loop_syntax.py`: 7/7 historical free-hole checks passed. It remains a model
  of the older free-hole argument rather than the current binding runtime.
- `git diff --check 7fd6dc1..f824a81`: clean.
- Focused source-to-compilation, source-to-runtime, and source-to-resolution
  probes reproduced the findings above and were removed.
- The only pre-existing untracked path remains
  `.idea/.idea.Ronin/.idea/vcs.xml`; the audit did not modify it.

The 90 hand-aligned `dotnet format` whitespace differences are settled project
style, explicitly documented as non-gated in the workflow, and are **not a
finding**.

## Known outstanding work, not rediscovered here

The acknowledged backlog remains:

- joining resolution and later semantic/runtime phases to `Compilation`,
  including surfacing `NoParse` for adjacent return expressions;
- the remaining dangling `=>` and return-type work;
- the numeric tower and exactness rules;
- nullable analysis and the stronger analyzer backlog;
- replacing the bounded exponential brace parse with one parse/one decision;
- the resolver allocation/pooling wins; and
- the unimplemented items in `FAILUREMODES.md`, including module-composition
  semantics, recomputation cutoff, and live-edit lifetime.

Findings 1 and 2 are not merely consequences of the absent resolver join:
today's declaration-diagnostic pass already traverses scopes and enforces
identity and no-shadowing, and these source-reachable declarations and scopes
are missing from that pass.

## Recommended order

1. Preserve, validate, and scope-bind parameter declarations without flattening
   them early.
2. Make delegate scope discovery complete through nested value positions.
3. Settle and implement the empty parameter-block spelling.
4. Reorder width before writability and collapse the duplicate shape walks.
