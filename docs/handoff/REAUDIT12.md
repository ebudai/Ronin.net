# Twelfth re-evaluation — a later name rescues forbidden value runs

Audited at commit `bc1fe31` (`Close REAUDIT11`), against the previous audited
commit `c44ec6c`.

This review reproduced the four `REAUDIT11` boundaries, accepted the newly
stated design that Ronin has no immediate delegate application today, and
followed the replacement reference grammar through `Value`, `Reference`,
`Member.Unresolved`, aggregate parsing, and the reflective diagnostic walk.

Sign-off is withheld. The intended forms of all four findings are repaired:
standalone delegates work, anonymous values retain indexers, the added
delegate branch is gone, common named/untyped collection shapes pass `Holds`,
and §4.8.2 now matches the delegate parser. Two completeness holes remain in
the repairs, and the reference-first architecture still discards and reparses
every standalone anonymous value.

## 1. A later name turns a forbidden run of anonymous values into one reference

**Severity: high language correctness — missing separators and the explicitly
unsupported application shape compile with zero findings**

The new guard correctly refuses the exact rows in its test:

```ronin
var v = { { 1 } { 2 } };
var r = f ({ 1 } { 2 });
```

`Reference.Parse` still collects every component before deciding what kind of
reference it is. `Member.Unresolved` then accepts the result when either:

```csharp
reference.IsIndexed
```

or **any component anywhere** is a name.

Consequently, appending a name makes all of these compile with zero findings:

```ronin
var v = { { 1 } { 2 } name };
var v = { (1) (2) name };
var r = (x) => { return x; } (1) name;
var r = x => { return x; } (1) name;
```

The first two bypass the comma rule in exactly the way the new §4.7 text says
must be refused. The latter two restore the delegate-plus-input shape the same
section explicitly says is not immediate application: adding a later name
changes the entire prefix from separate statements into one unresolved
reference.

The comment that the name requirement is “load bearing” is therefore true only
for a run containing no later name. It prevents exactly `{ 1 } { 2 }`, while
the same invalid run followed by any word passes the test at
`Member.cs:41-47`.

The implementation and written rule are also not equivalent before reaching
that counterexample. Section 4.7 says, “Precisely: a run of words with whatever
follows them, or one anonymous value with an optional indexer,” then lists
`3..test`, which begins with an anonymous value and continues with a symbol and
a name. The code accepts the latter through the “any name anywhere” rule.
There is no precise production here yet; the prose names two shapes while its
own example and implementation require at least a third.

**Recommendation:** settle and encode the component sequence rather than
classifying only after an unrestricted `ParseRepeating`. The grammar appears
to need at least these distinct starts:

- a word-led reference and its arguments;
- an anonymous value followed by a symbolic operator and the rest of the
  expression (`3..test`); and
- exactly one anonymous value followed by an indexer.

An anonymous-value-led run must not become legal merely because a name occurs
later. Add the four source rows above, plus prefixes ending after each
component, and assert the complete syntax tree/finding kind rather than only
whether findings are empty. Keep the positive `thing 7 ("stuff")`, `x > 3`,
`3..test`, and anonymous-value-plus-index cases beside them so tightening the
sequence does not erase valid references.

If a later name is intended to connect earlier anonymous values, the new
“precisely” paragraph and separator rule need another design pass: that rule
would make `{ { 1 } { 2 } name }` intentionally comma-free, contradicting
§4.6 and the REAUDIT5/6 decision.

## 2. `Holds` still loses an untyped collection with an unrelated generic
interface

**Severity: low structural risk — the advertised reflective completeness is
still not closed over ordinary type shapes**

The added cases now pass:

```text
Children : List<Statement>
System.Collections.IEnumerable
ArrayList
```

The implementation decides whether an enumerable is untyped only after
collecting **every generic argument on the type, every interface, and every
base**:

```csharp
var elements = Elements(type).ToArray();
if (elements.Length is not 0)
    return elements.Any(element => Holds(element, seen));

return typeof(IEnumerable).IsAssignableFrom(type);
```

Those generic arguments are not necessarily element types. This ordinary
counterexample still returns false:

```csharp
sealed class Children : ArrayList, IComparable<Children>
{
    public int CompareTo(Children other) => 0;
}
```

`Children` is an untyped enumerable capable of holding syntax. Its unrelated
`IComparable<Children>` supplies one generic argument, so the code never
reaches the conservative enumerable fallback. The recursive `Children`
argument is already in `seen`, `Any` returns false, and the child property
would disappear from the diagnostic walk.

The same broad scan creates the opposite error for non-collections: a type
such as `Func<Statement>` is admitted because it is generic over a syntax type,
even though `Children` cannot enumerate it. That is harmless for correctness
but means computed properties of such types are evaluated again, weakening
the optimization's “exact about what it rejects” claim.

**Recommendation:** ask collection interfaces about their element types rather
than treating every generic relationship as an element relationship.
Specifically:

- arrays use their element type;
- implemented `IEnumerable<T>` interfaces supply typed element candidates;
- a type assignable to non-generic `IEnumerable` with no usable
  `IEnumerable<T>` is conservatively admitted; and
- unrelated generic interfaces do not participate.

Add the counterexample above and a non-enumerable generic wrapper such as
`Func<Statement>` to guard both sides.

## 3. Reference-first parsing still parses every standalone anonymous value
twice

**Severity: low pessimization — duplicate tree construction and duplicate
consumption of the parser work budget**

Removing the new direct `Delegate.Parse` branch from `Value.Parse` eliminates
one added speculative attempt. It does not make the selected path own the
parse once.

For any standalone anonymous value, the current path is:

1. `Value.Parse` tries `Member.Unresolved`.
2. `Reference.Parse` parses the entire value as one `Temporary` component.
3. It rejects the candidate because a lone temporary is a value, not a
   reference.
4. `Value.Parse` calls `Temporary.Parse`, which parses the same value again.

This applies to delegates, inputs, lists, lookups, and index values. A bare
delegate additionally scans its name in lookahead, parses the complete delegate
inside the rejected reference, and parses it again as the final value.

The cost is material to this parser because speculative aggregate parses call
`Parser.Nest`, whose total `groups` budget deliberately does not roll back.
Nested brace values already have a documented exponential alternation; the
reference/value boundary doubles that work and spends the refusal budget on
both copies.

**Recommendation:** return a discriminated candidate from the shared parse
(standalone temporary versus reference) or otherwise retain the parsed
temporary when `Reference` discovers it has exactly one component. The caller
can then choose the node kind without rebuilding the subtree. An instrumented
source-level regression should count aggregate entries/work-budget consumption;
752 tests and 100% branch coverage execute both parses and cannot detect that
they describe the same input twice.

## `REAUDIT11` repair status

1. **Delegate/reference precedence: design-resolved, with a remaining sequence
   bug.** The language now explicitly has no immediate application.
   Standalone bare/parenthesised delegates compile, and anonymous values with
   an indexer remain one statement/reference. Finding 1 covers the unrestricted
   component run that contradicts the new boundary.
2. **Reflective slot closure: common counterexamples pass; arbitrary interface
   composition remains open.** Named generic collections, bare
   `IEnumerable`, and `ArrayList` are admitted. Finding 2 is the remaining
   type-shape hole.
3. **Duplicate direct delegate branch: removed.** The additional REAUDIT11
   branch is gone and bare-delegate lookahead occurs inside
   `Reference.Component`. Finding 3 records the older shared-boundary duplicate
   that the repair still exposes.
4. **Delegate production: passes.** Section 4.8.2 now groups
   `(name | delegate parameters) => body`, documents typed parameters inside
   brackets, and tests the accepted/rejected source forms.

The earlier parameter-name, width-order, runtime block, renderer-totality, and
diagnostic fixes remain intact.

## Validation

- Locked restore succeeded without changing lock files.
- Debug: 752 tests passed, zero skipped.
- Release: 752 tests passed, zero skipped.
- Exact non-incremental Release build with `-warnaserror`: zero warnings and
  zero errors.
- Release coverage: 100% line, branch, and method.
- `fuzz_verify.py`: 2,382,240 resolutions, 91 pattern pairs, 24 R6 refusals,
  zero ties.
- `loop_syntax.py`: 7/7 historical checks passed.
- `git diff --check c44ec6c..bc1fe31`: clean.
- Focused source and reflection probes reproduced findings 1 and 2 and were
  removed.
- The only pre-existing untracked path remains
  `.idea/.idea.Ronin/.idea/vcs.xml`; the audit did not modify it.

The formatter still reports 89 whitespace differences under the current SDK.
They remain the settled hand-aligned continuation style documented in the
workflow and are **not a finding**.

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
  higher-order-cell decision.

## Recommended order

1. Define and enforce the legal reference-component sequences.
2. Make `Holds` inspect enumerable element contracts, not unrelated generics.
3. Preserve the once-parsed temporary across the value/reference decision.
