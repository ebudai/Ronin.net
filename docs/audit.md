Release-blocking findings
1.
The live scoping work has a failing regression test.
ScopeBuilding.cs expects one R5 violation but receives none. Pattern.Glue excludes literals before the first hole, so compute total for (_) has an empty glue set even though the validation expects for to conflict with the outer name. See Resolver.cs.
The rule’s definition and representation disagree; changing only the test or comparison loop will not resolve that semantic mismatch.
2.
The compiler executable exits before its work is finished.
Program.cs queues parsing on the thread pool but never waits for it. The process can terminate before any file finishes.
It also:
◦
Recurses through every directory and attempts to parse every filesystem entry.
◦
Does not filter source-file extensions or exclude .git, bin, and obj.
◦
Can pass a null/non-file entry to File.ReadAllText.
◦
Drops parsing results into an unused bag.
◦
Provides no deterministic ordering, diagnostics, exception aggregation, cancellation, or backpressure.
3.
Empty source files crash the parser.
Lexer.Lex returns null for an empty input instead of a sentinel. Parser treats null as unfinished and eventually dereferences Token.Next.
4.
A keyword at end-of-file throws IndexOutOfRangeException.
Keyword.Lex reads lexer[keyword.Length] after matching the keyword. For input exactly equal to if, var, compiled, and similar keywords, that indexes one character past EOF.
The same faulty logic is independently duplicated across modifier and mutability token classes.
5.
Inline comments corrupt lexer position.
Lexer.IndexOf returns an absolute source index. Comment.Lex treats it as a relative length and advances by that amount. Comments after earlier tokens can swallow following code or index beyond the source.
6.
Malformed and truncated aggregates are silently accepted.
Aggregate.Parse:
◦
Returns successfully at EOF without finding the closing delimiter.
◦
Makes separators optional, accepting adjacent values.
◦
Accepts a trailing separator despite the grammar specification forbidding it.
Because this generic parser handles blocks, lists, lookups, indexes, and parameter lists, one loose policy affects several grammars.
7.
The module parser silently ignores invalid trailing input.
Module.Parse stops when statement parsing returns null and does not require the cursor to be at the sentinel. Unmatched delimiters, leading punctuation, or unsupported tokens can cause the remainder of a file to be discarded without an error. Statement terminators are also optional despite the specification requiring them.
8.
while does not parse through the normal statement path.
Scope.Parse omits Scope.Repeating, even though that parser exists. Direct unit tests pass because they invoke Repeating.Parse themselves; real parsing routes valid while syntax into Unknown.
9.
Several declarations accept dangling syntax.
◦
Datum.Parse accepts a missing type after => or a missing initializer after =.
◦
Function.Parse accepts a missing return type.
◦
Type.Parse accepts a dangling = and potentially a missing definition.
These create apparently valid ASTs from syntactically incomplete source.
10.
Runtime function calls do not validate arity.
Runtime/Scope.Invoke binds parameters with Zip. Extra arguments are silently discarded; too few arguments leave names unbound and can cause a later KeyNotFoundException.
Serious semantic and runtime issues
•
Overload resolution currently creates false ambiguity. Duplicate pattern shapes are inserted as separate syntactic derivations in Declarations.cs and Resolver.cs. The runtime then reports ambiguity whenever more than one declaration remains, without the promised type filtering.
•
Resolver ambiguity counts can overflow. Derivation counts use unchecked long addition and multiplication. A genuinely ambiguous parse can wrap to zero or one and be reported as resolved. Counts should saturate at two when only unique-versus-ambiguous matters.
•
Graph declarations silently overwrite existing state. Graph.Declare replaces nodes by name, while constants similarly overwrite constants. Constants also mask same-named nodes during reads. Existing dependency edges may still reference the replaced node, leaving a corrupted graph.
•
A shadow-only step can miss triggers. Graph.Step marks dependants of old values dirty, but its cascade loop runs only while normal writes are pending. A turn containing only shadow advancement can therefore dirty a trigger without ever firing it.
•
Multiple reactive writes have undefined conflict semantics. Multiple when bodies can write the same cell in one round; the final value depends on traversal/insertion order.
•
Runtime exception handling is incomplete. Reactive calculations catch purity violations but allow casts, arithmetic delegates, extensions, and trigger bodies to throw through the graph. This contradicts the spreadsheet-style error model and can terminate evaluation.
•
Error propagation is not enforced by the raw graph API. A body can receive an Error dependency and continue running, despite documentation saying it should become an error without executing.
•
Numeric semantics disagree with the README. Evaluator.Value parses all numbers as double, losing integer precision beyond 2^53. Division by zero produces infinity or NaN rather than an error. Dates lex successfully but evaluate as unsupported.
•
Text escape handling is incorrect. Text.Lex only checks the immediately preceding backslash. It does not account for even/odd backslash runs, so a quote following an even number of backslashes can be misclassified. Evaluation also returns raw escaped text without unescaping it.
Performance and denial-of-service risks
•
Resolver memory is quadratic and object-heavy. Resolver.Resolve eagerly creates roughly O(binding-powers × tokens²) cells plus multidimensional tables. Every cell eagerly owns collections.
•
Resolver time approaches cubic before pattern matching. It scans each span at each binding level, repeatedly joins token text, and attempts patterns across many spans. Patterns containing holes can add combinatorial behavior.
•
Interactive resolution has no cancellation or input limit. Workbench.cs performs this work synchronously on text changes, making large pastes a UI-freeze vector.
•
Completion repeatedly splits and scans every name and pattern. A trie or anchor index would avoid repeated suffix-by-symbol work.
•
Graph traversal is recursion-heavy. Dirty propagation and cycle detection can stack-overflow on deep graphs.
•
Cycle construction is unnecessarily expensive. Cascades.cs uses recursive DFS and repeated path searches. Trigger precedence compares every trigger to every other trigger. Strongly connected components and topological processing would be safer and cheaper.
•
Dirty triggers are globally rescanned each cascade round. A dirty-trigger queue would avoid inspecting every registered trigger.
•
The lexer allocates one linked object per token and uses substring/regex work for numbers. Span-based token records and a direct numeric scanner would reduce allocation and pointer chasing.
Equality and mutability defects
•
Aggregate.Equals compares elements, but GetHashCode uses the backing list’s identity. Equal aggregates can produce different hashes.
•
Name.GetHashCode hashes token memory objects rather than clearly hashing their textual contents. This risks disagreement with content-based equality.
•
Token.Append increments ReadOnlySequenceSegment.RunningIndex by one rather than the preceding segment’s memory length. Sequence positions are therefore incorrect.
•
Patterns, argument lists, groups, and declarations retain caller-owned mutable collections. Mutating one after it becomes a dictionary key can invalidate hashes, cached renderings, and resolver tables.
DRY and architecture failures
•
Keyword boundary logic is copied across modifier and mutability classes even though a generic keyword helper already exists. The duplication propagated the EOF crash.
•
Operator syntax lives separately in resolver metadata and runtime implementation. Adding an operator in one place can produce syntax/runtime drift. A single operator registry should describe spelling, precedence, associativity, and evaluator behavior.
•
Compound assignment lexers duplicate punctuation-matching mechanics.
•
Error AST classes repeat the same Reason/Tokens boilerplate.
•
Aggregate<T> is an example of harmful over-generalization: different constructs need different emptiness, separator, trailing-separator, and terminator policies.
•
The executable does not connect the compiler’s phases. It lexes/parses and discards modules; declaration building, name resolution, type-directed overload selection, imports, runtime lowering, and useful diagnostics are not part of one pipeline.
•
Source locations are missing from diagnostics, making real multi-file errors difficult to action.
Build, dependency, and quality-system issues
•
Nullable analysis is disabled in Compiler.csproj. Several discovered crashes are exactly the class of error nullable analysis would expose.
•
Tiered compilation is explicitly disabled, preventing adaptive runtime optimization without an evident justification.
•
Unsafe code is enabled even though no unsafe implementation was found.
•
No SDK pin, package lock, CI configuration, benchmark project, or enforced analyzer/formatter gate was found.
•
Lexer prefix matching is culture-sensitive; analyzer CA1310 correctly flags the missing ordinal comparison.
•
Formatting verification currently fails across many files.
Vulnerable packages
•
Test dependencies transitively include System.Net.Http 4.3.0, affected by a high-severity information-disclosure advisory: GHSA-7jgj-8wvc-jh57.
•
Test dependencies transitively include System.Text.RegularExpressions 4.3.0, affected by a high-severity ReDoS advisory: GHSA-cmhx-cq75-c4mj.
•
Scratch transitively includes Tmds.DBus.Protocol 0.20.0, affected by high-severity signal-spoofing, resource-exhaustion, and malformed-message issues: GHSA-xrw6-gwf8-vvr9.
The first two mainly affect developer/CI exposure; the DBus dependency affects the executable Scratch surface on Linux.
Why the high coverage did not catch this
The coverage number is misleadingly reassuring. Tests commonly hand-construct token chains instead of passing source through the lexer, and even the parser integration test builds tokens manually. Parser tests frequently assert only the returned subtype, not that parsing consumed the complete input.
Consequently, coverage misses the actual phase boundaries where most failures occur: empty input, keyword-at-EOF, inline comments, missing delimiters, ignored trailing input, while integration, malformed declarations, and executable shutdown.
Recommended remediation order
1.
Freeze the scoping semantics and repair the currently failing R5 test/implementation contract.
2.
Fix lexer EOF/null/comment cursor defects and add source-to-AST tests.
3.
Make parsing strict: required closers, separators, terminators, and complete-input consumption.
4.
Add while to normal scope parsing and reject dangling declaration syntax.
5.
Repair runtime arity, duplicate-name handling, reactive settling, error propagation, and numeric semantics.
6.
Redesign resolver storage/counting before exposing it to arbitrary editor input.
7.
Add a real end-to-end compiler pipeline with deterministic file discovery and awaited work.
8.
Enable nullable analysis, analyzers, formatting, dependency updates, CI, fuzz/property tests, and performance benchmarks.
