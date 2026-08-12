# Fresh audit 16 — equal arguments at different positions are not the same occurrence

**Re-audited:** `5c37b9f..a105f42`, the commit addressing
`FRESHAUDIT15`.

**Result:** no sign-off. Both exact `FRESHAUDIT15` reproductions are fixed and
maintained:

- `f a with b end` receives two distinct one-pair repairs and two editor
  actions, without the shared first argument being bracketed;
- the 253-lexeme production boundary receives both 255-lexeme repairs instead
  of growing either candidate past the resolver ceiling.

The replacement test for a “shared” argument confuses structural value with
source occurrence. It skips a target argument when *any* competitor argument
has the same stripped tree, regardless of where that argument occurs. Repeating
the same name in two holes therefore makes the later, genuinely different
boundary look shared merely because the competitor's earlier hole contains an
equal name.

The production reproduction is only five expression lexemes:
`f a with a end`. It has two displayed readings and two verified one-pair
answers, but compilation publishes one repair and the editor exposes one
action. The missing repair selects `f a with (a) end`; it compiles cleanly.

This is one high-severity finding. All maintained gates are green: locked
restore, warning-as-error Release build, all 1,189 tests in Debug, all 1,189
tests in the exact Release coverage gate, 100% line/branch/method coverage for
`Ronin` and `Ronin.Server`, and the transitive NuGet vulnerability audit.

The deliberately open `FRESHAUDIT8` findings 6 and 7 remain outside this
re-audit and are not counted again. The programmer's accepted K-fold cost
residual and decision not to maintain the twenty-child case are also not
findings here. This reproduction has two visible readings, five lexemes, and a
one-pair answer.

No production, maintained test, or existing documentation file was changed
during this re-audit. This file is the only repository artifact added.

---

## Disposition of `FRESHAUDIT15`

| prior finding | re-audit result |
|---|---|
| 1. a different call pattern brackets a shared argument and can outgrow its answer | **Closed on both exact reproductions; incomplete when equal argument values repeat at different positions.** The maintained distinct-argument case and its resolver-ceiling boundary now receive the expected one-pair repairs/actions. The new all-to-all structural comparison mistakes a different occurrence of the same argument for the shared occurrence and suppresses a repair; see finding 1. |

---

## 1. Structural equality across all arguments suppresses a valid repair when a value repeats

**Severity: high — a legal five-lexeme production expression reports two
readings but offers only one repair and one code action, although both readings
have verified one-pair edits that compile cleanly.**

At calls using different patterns, `Divergence` builds `shared` from all of the
competitor's arguments, then skips a target argument if its stripped tree is
structurally equal to any member (`Compiler/Resolution/Repair.cs:324-337`):

```csharp
var shared = c is Node.Call held ? held.Arguments : [];

foreach (var argument in t is Node.Call diverging ? diverging.Arguments : [t])
{
    var span = Range(argument);

    if (avoid.Contains(span) || span.To - span.From >= lexemes.Count) continue;
    if (shared.Any(other => Node.Same.Equals(Stripped(argument), Stripped(other)))) continue;

    return span;
}
```

`Node.Same` deliberately compares derivation shape rather than source extent.
That is correct for reading identity, but it cannot establish that two equal
subtrees are the same *occurrence* or occupy the same argument boundary.

Use the same overlapping patterns as the fixed audit case, but repeat `a` in
the second hole:

```text
patterns: f _ with _ end
          f _ with _
names:    a
          a end
source:   f a with a end
```

The two readings are:

```text
f «a» with «a» end
f «a» with «a end»
```

For the first target, the target arguments are `[a, a]`; the competitor
arguments are `[a, a end]`. The first target `a` is genuinely shared at the
first source occurrence. The second target `a` is not: its range is the prefix
of the competitor's later `a end` argument, and that is the boundary that must
be bracketed.

Line 335 nevertheless skips both target arguments. Each is structurally equal
to the competitor's *first* `a`, so the all-to-all `Any` comparison treats the
second occurrence as shared too. `Divergence` returns no span and `Selecting`
publishes no repair for that target. The reverse target happens to work because
`a end` is not structurally equal to either competitor argument.

Observed through the direct resolver and repair layer:

```text
kind=Ambiguous, total=2, displayed=2, repairs=1
```

Observed through production `Compilation` and `Language.Actions`:

```text
findings=1, ambiguity total=2, readings=2, repairs=1, actions=1
```

Both intended edits were applied independently:

```ronin
f a with (a) end
f a with (a end)
```

Each resolves uniquely; both corresponding full production files compile with
zero findings. The first is the repair the search drops.

The new maintained case uses distinct values in the two holes: `[a, b]` versus
`[a, b end]`. There, structural equality happens to identify the aligned first
occurrence because no equal value appears elsewhere. Its comment says aligned
arguments are walked across patterns, but the implementation performs an
all-to-all value comparison rather than alignment.

This is independent of every previous resource boundary: all readings are
visible, the budget is ample, the source is five lexemes, and neither candidate
approaches `Resolver.MaxLexemes`. It is a repair-completeness failure caused
solely by discarding occurrence identity.

**Recommendation:** decide sharing by the source occurrence/segmentation a
subtree occupies, not structural equality with an argument anywhere in the
competing call. Structural equality can supplement an aligned extent check but
cannot replace it in a language where the same name or literal commonly appears
more than once. Maintain this exact repeated-`a` source through `Compilation`
and `Language.Actions`, asserting two distinct one-pair repairs/actions and that
both applied files compile cleanly.

---

## Verification record

- `git diff --check 5c37b9f..a105f42` — passed.
- `dotnet restore Ronin.sln --locked-mode` — passed.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror` —
  passed with zero warnings and zero errors.
- Exact maintained Release coverage command — 1,189 passed; 100% line, branch,
  and method coverage for `Ronin` and `Ronin.Server`.
- `dotnet test Ronin.sln --no-restore --configuration Debug` — 1,189 passed.
- `dotnet list Ronin.sln package --vulnerable --include-transitive` — no known
  vulnerable direct or transitive packages in any project.
- Exact `FRESHAUDIT15` maintained cases — two one-pair repairs for the
  five-lexeme distinct-argument source, two editor actions, and two repairs at
  the 253/255-lexeme boundary.
- Direct repeated-argument Release probe — two readings, one repair; both manual
  one-pair sources resolve uniquely.
- Production repeated-argument probe — one ambiguity with two readings but one
  repair and one action; both manually repaired full files compile with zero
  findings.
- The pre-existing dirty `docs/spec` edits and untracked handoff material were
  preserved. No temporary audit source remains.
