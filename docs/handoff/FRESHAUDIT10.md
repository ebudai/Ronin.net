# Fresh audit 10 — the remediation is real and still stops one dimension early

**Re-audited:** `3229cf0..5724372`, the seven commits addressing
`FRESHAUDIT9`.

**Result:** no sign-off. The named reproductions are substantially fixed: the
two non-injective trees now receive different edits, a two-child product gets
two bracket pairs, adjacent-hole matching is bounded, nested exits have their
own spans, `shutdown` followed by `exit` terminates, simple duplicate signatures
are separated from simple overloads, and an editor can request code actions.

The remaining result is one high-severity repair-completeness defect, three
medium editor/protocol defects, and one low overload-classification defect. The
high defect is the same dimensional boundary that escaped the first repair
property: the new search generalises from one bracket pair to exactly two, so
three independently ambiguous children still have readings and no selectable
repairs. The repository already contains that exact three-child source as a
count/cap test, but it never asks whether any of its readings can be selected.

All maintained gates are green: locked restore, a warning-as-error Release
build, all 1,147 tests in Debug, all 1,147 tests in the exact Release coverage
gate, 100% line/branch/method coverage for `Ronin` and `Ronin.Server`, and the
transitive NuGet vulnerability audit. The server host is now measured rather
than blanket-excluded; the protocol defects below are relationship and input-
domain gaps that line and branch execution do not reveal.

The deliberately open `FRESHAUDIT8` findings 6 and 7 remain outside this
re-audit and are not counted again.

No production, maintained test, or existing documentation file was changed
during this re-audit. This file is the only audit artifact added.

---

## Disposition of `FRESHAUDIT9`

| prior finding | re-audit result |
|---|---|
| 1. structure lost before repair | **Partial.** Repair selection is structural and the two trees receive different edits. Their editor-visible titles are still identical; see finding 2. |
| 2. Cartesian ambiguity needs multiple pairs | **Partial.** Exactly two pairs work. Three independent children receive no repair; see finding 1. |
| 3. the cap did not bound resolver work | **Closed on the reported path.** Matching is memoised and retains a bounded cheapest frontier; the maintained 25-lexeme allocation guard exercises the former exponential case. |
| 4. repairs stop before the editor | **Partial.** Code actions and workspace edits are wired end to end. Completeness and presentation still fail in findings 1 and 2. |
| 5. nested exits share the statement span | **Closed.** Call nodes carry extents and the two-site production case reports two precise findings. |
| 6. `exit` does not terminate the server | **Closed narrowly.** `shutdown` then `exit` now returns status 0 and stops before a following message. The surrounding lifecycle/synchronisation state and framing remain incomplete; see findings 3 and 4. |
| 7. duplicates share the overload diagnostic | **Partial.** A simple same-types pair is split correctly. Block structure and mixed signature sets are classified incorrectly; see finding 5. |

---

## 1. Repair search supports exactly two bracket pairs, not an arbitrary minimal set

**Severity: high — a small source-reachable ambiguity has eight readings and
zero editor actions, so the central selectable-repair promise still fails.**

`Repairs.Search.Selecting` at `Compiler/Resolution/Repair.cs:149-175` tries
every single tree span and then calls `Pairs(spans)`. `Pairs` at lines 177-184
can produce only two non-overlapping spans. If neither level selects the target,
the reading is omitted from `Repairs`.

That fixes the two-child reproduction from `FRESHAUDIT9`, but it is the same
fixed-arity assumption moved up by one. Use three independently ambiguous
children:

```ronin
function send (x => Number) { return x; }
function send (x => Number) to (y => Number) { return x; }
var a to b => Number;
var a => Number;
var b => Number;
var result = (send a to b) + (send a to b) + (send a to b);
```

The production server publishes one ambiguity with `Total = 8` and five shown
readings, then answers `textDocument/codeAction` over that diagnostic with:

```json
{"result":[]}
```

Every complete reading fixes a meaning for all three children, so every repair
needs three bracket pairs. A single or pair leaves at least one child ambiguous.
This is not the 4,000-candidate budget firing; the search exhausts its entire
two-level candidate grammar on a short expression and has no representation for
the valid answer.

The same source is already maintained at
`Test/Integration/Ambiguities.cs:78-92`. The test asserts eight total readings,
the five-reading cap, and the wording, but never inspects `Repairs` or
`Language.Actions`. The two-child test at lines 401-428 therefore proves only
the newly hard-coded dimension.

**Recommendation:** search bracket sets of increasing cardinality until the
target is selected or the work budget is exhausted, preferably by deriving the
necessary child decisions from the structured forest instead of re-resolving
the Cartesian power set. Keep the budget, but distinguish “budget exhausted”
from “the algorithm cannot express this repair.” Extend the existing
three-child test through `Compilation` and `Language.Actions`, and apply every
offered edit to prove it compiles to its associated structural reading.

---

## 2. Structurally different code actions still have identical titles

**Severity: medium — the editor exposes two working choices under the same
label, so a person cannot tell which meaning either action selects.**

The structural repair fix is correct: for the four-pattern case, the server now
returns two different edit ranges. The presentation remains non-injective.
`Repair.Reading` is still `Node.ToString()`, and `Language.Actions` constructs
the title directly from it at `Server/Language.cs:102-110`.

Production reproduction:

```ronin
function send (x => Number) { return x; }
function send (x => Number) to (y => Number) { return x; }
function print (x => Number) { return x; }
function print (x => Number) to (y => Number) { return x; }
var a => Number;
var b => Number;
var result = print send a to b;
```

The two returned actions have different edits but identical titles:

```text
Read it as print send «a» to «b»
Read it as print send «a» to «b»
```

One inserts `print (send a to b)` and the other `print (send a) to b`. The
integration regression at `Test/Integration/Ambiguities.cs:370-399` explicitly
asserts that the readings print alike and proves only that the edited files are
different and compile. The editor test covers a simpler case whose two
renderings already differ.

This leaves the commit's own title contract false: the title is meant to name
the meaning because that is what a person can judge, but these two titles do not
identify either meaning.

**Recommendation:** make the display of a structural alternative injective at
call boundaries, or title the action with a bracketed source preview such as
`print (send a to b)` versus `print (send a) to b`. Keep structural identity out
of presentation, but require the presentation offered for selection to
distinguish every pair in that menu. Add an assertion that action titles are
distinct for this exact regression.

---

## 3. The server terminates on `exit`, but its lifecycle and document state are still incomplete

**Severity: medium — ordinary protocol messages can be processed in forbidden
states, and a closed document remains live for hover and code actions.**

`Host` now has one `closing` boolean, but no initialized/running state.
`Server/Host.cs:132-170` continues dispatching every request after `shutdown`.
A framed probe sent `shutdown`, then a second `initialize`, then `exit`; the
server returned a successful capability result for the second request and
exited 0. LSP requires requests received after shutdown to fail with
`InvalidRequest`, and permits `initialize` only once.

There is also no `textDocument/didClose` case. The `open` dictionary at
`Server/Host.cs:288-300` retains the text indefinitely. A probe opened the
ambiguous six-line source, sent `didClose`, and requested a code action for the
closed URI; the server still returned both actions.

This is not an optional corner of the advertised synchronisation capability.
The [LSP 3.18 specification](https://microsoft.github.io/language-server-protocol/specifications/lsp/3.18/specification/#textDocument_synchronization)
requires a server to implement open, change, and close together. Its
[shutdown contract](https://microsoft.github.io/language-server-protocol/specifications/lsp/3.18/specification/#shutdown)
requires post-shutdown requests to receive `InvalidRequest`.

**Recommendation:** model explicit pre-initialize, running, shutdown, and exited
states; allow `initialize` once; drop pre-initialize notifications and return
the specified errors for invalid requests; after shutdown accept only `exit`.
Handle `didClose` by removing document state and, if appropriate, clearing
published diagnostics. Extend the byte-boundary tests with pre-initialize,
double-initialize, post-shutdown request, and open/close/query sequences.

---

## 4. Malformed `Content-Length` still crashes the server process

**Severity: medium — malformed framing bypasses the new graceful protocol
boundary and ends the process with an unhandled exception.**

`Host.Read` calls `int.Parse` at `Server/Host.cs:80-81` and allocates
`new byte[length]` at line 84 without validating the header value or bounding a
frame. The new malformed-body test starts after those operations and therefore
does not cover malformed framing.

Three direct process probes produced:

| header | result |
|---|---|
| `Content-Length: nope` | unhandled `FormatException`, status `-6` |
| `Content-Length: -1` | unhandled `OverflowException`, status `-6` |
| an integer larger than `Int32` | unhandled `OverflowException`, status `-6` |

A large in-range length additionally controls a single allocation and has no
frame-size ceiling. The language's 256-lexeme bound does not protect the bytes
allocated before JSON or source text exists.

**Recommendation:** parse with `TryParse`, require a non-negative length, and
set a documented maximum frame size before allocating. Return a structured
read outcome so EOF, malformed header, malformed JSON, and a valid message do
not share `null`; terminate malformed streams deliberately with status 1 rather
than via an unhandled exception. Cover nonnumeric, negative, overflowing,
missing, duplicate, and oversized lengths at the byte boundary.

---

## 5. Duplicate-signature comparison flattens parameter blocks and misidentifies mixed sets

**Severity: low — declarations are refused either way today, but the diagnostic,
remedy, related span, and expiry classification can all be wrong.**

`Typed` at `Compiler/Grammar/Declarations.cs:165-167` length-prefixes each type
but concatenates every block without encoding block boundaries. Its comment
claims this distinguishes one block containing `a, b` from two blocks
containing `a` and `b`; the implementation produces the same key for both.

Legal production reproduction:

```ronin
function arrange (a => Number, b => Text) with (c => Number) { return a; }
function arrange (a => Number) with (b => Text, c => Number) { return a; }
```

Both declarations have shape `arrange (_) with (_)`, but their per-hole
signatures are:

```text
[(Number, Text), (Number)]
[(Number), (Text, Number)]
```

Future type/arity selection can distinguish those shapes at the two arguments.
The current compiler reports permanent `DuplicateSignature` because both
flatten to `Number, Text, Number`; it should report the temporary `Overloaded`
finding.

The set-level branch at `Compiler/Grammar/Declarations.cs:129-144` has a second
problem: if any duplicate exists among three or more declarations, it emits one
`DuplicateSignature` between `spans[0]` and `spans[^1]` and suppresses the
distinct overload set. In declaration order `Number, Number, Text`, those two
spans are not even the duplicate pair.

**Recommendation:** compare a structural key that preserves blocks, parameter
positions, and explicit omitted types while excluding parameter names. Group
the actual declarations by that key; report duplicate groups against their real
members, then independently classify multiple distinct groups as the temporary
overload refusal. Add redistributed-block and mixed `A, A, B` orders.

---

## Verification record

- `dotnet restore --locked-mode` — passed.
- `dotnet build --no-restore --configuration Release -warnaserror` — passed
  with zero warnings and zero errors.
- `dotnet test --no-restore` — 1,147/1,147 passed in Debug.
- Exact Release CI coverage command — 1,147/1,147 passed; `Ronin` and
  `Ronin.Server` each reported 100% line, branch, and method coverage.
- `dotnet list Ronin.sln package --vulnerable --include-transitive` — no
  vulnerable package reported for any project.
- `git diff --check 3229cf0..5724372` — clean.
- Framed server probes reproduced the three-child zero-action result, duplicate
  action titles, post-shutdown request acceptance, stale state after
  `didClose`, and the three malformed-length crashes. All probe processes
  terminated.
