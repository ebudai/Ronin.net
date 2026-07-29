# Fourth re-evaluation

Audited at commit `1a6895b` (`Spell the loop «for each bank in banks»`).

The four implementation findings in `REAUDIT3.md` are substantially corrected:

- typed functions and arrow-bodied scopes now retain their intended AST shapes;
- a wide plain name is no longer mistaken for an over-wide pattern;
- operator construction validates binding power and implementation;
- `for each <name> in <collection>` reaches `Scope.Iterating`, and its variable is bound in the body.

The implementation and Python model both pass their suites. Sign-off is nevertheless withheld because two source-level keyword defects remain, and one deliberate implementation choice contradicts the designer's explicit R6 checklist.

## 1. Keywords cease to be keywords immediately before punctuation or symbols

**Severity: high**

`Keyword.Lex` recognizes a spelling only at end of input or when the next character is whitespace:

```csharp
if (lexer.Length > keyword.Length &&
    char.IsWhiteSpace(lexer[keyword.Length]) is false) return null;
```

That is not the language's token boundary. `Word.Lex` also stops at symbols and punctuation. An exact keyword followed by `=>`, `(`, `;`, or another delimiter therefore fails the keyword check and is immediately accepted as an ordinary `Word`.

Confirmed through `Compilation.Of`, all with zero findings:

```ronin
constant in=>Number;
type in;
function in(x=>Number) { return x; }
function f (in=>Number) { return in; }
var if=>Number;
```

Adding whitespace changes their meaning: `in =>` and `if =>` become keyword tokens and are rejected. This makes token identity depend on formatting around punctuation.

It directly defeats both relevant contracts:

- `in` is documented and implemented as reserved outright, yet it remains usable as a type name, function name, parameter name, constant name, and reference when punctuation follows it.
- The routing fix says a name cannot begin with a production keyword, yet `var if=>Number` declares exactly such a name.

`var in=>Number` happens to receive a `GlueInInjectedName` finding because mutable data injects `old in`; that is an accidental secondary catch. A constant, type, function, or parameter has no such catch and compiles cleanly.

The loop's multi-word capture hazard remains blocked by R5, so this does not reintroduce the exact silent loop rewrite demonstrated in `LOOPSYNTAX.md`. It does mean the stronger “`in` may not appear in any name” policy is not true.

**Recommendation:** define a keyword boundary using the same continuation rule as `Word.Lex`. A spelling is a keyword when it is followed by end of input or by a character that cannot continue a word—whitespace, symbol, or punctuation. Add a table-driven test for every keyword followed by:

- end of input;
- whitespace;
- `=>`;
- `(`;
- `;`;
- an identifier-continuation character, which must keep forms such as `iffy` as words.

Include multi-word keywords (`for each`, `part of`) in that matrix.

## 2. The loop change rejects every non-modifier keyword anywhere in a name

**Severity: medium language regression**

The routing fix at `48a75d6` deliberately rejected production keywords only at the beginning of a name. Its explanation correctly said a keyword in the middle announces nothing and should remain legal.

The loop implementation changed `Name.Parse` to stop at every `Keyword` except `Modifier`, regardless of position:

```csharp
while (parser.Token is Word && parser.Token is not Keyword or Modifier)
```

Only `in` needs the new anywhere-in-a-name reservation. The blanket change also removes unrelated words such as `if`, `function`, `while`, and `when` from every later position.

Confirmed source regressions:

```ronin
var ready if needed => Number;
var total function count => Number;
function compute while ready (x => Number) { return x; }
```

All now produce malformed-input findings. The guide still states that only the first word of an identifier's first word component cannot be a keyword, so implementation and documentation disagree.

**Recommendation:** retain position while parsing a name:

- reject production-announcing keywords at the first word;
- reject `in` at every position if outright reservation remains the chosen policy;
- continue accepting other keywords after the first word.

If the intended design is instead to reserve every keyword everywhere, that is a broader language change requiring an explicit designer decision and corresponding guide/specification update. It was not part of the loop decision.

## 3. The implementation deliberately does not apply the designer's stated R6 result

**Severity: contract decision required; not a demonstrated safety failure**

`LOOPSYNTAX.md` says:

> R6 rejects `for (_)` beside `for each (_) in (_)`.

Its implementation checklist explicitly requires that rejection and test 8 expects it. The compiler instead lexes `for each` as one indivisible keyword token, represents the builtin anchor as the single segment `"for each"`, and permits:

```ronin
function for (x => Number) { return x; }
```

The new test suite records this as an intentional divergence. Under the implemented token model, `"for"` is not a segment prefix of `"for each"`. Reserving `in` and enforcing R5 also make the rejected pairing unnecessary for loop safety, as the design note itself observes.

The implementation is internally coherent, but it refines R6 contrary to the designer's instruction to implement the conservative rule for now. This needs explicit designer acceptance or a change to align with the checklist.

Approval should cover all consequences of the one-token choice:

- `for (_)` remains a legal user pattern;
- `for each` is one lexer token rather than two anchor words;
- R6 is defined over lexer-token segments, not the reader-visible words;
- the keyword's internal spelling contains one literal space, so `for  each` and `for<TAB>each` are not the same keyword.

## LOOPSYNTAX checklist status

| Item | Status |
| --- | --- |
| Canonical `for each <name> in <collection>` AST | **Pass** through `Module`, `Compilation`, and CLI |
| Remove implemented `iterate ... => ...` production | **Pass** |
| Bind loop variable in its body | **Pass** |
| Run ordinary declaration/R5 checks on loop variable | **Pass**, demonstrated with non-keyword glue `to` |
| Include builtin loop glue in every scope | **Pass** |
| Reserve single-word `in` outright | **Chosen, but incomplete** because finding 1 bypasses the keyword |
| R6 reject `for (_)` beside builtin loop | **Deliberately not implemented**; finding 3 |
| Reproduce the silent-capture hazard | **Pass** |
| Hazard becomes no-parse with forbidden names absent | **Pass** in resolver test/model |
| Production unresolved-name/no-parse diagnostic | **Pending resolver-to-Compilation integration**, as previously acknowledged |

The outright reservation choice also changes checklist case 5: `for each in flight order in orders` is currently a generic malformed loop (`expected name`), not the requested typed R5 diagnostic on the variable with the loop pattern as a related span. That is a predictable consequence of moving `in` into the lexer, but it should be included in the designer's approval of the stronger reservation policy.

## Previous findings rechecked as closed

- `function f => Number`, `if ready => result`, `while`, `when`, and `when changing` all produce their intended source-level AST nodes.
- A 129-word plain name compiles without `PatternTooWide`; a 129-segment pattern still produces the typed finding.
- Invalid operator powers now throw `ArgumentOutOfRangeException` at construction.
- A null operator implementation now throws `ArgumentNullException` at construction.
- Added and replaced operator implementations are still carried from resolution into evaluation.
- Cold/cleared-cache concurrent compilation no longer corrupts the reflection cache.

## Validation

- Release: 539 tests passed.
- Debug: 539 tests passed.
- Release coverage: 100% line, branch, and method.
- `docs/handoff/loop_syntax.py`: 7/7 checks passed against `dp_resolver.py`.
- The command-line executable reports one statement for the documented loop and two correctly shaped statements for the prior typed-function/conditional routing reproduction.
- Focused boundary cases were exercised through `Compilation.Of`, not hand-built token chains.

The documented hand-aligned formatter differences remain settled policy and are not a finding.

## Documentation/test cleanup

`Test/Integration/Progress.cs` still says `for each` compiles cleanly, calls `iterate` the implemented keyword, and describes the language decision as open. Those comments are now false and should be updated with the keyword fixes.

## Recommended order

1. Correct keyword boundary recognition and add the punctuation matrix.
2. Narrow the anywhere-in-name restriction to `in`, or obtain a broader language decision.
3. Ask the designer to accept or reject the one-token/R6 refinement explicitly.
4. Update stale progress-test commentary.
