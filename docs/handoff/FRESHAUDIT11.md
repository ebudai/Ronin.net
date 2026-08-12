# Fresh audit 11 — the named fixes land, but the editor boundary is not ready

**Re-audited:** `5724372..ba7cbdb`, the four commits addressing
`FRESHAUDIT10`.

**Result:** no sign-off. All five named findings were addressed on their exact
reproductions. Three-child repair sets are generated and verified against their
structural readings; bracketed action titles distinguish the formerly identical
renderings; lifecycle and close-document state exist; unusable lengths terminate
deliberately; and overload grouping now preserves parameter blocks and reports
mixed duplicate/overload sets independently.

The re-examination found two high-severity production-path failures and one
medium protocol-boundary failure:

1. the generalized repair search can spend 6–9 seconds and allocate 6–11 GB
   cumulatively on 41–55 lexemes, then omit most or all repairs;
2. the server advertises incremental synchronization but replaces the whole
   document with the first incremental fragment, so an ordinary edit immediately
   desynchronizes hover, diagnostics, and actions;
3. well-framed invalid JSON-RPC messages can still throw out of the host, while
   unknown or structurally invalid requests are answered as successful `null`
   results instead of protocol errors.

All maintained gates are green: locked restore, warning-as-error Release build,
all 1,161 tests in Debug, all 1,161 tests in the exact Release coverage gate,
100% line/branch/method coverage for `Ronin` and `Ronin.Server`, and the
transitive NuGet vulnerability audit. These findings are missing input domains
and relationships, not uncovered branches.

The deliberately open `FRESHAUDIT8` findings 6 and 7 remain outside this
re-audit and are not counted again.

No production, maintained test, or existing documentation file was changed
during this re-audit. This file is the only repository artifact added.

---

## Disposition of `FRESHAUDIT10`

| prior finding | re-audit result |
|---|---|
| 1. repair search stops at two pairs | **Closed on the reported three-child case, incomplete as a bounded algorithm.** Arbitrary set sizes are expressible. The 4,000-resolution budget takes seconds and is exhausted before reaching the needed set on another short source; see finding 1. |
| 2. code-action titles collide | **Closed.** Titles preview the actual bracket insertions, and the two formerly identical readings now produce distinct, applicable labels. |
| 3. lifecycle/document state is incomplete | **Closed on the named state transitions.** Pre-initialize and post-shutdown requests are refused, initialization is single-use, and `didClose` removes text. The synchronization mode itself is false; see finding 2. |
| 4. malformed `Content-Length` crashes | **Closed on the named framing values.** Nonnumeric, negative, overflowing, and oversized lengths now terminate with status 1 rather than throwing. The parsed message layer still throws on invalid request shapes; see finding 3. |
| 5. signature comparison flattens blocks/mixed groups | **Closed.** The key is structurally decodable by block arity and type length, duplicate groups name their actual sites, and distinct groups independently produce the overload finding. |

---

## 1. The repair budget permits multi-second, multi-gigabyte searches and still omits short-source repairs

**Severity: high — requesting actions for a 55-lexeme expression blocks the
single-threaded server for about nine seconds, cumulatively allocates over 10 GB,
and returns no actions for five displayed readings.**

`Repairs.Search.Selecting` at `Compiler/Resolution/Repair.cs:180-185` enumerates
sets by increasing cardinality. Each candidate reaches `Selects` at lines
234-245, which rebuilds the bracketed lexeme list and fully resolves it. The
only bound is `Budget = 4000` resolutions at line 97.

That count is not a usable editor-work budget. A full resolve gets more
expensive with the statement, and the search must spend every lower-cardinality
candidate before it reaches the set that pins every independent child. The
`OrderBy(Total)` at line 182 additionally materializes a cardinality's disjoint
sets before the first one is tried.

Production-reachable reproduction:

```ronin
function send (x => Number) { return x; }
function send (x => Number) to (y => Number) { return x; }
var a to b => Number;
var a => Number;
var b => Number;
var result = (send a to b) + (send a to b) + (send a to b) + (send a to b)
           + (send a to b) + (send a to b) + (send a to b) + (send a to b);
```

The expression is 55 lexemes, far below the 256-lexeme ceiling. Production
`Compilation` reports one ambiguity with `Total = 256`, five displayed readings,
and zero repairs. `Language.Actions` therefore has nothing to expose for any of
the meanings it shows.

A Release probe separated the initial resolution from `Repairs.For` and repeated
the allocation measurement after warm-up:

| ambiguous children | expression lexemes | displayed | repairs | repair time | cumulative managed allocation |
|---:|---:|---:|---:|---:|---:|
| 6 | 41 | 5 | 5 | 4.35–4.68 s | 6.248 GB |
| 7 | 48 | 5 | 1 | 6.79–6.94 s | 8.898 GB |
| 8 | 55 | 5 | 0 | 9.02–9.15 s | 10.867 GB |

The unbracketed resolver took 1–2 ms in the same probe; the repair verification
accounts for the delay. “Cumulative managed allocation” is allocation churn
reported by `GC.GetTotalAllocatedBytes`, not retained heap size. Two runs at
each size agreed within 0.01%, avoiding the stale/single-sample measurement
problem called out in the handoff.

The new low-budget unit test proves only that the counter is checked inside a
search. The three-child regression proves only the smallest newly expressible
cardinality. Neither exercises the production budget at a size where the safety
mechanism is needed.

**Recommendation:** do not discover a Cartesian product's repair by repeatedly
resolving its power set. Derive the child decisions from the structured
alternative/target tree and verify the completed candidate once, or carry a
bounded frontier whose cost is proportional to the selected reading. Put an
editor-facing wall/allocation guard around any remaining verification work and
add maintained 6–8-child latency/allocation/action-count cases. Lowering 4,000
alone trades the pause for missing repairs at an even smaller expression; it
does not close the algorithmic defect.

---

## 2. `textDocumentSync = 2` is advertised, but incremental changes replace the whole document

**Severity: high — the first normal edit from a conforming incremental client
corrupts server document state, so every subsequent diagnostic, hover, and code
action is computed against a fragment rather than the file.**

`Capabilities` at `Server/Host.cs:231-240` returns:

```json
"textDocumentSync": 2
```

In LSP, `2` is `TextDocumentSyncKind.Incremental`: open carries the full content
and later changes carry ranged fragments. The official specification defines
that mapping directly and says language features are computed from synchronized
document state:

<https://github.com/microsoft/language-server-protocol/blob/gh-pages/_specifications/lsp/3.18/specification.md>

`Publish` at `Server/Host.cs:242-250` instead does this for every change:

```csharp
open[uri] = changes[0]["text"].GetValue<string>();
```

It ignores the event's `range`, discards the old text, and ignores every change
after the first.

Byte-boundary reproduction:

1. initialize and open this text:

   ```ronin
   var a => Number;
   var b => Number;
   var result = a;
   ```

2. send a conforming `didChange` replacing line 2, characters 13–14 with `b`:

   ```json
   {
     "textDocument": { "uri": "file:///p.ron", "version": 2 },
     "contentChanges": [{
       "range": {
         "start": { "line": 2, "character": 13 },
         "end": { "line": 2, "character": 14 }
       },
       "text": "b"
     }]
   }
   ```

3. request hover at line 2, character 13.

Applying the edit correctly produces `var result = b;`, for which
`Language.Hover` returns `«b»`. The server returns `result: null`, because its
entire stored document is now the one-character string `b`.

The maintained change test sends a full document as the first event while the
server advertises incremental mode, so it exercises the implementation's private
assumption rather than the negotiated protocol.

**Recommendation:** either advertise `TextDocumentSyncKind.Full` (`1`) and
require one whole-document event, or implement incremental synchronization by
applying every `contentChanges` entry in order to the previous version, using
the negotiated/default position encoding. Add a byte-boundary ranged replacement
followed by hover, and a multi-event notification whose later range is relative
to the text produced by the earlier event.

---

## 3. Valid JSON with an invalid request shape still escapes the host or receives false success

**Severity: medium — a malformed client message can still terminate the language
server with an unhandled exception, and unsupported requests are reported as
successful even though the protocol defines errors for both cases.**

The framing fix prevents hostile `Content-Length` values from reaching an
allocation. Once a body parses as a `JsonObject`, however, there is no request
validation boundary.

`Serve` at `Server/Host.cs:61-65` reads `method` as a string before dispatch:

```json
{"jsonrpc":"2.0","id":1,"method":7}
```

This throws `InvalidOperationException` from `GetValue<string>()` out of
`Host.Serve`. After initialization, this notification:

```json
{"jsonrpc":"2.0","method":"textDocument/didOpen"}
```

reaches `Publish` and throws `NullReferenceException` while indexing absent
`params`. Both bodies are correctly framed and parse as JSON, so neither is
covered by the new malformed-length cases.

The opposite failure occurs for structurally invalid or unsupported requests.
The default at `Server/Host.cs:225-227` returns a successful null result, so both
an unknown method and an object with an id but no method receive:

```json
{"jsonrpc":"2.0","id":7,"result":null}
```

JSON-RPC 2.0 defines `-32600` for an invalid request, `-32601` for a method that
does not exist, and `-32602` for invalid parameters. Its examples include a
numeric `method` as an invalid request:

<https://www.jsonrpc.org/specification>

The maintained tests currently enshrine null success for unknown and missing
methods, and cover malformed parameter shapes only while the lifecycle drops
them before dispatch.

**Recommendation:** parse a framed body into a validated request/notification
envelope before lifecycle or method dispatch. Validate `jsonrpc`, `method`, `id`,
and each supported method's parameters without exception-based indexing; return
the standard error for requests and drop invalid notifications. Unknown methods
must return `MethodNotFound`, not a successful result. Keep the byte-boundary
tests and add numeric/missing methods plus missing/wrongly typed parameters after
initialization.

---

## Verification record

- `dotnet restore --locked-mode` — passed.
- `dotnet build Ronin.sln --no-restore -c Release -warnaserror` — passed with
  zero warnings and zero errors.
- Exact maintained Release coverage command — 1,161 passed; 100% line, branch,
  and method coverage for `Ronin` and `Ronin.Server`.
- `dotnet test --no-restore --configuration Debug` — 1,161 passed.
- `dotnet list Ronin.sln package --vulnerable --include-transitive` — no known
  vulnerable direct or transitive packages in any project.
- `git diff --check 5724372..ba7cbdb` — passed.
- Direct production `Compilation` probe for the eight-child source — one
  ambiguity, 256 total readings, five displayed, zero repairs.
- Direct Release `Repairs.For` probes — repeated time/allocation/action results
  recorded in finding 1.
- Direct byte-boundary `Host.Serve` probes — incremental edit loses synchronized
  state; numeric method and missing params throw; unknown and missing methods
  return successful null.
