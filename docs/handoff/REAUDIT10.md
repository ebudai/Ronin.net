# Tenth re-evaluation — recursive parameter holes and the real parse boundary

Audited at commit `5b0f600` (`Close REAUDIT9`), against the previous audited
commit `f824a81`.

This review read `EMPTYBRACKETS.md` and its correction in `UNDERSCORE.md`,
reproduced all four `REAUDIT9` findings, and followed the new parameter and
scope walks through source, diagnostics, resolution, and runtime construction.

Sign-off is withheld. The ordinary forms from all four findings are repaired,
and the reflective delegate-scope walk is a real completeness improvement.
Two declaration boundaries remain incomplete: a parameter identifier may
itself contain holes without receiving the new validation, and the documented
bare delegate never reaches the new delegate-binding path through the actual
value parser. The width optimization is also still bypassed by the reflective
error walk before declaration analysis begins.

## 1. Parameter validation still accepts and erases nested holes

**Severity: high — accepted source is silently changed into a different
parameter name, including an empty runtime key**

The new body-entry path calls:

```csharp
Receive(parameter)
```

but `Receive` applies only:

```csharp
Unwritable(parameter)
Refused(parameter.Words, ...)
```

It does not apply `HasEmptyHole`, `TryPattern`, `LeadingHole`, the width limit,
or any rule that says a parameter identifier must be a name rather than a
pattern. `HasEmptyHole` is checked only in `Declare`, on the outer member
identifier.

Parameters are datum declarations and use the general `Identifier` parser, so
all of these are source-reachable and compile with zero findings:

```ronin
function outer (callback () => Number) { return 1; }
function outer (() => Number) { return 1; }

function outer
    (callback (x => Number) => Number)
{
    return 1;
}

function outer
    ((x => Number) rounded => Number)
{
    return 1;
}
```

The first two contain exactly the empty hole that `EMPTYBRACKETS.md` settles as
ill-formed. The latter two contain pattern-shaped identifiers where a runtime
parameter name is required.

`Identifier.Named` flattens each parameter through `Identifier.Words`, which
drops every parameter block. The resulting block names are:

```text
source parameter                       runtime block name
callback (x => Number) => Number       "callback"
(x => Number) rounded => Number        "rounded"
() => Number                           ""
```

The source brackets and nested declaration have disappeared with no finding.
The bare-empty case is worse than a diagnostic gap: the declaration table
admits the empty string as a name. When runtime declarations are joined,
`Declaration` will reject that source-derived empty string and turn a
zero-finding compilation into an exception during the later phase.

The runtime last line of defence is also incomplete for the original dead-hole
shape. This still constructs successfully:

```csharp
new Declaration(new Pattern(["ping", null]), [[]], body)
```

The duplicate/null-name check flattens the blocks and sees no parameter at all,
so an empty block vacuously passes. That reinstalls the `ping (_)` declaration
that no ordinary argument can bind to zero names.

**Recommendation:** give every identifier one role-aware analysis result rather
than calling different subsets of checks from `Declare`, `Bind`, and `Receive`.
A runtime parameter role must require a non-empty, hole-free name; if it
contains `()`, produce `EmptyHole` at the nested identifier; if it contains a
non-empty parameter block, either add the language feature deliberately or
produce a finding instead of flattening it. Make `Declaration` reject null
inner blocks and zero-name blocks as well as null/blank/duplicate names.

Add every source above for function and typed delegate parameters. Assert the
finding kind, nested span, and that neither an empty symbol-table key nor a
flattened runtime block is produced.

## 2. The documented bare delegate is still lost before `Delegate.Parse`

**Severity: medium — advertised source syntax is rejected by the real parser**

`Delegate.Parse` explicitly supports:

```ronin
x => { return x; }
```

and the class documentation uses it as its first example. The new scope code
also says that `x => …`, `(x) => …`, and `(x => Number) => …` all declare `x`.

Through `Compilation`, however:

```ronin
var callback = x => { return x; };
```

produces one `Malformed` finding. `Value.Parse` tries
`Member.Unresolved.Parse` before `Temporary.Parse`; the former accepts `x` as a
reference, so the alternation commits before `Delegate.Parse` can see the
following `=>`.

The old unit test calls `Delegate.Parse` directly over a hand-constructed token
chain, so it never exercises this competition. The new regression named
“typed or not” uses `(name)` for its untyped row. That is parenthesized and
therefore avoids the exact boundary the implementation comment calls “bare”.

This is another instance of hand-built or lower-level data proving a component
while the real path chooses a different component first.

**Recommendation:** resolve the alternation at `Value.Parse`, with lookahead or
ordering that lets a word followed by the delegate arrow commit to
`Delegate.Parse` without changing ordinary references. Add the exact datum
initializer above through `Compilation`, plus bare delegates in lists, inputs,
lookups, parameter defaults, and delegate bodies. Assert the parsed initializer
and that its parameter is declared into the delegate body.

## 3. The reflective error walk still performs writability before the width
guard

**Severity: medium pessimization — finding 4's hostile-input bound is not real
on the source compilation path**

The local `TryPattern` repair is correct:

```csharp
BeginsWithHole
|| segments.Count > Pattern.MaxSegments
|| Writable is false
```

and the shape decomposition is now shared.

`Compilation` does not reach that order first. Before it builds declarations it
runs `Errors(Module)`. `Errors` uses `Children`, and `Children` reflectively
calls `GetValue` on **every readable property declared by a grammar type** before
it asks whether the returned value is syntax.

`Identifier.Writable` is such a property:

```csharp
public bool Writable => Pattern.Writable(Shaped);
```

so every identifier is rendered and re-lexed during the error walk. Only later
does `TryPattern` see the width and short-circuit. An over-width hostile
declaration therefore still pays the complete readback allocation before the
guard intended to bound that work.

For an ordinary in-width declaration the situation is worse: the error walk
calls `Writable` once and `Declare`/`TryPattern` calls it again. `Shaped` is
cached, but the rendered string, lexemes, and sequence comparison are not, so
the real path performs two full readbacks per declaration.

The comments already identify this exact hazard for `Declares` and `Reads`,
which were made methods because the reflective walker invokes properties. The
same rule was not applied to `Writable`.

**Recommendation:** do not invoke semantic/computed properties while
discovering syntax children. Either filter members by types capable of holding
syntax before `GetValue`, mark actual child slots explicitly, or move
`Writable` and other non-child computations out of reflected properties.
Retain the width-before-writability order after that boundary is fixed. Add an
instrumented regression that counts readbacks through `Compilation.Of`, not
only a result-kind test.

## 4. The “exact width” regression matrix is still offset from every stated
width

**Severity: low test defect — the boundary repair is not guarded at its
boundary**

The new test is non-vacuous now: `Assert.Single` correctly replaces the old
`Assert.All` over a possibly empty collection. Its width arithmetic is wrong.

For:

```csharp
function compute
    {gaps × "part /* gap */ of"}
    {filler × word}
    (x)
```

the written width is:

```text
filler + 2 + (2 × gaps)
```

The extra two are `compute` and the final hole. Therefore the rows labelled:

```text
(128, 0)  (128, 1)  (128, 2)
```

actually exercise widths:

```text
130       132       134
```

and the 129/130 rows are displaced similarly. No row reaches width 128, so the
matrix does not test the legal maximum or an unwritable declaration exactly at
that maximum. Every row is comfortably over-width and naturally produces
`PatternTooWide`.

**Recommendation:** make the first input the target width and derive
`filler = target - 2 - 2*gaps`. At width 128, zero gaps should be accepted and
one/two interruptions should produce `UnwritableName`; at widths 129 and 130,
all rows should produce `PatternTooWide` because width has priority.

## `REAUDIT9` repair status

1. **Ordinary parameter identity and body scope: direct cases pass, recursive
   identifier validation remains open.** Unwritable plain parameters,
   duplicates, member/parameter shadowing, and parenthesized typed/untyped
   delegates now produce findings. Finding 1 is the nested-hole boundary.
2. **Delegate scope traversal: passes.** Duplicate declarations are found in
   delegate initializers, lists, lookups, inputs, parameter defaults, typed
   delegates, and nested delegates. The walk stops at each owned body and
   resumes it with its enclosing declaration scope.
3. **Empty function holes: direct cases pass, nested and runtime cases remain
   open.** `function ping ()`, medial empty holes, and empty holes in type bodies
   produce `EmptyHole`. Finding 1 covers parameter identifiers; the runtime
   empty-block invariant is also still absent.
4. **One source shape and local width order: passes locally.** `TryPattern`
   consumes the one cached decomposition and checks width before writability.
   Finding 3 is the earlier reflective caller that defeats the order.

The `UNDERSCORE.md` correction is implemented consistently: `_` and `(_)` are
pattern notation rather than Ronin source, `Pattern.Parse(Render(pattern))`
round-trips free-hole patterns, and source declarations still require named
holes. Renderer totality includes `EmptyHole`, and the golden diagnostic is
current.

## Design/documentation note

The `EmptyHole` message and §4.4 now say broadly that “Ronin has no parameter
lists”, while `Delegate.Parse`, its tests, and §4.8.2 still accept:

```ronin
() => { ... }
(a, b, c) => { ... }
```

The implementation can consistently distinguish a function identifier's hole
block from a delegate signature, but the wording does not make that contextual
distinction. If zero-parameter delegates remain valid, narrow the explanation
to named word-pattern/function declarations so a valid `() => …` does not
appear to contradict the diagnostic.

## Validation

- Locked restore succeeded without changing lock files.
- Debug: 711 tests passed, zero skipped.
- Release: 711 tests passed, zero skipped.
- Exact non-incremental Release build with `-warnaserror`: zero warnings and
  zero errors.
- Release coverage: 100% line, branch, and method.
- `fuzz_verify.py`: 2,382,240 resolutions, 91 pattern pairs, 24 R6 refusals,
  zero ties.
- `loop_syntax.py`: 7/7 historical free-hole checks passed.
- `git diff --check f824a81..5b0f600`: clean.
- Focused source-to-compilation, source-to-symbol-table, and runtime-constructor
  probes reproduced the findings above and were removed.
- The only pre-existing untracked path remains
  `.idea/.idea.Ronin/.idea/vcs.xml`; the audit did not modify it.

The 90 hand-aligned `dotnet format` whitespace differences remain settled
project style and are **not a finding**.

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

## Recommended order

1. Make parameter-name validation role-aware and recursive; reject every nested
   hole before flattening to runtime strings.
2. Repair bare delegates at the `Value.Parse` alternation and test the real
   compilation path.
3. Stop the reflective syntax walk from evaluating writability and other
   semantic properties.
4. Reject zero-name runtime blocks and correct the exact-width matrix.
