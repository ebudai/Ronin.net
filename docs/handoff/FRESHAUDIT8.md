# Fresh audit 8 — ambiguity is not yet safely an error

**Audited:** `2e36c10..19269aa`, with a fresh adversarial pass over the current
resolver, declaration rules, diagnostics, registry, and their maintained tests.

**Result:** no sign-off. Two high-severity correctness defects remain in the new
ambiguity direction, followed by four medium implementation/safeguard defects
and three low completeness/maintenance defects. The most serious one silently
returns `Resolved` for two different, bracket-selectable call trees. The other
admits a name the settled pre-type-checker rule is specifically meant to keep
out: its own span is a glued call and no bracket can select the name.

The maintained gates are green: locked restore, a warning-as-error Release
build, all 1,063 tests, 100% line/branch/method coverage, and the current NuGet
vulnerability audit. That is important evidence about the repository, but it is
not evidence against these findings: the new property test asks the resolver
which readings exist, so a reading the resolver silently erases is absent from
the oracle too.

No production or maintained test file was changed during this audit. Temporary
adversarial tests were removed after their failures were reproduced. This file
is the only audit artifact added.

---

## 1. Distinct call trees with the same display string are collapsed into one and silently resolved

**Severity: high — a source-reachable ambiguous statement is accepted with one
meaning, and declaration order can decide which function it calls.**

`Compiler/Resolution/Resolver.cs:629-694` keys costs, derivation counts, and
witnesses by `node.ToString()`. The comment at lines 629-631 makes the key an
identity claim:

> two derivations that read the same way ARE the same reading

That is false for nested word-pattern calls. `Node.Call.Render` inserts its
arguments' renderings but does not delimit the call node itself
(`Compiler/Resolution/Node.cs:140-151`). Consequently two different call trees
can produce the same guillemet string.

All four of these patterns are legal together:

```text
send _
send _ to _
print _
print _ to _
```

With names `a` and `b`, resolve:

```text
print send a to b
```

There are two meanings:

```text
print( send(a, b) )
print-to( send(a), b )
```

Both currently render as:

```text
print send «a» to «b»
```

The dictionary therefore retains one entry with a derivation count of one, and
the resolver returns `Resolved`. Both meanings are real and independently
selectable:

```text
print (send a to b)   -> print ⟨send «a» to «b»⟩
print (send a) to b   -> print ⟨send «a»⟩ to «b»
```

A production-source probe declaring those four functions produced no finding,
so this is not a hand-built symbol-table state forbidden by the declaration
rules.

This predates the removal of minimum lookup as an identity assumption, but the
new semantics make it load-bearing: ambiguity is now meant to retain every
meaning, while the cell identifies meanings by a presentation string that is
not injective.

**Recommendation:** give a derivation a structural identity independent of its
diagnostic rendering. Pattern identity plus the identities of its argument
trees is the minimum for a call. Keep identical declarations of one pattern
collapsed at declaration/overload handling, where that policy already lives;
do not collapse different parse trees in `Cell`. The diagnostic rendering must
also expose the call boundary sufficiently to show why these two trees differ.
Add the exact four-pattern source case in both declaration orders and assert
`Ambiguous`, the two structural alternatives, and both bracket repairs.

This should be fixed before relying on any ambiguity count or repair property:
it is possible for both to agree with the bug.

---

## 2. The surviving self-ambiguity rule misses names that are themselves glued calls

**Severity: high — the compiler admits an unwriteable name, violating the rule
that makes ambiguity-as-error viable before type filtering exists.**

`Rules.Shadowing` narrows the candidate patterns to `Pattern.IsAnchorOnly` at
`Compiler/Diagnostics/Rules.cs:231-251`. That filter belonged to the old split:
a pattern with glue was omitted here because the blanket glue-in-name rule
caught any name capable of spanning it. The blanket glue rule was correctly
deleted, but its smaller self-call subset disappeared with it.

Production reproduction:

```ronin
var x => Number;
var y => Number;
var send x to y => Number;
function send (left => Number) to (right => Number) { return left; }
```

`Compilation.Findings` is empty. Yet, against that scope, the name's own span:

```text
send x to y
```

is ambiguous between `«send x to y»` and `send «x» to «y»`.
No bracket selects the name: a bracket inside the span breaks the name and
selects the call, while a bracket around the whole span preserves the same
ambiguity inside it.

This is not an argument to restore the blanket glue rule. `a to b` can remain a
name: it does not itself begin a `send` call, and the ambiguity it creates
elsewhere is repairable. The missing subset is a name whose own complete token
span conforms to a pattern with glue (pessimistically over possible argument
word runs), not every name containing a glue word.

The claim in `AMBIGUITYASERROR.md` §3 that the pessimistic own-span predicate
is exactly the old `Shadowing(names) + Infixes(names)` is therefore false for a
glued pattern. The newer `DONTDOTHAT.md` reinforces the required current policy:
until type filtering exists, self-ambiguous names ship refused and only shrink
later.

**Recommendation:** implement the stated own-span rule rather than reusing the
old anchor-only approximation. Test at least:

- `a to b` beside `send _ to _` remains legal;
- `send x to y` beside `send _ to _` is refused;
- the latter's name reading is unreachable by exhaustive bracket insertion;
- action and value patterns are tagged separately for the later type-filter
  expiry described in `DONTDOTHAT.md`.

This is also a design-proof correction: the equivalence claim and the generator
that supported it need to include glued self-calls before they can justify the
rule boundary.

---

## 3. An outer alternative hides additional readings carried by an ambiguous child

**Severity: medium — ambiguity is reported, but one valid bracket repair and
one meaning are omitted from the promised diagnostic.**

`Cell.Witness` at `Compiler/Resolution/Resolver.cs:624-627` makes an exclusive
choice:

- if the current cell has more than one node in `order`, return those nodes;
- otherwise propagate a child's witness.

When both facts are true — the current span has an alternative and one of its
nodes contains an ambiguous child — the child's remaining readings vanish.
`TryBest` also carries only `order[0]` as the representative tree
(`Resolver.cs:587-596`), so the parent cannot reconstruct them later.

Concrete legal table:

```text
names:    a, b, c, a to b around c, b around c
patterns: send _, send _ to _, print _, print _ around _
source:   print send a to b around c
```

There are three distinct readings, ranked in this order:

```text
print send «a to b around c»
print send «a» to «b around c»
print send «a» to «b» around «c»
```

The current resolver returns only the first and third. The second is the
additional reading inside the first outer shape and is dropped when the second
outer shape makes `order.Count > 1`.

That directly contradicts the comment at `Resolver.cs:761-767` and the
maintained test named "a tie shows every repair where it is": listing two of
three does hide a repair here.

**Recommendation:** represent alternatives as a bounded packed forest (or an
equivalent structural alternative set) so a parent can merge its own choices
with choices inside each child. Apply the display cap only at the diagnostic
boundary while preserving an exact/saturated total separately. Add this
three-reading composition case; the existing tests cover a local three-way tie
and a buried two-way tie separately, but never their conjunction.

---

## 4. The signed-off ambiguity diagnostic is not implemented or connected to compilation

**Severity: medium — the headline user-facing behavior remains an internal
resolver string, so real source gets neither the error nor a selectable repair.**

`AMBIGUITYASERROR.md` §5 specifies a structured payload:

```text
ambiguity {
  span, shown, total
  readings : [ { rendering, insertions : [ {at, text} ], rank } ]
}
```

The current `Resolution` at `Compiler/Resolution/Resolver.cs:1412-1468` carries
only `Kind`, one cell-wide `Cost`, and a collection of rendering strings. Its
own comment says a repair "will one day" offer those strings. There is no source
span, no insertion offsets/text, no per-reading rank, no display cap, no shown
versus total count, and no ambiguity `Finding` for the renderer/editor to
dispatch.

More fundamentally, `Compilation.Of` only parses and declares
(`Compiler/Compilation.cs:42-51,69-92`). No production file outside resolver
tests constructs a `Resolver`; `Compiler/Program.cs:22-24` still explicitly
lists name resolution among the disconnected phases. An ambiguous expression
in a real source file therefore contributes no compilation finding and cannot
make the CLI fail.

The general pipeline join has been openly deferred in earlier handoffs, so this
is not presented as a newly discovered hidden join. It is nevertheless a
blocking implementation gap against the statement that the ambiguity work is
now complete, and it prevents the central promise — "ambiguity becomes an error,
and the error offers the bracketings selectably" — from being true anywhere a
user interacts with the compiler.

**Recommendation:** implement the structured finding and its minimal insertion
search before calling this direction complete, then join statement resolution
through `Compilation` with source spans. Keep ranking/capping in presentation,
not admission. Add a CLI/full-source test that an ambiguous statement exits
nonzero and exposes two machine-readable fixes, plus cap/total and minimality
tests.

---

## 5. `Cell.Merge` assigns the cell-wide minimum cost to every reading

**Severity: medium — the new ranking policy is lost at every table merge and
insertion order can replace likelihood order.**

The cell now correctly stores a separate minimum in `costs[reading]`
(`Compiler/Resolution/Resolver.cs:682-685`). `Merge`, however, loops each node
and calls:

```csharp
Offer(other.Cost, node, ...)
```

at `Resolver.cs:697-705`. `other.Cost` is the cheapest cost of the entire cell,
not the cost of that reading. After the first merge, every alternative is tied
at the global minimum, and the stable `OrderBy` in `Witness` preserves pattern
insertion order instead of ranking.

Reproduction:

```text
names:    a, b, a to b
patterns: send _ to _, send _      # expensive reading deliberately inserted first
source:   send a to b
```

Expected by the settled ranking rule:

```text
send «a to b»       # 2 lookups
send «a» to «b»   # 3 lookups
```

Actual order is reversed. Existing ranking fixtures insert the cheaper pattern
first, so they pass even when costs have been flattened.

**Recommendation:** merge with `other.costs[reading]`, not `other.Cost`, and add
reverse-declaration-order tests. Exercise alternatives moving through `closed`,
`open`, bracket, operator, and outer-call cells so the invariant is protected at
every merge boundary.

---

## 6. The repair-completeness property uses the resolver and hand-built admission as its own oracle

**Severity: medium safeguard gap — its exact exhaustive count can encode a
resolver bug and cannot protect the declaration-rule half it says it protects.**

`Test/Unit/RepairCompleteness.cs:38-45` populates `SymbolTable` directly. It
never parses declaration source and never calls `Rules.Validate`. The comments
at lines 20-25 say admitting `send a` or `a is b` would make the test fail, but a
change to either declaration rule cannot affect this test: those names are
simply absent from its hand-written table.

At lines 138-146 it asks the production resolver whether a statement is
ambiguous, then checks only the readings the same resolution returned. This has
two consequences demonstrated above:

- the silently collapsed `print send a to b` case is in the generated first
  vocabulary, but is classified `Resolved` and skipped;
- the hidden third reading is never checked because it is absent from
  `resolution.Readings`.

The asserted ambiguity totals therefore pin current behavior, including false
`Resolved` results, rather than independently establishing completeness.
`Test/Integration/Comparisons.cs` contains the same boundary smell: it manually
adds `y is x` to a `SymbolTable` and says declaring it is fine, although the real
compilation path correctly refuses that self-ambiguous name.

**Recommendation:** separate three independent properties:

1. production declarations admit/reject generated names according to an
   independently computed own-span predicate;
2. an independent structural parser/forest counts meanings, so `Resolved` and
   `Readings` are not their own oracle;
3. bracket insertion selects every independently enumerated admitted meaning.

At minimum, route candidate declarations through `Compilation`/`Rules`, include
the forbidden candidates rather than manually omitting them, and add mutation
tests proving that relaxing either keeper and collapsing two structural trees
both fail the suite.

---

## 7. The deleted name rules were not re-shipped as lint

**Severity: low functional completeness — legal but predictably bracket-costly
names have lost the advisory channel the settled design scheduled with the
relaxation.**

`AMBIGUITYASERROR.md` §7 and implementation step 7 require the deleted
glue-name and refining-name predicates to ship as switchable lint. The current
compiler has no lint/severity channel and no corresponding predicates or tests.
The generated registry labels glue costs as advice, but a checked-in registry
is not a per-declaration, switchable compiler lint.

This does not make a program wrong, so it is lower severity than the resolver
and self-ambiguity failures. It does mean the agreed replacement for the
deleted guardrails is missing, and usage cannot be measured against real code
as the design intended.

**Recommendation:** add the advisory channel and restore the two deleted
predicates there, with enable/disable tests and a clear nonzero/zero-exit policy.
Do not implement this as ordinary `Finding` errors; that would silently restore
the language restriction the design removed.

---

## 8. Implementation comments and tests still state the deleted minimum-wins semantics

**Severity: low maintenance risk — the code's local contracts contradict the
algorithm now being relied upon.**

Examples in the implementation itself:

- `Compiler/Resolution/Resolver.cs:11-38` says resolution is "by minimum lookup
  count", "the cheapest scoring wins", and only equal-cost readings are errors;
- `Resolver.cs:556-580` and `Resolver.cs:728` still describe counts and `Best`
  as cheapest-only;
- `Compiler/Diagnostics/Finding.cs:170-185,232-267` and several messages say a
  cheaper name wins with nothing to report, although every derivation is now
  retained;
- `Compiler/Diagnostics/Glue.cs:9-47,124-149` still documents the deleted
  blanket name-reservation rationale;
- `Test/Integration/Comparisons.cs` says a hand-built `y is x` demonstrates that
  "declaring" it is fine, while production declaration rejects it.

This is more than broad prose alignment: several are the contracts immediately
beside the changed state and the rationale a future maintainer will use when
touching it. The old sentence is especially hazardous here because restoring
what it says recreates silent capture.

**Recommendation:** rewrite the local contracts around "retain all structural
readings; cost ranks only", and distinguish an illegal-table resolver probe
from a production-declarable name. Broad `docs/spec` alignment remains the
owner-reserved separate audit and is not expanded into another finding here.

---

## 9. The formatter's documented non-gated exception now includes one import-order violation

**Severity: low gate/documentation drift.**

The workflow deliberately does not gate hand-aligned continuation whitespace;
that is settled and is **not a finding**. Current `dotnet format ...
--verify-no-changes` output also reports an `IMPORTS` error at
`Test/Unit/Admission.cs:1`: `System.IO` appears after `Test` and the alias. The
workflow comment says import ordering is satisfied, so the exception and the
tree no longer match.

**Recommendation:** move `using System.IO;` beside the other `System` imports.
Do not reflow the settled aligned continuations.

---

## Gates and audit evidence

At `19269aaf9f4afda57f9ffa9ed5b0c835eda74529`:

- `dotnet restore --locked-mode`: passed.
- `dotnet build --no-restore --configuration Release -warnaserror`: passed,
  zero warnings and zero errors.
- exact Release coverage gate with `Threshold=100`, line and branch, total:
  passed; 1,063 tests, 0 failed, 100% line, 100% branch, 100% method.
- `dotnet list Ronin.sln package --vulnerable --include-transitive`: no known
  vulnerable packages from the current NuGet source for Compiler, Test, or
  Scratch.
- `git diff --check`: clean.
- no temporary audit test remains.

The first local coverage invocation used a literal comma and MSBuild rejected
the property before tests ran; rerunning with the repository's documented
`line%2Cbranch` spelling produced the passing result above. This is an audit
command correction, not a repository failure.

The worktree was already intentionally dirty with the six `docs/spec` edits and
untracked handoff/design material. `DONTDOTHAT.md` and its probe appeared while
the audit was running and were read without modification. Those files and all
pre-existing edits were preserved.

The owner-authorized documentation-warning suppressions remain deferred. The
hand-aligned formatter differences are settled by workflow and were not counted
as findings. Broad authoritative-document alignment remains the separately
reserved audit; finding 8 is limited to contracts immediately beside the new
implementation and a test that asserts the wrong production premise.

---

## Recommended order

1. Replace display-string identity with structural derivation identity and add
   the four-pattern silent-resolution regression.
2. Correct the self-ambiguity predicate for glued self-calls and revise the
   design proof/generator that claimed equivalence.
3. Preserve nested alternatives through parent choices; decide the packed
   representation together with the cap/total diagnostic payload so correctness
   does not require materialising an unbounded forest.
4. Fix per-reading merge costs.
5. Replace the circular property oracle, then join the structured ambiguity
   finding to real compilation.
6. Add the promised lint channel and clean the implementation-local semantic
   comments/tests.
7. Fix the one non-whitespace formatter discrepancy; leave settled alignment
   alone.
