# Eighth re-evaluation — identifier writability and the surviving binding boundary

Audited at commit `7fd6dc1` (`Close REAUDIT7`), against the previous audited
commit `297fa1e`.

This review reproduced all seven `REAUDIT7` findings, then audited the new
canonical-word, identifier parsing, pattern-writability, binding-node,
ambiguity-counting, and completion paths as new code.

Sign-off is withheld. Five findings close cleanly. Canonical identity and
binding are partial: the direct examples now pass, but the invariants stop at
pattern declarations and resolved-tree construction respectively. The new
pattern diagnostic also introduces a source-triggered compiler exception when
writability and the width limit meet.

## 1. Unwritable plain names still collapse distinct canonical sequences

**Severity: high — source identity disagrees with symbol identity**

The new source diagnostic is applied only after `TryPattern` has found a
parameter block. `TryPattern` returns before it computes `Writable` when an
identifier has no holes:

```csharp
if (holes.Count is 0)
{
    pattern = null;
    return false;
}
```

Consequently this source still compiles with no findings:

```ronin
var ready part /* gap */ of world => Number;
```

Its canonical word sequence is:

```text
ready | part | of | world
```

The sequence cannot be rendered without changing its identity: the rendering
`ready part of world` lexes as:

```text
ready | part of | world
```

The specification added by this commit says that an identifier's words must
read back as themselves, but the implementation enforces that only for
patterns. Plain data, constants, types, functions without parameters, and
bracketed loop variables all bypass it.

This is observable beyond the missing finding. These two declarations produce
a `Shadowed` finding:

```ronin
var ready part /* gap */ of world => Number;
var ready part of world => Number;
```

Yet the two `Name` objects compare unequal—the newly corrected equality sees
their different canonical sequences—while their `Words` renderings are equal.
`Declarations.Cell` keys `SymbolTable.Names` on that rendering, so the symbol
table calls two names equal that `Name.Equals` calls different.

Resolution and completion inherit the same loss. The resolver joins a lexeme
span into a space string for lookup, and completion re-lexes the stored string.
Neither can recover whether the declaration held `part of` as one word or
`part` and `of` as two. In particular, completion after
`ready part /* gap */` cannot offer the separate next word `of`; the stored
rendering re-lexes with `part of` already fused.

The direct R5 repair is real: `Declared.Words` now lets the rule reject the
`hello part of alice` reproduction correctly. It does not make the
space-rendered `HashSet<string>` a canonical symbol table.

**Recommendation:** make writability an identifier invariant rather than a
pattern-only invariant, including loop variables, or replace string symbol
keys with immutable canonical sequences so both spellings can remain distinct.
Do not keep `Name.Equals` sequence-based while shadowing, resolution, and
completion remain rendering-based.

Add the accepted plain-name source above, the two-declaration collision, a
bracketed loop variable containing the same comment-separated pair, and
completion through the comment-separated prefix.

## 2. An unwritable over-width pattern throws while constructing its finding

**Severity: high — ordinary source terminates compilation**

The new code correctly avoids sending an ordinary unwritable pattern into the
throwing `Pattern` constructor. It then constructs the diagnostic by calling:

```csharp
member.Identifier.Reads()
```

`Reads` calls:

```csharp
Pattern.Parse(Shape)
```

which does use the throwing constructor. `Declarations.Declare` checks
writability before width, so an identifier that is both unwritable and still
over-width after being rendered reaches this path before `PatternTooWide`.

A focused source probe constructed:

```text
function compute part /* gap */ of
    word0 word1 ... word126
    (x => Number) { return x; }
```

The exact number of filler words is not special; it only keeps the rendered
shape above `Pattern.MaxSegments` after `part | of` fuses to `part of`.
`Compilation.Of` throws:

```text
ArgumentException: a word pattern may have at most 128 words and holes
```

instead of producing either `PatternTooWide` or `PatternUnwritable`.

The comment above `Declares` and `Reads` explicitly notes that `Reads` throws
for a pattern that is also too wide. Changing those values from properties to
methods prevents the reflective error walk from calling them automatically;
it does not prevent `Declarations` from calling the same throwing method while
building the finding.

The order also adds avoidable work to hostile over-width declarations:
`Pattern.Writable` renders and re-lexes the entire shape even though the
matcher width has already determined that it cannot be admitted.

**Recommendation:** check `PatternTooWide` before writability, after the
already-settled leading-hole check. Build diagnostic readback through a
non-throwing lexical decomposition rather than `Pattern.Parse`, so no finding
formatter crosses the constructor invariant it is reporting. Add a matrix at
widths 128, 129, and 130 with zero, one, and two comment-interrupted composite
keywords; assert finding kind and, above all, that `Compilation.Of` never
throws.

## 3. A binding node is still eagerly converted to an error before invocation

**Severity: high integration blocker, currently masked by the absent loop runtime**

The resolver half is substantially corrected:

- the declared occurrence is preserved as `Node.Binding`;
- the collection remains `Node.Name`;
- keyword-led names are rejected consistently with the parser;
- rendering retains the source spelling;
- the binding contributes no lookup cost.

The runtime path remains eager. `Evaluator.Invoke` evaluates every call
argument:

```csharp
call.Arguments.Select(argument => Evaluate(graph, argument, insideLet))
```

and evaluating a `Node.Binding` returns:

```text
«bank» is being declared here, and nothing has given it a value yet.
```

A focused probe registered an actual runtime `Declaration` for
`SymbolTable.Builtins[0]`, resolved `for each bank in banks`, and evaluated the
whole call. The declaration body never ran; its binding argument arrived as
that `Error`, and `Scope.Invoke` correctly refused to invoke a body on an error
input.

The new unit test evaluates the binding argument *alone* and asserts that its
error is no longer the graph's “not declared” error. That proves the node is
not a read, but not that a declaring construct can consume it. Changing which
error stops the call does not provide the non-eager runtime path requested in
`REAUDIT7`.

This is distinct from demanding a complete loop implementation now. The
binding metadata exists, which is important progress, but the only generic
call boundary erases its usefulness before any declaration can decide what
value or scope it introduces.

**Recommendation:** make invocation binding-aware. A pinned/binding argument
must reach the declaration as metadata or a binding value without ordinary
evaluation; value arguments should remain eager. Evaluating `Node.Binding`
outside a declaring call may still be an error. Add the full-call regression
with a registered declaration and assert that the body receives `bank` as
binding metadata and actually runs.

If this is intentionally deferred with the loop runtime, track it explicitly
under that work rather than calling `REAUDIT7` finding 2 fully closed.

## The seven `REAUDIT7` repairs

1. **Canonical R5 data: direct fix passes, invariant partial.** The composite
   glue reproduction now yields one `GlueInName`, and canonical `Name`
   equality/hash pass across whitespace. Finding 1 is the remaining
   non-pattern/string-key boundary.
2. **Binding: parser/resolver and tree shape pass, runtime partial.** Invalid
   names agree across the parser and resolver, and `Node.Binding` survives into
   the tree. Finding 3 is the eager invocation boundary.
3. **Keyword after a parameter block: passes.** The keyword restriction is
   applied once at the identifier's leading component; all new source controls
   behave as documented.
4. **Pattern construction: direct fix passes.** Exact round brackets are
   required, mismatched pairs are refused, and the constructor rejects dead
   literal segments. The source diagnostic introduced for the round-trip case
   leads to findings 1 and 2.
5. **Duplicate-reading ambiguity: passes.** Two identical pattern entries
   resolve as one reading, while every tested ambiguity has at least two
   distinct witnesses.
6. **CI duplicate row: passes.** The duplicate `InlineData` is gone, the exact
   non-incremental `-warnaserror` build succeeds with zero warnings, and no test
   is skipped.
7. **Completion multiplicative re-lexing: passes at the recommended minimum.**
   Each stored name is re-lexed once per request rather than once per suffix.
   Finding 1 explains why it still cannot use a preserved sequence.

The additional null-namespace repair in `Compilation` is correct and prevents
the reflective walker from throwing on compiler-synthesised collection
wrappers.

## Known-stricter implementation choices

The two previously supplied choices remain non-findings:

- `LeadingHole` rejects every leading hole although the settled design can
  describe a leading bracket-required hole; no source syntax can express it.
- R6 compares anchor runs rather than determinate prefixes; those are identical
  for every pattern source can currently declare.

## Validation

- Locked restore succeeded without changing lock files.
- Debug: 681 tests passed, zero skipped.
- Release: 681 tests passed, zero skipped.
- Exact non-incremental Release CI build with `-warnaserror`: zero warnings and
  zero errors.
- Release coverage: 100% line, branch, and method.
- `fuzz_verify.py`: 2,382,240 resolutions, 91 pattern pairs, 24 R6 refusals,
  zero ties.
- `loop_syntax.py`: 7/7 historical free-hole checks passed. It still does not
  model binding-node invocation.
- `git diff --check 297fa1e..7fd6dc1`: clean.
- Four focused audit probes reproduced the findings above and were removed.

The hand-aligned `dotnet format` differences remain settled project style and
are not a finding.

## Known outstanding work, not rediscovered here

The acknowledged backlog remains:

- joining resolution and later semantic/runtime phases to `Compilation`,
  including surfacing `NoParse` for adjacent return expressions;
- the remaining dangling `=>` and return-type work;
- the numeric tower and exactness rules;
- nullable analysis and the stronger analyzer backlog;
- replacing the bounded exponential brace parse with one parse/one decision;
- the resolver allocation/pooling wins;
- the unimplemented items in `FAILUREMODES.md`, including module-composition
  semantics, recomputation cutoff, and live-edit lifetime.

Finding 3 may be moved into the runtime-join item if that deferral is explicit;
it is not implemented by returning an error from an eagerly evaluated binding.
Findings 1 and 2 are current source/compiler defects independent of the
pipeline join.

## Recommended order

1. Make canonical writability/symbol identity consistent for every identifier.
2. Put width ahead of readback and make diagnostic readback non-throwing.
3. Preserve binding metadata through invocation, or explicitly track that half
   with the deferred loop runtime.
