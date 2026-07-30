# Fourteenth re-evaluation — modules do not enforce statement terminators

Audited at commit `e6d2680` (`Close REAUDIT13`), against the previous audited
commit `69ba640`.

Both `REAUDIT13` findings are incorporated correctly. Indexed anonymous values
now remain attached when they enter an operator expression, including longer
index chains and nested source contexts. The reflective child-slot predicate
now asks whether any concrete grammar node is assignable to the declared slot
type, so the interface/base-type hole is closed.

Sign-off is withheld. The sequencing probes exposed a broader pre-existing
boundary defect at the top level of a file: `Module.Parse` treats a statement
terminator as entirely optional. Ordinary statements with no separator are
silently accepted as separate statements. The new reflection code also adds
one minor configured naming diagnostic.

## 1. `Module.Parse` accepts ordinary statements with no terminator between
them

**Severity: high language correctness — missing punctuation silently becomes a
different valid program**

The specification says:

> All [statements] are completed with either punctuation or the end of file.

The block rule adds one explicit elision: a statement whose last token is `}`
needs no `;`. The integration suite also says that a module is a statement
aggregate “with the same separator rule.”

`Module.Parse` does not implement either rule. Its loop parses a statement and
then merely tries to consume a terminator:

```csharp
while (Statement.Parse(ref parser) is Statement statement)
{
    scope.Statements.Add(statement);
    parser.TryAdvance<Terminal>();
}
```

Failure to advance is ignored, regardless of the token that ended the statement
or what follows it. Through the real compilation path, both of these produce
zero findings:

```ronin
1 2;
```

This becomes two literal statements.

```ronin
var first = 1 var second = 2;
```

This becomes two `Datum` declarations. The missing semicolon changes no finding
and no declaration; the source is accepted exactly as if it had contained one.

The defect also hides the negative side of the repaired leading-value rule:

```ronin
var r = { 1 } [0] (2);
```

`Reference.Leads` correctly refuses to treat the input block as a continuation
of the indexed value—immediate application is not part of the language. The
module then accepts the fallback pieces as three statements:

```text
Datum, Index, Inputs
```

with zero findings. The same happens with:

```ronin
var r = { 1 } [0] { 2 };
var r = x => { return x; } [0] (2);
```

Inside a braced definition the aggregate parser does enforce the separator
after the intermediate `]`; at module level it does not. Statement validity
therefore changes solely because the same tokens were moved out of a block.

This is why the existing top-level generator does not catch it. Its “simple”
element already contains `;`, while every element without one ends in `}`.
It exercises only the two legal cases and never removes the terminator from an
ordinary statement.

**Recommendation:** give module sequencing the same explicit separator policy
as `Aggregate<..., Statement, Terminal, ...>`:

- accept a consumed `Terminal`;
- accept end of file after the final statement;
- allow omission before another statement only when the preceding statement's
  last non-trivia token is `Close.Brace`; and
- otherwise produce one malformed/unexpected-input finding and recover at the
  next structural boundary.

Prefer extracting the statement-sequence policy rather than maintaining a
second copy beside `Aggregate.Ended`; the test already claims the two paths
share a rule.

Add a source matrix at both module and block level:

```text
1; 2;                         accepted
1 2;                          refused
var first = 1 var second = 2; refused
var first = 1                 accepted at EOF
function f {} var second = 2; accepted by brace elision
var first = { 1 } var second = 2; accepted by brace elision
```

Assert the complete statement tree and findings. Empty findings alone are the
failure mode here.

## 2. The new `Nodes` field violates the configured private-field naming rule

**Severity: trivial maintainability/style — one new unique analyzer diagnostic**

The assignability repair introduces:

```csharp
private static readonly System.Type[] Nodes = ...
```

The project naming rule requires private fields to begin with a lowercase word,
as the nearby `members` cache does. `dotnet format` reports:

```text
Compilation.cs(416,43): IDE1006: 'Nodes' must begin with a lower case character
```

It appears twice in diagnostic output because two formatter analysis passes
report it. This is not one of the settled hand-aligned whitespace differences
and was introduced in the audited delta.

**Recommendation:** rename the field to `nodes`.

## `REAUDIT13` repair status

1. **Indexed values entering operator expressions: passes.** The stateful rule
   consumes zero or more attaching indexers and then accepts either the end of
   the reference or a symbol continuing the expression. One and several
   indexers, nested list/lookup contexts, return values, conditions, and a
   delegate-plus-index operand remain one source statement. Finding 1 above is
   the independent module separator that can hide correctly refused
   continuations.
2. **Direct interface/base child slots: passes.** The concrete grammar-node set
   closes `Holds` over assignability. `IError`,
   `IParsable<Statement>`, ordinary grammar bases, object, and enumerable forms
   are admitted; `Func<Statement>`, `Lazy<Statement>`, recursively irrelevant
   collections, and string collections remain rejected.

The previous parse-once, trailing-name, delegate/reference, diagnostic,
aggregate-separation, keyword-boundary, resolver-pinning, and renderer repairs
remain intact in the reviewed delta and focused regressions.

## Validation

- Locked restore succeeded without changing lock files.
- Focused `StatementShapes`/`Compilations`: 138 tests passed.
- Debug: 782 tests passed, zero skipped.
- Release: 782 tests passed, zero skipped.
- Exact non-incremental Release build with `-warnaserror`: zero warnings and
  zero errors.
- Release coverage: 100% line, branch, and method.
- `fuzz_verify.py`: 2,382,240 resolutions, 91 pattern pairs, 24 R6 refusals,
  zero ties.
- `loop_syntax.py`: 7/7 historical checks passed.
- `git diff --check 69ba640..e6d2680`: clean.
- Focused source probes reproduced finding 1 and were removed.
- The only pre-existing untracked path before this report remained
  `.idea/.idea.Ronin/.idea/vcs.xml`; the audit did not modify it.

The formatter still reports 89 whitespace differences. They remain the settled
hand-aligned continuation style documented as non-gating in the workflow and
are **not a finding**. It reports 21 `IDE1006` lines in total; 19 are the prior
repeated baseline and the two new lines are the single `Nodes` diagnostic in
finding 2.

## Known outstanding work, not rediscovered here

The acknowledged backlog remains:

- joining resolution and later semantic/runtime phases to `Compilation`,
  including surfacing `NoParse` for adjacent return expressions and malformed
  operator operands;
- the remaining dangling `=>` and return-type work;
- the numeric tower and exactness rules;
- nullable analysis and the stronger analyzer backlog;
- replacing bounded exponential brace parsing with one parse/one decision;
- resolver allocation/pooling wins; and
- the unimplemented `FAILUREMODES.md` items: module-composition semantics,
  recomputation cutoff, live-edit lifetime, outward-in-only typing, and the
  higher-order-cell decision.

## Recommended order

1. Enforce the shared statement-separator policy in `Module.Parse`.
2. Rename `Nodes` to satisfy the configured field convention.
