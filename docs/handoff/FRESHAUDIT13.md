# Fresh audit 13 — narrowest-first growth can outgrow its answer

**Re-audited:** `2e14d0f..214290d`, the two commits addressing
`FRESHAUDIT12`.

**Result:** no sign-off. The named reproductions moved in the right direction:

- the list-containing ambiguity now has both repairs through `Compilation` and
  both actions through `Language.Actions`;
- the exact twenty-independently-ambiguous-child probe now returns all five
  displayed repairs, each with the required twenty bracket pairs;
- request methods sent as notifications are dropped without replies or crashes,
  initialize/shutdown notifications no longer change lifecycle state, and the
  ordinary dispatch path rejects a missing or wrong `jsonrpc` version.

The grow replacement nevertheless relies on a false invariant. Narrowest-first
growth does not keep a candidate no larger than its answer: it accumulates every
narrower subtree before reaching a wide span, whether those earlier brackets
help or not. A legal 89-lexeme expression with only two readings and a direct
91-lexeme repair grows 85 bracket pairs, reaches 259 lexemes, crosses the
resolver's 256-lexeme ceiling, and publishes zero repairs/actions. Just below
that boundary, an 85-lexeme version takes about 12 seconds and 3.21 GB of
cumulative managed allocation to recover two one-pair answers. This is not the
programmer's acknowledged K-fold-ambiguity residual: there are only two outer
readings, and the large subtree is unambiguous.

The notification crash is closed, but envelope validation is still bypassed by
`exit`: the serve loop terminates on the method text before checking the
protocol version or whether the message is a notification. The dispatch also
still equates an absent id with an explicit null id and accepts Boolean ids.

This re-audit has one high, one medium, and one low finding. All maintained
gates are green: locked restore, warning-as-error Release build, all 1,178 tests
in Debug, all 1,178 tests in the exact Release coverage gate, 100% line/branch/
method coverage for `Ronin` and `Ronin.Server`, and the transitive NuGet
vulnerability audit.

The deliberately open `FRESHAUDIT8` findings 6 and 7 remain outside this
re-audit and are not counted again. The programmer's decision not to maintain
the twenty-child test, and the acknowledged residual latency of that exact
K-fold shape, are also not findings here.

No production, maintained test, or existing documentation file was changed
during this re-audit. This file is the only repository artifact added.

---

## Disposition of `FRESHAUDIT12`

| prior finding | re-audit result |
|---|---|
| 1. a reading containing a collection gets no repair | **Closed.** The candidate walk and `Stripped` now share the same opaque-node boundary. The exact source has two distinct repairs and two applicable editor actions. |
| 2. full-tree repair verification is expensive and crosses the resolver ceiling | **Partial.** The exact twenty-child source now has all five repairs, so the reported correctness case is fixed. Narrowest-first accumulation recreates both the ceiling failure and multi-second/gigabyte cost around a large unambiguous subtree; see finding 1. |
| 3. notification error paths clone a missing id and the envelope is not validated | **Partial.** The reported no-id crashes and unsolicited replies are fixed, and ordinary dispatch validates `jsonrpc`. `exit` bypasses that validation, while id presence/type is still not validated; see finding 2. |

---

## 1. Narrowest-first growth accumulates irrelevant brackets until it crosses the resolver ceiling

**Severity: high — an 89-lexeme production expression reports two readings but
offers zero repairs and zero code actions, although each reading has a verified
91-lexeme, one-pair repair. The 85-lexeme neighboring case takes about twelve
seconds and 3.21 GB of allocation churn.**

`Selecting` at `Compiler/Resolution/Repair.cs:193-228` orders all candidate
subtrees narrowest first and appends each span to one growing set. It does not
ask whether the span just added distinguishes the target; it retains every
earlier span until a later prefix happens to select the target. Trimming starts
only after such a prefix has resolved successfully.

That makes the claims at lines 180-186 and 207-215 false. The intermediate
candidate is not bounded by the final answer. A wide call can be the only useful
bracket while a large, unambiguous argument contributes names and operation
nodes that all sort before it.

Production reproduction, where `E42` means 42 occurrences of `a` joined by
`+`:

```ronin
function send (x => Number) { return x; }
function send (x => Number) to (y => Number) { return x; }
function print (x => Number) { return x; }
function print (x => Number) to (y => Number) { return x; }
var a => Number;
var b => Number;
var result = print send (E42) to b;
```

For example, `E4` expands to `a + a + a + a`; `E42` is the same ordinary
left-associative expression with 42 terms.

The result statement has 89 lexemes, two structural readings, and two direct
repairs:

```ronin
var result = print (send (E42) to b);
var result = print (send (E42)) to b;
```

Each edited statement has 91 lexemes, resolves uniquely, and its stripped tree
matches exactly one of the original alternatives under the same structural
comparison `Repairs` uses. Production `Compilation` nevertheless returns one
`Ambiguous` finding with `Total = 2`, two displayed readings, and
`Repairs.Count = 0`; `Language.Actions` returns zero actions for its range.

The boundary is deterministic:

| terms in the unambiguous argument | source lexemes | readings | repairs | repair time | cumulative managed allocation |
|---:|---:|---:|---:|---:|---:|
| 40 | 85 | 2 | 2 | 12.293 / 12.034 s | 3,206 MB / 3,206 MB |
| 42 | 89 | 2 | 0 | 4.77–4.94 s | 1,199 MB |

The 40-term measurement was repeated in one warmed Release process; allocation
is `GC.GetTotalAllocatedBytes` churn, not retained heap. The 42-term result was
repeated independently. It is faster only because the oversized candidates are
rejected before doing a full resolution; it still spends nearly five seconds
to conclude that neither short answer exists.

For either 42-term target, the walk produces 85 distinct candidates after
excluding the whole statement. The useful nested call is candidate 85 because
all names and increasingly wide operation nodes precede it. By the time that
call is appended, the verification input is:

```text
89 source lexemes + (85 bracket pairs × 2 lexemes) = 259 lexemes
```

`Resolver.Resolve` rejects anything over `Resolver.MaxLexemes = 256`
(`Compiler/Resolution/Resolver.cs:62,115`), so the prefix can never select the
target. The actual answer adds one pair, not 85.

This also explains why the 40,000-lexeme budget does not make the neighboring
case editor-safe. The pass stays within that accounting unit while repeatedly
resolving ever larger prefixes; lexeme count is not the resolver's DP cost, and
the final repair may be tiny even though the path to it is not. This is separate
from doing unavoidable O(K) work for K independently ambiguous children: the
reproduction has one unambiguous arithmetic child and only two outer readings.

**Recommendation:** derive distinguishing spans from the target and its
competitors, or otherwise ensure that adding a wide necessary span does not
require retaining every narrower irrelevant span. Verification candidates must
be bounded by the repair being verified, not by a prefix of a global width
ordering. Maintain this production source (a smaller boundary can be chosen if
the algorithm changes) through both `Compilation` and `Language.Actions`, assert
two distinct applicable repairs, and retain an allocation/latency guard for the
two-reading/one-pair shape as well as the independently ambiguous shape.

---

## 2. `exit` bypasses envelope validation, and id value is still used as id presence

**Severity: medium — a wrong-version notification or a request named `exit`
terminates the server before validation, ignores subsequent valid messages, and
leaves requests unanswered. Other invalid id envelopes are silently dropped or
accepted as successful requests.**

The new validation lives in `Handle` at `Server/Host.cs:182-203`. `Serve`,
however, checks only the method text and breaks first at lines 59-65:

```csharp
if (Method(message) is "exit") break;
```

Consequently, after a valid initialize, each of these causes immediate status 1
termination and prevents a following valid `shutdown` request with id 8 from
being read:

```json
{"jsonrpc":"1.0","method":"exit"}
{"jsonrpc":"2.0","id":7,"method":"exit"}
{"id":7,"method":"exit"}
```

The request cases receive no response for id 7. This contradicts both policies
the patch establishes elsewhere: missing/wrong versions are refused or dropped,
and a request gets an answer. LSP defines `exit` as a notification; method text
alone is not a valid exit envelope.

There is a second envelope mismatch at `Server/Host.cs:184`. Reading
`message["id"]` into a nullable `JsonNode` makes an absent member and an explicit
JSON null indistinguishable. A request

```json
{"jsonrpc":"2.0","id":null,"method":"initialize","params":{}}
```

is dropped as a notification, and a following shutdown is refused because the
server is still uninitialized. Conversely, a Boolean id is accepted: an
initialize request with `"id":true` receives capabilities with `"id":true`.

JSON-RPC 2.0 defines a notification by *absence* of the `id` member and permits
only String, Number, or Null when it is present (Null is discouraged, not made
equivalent to absence): <https://www.jsonrpc.org/specification>. The current LSP
specification identifies `exit` as a lifecycle notification:
<https://microsoft.github.io/language-server-protocol/specifications/lsp/3.18/specification/>.

**Recommendation:** validate one complete envelope before any lifecycle or
method dispatch, producing an explicit request/notification/invalid result.
Track `id` member presence separately from its JSON value, validate its allowed
type, and honor `exit` only for a valid JSON-RPC 2.0 notification. Add
byte-boundary cases for wrong/missing-version exits, an exit carrying an id,
explicit-null and Boolean ids, and a valid message after each invalid one to
prove the loop continued.

---

## 3. The lexeme counter can spend past the limit it promises

**Severity: low — the production overshoot is bounded by one candidate, but the
method's “at most budget lexemes” contract and its maintained test name are not
true.**

`Repairs.For` promises at `Compiler/Resolution/Repair.cs:123-126` to resolve at
most `budget` lexemes. `Selects` checks only whether the amount already spent has
reached the budget, then adds the whole next candidate at lines 248-259:

```csharp
if (spent >= budget) return false;
spent += bracketed.Count;
resolver.Resolve(bracketed);
```

With a budget of 1 and the four-lexeme `send a to b`, the first candidate is
still resolved and `spent` becomes 4. More generally, any positive remainder
admits one whole candidate. The resolver's own input ceiling limits the
production impact, so this is not the source of finding 1, but it means the
newly advertised resource bound is approximate rather than enforced.

The budget test at `Test/Unit/RepairBudget.cs:81-108` observes only that fewer
repairs are returned at 80; it cannot observe how many lexemes were actually
resolved and therefore passes with the overshoot.

**Recommendation:** test the next charge before resolving (using subtraction to
avoid overflow), or rename/document the contract as a soft stop after a
candidate. If the bound remains exact, expose a counting resolver boundary or a
small accounting result so the maintained test measures the claimed unit rather
than inferring it from repair count.

---

## Verification record

- `git diff --check 2e14d0f..214290d` — passed.
- `dotnet restore Ronin.sln --locked-mode` — passed.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror` —
  passed with zero warnings and zero errors.
- Exact maintained Release coverage command — 1,178 passed; 100% line, branch,
  and method coverage for `Ronin` and `Ronin.Server`.
- `dotnet test Ronin.sln --no-restore --configuration Debug` — 1,178 passed.
- `dotnet list Ronin.sln package --vulnerable --include-transitive` — no known
  vulnerable direct or transitive packages in any project.
- Exact collection reproduction — two compiler repairs and two editor actions;
  both edited sources compile cleanly.
- Direct Release twenty-child probe — 139 source lexemes, `Total = 1,000`
  (bounded), five displayed readings, five repairs, and twenty bracket pairs per
  repair. Two independent runs took about 10.99 seconds and allocated 3,239 MB
  cumulatively on this audit host. The result confirms the programmer's
  correctness claim; the already acknowledged cost is not counted as a finding.
- Direct Release grow probes — repeated 40/42-term timings and allocations above;
  candidate ranges/counts inspected from the production trees; both manual
  42-term repairs resolved uniquely and structurally matched distinct targets.
- Production boundary probe — the 42-term file produced one ambiguity with two
  readings and zero repairs; `Language.Actions` produced zero actions.
- Direct byte-boundary `Host.Serve` probes — all invalid `exit` and id-envelope
  outcomes above reproduced without a test-only transport shortcut.
- The pre-existing dirty `docs/spec` edits and untracked handoff material were
  preserved. No temporary audit source remains.
