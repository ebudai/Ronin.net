# Fresh audit 15 — a call disagreement does not make every argument disagree

**Re-audited:** `695063d..5c37b9f`, the commit addressing
`FRESHAUDIT14`.

**Result:** no sign-off. The exact capped-alternative reproduction from
`FRESHAUDIT14` is repaired correctly now: all five displayed readings receive
distinct four-pair repairs through both compilation and editor actions, and the
new allocation guard is green.

The rewrite nevertheless relies on a false stronger invariant. When two calls
use different patterns, `Divergence` does not locate an argument whose boundary
actually differs between the calls. It returns the first target argument not
already bracketed. Two overlapping patterns can have one or more identical
early arguments and disagree only at a later one, so the search adds brackets
that change no reading. The deleted trim pass was therefore not dead.

The smallest reproduction is five lexemes. Both returned repairs contain two
bracket pairs although one pair selects each reading. At the resolver boundary,
one surplus pair is enough to make the generated candidate 257 lexemes while
the real answer is 255: a legal 253-lexeme production statement reports two
readings but publishes zero repairs and zero editor actions. Both one-pair edits
compile cleanly and were independently verified to select exactly one original
tree.

This is one high-severity finding. All maintained gates are green: locked
restore, warning-as-error Release build, all 1,185 tests in Debug, all 1,185
tests in the exact Release coverage gate, 100% line/branch/method coverage for
`Ronin` and `Ronin.Server`, and the transitive NuGet vulnerability audit.

The deliberately open `FRESHAUDIT8` findings 6 and 7 remain outside this
re-audit and are not counted again. The programmer's accepted K-fold cost
residual and decision not to maintain the twenty-child case are also not
findings here. This reproduction has only two readings and a one-pair answer.

No production, maintained test, or existing documentation file was changed
during this re-audit. This file is the only repository artifact added.

---

## Disposition of `FRESHAUDIT14`

| prior finding | re-audit result |
|---|---|
| 1. hidden alternatives make a necessary span look shared | **Closed on the exact reproduction; the claimed candidate-size invariant is still false.** Re-resolving exposes the capped competitor and the maintained 16-reading cases now receive five four-pair repairs/actions. A different-pattern call can still add shared arguments before the argument that distinguishes it, recreating the ceiling failure by another route; see finding 1. |

---

## 1. A different call pattern causes shared arguments to be bracketed, so the candidate can still outgrow its answer

**Severity: high — a legal 253-lexeme production expression reports two
readings but offers zero repairs and zero code actions, although each reading
has a verified 255-lexeme, one-pair repair. Ordinary five-lexeme calls receive
surplus edits too.**

`Selecting` retains every span returned by `Diverging` and no longer trims the
result (`Compiler/Resolution/Repair.cs:197-221`). That is safe only if every
returned span is necessary.

The recursive cases do find a structural disagreement inside calls of the same
pattern and operations of the same symbol (`Repair.cs:297-307`). At calls with
different patterns, however, lines 315-316 simply enumerate the target call's
arguments and return the first one not already used:

```csharp
foreach (var span in (t is Node.Call diverging ? diverging.Arguments : [t]).Select(Range))
    if (avoid.Contains(span) is false && span.To - span.From < lexemes.Count) return span;
```

A difference somewhere in two call segmentations does not mean every argument
boundary differs. Consider these legal patterns and names:

```text
patterns: f _ with _ end
          f _ with _
names:    a
          b
          b end
source:   f a with b end
```

The two readings have the same first argument, `a`. They differ only in whether
the second argument is `b` before the fixed word `end`, or the name `b end`.
Bracketing the first argument leaves the statement ambiguous:

```text
f (a) with b end       # still two readings
```

Nevertheless, the search chooses `a` first for both targets. It then chooses
the second argument and publishes:

```text
f (a) with (b) end
f (a) with (b end)
```

Both edits work, but the first pair in each is idle. These one-pair edits select
the same respective targets:

```text
f a with (b) end
f a with (b end)
```

This directly contradicts the no-trim claim at `Repair.cs:217-220` that every
added bracket is needed. It also contradicts the proportionality claim at
lines 190-194. The earlier remarks at lines 172-177 still say the result is
trimmed, but no such pass remains.

The surplus bracket becomes a correctness failure at the existing resolver
limit. This production source shape is enough; `TAIL123` is 123 occurrences of
`a` joined by `+`:

```ronin
function f (x => Number) with (y => Number) end { return x; }
function f (x => Number) with (y => Number) { return x; }
var a => Number;
var b => Number;
var b end => Number;
var result = (f a with b end) + TAIL123;
```

The result expression is 253 lexemes and has exactly two readings. For either
target the search proceeds as follows:

1. Resolve the original 253 lexemes and choose the shared first argument `a`.
2. Resolve the still-ambiguous 255-lexeme candidate containing `(a)` and then
   choose the genuinely distinguishing second argument.
3. Resolve the two-pair, 257-lexeme candidate. `Resolver.MaxLexemes` is 256
   (`Compiler/Resolution/Resolver.cs:58-62,111-115`), so this returns `TooLong`.
   It has no alternatives for `Diverging` to inspect, and `Selecting` returns no
   repair.

Through production `Compilation`, the file has one `Ambiguous` finding with
`Total = 2` and `Repairs.Count = 0`. `Language.Actions` returns zero actions.
The two direct edits are only 255 lexemes:

```ronin
var result = (f a with (b) end) + TAIL123;
var result = (f a with (b end)) + TAIL123;
```

Both full files compile with zero findings. Direct resolution also confirmed
that each stripped result tree matches exactly one of the two original target
trees. The repair search itself took about 1.15 seconds in repeated Release
probes; this failure is the resolver ceiling, not the 40,000-lexeme work budget.

This is not the acknowledged unavoidable O(K) work for K independently
ambiguous children. There is one binary ambiguity, the answer has one bracket
pair, and the long arithmetic sibling is unambiguous. Nor does it involve the
display cap: both readings are visible. Re-resolving after every bracket finds
hidden competitors correctly, but it does not prove that the proposed bracket
eliminates the competitor currently in view.

**Recommendation:** distinguish the argument boundary that actually differs,
or otherwise verify that a proposed span removes a surviving competitor before
retaining it. If that property cannot be made structural, the trim step remains
correctness-relevant rather than dead. Maintain the five-lexeme overlapping
pattern case with an assertion that each repair contains one pair, and maintain
the production boundary through both `Compilation` and `Language.Actions` so a
surplus pair cannot silently turn valid answers into `TooLong` candidates.

---

## Verification record

- `git diff --check 695063d..5c37b9f` — passed.
- `dotnet restore Ronin.sln --locked-mode` — passed.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror` —
  passed with zero warnings and zero errors.
- Exact maintained Release coverage command — 1,185 passed; 100% line, branch,
  and method coverage for `Ronin` and `Ronin.Server`.
- `dotnet test Ronin.sln --no-restore --configuration Debug` — 1,185 passed.
- `dotnet list Ronin.sln package --vulnerable --include-transitive` — no known
  vulnerable direct or transitive packages in any project.
- Exact `FRESHAUDIT14` capped-alternative cases — five distinct four-pair
  compiler repairs, five editor actions, and the allocation guard all passed as
  maintained tests.
- Direct five-lexeme Release probe — two readings and two repairs; both repairs
  contain two pairs, while removing the shared `(a)` pair leaves a unique result
  selecting the same target.
- Direct 253-lexeme Release probe — two readings, zero repairs; both 255-lexeme
  one-pair alternatives resolve uniquely and structurally match one target.
- Production boundary probe — one ambiguity with two readings and no repairs;
  zero actions; both manually repaired full files compile with zero findings.
- The pre-existing dirty `docs/spec` edits and untracked handoff material were
  preserved. No temporary audit source remains.
