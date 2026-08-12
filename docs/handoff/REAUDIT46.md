# Re-audit 46 — generated-name ownership and equality safeguards

**Re-audited:** `d43d8b6..2e36c10`

**Date:** 2026-08-05

## Result

**One high-severity prior correctness finding remains open, with one new
medium diagnostic-correctness finding and one new low diagnostic-amplification
finding in the implemented half. No sign-off.**

FRESHAUDIT7 findings 2, 3, and 4 are resolved. Chained heterogeneous equality
is now tested as the current `false` result while the intended type error is
honestly recorded as an unmet type-layer dependency. Resolution kind and the
resolver-to-evaluator join are asserted. Instance identity is exercised through
the real graph allocator, including generation reuse, and its safeguard rejects
an equality implementation that ignores generation. The operator comments and
allocation ceiling now each have one accurate owner.

Finding 1 was only half incorporated. Generated names now participate in the
anchor-pattern shadowing rule, and the maintained source test detects restoring
the old exemption. Generated names remain categorically skipped by the infix
rule, however. The incorporating commit explicitly says “THE OTHER HALF IS NOT
FIXED and needs a ruling,” and the production comment says “Awaiting a ruling.”
The original `index of is valid` witness still compiles with no finding and
silently selects the generated counter.

The implemented pattern half also treats every generated collision as though
only its pattern can be changed. That is true when the pattern is exactly
`index of (_)`, because every counter has that fixed prefix. It is false when a
longer pattern overlaps words copied from the loop variable: renaming the later
loop variable can remove that collision, yet the diagnostic points at the
earlier pattern and insists it be respelled. In the fixed-prefix case, two loops
then produce the same pattern error twice, once per generated counter.

All maintained gates pass: 1,105 tests, the warning-as-error Release build, and
100% line/branch/method coverage. The failures require relationships between
parsed source, generated declarations, and sibling scopes that the current
single-witness tests do not establish.

## Disposition of FRESHAUDIT7

| Prior finding | Re-evaluation |
|---|---|
| 1. Generated names bypass collision rules | **Partially resolved.** The `index of (_)` pattern half is now reported and mutation-protected. The `is` infix half remains explicitly unimplemented and its original silent-capture witness still passes compilation. The implemented half also introduced findings 2 and 3 below. |
| 2. Chained equality claims an absent type error | **Resolved for the current boundary.** The test executes the resolver tree, asserts the observed `false`, and names the intended error as future type-layer work instead of current behaviour. |
| 3. Equality tests splice the pipeline and omit identity | **Resolved.** Resolution kind, resolver-to-evaluator wiring, failure propagation, graph-produced identities, equal-member distinct instances, and slot-generation reuse are covered. A mutation that compared instances by type and slot but ignored generation failed the new test. |
| 4. Stale binding-power and allocation explanations | **Resolved.** The `is` and `otherwise` rationales sit beside their own entries, and one `Ceiling` constant drives both the allocation assertion and message. |

## Findings

### 1. Generated infix names are still exempt and still capture comparisons silently

**Severity: high — valid source still compiles without a finding and resolves
to a different value from the expression its author wrote**

`Rules.Infixes` continues to skip every declaration with an `InjectedBy`
origin (`Compiler/Diagnostics/Rules.cs:574-605`). The expanded comment now
describes the defect accurately, including the exact `index of is valid`
witness, but comments do not change validation. Lines 586-593 explicitly leave
the case awaiting a design ruling.

The original source remains a complete reproduction:

```ronin
var index of => Number;
var valid => Number;
var banks => Number;
for each (is valid) in banks { return index of is valid; }
```

Actual compilation findings: none. The loop body still contains the generated
name `index of is valid`, which wins over the intended
`(«index of» is «valid»)` comparison.

This is not a claim that the programmer overlooked the case: commit `a849a04`
states in its message that this half was not fixed. It is nevertheless still a
production correctness failure, so the incorporation as a whole cannot be
signed off.

**Recommendation:** obtain the pending language ruling and implement it before
sign-off. The two coherent routes visible in the current design correspondence
are:

- retain declaration-time collision rules and validate the complete generated
  declaration against actual operator rivals, attributing a conditional
  collision to its written origin; or
- adopt the broader `SIMPLERRULES` direction and remove the declaration rules
  together with minimum-lookup winner selection, making multiple derivations a
  use-site error.

Either route must add the exact source witness above and prove the nested body
does not silently select the generated counter. Merely documenting the skip or
testing the fixed prefix cannot close it.

### 2. Generated pattern collisions always blame the pattern even when the loop variable is the effective later declaration

**Severity: medium — the compiler refuses unsafe source, but points at an
innocent earlier API and prescribes a larger change than the one that fixes it**

The new `Shadowing` path forces `blamed` to false for every injected declaration
(`Compiler/Diagnostics/Rules.cs:256-269`). `NameShadowsPattern` then always says
the pattern cannot be declared and that the generated name is not the author's
to change (`Compiler/Diagnostics/Finding.cs:266-290`). That is correct for the
maintained witness:

```ronin
function index of (x => Number) { return x; }
for each bank in banks { ... }
```

Every counter begins `index of`, so no loop-variable rename can make that
specific pattern coexist.

It is not true for patterns which extend into the subject copied from source:

```ronin
function index of bank (x => Number) { return x; }
var account => Number;
var banks => Number;
for each (bank account) in banks { return index of bank account; }
```

Actual: one `NameShadowsPattern` whose primary span is the function on line 1
and whose message says to respell `index of bank (_)`.

The pattern predates the loop. Renaming only the later loop variable from
`bank account` to `branch account` makes the complete source compile with zero
findings while leaving the function and its call unchanged. The generated text
is not directly writable, but its subject is; treating those as the same fact
loses the established later-declaration ownership rule and produces the wrong
repair.

**Recommendation:** distinguish the injector's fixed prefix from the copied
subject. If the pattern's entire anchor is within the fixed `index of` prefix,
the pattern is the only repair. If the collision extends into subject words,
order the pattern against the originating declaration and blame whichever was
introduced later; the origin's span and name are already carried by `Declared`.
Add both declaration orders and verify that applying the requested rename or
respelling actually removes the finding.

### 3. One fixed-prefix pattern is reported once per loop

**Severity: low — ordinary repetition produces a wall of diagnostics with one
repair between them**

This source has one invalid relationship and one possible repair:

```ronin
function index of (x => Number) { return x; }
var banks => Number;
var branches => Number;
for each bank in banks { return index of bank; }
for each branch in branches { return index of branch; }
```

Actual: two `NameShadowsPattern` findings, both with the function's line-1 span.
They differ only because the messages interpolate `index of bank`/`bank` and
`index of branch`/`branch`.

Each loop body is validated as its own merged scope. `Compilation.Add` normally
deduplicates an inherited conflict by kind, primary span, and message
(`Compiler/Compilation.cs:548-577`). The generated finding's message embeds the
per-loop subject, so the two instances evade that deduplication even though
both instruct the author to make the same one edit to the same pattern. With N
loops, the pattern yields N diagnostics. This is the same amplification shape
the sound-pattern filter explicitly exists to prevent.

**Recommendation:** for the unconditional fixed-prefix case from finding 2,
identify the conflict by pattern plus injection descriptor rather than by one
generated example, and report it once. Subject-dependent collisions should
remain distinct when their repair is a distinct originating declaration. Add a
two-loop source test that asserts the complete finding set, not `Only(...)` over
one loop.

## Adversarial verification

### Generated pattern safeguard

I temporarily restored the former `InjectedBy` skip in `Rules.Shadowing`. The
new maintained source test failed with an empty finding collection. The
mutation was reverted. The fix therefore protects the exact `index of (_)`
case it claims to protect.

I then tested beyond that single row:

- the original generated-infix witness still produced zero findings;
- the longer `index of bank (_)` witness blamed line 1, while renaming only the
  line-4 loop variable removed every finding; and
- two loops beside one `index of (_)` pattern produced two findings at the same
  primary span.

### Equality and identity safeguards

I temporarily changed instance equality to compare only type and slot, ignoring
generation. `AndTwoInstancesAreTwoInstancesHoweverAlike` failed on the reused
slot assertion (`true` instead of `false`). The mutation was reverted. The test
therefore guards the least visible part of instance identity, not merely two
currently distinct slot numbers.

The new `Value` helper obtains a resolved tree with `TryTree`, populates a real
`Graph`, and invokes `Evaluator`, so the scalar and failure rows genuinely
cross the resolver/evaluator boundary. The separate `Read` helper now asserts
`Resolved` before exposing a rendering.

## Verification performed

- Inspected both incorporating commits, their full production/test diff, and
  the adjacent declaration, diagnostic deduplication, resolver, evaluator,
  equality, and instance-allocation paths.
- Focused `Comparisons`, `NameShadowing`, `ResolverCost`, `GlueRegistry`, and
  `Instances` suite: **100 passed**.
- Full Debug suite: **1,105 passed, 0 failed, 0 skipped**.
- `dotnet restore --locked-mode`: passed.
- `dotnet build --no-restore --configuration Release -warnaserror`: passed with
  zero warnings and zero errors.
- Exact Release coverage gate: **1,105 passed, 0 failed, 0 skipped; 100% line,
  branch, and method coverage**.
- `git diff --check`: passed; `git diff -- Compiler Test` is empty after all
  mutation and probe cleanup.

The pre-existing `docs/spec` edits and untracked handoff/design files were
preserved.

## Settled exclusions

This re-audit does not turn separately disclosed work into duplicate findings:

- the type error intended for heterogeneous chained equality remains an
  explicitly recorded dependency of the future type layer;
- the lookup representation/runtime work needed before the earlier finding 9
  can be completed;
- owner-authorized warning suppressions reserved for their dedicated round;
- broad document alignment, which the owner reserved for a separate audit.

The documented hand-aligned `dotnet format` whitespace differences remain
settled project style and are not a finding.
