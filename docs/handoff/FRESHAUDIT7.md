# Fresh audit 7 — equality and generated names

**Audited:** project-wide at `d43d8b6`; newest implementation slice
`d87aa7d..d43d8b6`

**Date:** 2026-08-05

## Result

**One high-severity correctness finding, one medium semantic/dependency
finding, and two low safeguard/maintenance findings. No sign-off.**

The new `is` operator itself is registered through the shared operator table,
has the measured binding power, reaches the evaluator, inherits failures, and
uses the existing structural equality implementation. The reservation boundary
also accepts the three promised edge names and rejects the measured interior
forms.

The serious failure is at the join with compiler-injected names. Diagnostics
categorically omit generated declarations from both operator and pattern
collision rules. A legal loop variable beginning with `is` therefore moves the
operator into the interior of its generated `index of ...` counter and silently
captures an expression the author wrote. A second source witness shows that the
same exemption lets the counter capture an existing `index of (_)` function.
The current registry test proves only that the injector's fixed prefix words are
protected; it does not prove that the complete generated declaration is safe.

There is also a settled-behaviour contradiction at the new operator boundary.
The maintained test and design say chained equality reaches a type error, but
the executable resolver/evaluator path returns `false`. That may properly wait
for the type layer, but it is not current behaviour and must not be presented as
an established invariant in a passing test.

The maintained Release build and 1,098-test 100% line/branch/method coverage
gate pass. The findings are therefore also test-quality findings: each lies
outside what those assertions currently establish.

## Findings

### 1. Generated names bypass collision rules and silently capture source expressions

**Severity: high — valid source can compile without a finding and resolve to a
different value from the expression its author wrote**

`Rules.Shadowing` skips every declaration whose `InjectedBy` is set
(`Compiler/Diagnostics/Rules.cs:241-245`). `Rules.Infixes` makes the same
categorical exception (`Rules.cs:564-572`), with a comment claiming that
injected names cannot be built from an operator word because their prefixes are
only `old` and `index of`.

That claim accounts for the fixed prefix and omits the subject copied from
source. The complete generated counter is `index of` plus the loop variable
(`Compiler/Grammar/Declarations.cs:142-178`). The new R5-prime boundary makes a
leading or trailing `is` legal in the written variable, but prefixing it can
move `is` into a reserved interior position.

This real-source program reports no compilation finding:

```ronin
var index of => Number;
var valid => Number;
var banks => Number;
for each (is valid) in banks { return index of is valid; }
```

I built the loop body's symbols through the production `Declarations.Of` path
and resolved its return expression. Actual:

```text
«index of is valid»
```

That is the injected counter. Removing only that generated declaration from
the same symbol table produces the source author's comparison:

```text
(«index of» is «valid»)
```

The infix diagnostic never examines the generated declaration, so the capture
is silent. This is not merely an equality edge case. An independent real-source
witness exercises R6b:

```ronin
function index of (x => Number) { return x; }
var banks => Number;
for each bank in banks { return index of bank; }
```

It also reports no finding. The actual body resolves `index of bank` as the
injected name; without that injection it resolves as the call `index of
«bank»`. The generated declaration is therefore exempt from the exact
name-versus-pattern capture rule that written declarations must obey.

`EveryInjectedShapeJoinsOnAProtectedWord` does not cover either failure
(`Test/Unit/GlueRegistry.cs:34-63`). It checks each descriptor's fixed words
against the protected-word set and checks that concatenation constructs the
advertised text. Protected fixed glue prevents one class of breakage, but it
cannot establish that the source-provided subject is safe after composition or
that an anchor pattern cannot rival the whole result.

**Recommendation:** validate each complete generated declaration at the point
where its subject is known. Do not simply delete the two `InjectedBy` skips and
emit the existing messages: the author does not own the generated spelling,
and blanket operator rejection would also reject harmless shapes such as an
`old is valid` shadow whose left operand cannot resolve. Model descriptor plus
subject as one first-class declaration shape, check the actual resolver rivals,
and attribute the finding and repair to the originating declaration. The fixed
counter prefix's collision with `index of (_)` may need a language-design
decision because renaming only the loop variable does not remove that anchor.

Add both witnesses through `Compilation.Of` and, crucially, build the nested
body with `Declarations.Of`; a hand-built table or a top-level finding assertion
alone misses the capture. For each, compare resolution with and without the
generated symbol. Extend the injection census to prove safety of the complete
generated shape, not only membership of its prefix words.

### 2. Chained equality returns `false` where the settled contract says type error

**Severity: medium — the executable behaviour contradicts the new test's name,
comment, and the recorded design decision**

The new test calls `a is b is c` the place “where the type error goes” and says
that comparing the intermediate truth with `c` is a type error
(`Test/Integration/Comparisons.cs:62-74`). `OPENDECISIONS.md:93-100` records the
same decision. The assertion, however, checks only the resolver's rendered
parentheses. It never asks any layer to produce the promised error.

I exercised the real resolver tree and `Evaluator` with `a`, `b`, and `c` all
equal to `1`. The left-associative first operation returns `true`; the second
reaches `Builtin.Same(true, 1d)`. `Same` delegates non-list values directly to
`object.Equals` (`Compiler/Runtime/Values.cs:294-305`), so the final result is:

```text
System.Boolean: false
```

not a runtime `Error` or diagnostic. `Evaluator.Apply` performs no compatibility
check before invoking the operator (`Compiler/Runtime/Evaluator.cs:122-142`).

This is likely a dependency on the acknowledged, not-yet-joined type layer,
rather than a request to add an ad hoc check to `Same`. A CLR-type equality
guard would pre-empt language decisions around optionals, `Nothing`, and the
numeric tower.

**Recommendation:** choose and record one honest boundary now:

- if heterogeneous equality is total and returns false, amend the design and
  the test prose and add the executable row;
- if the settled type-error decision stands, mark it explicitly as an unmet
  type-system dependency and move/add a contract test at the layer that will
  enforce it. Do not let a resolver-only assertion claim that the error already
  exists.

Whichever answer is chosen, add an executable `Resolver` → `Tree` → `Evaluator`
test. The current passing test cannot distinguish the promised type error from
the observed `false`.

### 3. Equality tests splice both sides of the operator pipeline and omit the central identity case

**Severity: low — the implementation works in the paths probed, but its main
semantic and wiring claims have no regression guard**

The precedence tests construct a `SymbolTable`, invoke `Resolver`, and compare
only `Resolution.Reading` (`Test/Integration/Comparisons.cs:30-74`). They do not
assert `Resolution.Kind == Resolved`. The semantic test takes
`Builtin.Operators["is"]` directly and invokes `Apply` on hand-built values
(`Comparisons.cs:76-103`). Thus neither half proves that the resolver-selected
operator reaches evaluation; it is the same hand-built-data risk that earlier
audits repeatedly found at parser, declaration, and runtime joins.

I temporarily exercised the complete resolver/evaluator path for `is` and the
three legal edge-name shapes; those probes passed, so this is a missing
safeguard rather than a second observed wiring bug.

The class documentation's central identity promise is also untested: “two boxes
with equal members are two boxes” (`Comparisons.cs:20-24`). The rows cover
numbers, lists, `Nothing`, and failures, but no `Instance`. `Instance` is a
slot-and-generation handle (`Compiler/Runtime/Instances.cs:30`), so a future
change to `Same`, admission, or handle equality could violate the explicitly
advertised reason there is no separate identity operator while this suite stays
green.

**Recommendation:** assert `Kind` in each resolution test and add source-shaped
resolver/evaluator rows for scalar, list, `Nothing`, and failure propagation.
Add instance rows proving that the same handle is equal, distinct instances
with equal member values are unequal, and a reused slot with a new generation
is unequal. Prefer values obtained through `Graph`'s public-internal instance
path over constructing `Instance` records directly, so the test guards the
identity producer as well as its consumer.

### 4. The binding-power edit left two stale explanations in maintained code

**Severity: low — comments and a failure message now state obsolete limits**

The long block immediately above `is` begins “Loosest of them” and explains why
`otherwise` is at six (`Compiler/Runtime/Values.cs:142-160`). `is` is now at
five, so `otherwise` is no longer the loosest operator, and its rationale is
visually attached to the `is` entry rather than to `otherwise` at line 184. The
next section correctly explains equality's five. These should be two comments
beside the two entries they describe.

The allocation guard was raised to 32 MB, but its interpolated failure still
says “past the 26 MB ceiling” (`Test/Unit/ResolverCost.cs:67-86`). A regression
between 26 and 32 would therefore fail while reporting a limit the assertion
does not enforce.

**Recommendation:** split the operator rationale at the dictionary entries and
describe `otherwise` as the fallback level rather than globally loosest. Give
the allocation ceiling one named constant used by both comparison and message,
so the next measured binding-power change cannot update only one copy.

## Adversarial checks

### Equality joins and boundaries

- Verified the production resolver/evaluator join for `is` with ordinary
  scalar operands.
- Verified all three legal declarations — `is valid`, `valid is`, and `is` —
  can be read through the evaluator, not merely admitted by diagnostics.
- Reproduced the chained-equality result with three numeric graph values.
- Ran 20,000 deterministic random token sequences containing `is`,
  `otherwise`, arithmetic/index operators, names, literals, and brackets
  through `Resolver`; no unhandled exception occurred.

### Generated-declaration joins

- Reproduced both finding-1 witnesses from parsed source and the actual nested
  declarations built by `Declarations.Of`.
- Compared each body resolution after removing only the injected rival, which
  establishes the intended alternative reading rather than inferring it from
  spelling.

### Design measurements

The maintained design probes `is_binding_power.py`, `glue_position2.py`,
`word_infix_ops.py`, and `is_article.py` completed successfully.
`article_rule.py` has no current corpus and divides by zero after reporting zero
subjects; it is a historical handoff measurement, not a production gate, so I
did not turn that script failure into a compiler finding.

## Verification performed

- Inspected the complete production/test diff for `d87aa7d..d43d8b6` and the
  adjacent declaration, diagnostic, resolver, evaluator, equality, instance,
  and registry paths.
- Focused `Comparisons`, `Evaluations`, `Indexing`, `Instances`,
  `NameShadowing`, `ResolverCost`, and `GlueRegistry` suite: **151 passed**.
- Full Debug suite: **1,098 passed, 0 failed**.
- `dotnet restore --locked-mode`: passed.
- `dotnet build --no-restore --configuration Release -warnaserror`: passed with
  zero warnings and zero errors.
- Exact Release coverage gate: **1,098 passed, 0 failed, 0 skipped; 100% line,
  branch, and method coverage**.
- `git diff --check`: passed; `git diff -- Compiler Test` is empty after all
  temporary probe cleanup.

The pre-existing `docs/spec` edits and untracked handoff/design files were
preserved.

## Scope and settled exclusions

This is a fresh audit of the current project with additional attention to the
new equality slice. It does not turn separately disclosed future work into
duplicate findings:

- the lookup representation/runtime work needed before the earlier finding 9
  can be completed;
- future multi-word `is` forms and the broader type table, except for the
  concrete current-behaviour contradiction in finding 2;
- owner-authorized warning suppressions reserved for their dedicated round;
- broad document alignment, which the owner reserved for a separate audit.

The documented hand-aligned `dotnet format` whitespace differences remain
settled project style and are not a finding.
