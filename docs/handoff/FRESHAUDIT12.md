# Fresh audit 12 — the repair is linear in candidates, not in cost or validity

**Re-audited:** `ba7cbdb..2e14d0f`, the two commits addressing
`FRESHAUDIT11`.

**Result:** no sign-off. The exact reproductions moved substantially:

- the eight-child source now receives all five displayed repairs instead of
  none, and its repair pass fell from about nine seconds / 10.9 GB cumulative
  allocation to 0.43 seconds / 0.42 GB;
- the server now honestly advertises full synchronization and a query after a
  full change reads the changed document;
- numeric/missing methods and the reported malformed request parameters no
  longer escape the host, and unknown methods receive `MethodNotFound`.

The replacement repair invariant — bracket every subtree, verify, then trim —
is not valid for every node kind and still is not an editor-safe work bound. A
small ambiguity containing a list has two manually verified repairs and zero
generated repairs. At a larger but legal expression, the verification input
itself crosses the resolver's lexeme ceiling and four of five displayed repairs
vanish. Before that boundary, 111 lexemes still cost about 5.3 seconds and 3.28
GB cumulative managed allocation.

The parsed-message hardening also retains one relational hole: error paths for
recognized request methods assume an id exists. After initialization, malformed
`hover` and `codeAction` notifications and a second `initialize` notification
still throw `NullReferenceException` out of `Host.Serve`.

This re-audit therefore has two high-severity repair findings and one medium
protocol finding. All maintained gates are green: locked restore,
warning-as-error Release build, all 1,171 tests in Debug, all 1,171 tests in the
exact Release coverage gate, 100% line/branch/method coverage for `Ronin` and
`Ronin.Server`, and the transitive NuGet vulnerability audit.

The deliberately open `FRESHAUDIT8` findings 6 and 7 remain outside this
re-audit and are not counted again.

No production, maintained test, or existing documentation file was changed
during this re-audit. This file is the only repository artifact added.

---

## Disposition of `FRESHAUDIT11`

| prior finding | re-audit result |
|---|---|
| 1. repair work takes seconds/gigabytes and omits short-source repairs | **Partial.** Subset enumeration is gone and the exact eight-child source is repaired. The full-tree/trim replacement still takes seconds and gigabytes at 111 lexemes, fails across collections, and makes its own verification exceed the lexeme ceiling; see findings 1 and 2. |
| 2. incremental synchronization is advertised but not implemented | **Closed.** The server advertises `TextDocumentSyncKind.Full` (`1`), consumes the full changed text, and a subsequent hover reads it. |
| 3. invalid request shapes escape or receive false success | **Partial.** The reported request cases and unknown-method result are fixed. Equivalent notification paths still dereference the absent id and crash; the request/notification envelope remains unvalidated; see finding 3. |

---

## 1. Bracketing every subtree does not select a target containing a collection

**Severity: high — a seven-lexeme production expression has two readings and two
working bracket repairs, but `Compilation` publishes zero repairs and the editor
therefore offers zero actions.**

`Selecting` at `Compiler/Resolution/Repair.cs:179-207` obtains every span from
`target.Whole`, brackets the entire set, and refuses the reading if that one
candidate does not compare equal to the target. The premise in lines 190-193 is
that every subtree made explicit pins the structure by construction.

That premise disagrees with the tree walk and comparison. `Node.Group.Within`
at `Compiler/Resolution/Node.cs:230` exposes a collection's elements to
`Node.Whole`, so an element such as `a` in `[a]` receives its own repair group.
`Stripped` at `Compiler/Resolution/Repair.cs:332-342`, however, recurses only
through calls and operations. A collection is returned unchanged, so the
candidate contains a grouped element where the target contains the bare element
and `Same` rejects it.

Production reproduction:

```ronin
function send (x => Number) { return x; }
function send (x => Number) to (y => Number) { return x; }
function print (x => Number) { return x; }
function print (x => Number) to (y => Number) { return x; }
var a => Number;
var b => Number;
var result = print send [a] to b;
```

Production `Compilation` returns one `Ambiguous` finding with `Total = 2`, two
displayed readings, and `Repairs.Count = 0`. The readings render alike, but the
two meanings and their source repairs are distinct:

```ronin
var result = print (send [a] to b);
var result = print (send [a]) to b;
```

Compiling either edited source returns no findings and one resolved reading.
The first selects `print(send-to([a], b))`; the second selects
`print-to(send([a]), b)`. The absent repairs are therefore not a language
limitation or the work budget firing; the newly asserted full-candidate
construction rejects valid targets.

This class is broader than lists. Any node whose descendants are included by
`Whole` but not mirrored by `Stripped` can acquire repair groups that survive
comparison; multi-part groups and lookups share the collection structure.

**Recommendation:** make the repair-span walk and repair-group stripping obey
one structural contract. For an ambiguity outside a collection, treating the
collection as an opaque subtree matches `Compilation`'s existing policy that
its element references are reported/repaired separately. Alternatively recurse
through every node kind in the comparison if inserting inside it is intended.
Whichever policy is chosen, assert the full candidate's structural equality for
lists, lookups, multi-part groups, previous values, calls, and operations before
using it as the derivation invariant. Add this exact source through
`Compilation` and `Language.Actions`, and apply both actions to their associated
structural readings.

---

## 2. Full-tree verification still takes seconds/gigabytes and can exceed the source limit by itself

**Severity: high — a 111-lexeme action computation blocks for about 5.3 seconds
and allocates 3.28 GB cumulatively; at 139 source lexemes, four of five displayed
readings lose their repairs because the verifier inflates its own candidate past
the 256-lexeme ceiling.**

The subset power set is gone, but `Selecting` still performs one complete
resolution for the full bracket set and then one complete resolution for every
span it tries to remove (`Compiler/Resolution/Repair.cs:194-205`). Candidate
count is linear in tree nodes; candidate *cost* is not. Every candidate reruns
the resolver over a longer statement, and the server performs the pass for each
of five displayed alternatives.

A warmed Release probe used the same legal table and repeated independently
ambiguous children from the prior audit:

```text
names:    a, b, a to b
patterns: send _, send _ to _
source:   (send a to b) + ... + (send a to b)
```

Results from the repaired implementation:

| children | source lexemes | displayed | repairs | repair time | cumulative managed allocation |
|---:|---:|---:|---:|---:|---:|
| 6 | 41 | 5 | 5 | 0.15 s | 0.185 GB |
| 8 | 55 | 5 | 5 | 0.43 s | 0.423 GB |
| 12 | 83 | 5 | 5 | 1.79 s | 1.410 GB |
| 16 | 111 | 5 | 5 | 5.29–5.42 s | 3.276 GB |

“Cumulative managed allocation” is allocation churn from
`GC.GetTotalAllocatedBytes`, not retained heap. The 16-child measurement was
repeated after warm-up: 5,424 / 5,291 ms and 3,276,213,584 / 3,276,251,656
bytes. The agreement avoids treating JIT work or a stale measurement as the
result.

The maintained allocation tripwire covers only six children and allows 300 MB.
It distinguishes the new path from the former six-gigabyte case, but it does
not bound the curve where an editor actually becomes unresponsive.

There is a separate correctness boundary. `Resolver.MaxLexemes` is 256
(`Compiler/Resolution/Resolver.cs:62,115`). With 20 children:

```text
source lexemes: 139
displayed readings: 5
generated repairs: 1
```

The full-tree candidates contain:

| displayed target | distinct subtree spans | candidate lexemes |
|---:|---:|---:|
| 1 | 58 | 255 |
| 2–5 | 59 | 257 |

The first target happens to fit by one lexeme. The other four fail the resolver's
ceiling before they can be compared, so `Selecting` treats them as unverified
and omits them. The source itself is 117 lexemes below the ceiling.

The second displayed reading has a direct repair well inside the limit: bracket
`a to b` in the first 19 children and bracket `a` in the last. That edited
statement is 179 lexemes, resolves uniquely, and structurally selects the omitted
reading. The failure is caused solely by the verifier's temporary overbracketing,
not by the source or required repair.

**Recommendation:** derive the necessary decision brackets from the target and
its competitors, then verify that completed candidate once. “Bracket everything
and delta-debug it” still pays one whole resolve per node and temporarily creates
an input much larger than the answer. If verification remains iterative, its
allocation/wall budget must be an editor budget rather than a count of 4,000
resolutions, and auxiliary candidates must not be rejected by the user's source
limit. Extend the maintained guard through 12/16 children and add the 20-child
candidate-inflation case asserting a repair for every displayed reading.

---

## 3. Invalid notifications still dereference the id that notifications do not have

**Severity: medium — well-framed JSON notifications can still terminate the
language server with an unhandled `NullReferenceException`, leaving the editor
without its server.**

The safe field readers at `Server/Host.cs:379-414` close the exact numeric-method
and missing-parameter request paths. Error emission still assumes it is serving
a request. `Fail` at lines 424-430 unconditionally executes `id.DeepClone()`.

After a valid initialization, each of these well-framed notifications throws
out of `Host.Serve`:

```json
{"jsonrpc":"2.0","method":"textDocument/hover","params":{}}
{"jsonrpc":"2.0","method":"textDocument/codeAction","params":{}}
{"jsonrpc":"2.0","method":"initialize","params":{}}
```

The first two take the invalid-params branches at `Server/Host.cs:244-253` and
call `Fail` with no id. The last takes the repeated-initialize branch at lines
198-207 and does the same. Direct byte-boundary probes produced
`NullReferenceException` in all three cases.

Recognized request methods sent without ids expose the converse envelope error.
A first `initialize` notification, a valid `hover` notification, and a
`shutdown` notification all receive unsolicited responses with `"id": null`;
the shutdown notification also transitions the server to closing. JSON-RPC
notifications must not receive responses. The maintained shutdown test currently
asserts the nonconforming null-id reply.

The envelope is also not validating `jsonrpc`: requests with `"jsonrpc":"1.0"`
or no `jsonrpc` member are processed successfully. JSON-RPC 2.0 defines invalid
request, invalid params, and method-not-found separately, and defines a
notification by absence of an id:

<https://www.jsonrpc.org/specification>

**Recommendation:** validate one request/notification envelope before lifecycle
or method dispatch. Require `jsonrpc == "2.0"`; distinguish notifications by id
*presence*; never call `Reply` or `Fail` for a notification; and require ids for
LSP request methods such as initialize, hover, code action, and shutdown. Invalid
recognized notifications should be dropped without state changes. Reverse the
shutdown-without-id expectation and add the three crashing notification probes,
wrong/missing protocol versions, and initialize-without-id followed by a request
that proves the host did not enter the initialized state.

---

## Verification record

- `git diff --check ba7cbdb..2e14d0f` — passed.
- `dotnet restore --locked-mode` — passed.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror` —
  passed with zero warnings and zero errors.
- Exact maintained Release coverage command — 1,171 passed; 100% line, branch,
  and method coverage for `Ronin` and `Ronin.Server`.
- `dotnet test --no-restore --configuration Debug` — 1,171 passed.
- `dotnet list Ronin.sln package --vulnerable --include-transitive` — no known
  vulnerable direct or transitive packages in any project.
- Direct production `Compilation` probes — collection ambiguity has two readings
  and zero repairs; both manual edits compile clean and select distinct readings.
- Direct warmed Release `Repairs.For` probes — repeated timing/allocation results
  recorded in finding 2; 20-child full-candidate sizes independently counted
  from the target trees.
- Direct byte-boundary `Host.Serve` probes — exact prior request cases survive
  with the correct codes; malformed recognized notifications still throw;
  recognized valid notifications receive null-id replies; wrong/missing
  `jsonrpc` versions are accepted.
