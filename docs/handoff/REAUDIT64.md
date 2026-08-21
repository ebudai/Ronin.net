# Re-audit 64 — nullary calls are not calls throughout the pipeline; value-return bodies and numeric evaluation remain unsound

> **Ledger** — `[A]` Audit of `15c94b3..26a6923`, requested by
> `FORAUDIT64`. **Not signed off:** four high-severity findings. The five direct
> `REAUDIT63` repairs appear present, but the nullary implementation is only
> recognised as a call by one type-checker helper, explicit value-return
> functions can still be actions, and the evaluator's new numeric branch can
> throw on a token the lexer classifies as numeric.
> supersedes: none
> superseded by: none

## Audit result

The direct repairs for truth literals, delegate return ownership, unification of
variable-bearing returns, omitted-return action inference, and source-order
diagnostic roles are present and their maintained witnesses pass. The registry
derivations and `Group.Flattened` change also follow the cited rulings.

I cannot sign off the range. The new nullary bridge leaves the resolver tree as
a `Node.Name` and teaches only `Compilation.Inferred` to reinterpret that name.
Every other consumer still sees a value name. That breaks dependency inference,
cycle detection, and runtime invocation. The bridge also reconstructs a pattern
with `Split(' ')`, causing a compiler exception on a valid composite-keyword
name. Two further boundary checks are absent: a written value return does not
require the body to answer, and the evaluator assumes every `char.IsDigit`
numeric can be parsed by invariant `double.Parse`.

## Finding 1 — high — a bare nullary function is still a `Node.Name`, so dependency analysis and evaluation do not treat it as a call

**Locations:** `Compiler/Compilation.cs:127-132`,
`Compiler/Compilation.cs:193-197`, `Compiler/Compilation.cs:782-787`, and
`Compiler/Runtime/Evaluator.cs:35-43`.

`NULLARYRULING` §1 rules that a bare `f` is a call. The implementation deliberately
keeps the resolver's result as a `Node.Name`, then adds an ad-hoc fallback only in
`Compilation.Inferred`:

```csharp
Node.Name name => sorts.GetValueOrDefault(name.Words) ?? Nullary(name, declared)
```

The inference graph and `NeverAnswers` predicate accept only `Node.Call`, while
the evaluator dispatches every `Node.Name` to `graph.Read`. Consequently, the
same resolved node means “call” only while one checker method is looking at it.

### Witness A — dependency ordering becomes source-offset dependent

```ronin
var padding => number;
function first { return second; }
function second { return 5; }
var x => text = first;
```

**Actual:** zero findings.

**Expected:** a `TypeMismatch` reporting that `first` answers with `number`.

The unrelated `padding` changes declaration offsets. With no `first -> second`
edge, Tarjan orders the independent components by the string form of those
offsets; `first` is inferred before `second`, never revisited, and its answer is
silently lost. Some layouts happen to pass because the lexical ordering of the
offset strings happens to put the callee first, which is not a valid dependency
order.

### Witness B — nullary recursion is accepted

```ronin
function loop { return loop; }
```

and:

```ronin
function left { return right; }
function right { return left; }
```

**Actual:** zero findings for each source.

**Expected:** `NeverAnswers` at the recursive return sites, as for shaped
functions. The graph has no self-edge or mutual edges because each answer is a
`Node.Name`.

### Witness C — runtime reads a graph cell instead of invoking the declaration

Using the ordinary runtime join:

```csharp
SymbolTable symbols = new();
symbols.WithNames("f");
new Resolver(symbols).Resolve("f").TryTree(out var tree);

Scope scope = new();
scope.Declare(new Declaration(new Pattern(["f"]), [], (_, _) => 5d));

var actual = new Evaluator(scope).Evaluate(new Graph(), tree, insideLet: false);
```

**Actual:** `error(«f» is not declared)` from `Graph.Read("f")`.

**Expected:** `5d`, by invoking the nullary declaration.

### Required repair and regression coverage

Make callable-vs-value identity authoritative before downstream consumers see
the tree. Prefer resolving a declared nullary function to a real call node (or an
equally explicit callable node/kind) rather than teaching each consumer to
reinterpret a name. Then maintain:

- the dependency-order witness above in both declaration orders and with offsets
  crossing decimal widths;
- nullary self-recursion and mutual recursion, with and without a grounding base;
- runtime invocation of a nullary declaration; and
- the control that an ordinary value name remains a graph read.

## Finding 2 — high — the nullary type bridge splits a rendered name and crashes on a valid composite-keyword name

**Locations:** `Compiler/Compilation.cs:827-835`; the violated invariant is
documented at `Compiler/Grammar/Name.cs:88-102` and the existing safe adapter is
`Compiler/Resolution/Lexemes.cs:45-65`.

`Compilation.Nullary` constructs the lookup pattern with:

```csharp
new Pattern(name.Words.Split(' '))
```

But a composite keyword such as `part of` is one canonical lexeme whose rendered
text contains a space. The codebase already states that consumers must not split
`Words` for exactly this reason.

### Witness

```ronin
function ready part of world { return 5; }
var x => text = ready part of world;
```

**Actual:** `Compilation.Of` throws:

```text
System.ArgumentException: a pattern's segments must be words the lexer produces,
and must read back as themselves: «ready» «part» «of» «world» does not.
```

The exception originates in `Pattern` construction through
`Compilation.Nullary`. This is well-formed source; the equivalent parameterised
composite-keyword function form is already maintained elsewhere.

**Expected:** compilation completes and reports a `TypeMismatch` whose actual
type is `number`.

If finding 1 is repaired by preserving callable identity in the resolved node,
this reconstruction should disappear. If a conversion remains necessary, use
the canonical lexeme sequence rather than splitting rendered text; the existing
`Lexemes.Words` adapter re-lexes composite keywords correctly. Add ordinary,
composite-keyword, and whitespace-normalised multi-word nullary controls.

## Finding 3 — high — an explicitly value-returning function may return no value or fall through with no finding

**Locations:** `Compiler/Compilation.cs:121-123`, which sends only omitted-return
functions through `Infer`, and `Compiler/Compilation.cs:900-915`, which skips a
return site whose `Answer` is null and has no check for zero sites.

`RETURNANDLITERALS` §1b says a function that answers requires `return (_)` and
refuses bare `return`; an action permits bare return. A written `=> number`
unambiguously declares a value answer, but both valueless body forms compile
cleanly:

```ronin
function f => number { return; }
```

```ronin
function f => number { }
```

**Actual:** zero findings in both cases.

**Expected:** the bare return is refused as a valueless exit from a value-answering
function, and the fall-through body is refused for never producing its declared
answer. A call to either function is nevertheless typed as `number` from the
signature, so call-site checking cannot recover this error; the published
signature currently promises a value the body does not produce.

Validate exit flavour for functions with written returns as well as omitted
returns. Regression coverage should include bare return, fall-through, a valid
value return, nested-block exits, and the control that an omitted-return action
remains legal.

## Finding 4 — high — the re-lexed numeric evaluator throws for numeric tokens outside `double.Parse`'s accepted alphabet

**Locations:** `Compiler/Runtime/Evaluator.cs:203-222` and
`Compiler/Lexicon/Literal.cs:90-107,120-123`.

The lexer defines numeric digits with `char.IsDigit`, which includes Unicode
decimal digits such as Arabic-Indic `١`. The new evaluator branch uses throwing
invariant `double.Parse` after the lexicon identifies the token as `Numeric`.
Invariant `double.Parse` does not accept that spelling.

### Witness

Resolve and evaluate the source expression:

```ronin
١
```

**Actual:** an unhandled `System.FormatException` from
`Evaluator.Value` at `double.Parse`.

**Expected:** evaluation must not throw for a token the lexer classified as a
numeric literal. Either the language's digit alphabet is ASCII `0-9`, in which
case the lexer must enforce that authority, or these Unicode digits are numeric,
in which case value conversion must support them. At minimum, a failed conversion
must produce a runtime `Error`, as the previous `TryParse` path did, rather than
escape as an exception.

Maintain a test across the lex-resolve-evaluate boundary for every accepted
numeric alphabet, plus a malformed/unread control. The ruling to classify via
the lexicon does not require an unchecked throwing conversion after
classification.

## Verification performed

- Reviewed the complete production diff for `15c94b3..26a6923` and the cited
  rulings and consultations.
- Release build with `-warnaserror`: clean, zero warnings and errors.
- Maintained Release suite: `1325` passed, `0` failed.
- `git diff --check`: clean before this report.
- Each witness above was run as a temporary xUnit probe against the real
  compiler/runtime path. All temporary probe files were removed after capture.

The green maintained gate is therefore confirmed, but it does not cover these
four boundaries. Signoff should wait for repairs and maintained regressions.
