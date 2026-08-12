# Fresh audit 9 — structured ambiguity stops at the repair boundary

**Audited:** `19269aa..3229cf0`, with an adversarial pass over the resolver's
enumeration rewrite, the compilation pipeline join, the new language server,
and the language changes in this range.

**Result:** no sign-off. Three high-severity defects remain in ambiguity
enumeration, repair generation, and work bounding; these are followed by three
medium language-server/diagnostic defects and one low overload-diagnostic
defect. The central regression is that the resolver now preserves distinct
structural alternatives, but `Resolution.Ambiguous` immediately reduces them
back to display strings. That makes two different meanings look identical and
prevents the repair layer from selecting both.

The maintained gates are green: locked restore, all 1,114 tests in Debug, all
1,114 tests in warning-as-error Release, the repository's exact coverage gate,
and the transitive NuGet vulnerability audit. Coverage reports 100% line,
branch, and method coverage for `Ronin` and `Ronin.Server`; however, the server
host is wholly excluded from coverage, which is material to findings 4 and 6.
`git diff --check 19269aa..3229cf0` is also clean.

The two deliberately open items from `FRESHAUDIT8` are not counted again here:
finding 6's property-test oracle is still coupled to the resolver, and finding
7's lint channel is still absent. The enumeration rewrite does address the
old audit's finding 3 at the resolver level; findings 1 and 2 below are the
second-order failures exposed at its new repair boundary. The corrected
allocation measurement from `18e0fde` was also checked as context and is not a
finding in this report.

No production, maintained test, or existing documentation file was changed
during this audit. This file is the only audit artifact added.

---

## 1. Structural alternatives are reduced to non-injective display strings before repairs are generated

**Severity: high — two independently selectable meanings receive the same
advertised repair, so one meaning cannot be selected from the diagnostic.**

The enumeration rewrite successfully retains alternatives by structural
shape. `Resolver.Resolve` then discards that identity at
`Compiler/Resolution/Resolver.cs:168-174`: an ambiguous top cell becomes
`Resolution.Ambiguous` containing only rendered strings.

`Repairs.For` consumes those strings at
`Compiler/Resolution/Repair.cs:82-90`, and `Selecting` is itself identified
only by the target string. This recreates the old rendering-as-identity defect
one layer later.

Production reproduction:

```ronin
function send (x) { return x; }
function send (x) to (y) { return x; }
function print (x) { return x; }
function print (x) to (y) { return x; }
var a => Number;
var b => Number;
var result = print send a to b;
```

The language server reports two readings, but prints both as:

```text
print send «a» to «b»
```

Their structural meanings are different:

```text
print( send-to(a, b) )
print-to( send(a), b )
```

They are independently selectable:

```text
print (send a to b)
print (send a) to b
```

Because both repair searches receive the same target string, deterministic
selection returns the same first matching insertion for both. The other valid
repair is omitted.

The regression test at `Test/Unit/Resolutions.cs:368-402` records that the two
readings cannot be distinguished by rendering, but asserts only ambiguity,
count, and reachability. It does not assert that the repair attached to each
alternative selects that alternative.

**Recommendation:** retain structured alternatives through repair generation.
Compare a repaired result against structural pattern/child identity, and render
only at the presentation boundary. The presentation should also delimit call
boundaries enough for the two meanings to be intelligible. Extend the existing
four-pattern regression to assert the two distinct insertion sets and verify
that applying each one selects its intended tree.

---

## 2. A Cartesian ambiguity can require more than one bracket pair, but every advertised repair searches for exactly one

**Severity: high — the compiler emits selectable-looking repair entries with
no insertions for valid readings of an ambiguous expression.**

`Compiler/Resolution/Repair.cs:54-65` states that one pair of brackets always
suffices. `Selecting` consequently searches only single pairs at lines 102-123.
When none selects the target, it returns an empty insertion collection, and
`Repairs.For` still wraps that result as a `Repair`.

Production reproduction:

```ronin
function send (x) { return x; }
function send (x) to (y) { return x; }
var a to b => Number;
var a => Number;
var b => Number;
var result = (send a to b) + (send a to b);
```

The joined pipeline correctly emits one four-reading diagnostic over the outer
expression, rather than double-reporting the bracketed children. But each outer
reading chooses one meaning for the left child and one for the right. Selecting
a particular reading therefore requires independently disambiguating both
children — two bracket pairs. No single pair can select the target globally,
and all four repair payloads are empty.

`Test/Property/RepairCompleteness.cs` generates flat word sequences of length
two through six and searches one bracket pair. It never composes ambiguous
children through grouping or an operator, so this product case is outside its
domain. This is separate from the deliberately open `FRESHAUDIT8` finding 6:
even a fully independent oracle over the current flat generator would not
exercise a repair requiring two pairs.

**Recommendation:** search minimal insertion sets, not exactly one insertion
pair, preferably from the structured alternative forest rather than repeated
string resolution. A failure to construct a repair must not be published as a
valid empty repair. Add grouped/operator product cases and assert both
minimality and that applying every advertised repair produces its associated
structural reading.

---

## 3. The five-reading cap bounds presentation only after the resolver has enumerated all derivations

**Severity: high — a small legal expression can monopolize the single-threaded
language server for more than ten seconds despite the diagnostic cap.**

`Match` at `Compiler/Resolution/Resolver.cs:614-630` recursively considers
every argument split and every child alternative. `Cell.Offer` at lines
798-840 stores every distinct shape in an unbounded `order` collection.
`Alternatives` applies `Take(5)` only after sorting that complete collection at
lines 765-768.

Repair generation multiplies this work. `Selecting` at
`Compiler/Resolution/Repair.cs:106-113` repeatedly resolves bracket candidates
and resolves the same candidate twice.

Two `textDocument/didOpen` probes used legal adjacent free holes:

- a pattern with seven holes, names `x`, `x x`, and so on through fifteen
  words, and a fifteen-word call completed in roughly 1.7–1.85 seconds;
- a pattern with ten holes, names through twenty words, and a 21-lexeme call
  exceeded a ten-second process timeout without producing a diagnostic.

This is far below the 256-lexeme guard. The number of possible non-empty splits
alone is combinatorial — `C(n-1, h-1)` for `n` words and `h` adjacent holes —
before child alternatives and repeated repair resolution are included. Because
`Host` handles requests synchronously, this stalls the whole editor connection.

**Recommendation:** enforce the bound during construction with a packed forest,
bounded top-K enumeration, or equivalent symbolic representation, while
tracking the total as an exact or saturated count. Cache each bracket
candidate's resolution and remove the duplicate call. Thread cancellation or a
work budget through resolution so document changes can supersede stale work.
Add a maintained adversarial gate for adjacent-hole split growth.

---

## 4. The language server discards the repair payload and offers no code-action route

**Severity: medium — repairs exist inside compilation but cannot be selected by
an editor, so the user-facing promise is still diagnostic prose only.**

`Server/Language.cs:24-31` defines `Reported` with only extent, message, and
code. Its mapping at lines 56-61 converts `Finding` values to that reduced form
and drops `Ambiguous.Repairs`.

`Server/Host.cs:100-108` advertises only text synchronization and hover. The
request switch handles open/change, hover, shutdown, and default error paths;
there is no code-action capability or request handler. Published diagnostics
contain range, severity, code, source, and message, but no recoverable identity
for later repair lookup.

Consequently no external consumer can turn a `Repair` into a workspace edit.
The CLI can print the ambiguity, and the server can publish it, but neither
makes the ranked alternatives selectable. That falls short of the range's
stated user-facing behavior even if findings 1 and 2 are fixed internally.

**Recommendation:** expose ambiguity repairs through `textDocument/codeAction`
and advertise `codeActionProvider`. Preserve or reproducibly recompute repair
payloads against a specific document version, return concrete `WorkspaceEdit`
insertions, and reject stale actions. Cover the complete initialize → publish
diagnostic → request action → apply edit flow at the JSON-RPC boundary.

---

## 5. Nested exit diagnostics use the enclosing statement span and collapse distinct sites

**Severity: medium — multiple invalid `return` or `stop` calls inside one
statement can produce one diagnostic over the entire statement instead of one
precise diagnostic per exit site.**

`Called(Reading)` at `Compiler/Compilation.cs:283-293` traverses the resolved
node tree for `return` and `stop`, but yields `reading.Span` for every match.
`Add` at lines 780-797 then deduplicates findings with the same kind, offset,
length, and message.

Production reproduction:

```ronin
function send (x) to (y) { return; }
var ready => Number;
when ready { send (return 1) to (return 2); }
```

The server returns one `AnsweringReaction` diagnostic spanning character 13
through 42 — the whole `send` expression. It should report two findings at the
two `return` sites. This contradicts the `Exits` contract at
`Compiler/Compilation.cs:257-259`, which says each site is reported separately
so each is a separate edit.

**Recommendation:** retain source extents on resolved call nodes, or maintain a
mapping from the resolved tree back to the lexemes that formed each call. Yield
the call's own span from `Called`, then test two same-kind nested exits, mixed
`return`/`stop`, and nesting below multiple call levels.

---

## 6. `shutdown` followed by `exit` does not terminate the language server

**Severity: medium — a conforming client cannot end the server cleanly, leaving
the process alive and blocked on input.**

`Server/Host.cs:44-50` runs an unconditional read loop. The switch at lines
93-127 replies to `shutdown` but records no shutdown state and does not stop the
loop. An `exit` notification falls through the default path; because it has no
request id, no response is written, and the host simply blocks on the next
read.

A framed JSON-RPC probe sent `shutdown`, then `exit`, while keeping stdin open.
The process was still alive 250 ms later. Under the LSP lifecycle, `exit` must
terminate the process, with a successful exit code when preceded by `shutdown`
and a failure code otherwise.

The coverage report does not protect this path: `Host` is wholly excluded from
code coverage at `Server/Host.cs:39`. Its stream-based boundary is nevertheless
amenable to deterministic protocol tests after the loop/lifecycle state is
factored from process termination.

**Recommendation:** track lifecycle state, recognize the `exit` notification,
break the host loop, and make the executable return the specified status.
Replace the blanket host exclusion with tests for framing, initialize,
shutdown/exit order, exit without shutdown, malformed input, and EOF. See the
[LSP 3.18 lifecycle specification](https://microsoft.github.io/language-server-protocol/specifications/lsp/3.18/specification/#exit).

---

## 7. Exact duplicate signatures still receive the temporary overload diagnostic

**Severity: low — a permanent duplicate declaration is presented as a future
type-directed overload, giving the wrong remedy and expiry behavior.**

`Compiler/Declarations.cs:118-127` reports `Overloaded` whenever at least two
declarations share a shape. The declaration data already records annotations
in `Signature` at lines 291-297, and the signature model contains both names
and types at lines 435-443, but overload classification does not use them.

`Compiler/Diagnostics/Finding.cs:429-439` therefore always says that
type-directed selection is not implemented and advises giving the declarations
different shapes. That is valid for same-shape declarations with different
types under the current phase boundary, but not for two declarations with the
same shape and the same types: no future type information can distinguish
them.

`docs/handoff/OVERLOADS.md:132-158` explicitly requires this split now. The
expiry case at `Test/Expiry.cs:69-87` also records that the same-type duplicate
currently shares a diagnostic and should not.

**Recommendation:** compare normalized signature type/block identities when
classifying same-shape declarations. Emit a permanent duplicate-signature
finding for identical signatures, retain the temporary overload finding only
for differing signatures, and give them separate expiry paths. Parameter names
should not participate in signature identity.

---

## Verification record

- `dotnet restore --locked-mode` — passed.
- `dotnet test --no-restore` — 1,114/1,114 passed.
- `dotnet test --no-restore -c Release -warnaserror` — 1,114/1,114 passed.
- Repository CI coverage command — passed; `Ronin` and `Ronin.Server` reported
  100% line/branch/method coverage, subject to the host exclusion above.
- `dotnet list package --vulnerable --include-transitive` — passed for every
  project with no vulnerable packages reported.
- `git diff --check 19269aa..3229cf0` — clean.
- Resolver, compilation, and framed language-server probes reproduced the
  behavior described above; no probe process was left running.
