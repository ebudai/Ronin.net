# Thirteenth re-evaluation — an indexed value cannot enter an expression

Audited at commit `69ba640` (`Close REAUDIT12`), against the previous audited
commit `bc1fe31`.

The user had not lost their place: `69ba640` explicitly incorporates
`REAUDIT12`, and each of its three repairs is present. The exact trailing-name
cases are refused, collection element types now come only from enumerable
contracts, and a lone anonymous value is retained rather than parsed twice.

Sign-off is withheld. One live grammar defect remains in the new leading-value
rule: its two permitted continuations do not compose. The reflective diagnostic
walk also retains a low-risk completeness hole for slots declared as interfaces
or base types outside the grammar namespace.

## 1. An indexer followed by an operator is silently split into statements

**Severity: high language correctness — a valid expression is irreversibly
fragmented before the resolver can see it, currently with zero findings**

Section 4.7 says that an indexer attaches to a leading anonymous value and that
a symbol takes a leading value as its left operand. Those operations must
compose: once the indexer has attached, its result is still a value and can be
the left operand of an operator.

`Leads` instead chooses between the two continuations by inspecting only
component 1:

```csharp
if (components[1].AsSymbolic is not null) return true;

for (var at = 1; at < components.Count; ++at)
{
    if (components[at].AsTemporary is not Index) return false;
}
```

That admits either:

- a symbol immediately after the value, with the remainder left for resolution;
  or
- an index-only suffix.

It cannot admit an index suffix followed by a symbol. Through the real lexer and
parser:

```ronin
var r = { 1, 2 } [0] + 3;
```

becomes **two statements**, while:

```ronin
var r = { 1, 2 } [0] [1] + 3;
```

becomes **three statements**. Both compilations currently produce zero
findings. Each failed attempt falls back to the anonymous value at its front;
the aggregate's closing-bracket terminator elision then makes the leftover
index/operator span look like another complete statement.

This is not something resolver integration can repair. The design says the
parser establishes statement boundaries and the resolver neither joins nor
splits them. Here the resolver would receive separate spans and could never
recover the intended `{ 1, 2 } [0] + 3` expression.

The existing positive tests stop exactly one component short of the defect:

```ronin
{ 1, 2 } [0]
{ 1, 2 } [0] [1]
3 + 4
```

They test each continuation in isolation, not their composition. The test also
asks only for a statement count in that table and asks for empty findings in a
separate word-led table. This case needs both assertions together; empty
findings alone would certify the current wrong split.

**Recommendation:** express the leading-value rule as states rather than as a
single test of component 1. At minimum:

1. consume the leading anonymous value;
2. consume zero or more attaching indexers;
3. either end the reference or accept a symbol and hand the complete remaining
   expression span to the resolver.

Add source-level regressions for one and several indexers followed by an
operator, asserting both one syntax statement and the expected finding state.
Keep the existing missing-separator and immediate-application negatives beside
them.

If indexed results are deliberately not operator operands, that is a language
decision missing from §4.7 rather than an implementation repair: the section
currently describes attachment and left operands independently and gives no
reason their ordinary composition would be forbidden.

### What is not part of this finding

A symbol immediately after a leading value switches the remainder into
expression territory. Therefore a structurally collected span such as:

```ronin
3 + { 1 } { 2 }
```

does not by itself prove another defect in `Leads`; the resolver is the layer
that must return `NoParse` for a malformed right operand. `Compilation` does not
yet invoke resolution, which is acknowledged backlog and explains today's
absence of a finding. Unlike the indexed-value case above, the parser has at
least preserved one span for the resolver to judge.

## 2. `Holds` does not recognise direct interface/base slots that can contain
syntax

**Severity: low structural risk — no current grammar property has the shape,
but the advertised reflective completeness is not closed over it**

The enumerable-contract repair is correct for the `REAUDIT12` counterexamples:

- `ArrayList` plus unrelated `IComparable<T>` is conservatively admitted;
- a recursively typed `List<Recursive>` terminates and is rejected;
- `Func<Statement>` and `Lazy<Statement>` are no longer mistaken for
  collections.

The direct-slot test remains:

```csharp
if (type == typeof(object) || IsSyntax(type)) return true;
```

`IsSyntax` means that the **declared type's namespace** begins with
`Ronin.Grammar`. It does not mean that the declared type can hold a grammar
node. For example:

```text
typeof(IParsable<Statement>).IsAssignableFrom(typeof(Statement)) == true
Holds(typeof(IParsable<Statement>))                              == false
```

The existing `IError` interface is a more diagnostic-specific example. It lives
in `Ronin.Compiler`, while many grammar recovery nodes implement it. A future
grammar property declared as `IError` could directly contain such a node, but
`Reflect` would exclude the property before `Children` reads its runtime value.
The error would disappear from the walk whose completeness is meant to prevent
exactly that silence.

There is no current source-reachable miss from this shape: the present grammar
properties use concrete grammar types, `object`, or collection contracts already
covered by `Holds`. This is therefore a contract/future-proofing finding rather
than a current malformed program that compiles cleanly.

**Recommendation:** make “could hold syntax directly” an assignability question,
not a namespace question about only the declared type. A cached set of concrete
grammar syntax types can answer whether:

```csharp
declared.IsAssignableFrom(concreteSyntaxType)
```

for any syntax type. Apply the same closure to enumerable element types. At
minimum add `IError`, `IParsable<Statement>`, and a non-syntax interface as
tests, so the stated completeness guarantee and the filter evolve together.

## `REAUDIT12` repair status

1. **Trailing names no longer rescue forbidden value runs: passes for the
   audited cases.** `{ { 1 } { 2 } name }`, `(1) (2) name`, and longer trailing
   names are refused. Finding 1 is a different, compositional hole in the new
   rule.
2. **Enumerable element discovery: passes the reported counterexamples.**
   `Compared`, the recursive collection, `Func`, and `Lazy` have the intended
   answers. Finding 2 concerns direct interface/base slots rather than another
   enumerable-contract error.
3. **Standalone anonymous values are parsed once: passes.** The parser hands the
   already-built temporary back to `Value.Parse`; the instrumented group counts
   are 2, 1, 1, 2, and 8 for the five regression rows rather than rebuilding
   their trees.

The earlier delegate/reference precedence, parameter-name, width-order, runtime
block, renderer-totality, aggregate-separation, keyword-boundary, and diagnostic
repairs remain intact in the reviewed delta and focused regressions.

## Validation

- Locked restore succeeded without changing lock files.
- Focused `StatementShapes`/`Compilations`: 132 tests passed.
- Debug: 776 tests passed, zero skipped.
- Release: 776 tests passed, zero skipped.
- Exact non-incremental Release build with `-warnaserror`: zero warnings and
  zero errors.
- Release coverage: 100% line, branch, and method.
- `fuzz_verify.py`: 2,382,240 resolutions, 91 pattern pairs, 24 R6 refusals,
  zero ties.
- `loop_syntax.py`: 7/7 historical checks passed.
- `git diff --check bc1fe31..69ba640`: clean.
- Focused source and reflection probes reproduced findings 1 and 2 and were
  removed.
- The only pre-existing untracked path before this report remained
  `.idea/.idea.Ronin/.idea/vcs.xml`; the audit did not modify it.

The formatter reports 89 whitespace differences and 19 repeated `IDE1006`
diagnostics under the current SDK. The whitespace differences remain the
settled hand-aligned continuation style explicitly documented as non-gating in
the workflow and are **not a finding**.

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

1. Settle and implement composition of attached indexers with following
   operators; guard the complete syntax tree, not only finding emptiness.
2. Close `Holds` over direct assignable interface/base slot types.
