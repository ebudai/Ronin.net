# Fresh audit 6 — refinement reservations and duplicate lookup keys

**Audited:** project-wide at `e34e9b2`; newest implementation slice
`3ff981a..e34e9b2`

**Date:** 2026-08-05

## Result

**One high-severity performance finding, four medium correctness/safeguard
findings, and three low performance/diagnostic/documentation findings. No
sign-off.**

The most serious regression is R7b's implementation shape. It recomputes every
pattern-pair relationship once for every name, and the relationship itself is
implemented with allocating LINQ slices. A 50-name/50-pattern scope with no
collisions took about 400 ms and allocated 140 MB in a warmed focused probe. A
200-by-200 probe did not finish in 30 seconds. The relationship should be
derived once per pattern table and indexed by its reserved first word.

That same missing derived representation causes two correctness/safeguard
failures: the relationship includes first-hole refinements that the settled
design assigns to R6, and the supposedly complete generated reservation
registry has no way to list R7b prefix reservations. Extracting the relationship
once, excluding R6 pairs, and sharing the result with both diagnostics and the
registry addresses all three at their structural source.

The new duplicate-key rule also has a real false positive. It concatenates
canonical token text without retaining token boundaries, so distinct keys such
as `a bc` and `ab c` both become `abc` and valid source is rejected. It also
copies every key twice and makes ordinary list parsing retain a token snapshot
that only lookups need.

The maintained Release build and 1,065-test 100% line/branch/method coverage
gate pass. The findings below are therefore also test-quality findings: each
lies outside what those assertions currently establish.

## Findings

### 1. R7b validation is cubic in declarations and allocates on every comparison

**Severity: high — ordinary declaration counts can exhaust compilation time and
memory even when there is nothing to report**

`Rules.Refining` nests every name over every ordered pair of patterns
(`Compiler/Diagnostics/Rules.cs:293-318`). For each triple it calls `Refines`,
which constructs several `Take`, `Skip`, `SequenceEqual`, and `Any` iterator
chains (`Rules.cs:325-344`). The work is therefore
`O(names × patterns² × segments)`, and almost every failed comparison allocates.

A warmed Debug validation probe used 50 unrelated two-word names and 50
unrelated patterns. No pair refined another and no finding was produced:

```text
patterns only:       <1 ms,       37,888 bytes
50 names × 50 pats:  398 ms, 139,903,224 bytes
findings:            0
```

The same probe at 200 names and 200 patterns remained in the validation call
after 30 seconds and was terminated. This is not resolver cost and not parsing
the generated source; it is the new scope-wide diagnostic alone.

The relation depends only on the pattern table. Recomputing it for each name is
derived data stored nowhere, which is also why the registry cannot consume it
(finding 5).

**Recommendation:** derive refinement reservations once per validated pattern
set as records such as `(Word, Shorter, Longer, LaterSpan)`, index them by
`Word`, and scan each eligible name only against the bucket for its first word.
Implement the segment comparison with indices/spans rather than LINQ slices.
This reduces the shape to `O(patterns² × segments + names + actual matches)` and
gives the registry one authoritative representation to enumerate. Add a
scaling/allocation regression; line coverage cannot constrain complexity.

### 2. Duplicate-key identity erases token boundaries and rejects distinct keys

**Severity: medium — valid source is diagnosed as a malformed lookup**

`Collection.Repeated` defines a key as:

```csharp
string.Concat(element.Key.ToArray().Select(token => token.Canonical))
```

(`Compiler/Grammar/Collection.cs:162-179`). Concatenation is not an injective
encoding of a token sequence. This real-source probe has two different keys:

```ronin
var v = [ a bc = 1, ab c = 2 ];
```

Actual result:

```text
Malformed: «abc» is the key of entry 1 and of entry 2
```

Both `[a, bc]` and `[ab, c]` flatten to `abc`. The diagnostic rendering also
hides why the compiler considered them equal. Composite keywords and literal
tokens make ad-hoc separator joining unsafe too: a separator can itself occur
inside a token's canonical text.

**Recommendation:** use structural sequence equality and hashing over canonical
tokens, or an unambiguous length-prefixed representation. Keep a separate
human-readable rendering for the message. Add positive source rows for
`a bc`/`ab c`, composite-keyword boundaries, and keys which differ only in a
canonicalised spelling; keep the existing negative duplicate rows.

### 3. First-hole pattern refinements produce an R7b finding after R6 already refused the pair

**Severity: medium — one structural pattern error grows into unrelated findings
against names in scope**

The implementation comments and maintained test say that insertion at the
first hole is R6's case and that R7b begins at a later hole
(`Compiler/Diagnostics/Rules.cs:275-284`,
`Test/Integration/NameShadowing.cs:318-326`). `Refines`, however, starts at hole
zero and returns that insertion (`Rules.cs:333-341`). The maintained test uses
only the two patterns, so no name is present to expose the second rule.

The full source witness is:

```ronin
var all things => Number;
function sum (x => Number) { return x; }
function sum all (x => Number) { return x; }
```

Actual: two findings, `AnchorPrefix` and `NameAbsorbsRefinement`.

Expected under the documented rule: the one `AnchorPrefix` finding. Adding more
names beginning with `all` adds more R7b findings even though every one has the
same repair: fix the already-invalid pattern pair. This is the same diagnostic
amplification shape the `sound` filter at `Rules.cs:94-103` exists to prevent for
structurally invalid glue patterns.

**Recommendation:** exclude first-hole refinements, or more generally exclude
pattern pairs already refused by R6 before deriving R7b reservations. Add the
name to the existing first-hole source test and assert the complete finding set,
not only `Only(...).Kind` on the patterns alone.

### 4. The R7b diagnostic is wrong about both the cost and the declaration at fault

**Severity: medium — an actionable compiler diagnostic can state a false cause
and ask for the declaration that did not arrive second**

`NameAbsorbsRefinement.Message` unconditionally says that the two readings cost
the same and that the name cannot be declared
(`Compiler/Diagnostics/Finding.cs:208-211`). Both claims are contradicted by
cases the maintained tests already establish:

1. `AndTheReadingItTakesCanBeCheaperRatherThanMerelyEqual` proves that for
   `all count of items`, the absorbed reading is cheaper, not tied
   (`Test/Integration/NameShadowing.cs:285-315`). The test never renders the
   finding, so the false sentence remains green.
2. `AndWhicheverWasWrittenLaterIsTheOneAskedToGiveWay` correctly moves the
   primary span to the later pattern when the name predates the patterns
   (`NameShadowing.cs:261-283`). The message still begins `«all things» cannot be
   declared`. The golden output locks this mismatch in: its caret is on the
   later function pattern while its sentence blames the earlier name
   (`Test/Unit/Findings.cs:248-249`).

The first error obscures why blanket R7b exists: a silent cheaper capture is the
stronger case. The second sends a programmer to change an earlier declaration
despite the project's explicit later-declaration convention.

**Recommendation:** make the sentence neutral about tie versus capture unless
the diagnostic carries and proves the actual relation, and make the requested
repair depend on which declaration owns the primary span. Render both
declaration orders and both equal/cheaper witnesses through
`Diagnostics.Report`; cost-only resolver tests cannot validate prose.

### 5. The generated reservation registry omits R7b name-prefix reservations

**Severity: medium — the breaking-change safeguard can stay green while the
language silently reserves a new class of names**

`Glue.Registry` describes itself and its test as the complete generated list of
what the language reserves. It generates:

- per-pattern glue through `Glue.Reserved` (`Compiler/Diagnostics/Glue.cs:50-55`);
- word operators (`Glue.cs:111-121`); and
- R6 name prefixes only for `IsAnchorOnly` patterns (`Glue.cs:131-148`).

R7b is a relationship between two patterns, implemented privately inside
`Rules`, so the registry never sees it. A focused probe asked for the registry
of:

```text
send (_) to (_)
send (_) to all (_)
```

The compiler refuses names beginning `all …` under R7b, but the registry's
`RESERVES A NAME PREFIX` section contains no `all` entry. `all` appears only as
ordinary glue, whose header explicitly says that an edge is free. The file
therefore tells a reader the opposite of the rule that validation enforces.

`TheRegistryMatchesWhatTheLanguageReserves` compares the checked-in file to the
same incomplete generator (`Test/Unit/GlueRegistry.cs:72-82`), so both can drift
together while the test stays green.

**Recommendation:** have the registry enumerate the shared derived refinement
records proposed in finding 1, with both responsible patterns in the line. Test
the synthetic pair above through `Glue.Registry`, then retain the built-in
golden file as the change detector. This also gives future multi-word operator
refinements one join rather than a third reservation table.

### 6. Duplicate-key capture taxes every list element and copies lookup keys again

**Severity: low — the lookup-only feature adds measurable allocation to ordinary
lists**

`Element.Parse` snapshots `start.AdvanceTo(parser)` before it knows whether an
assignment follows, then stores that `Key` on both lookup and list elements
(`Compiler/Grammar/Collection.cs:119-143`). `AdvanceTo` allocates a token array.
For an actual lookup, `Repeated` immediately calls `Key.ToArray()` again for
every entry (`Collection.cs:166-168`).

A warmed allocation probe parsed a 500-element ordinary list. Moving only the
snapshot below the successful `Assignment` check changed:

```text
current:                 652,154 bytes / parse
lookup-only snapshot:    592,154 bytes / parse
difference:               60,000 bytes (120 bytes per list element, 9.2%)
```

The production edit was reverted after measurement. It changed no parse result;
the list never consults `Key`.

**Recommendation:** retain the parser position after the destination, inspect
`Assignment`, and materialise the key token run only on the lookup path. Use the
stored memory directly in the structural comparer from finding 2 so duplicate
checking does not make a second array per entry.

### 7. `GlueAsName` tells a one-word name that an ambiguity already exists when it does not

**Severity: low — the refusal is settled, but its stated witness is false for
one of the arities it covers**

The blanket all-glue rule is a settled design decision, and this finding does
not challenge it. The problem is the single message shared by one- and
multi-word names. It says a call “has two readings at the same cost”
(`Compiler/Diagnostics/Finding.cs:390-393`).

The designer's measurement and the maintained resolver test establish the
opposite for a one-word name `to`: adding `to` changes zero statements, and the
tested call remains resolved. The ambiguity appears only after the separate
name `to to` is also present (`Test/Integration/NameShadowing.cs:180-186`). The
source comments are internally contradictory too: the type-level remarks say a
single-word name cannot capture anything (`Finding.cs:363-376`), while the
property remarks call it a capture (`Finding.cs:383-389`).

**Recommendation:** keep the blanket rule if desired, but give the one-word case
an honest conservative reason, or make the message describe what the class of
all-glue declarations permits rather than claiming this declaration already
created a tie. Pin the one-word report separately from the two-word actual tie.

### 8. New XML comments repeat the stacked-summary failure and attach documentation to the wrong types

**Severity: low — generated API documentation confidently describes the wrong
member, and the build has no semantic guard for it**

Two new edits repeat an existing documentation failure mode:

- the `NameShadowsPattern` summary and remarks at
  `Compiler/Diagnostics/Finding.cs:174-189` are stacked above the new R7b
  summary, so both attach to `NameAbsorbsRefinement`; `NameShadowsPattern` at
  line 214 receives neither;
- the `Mismatched` summary at `Compiler/Grammar/Collection.cs:200` is stacked
  above `Duplicated`, so generated XML describes `Collection.Duplicated` as both
  “part list and part lookup” and “one key used twice”; `Mismatched` at line 216
  receives no summary.

The generated Release XML confirms both attachments. A wider sweep found the
same shape already present at `Compiler/Diagnostics/Glue.cs:57-85`,
`Compiler/Compilation.cs:297-322`, `Compiler/Resolution/Resolver.cs:302-354`,
and `Compiler/Lexicon/Token.cs:10-42`. XML permits multiple `<summary>` elements,
so malformed-XML warnings do not catch this semantic mistake.

**Recommendation:** perform a mechanical comment-attachment sweep and leave
exactly one summary block immediately above each intended declaration. If the
project wants a gate, inspect the emitted documentation XML for duplicate
`summary` elements per member; this is distinct from the owner-authorized
missing-comment warning work.

## Verification performed

- Read the authoritative `docs/spec` and `docs/guide` surfaces relevant to the
  changed syntax, and used the newly written handoff documents only as design
  context.
- Reviewed the full compiler/runtime/test tree, with a commit-level pass over
  `3ff981a..e34e9b2` and focused attention on diagnostics, pattern relations,
  collection parsing, error totality, and generated safeguards.
- Ran the six current design probes relevant to this slice in `docs/handoff`;
  each completed successfully.
- Ran 20,000 deterministic random ASCII sources through `Compilation.Of`; no
  unhandled exception was found.
- Ran the focused diagnostics/grammar suites: 196 passed before the full gate.
- Reproduced findings 1, 2, 3, 5, and 6 with temporary focused probes. All probe
  code and the one temporary production measurement were removed/reverted.
- `dotnet restore --locked-mode`: passed.
- `dotnet build --no-restore --configuration Release -warnaserror`: passed with
  zero warnings and zero errors.
- Exact Release coverage gate: **1,065 passed, 0 failed, 0 skipped; 100% line,
  branch, and method coverage**.

The working tree's pre-existing `docs/spec` edits and untracked handoff/design
files were preserved. `git diff -- Compiler Test` was empty after probe cleanup.

## Deliberate exclusions and settled policy

- The remaining lookup runtime/equality work and the earlier finding 9 cannot be
  completed until that representation work lands; the owner disclosed this
  before the audit. It remains outstanding work, not a new finding here.
- The future differential declaration check, multi-word operator half of R7,
  and other explicitly deferred pipeline joins were not recast as defects in
  the slice that exists today.
- The owner-authorized warning suppressions remain deferred to their dedicated
  round and were not counted.
- The documented hand-aligned `dotnet format` whitespace differences are settled
  project style and are explicitly **not a finding**.
