# Eleventh re-evaluation — the standalone delegate fix stops too early

Audited at commit `c44ec6c` (`Record the delegate bracket rule and what it
forces`), against the previous audited commit `5b0f600`.

This review reproduced all four `REAUDIT10` findings, inspected the complete
`7cc1d66` repair, read `DELEGATES.md` and the corresponding
`FAILUREMODES.md`/spec changes, and exercised the repaired paths through
`Compilation` rather than their component parsers.

Sign-off is withheld. The four reported defects are repaired in their exact
forms, but the delegate alternation fix creates a new silent parse boundary:
a delegate works as a whole value and no longer works as the first anonymous
component of a larger reference. There are also two smaller completeness and
cost issues in the new reflection/parser changes, plus a now-visible mismatch
in the delegate grammar written in the spec.

## 1. Delegate-first value parsing steals the prefix of a larger reference

**Severity: high — valid reference syntax is either split into a different
program with zero findings or rejected as malformed**

The `REAUDIT10` standalone case is genuinely repaired:

```ronin
var callback = x => { return x; };
```

The repair is an unconditional ordering change in `Value.Parse`:

```csharp
Delegate.Parse(ref current)
?? Member.Unresolved.Parse(ref current)
?? Temporary.Parse(ref current)
```

That commits as soon as the delegate itself is complete. It does not ask
whether the delegate is the first component of a longer `Reference`.

The grammar explicitly permits anonymous values as reference components
(`grammatical-structure.md` §4.7), and `Reference.Component` admits a
`Temporary` for that purpose. These should therefore be one reference:

```ronin
var result = (x) => { return x; } (1);
var result = x => { return x; } (1);
```

Through `Compilation`, both produce **zero findings but two statements**. The
datum's initializer is only the `Delegate`; the trailing `(1)` becomes a
separate `Inputs` statement. The block-terminated-statement rule makes the
wrong split look complete, so no leftover-input diagnostic exposes it.

The same shape in a context where it cannot be split is rejected instead:

```ronin
var values = { (x) => { return x; } (1) };
var values = { x => { return x; } (1) };
```

Both produce one `Malformed` finding at the enclosing initializer. The parser
has committed the list element to the delegate, then cannot account for the
following input before the list closer.

This is not limited to immediate application as a feature name. It is the
general conflict between:

- a delegate as a complete `Value`; and
- the same delegate as the leading anonymous component of a `Reference`.

Trying `Member.Unresolved` first had the opposite defect for a bare delegate:
its leading name committed before the arrow. Trying `Delegate` first merely
moves the premature commitment to the other side of the boundary.

**Recommendation:** make one decision over the complete candidate rather than
swapping the two eager alternatives again. A reference parse must be able to
recognise a bare-delegate component, and the value parser must prefer the
longer reference when components follow the delegate while still returning
the delegate itself when it stands alone. Add both bare and parenthesised
forms:

- alone;
- followed by an input, index, and word component;
- as a datum initializer and inside a list/input/lookup; and
- through `Compilation`, asserting full token consumption and the resulting
  tree, not only an empty finding list.

If two brace-adjacent statements are intended to take precedence over the
longer reference, that is a language-design decision and §4.7 needs to state
the required disambiguation. The current implementation silently chooses that
precedence at top level and a different outcome inside a list.

## 2. The reflective child-slot predicate is not complete over slot types

**Severity: low structural risk — every current slot is covered, but an
ordinary type refactor can silently remove a subtree from diagnostics**

Filtering computed non-child properties before `GetValue` is the right repair.
An inventory of the current grammar properties found no child slot excluded by
the filter, so `Identifier.Writable` is no longer invoked by the error walk and
the width-before-readback bound is real on today's source path.

The stronger completeness claim in `Compilation.Holds`, however, is false:

```csharp
if (type == typeof(object) || IsSyntax(type)) return true;
if (type.IsArray) return Holds(type.GetElementType());
return type.IsGenericType && type.GetGenericArguments().Any(Holds);
```

A slot typed `System.Collections.IEnumerable`, `ArrayList`, or a non-generic
concrete subclass such as `sealed class Children : List<Statement>` can hold
syntax children, but all return false. `Children` later knows how to enumerate
all of those values; `Members` prevents it from ever seeing the property.

No current grammar property has one of those declarations, which is why the
source sweep passes. But changing `List<Statement>` to a named collection type
is exactly the unremarkable refactor under which a reflective completeness
argument is supposed to remain true. The existing test covers a generic
interface and an array, not implemented generic interfaces, base types, or an
untyped enumerable.

**Recommendation:** walk implemented interfaces/base types when determining
element capability, and conservatively admit a non-generic `IEnumerable` whose
element type cannot be proved. Add the three counterexample types above to the
`Holds` test. Alternatively, explicitly mark child properties and add a
reflection test enforcing that every grammar child slot is marked; that trades
automatic discovery for a mechanically checked contract.

## 3. The new value alternation speculates the delegate production twice

**Severity: low pessimization — repeated parsing and a smaller effective
hostile-input budget**

`Value.Parse` now tries `Delegate.Parse` directly, then falls through to
`Temporary.Parse`, whose second alternative is the same `Delegate.Parse`.
Every lookup/input/list/index that reaches the latter therefore pays the
delegate attempt twice. A parenthesised input whose contents also look like
delegate parameters can have the entire parameter aggregate parsed repeatedly
through `Value`, `Reference`, and `Temporary` before `Inputs` finally wins.

This is more than a duplicate method call. Every speculative aggregate calls
`Parser.Nest`, and the total `groups` counter deliberately does not roll back:
it is the work budget that stops adversarial backtracking. Duplicate
speculation both allocates/re-walks input and consumes that budget, causing
large valid files to reach the refusal ceiling sooner.

**Recommendation:** after resolving finding 1, make the selected value/reference
path own each alternative once. Do not retain both the new direct delegate
branch and the old delegate branch inside `Temporary`. A focused regression
should count aggregate attempts or group-budget consumption through the real
value parser, since result-only coverage executes both branches without
detecting duplicated work.

## 4. The written delegate production describes neither implemented form
exactly

**Severity: low documentation defect — the repaired syntax and the language
spec disagree**

Section 4.8.2 currently says:

```text
datum declaration | parameters => body
```

The parser implements:

```text
(name | delegate-parameters) => body
```

A bare untyped name (`x => { ... }`) is the form just repaired and tested, but
the production omits it. Conversely, a bare typed datum declaration is not
accepted; typed delegate parameters are inside the delegate's parameter
brackets. The missing grouping also makes it read as though only the
`parameters` alternative necessarily owns the arrow and body.

**Recommendation:** write the alternatives and grouping explicitly, including
the distinction between a bare `name` and the elements allowed inside
delegate parameters. Keep the zero-argument invocation decision below it; that
new text is otherwise internally consistent and correctly records the
higher-order-cell question as open.

## `REAUDIT10` repair status

1. **Recursive parameter identifiers: passes.** Function and delegate
   parameters containing `()`, leading/medial nested holes, or pattern-shaped
   identifiers produce `EmptyHole` or `HoleInName`; no empty or flattened key
   is installed. Shape facts no longer depend on `TryPattern` having run.
2. **Bare delegate as a standalone value: passes.** Initializer, list, lookup,
   parameter-default, and nested-delegate rows all reach `Delegate` through
   `Compilation`, and its parameter is declared in the body. Finding 1 covers
   the newly exposed longer-reference boundary.
3. **Reflective writability evaluation: passes for the current grammar.**
   Members are filtered by declared type before `GetValue`; semantic bool/string
   properties are not read. Finding 2 narrows the claimed future completeness.
4. **Exact-width matrix: passes.** Its filler is now derived as
   `width - 2 - 2*gaps`; width 128 exercises acceptance and
   `UnwritableName`, while 129/130 exercise `PatternTooWide`.
5. **Runtime empty blocks: passes.** `Declaration` rejects null blocks,
   zero-name blocks, null/blank names, and duplicates before cloning/binding.
6. **Diagnostic totality: passes.** `HoleInName` is present in the enum,
   source examples, renderer-totality test, and golden output.

## Validation

- Locked restore succeeded without changing lock files.
- Debug: 733 tests passed, zero skipped.
- Release: 733 tests passed, zero skipped.
- Exact non-incremental Release build with `-warnaserror`: zero warnings and
  zero errors.
- Release coverage: 100% line, branch, and method.
- `fuzz_verify.py`: 2,382,240 resolutions, 91 pattern pairs, 24 R6 refusals,
  zero ties.
- `loop_syntax.py`: 7/7 historical checks passed.
- `git diff --check 5b0f600..c44ec6c`: clean.
- Focused source-to-tree probes reproduced finding 1 in top-level and list
  contexts and were removed.
- The only pre-existing untracked path remains
  `.idea/.idea.Ronin/.idea/vcs.xml`; the audit did not modify it.

The formatter now reports 89 whitespace differences under the current SDK
(one fewer than the previously quoted 90). They remain the same settled class
of hand-aligned continuation formatting documented in the workflow and are
**not a finding**.

## Known outstanding work, not rediscovered here

The acknowledged backlog remains:

- joining resolution and later semantic/runtime phases to `Compilation`,
  including surfacing `NoParse` for adjacent return expressions;
- the remaining dangling `=>` and return-type work;
- the numeric tower and exactness rules;
- nullable analysis and the stronger analyzer backlog;
- replacing bounded exponential brace parsing with one parse/one decision;
- resolver allocation/pooling wins; and
- the unimplemented `FAILUREMODES.md` items: module-composition semantics,
  recomputation cutoff, live-edit lifetime, outward-in-only typing, and the
  now-explicit higher-order-cell decision.

## Recommended order

1. Resolve the delegate/reference precedence as one complete parse decision.
2. Close `Holds` over non-generic and named collection types.
3. Remove the duplicate delegate speculation while doing item 1.
4. Correct the delegate production in §4.8.2.
