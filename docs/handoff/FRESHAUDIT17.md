# Fresh audit 17 — an already-bracketed argument must still be searched inside

**Re-audited:** `a105f42..1ab4ce9`, the commit addressing
`FRESHAUDIT16`.

**Result:** no sign-off. The exact repeated-value reproduction from
`FRESHAUDIT16` is fixed and maintained: `f a with a end` now receives both
distinct one-pair repairs through compilation and both editor actions. Matching
arguments by the source range they occupy correctly distinguishes the two
occurrences of `a`.

The unified range walk introduces a different completeness failure for nested
repairs. Once an outer argument has been bracketed, its range is in `avoid`.
`Divergence` skips that argument before matching it to the competitor and
recursing, so it cannot discover a second necessary bracket inside the already
bracketed span. “Do not add the same pair again” has accidentally become “do
not search beneath this pair.”

An eight-lexeme production expression demonstrates the result:
`wrap print send a to b to a`. It has three displayed readings and three
verified selecting bracketings, but compilation publishes one repair and the
editor exposes one action. The two dropped targets need two nested pairs.

This is one high-severity finding. All maintained gates are green: locked
restore, warning-as-error Release build, all 1,191 tests in Debug, all 1,191
tests in the exact Release coverage gate, 100% line/branch/method coverage for
`Ronin` and `Ronin.Server`, and the transitive NuGet vulnerability audit.

The deliberately open `FRESHAUDIT8` findings 6 and 7 remain outside this
re-audit and are not counted again. The programmer's accepted K-fold cost
residual and decision not to maintain the twenty-child case are also not
findings here. This reproduction has three visible readings, eight lexemes,
and at most two necessary pairs.

No production, maintained test, or existing documentation file was changed
during this re-audit. This file is the only repository artifact added.

---

## Disposition of `FRESHAUDIT16`

| prior finding | re-audit result |
|---|---|
| 1. structural equality across all arguments suppresses a repair when a value repeats | **Closed on the exact reproduction; the new range walk does not descend through a span it already added.** The repeated-`a` compiler and editor cases now expose both one-pair answers. A target requiring a nested second pair is dropped because the outer pair's range is checked against `avoid` before aligned recursion; see finding 1. |

---

## 1. The `avoid` check prevents recursion into an aligned argument that already has an outer bracket

**Severity: high — a legal eight-lexeme production expression reports three
readings but offers only one repair and one code action, although all three
readings have verified bracketings that compile cleanly and structurally select
one original tree each.**

The new call walk matches target and competitor arguments by `Where`, which is
the source range their words occupy. In the loop at
`Compiler/Resolution/Repair.cs:318-329`, however, an already-added range is
discarded before alignment and recursion:

```csharp
foreach (var argument in t is Node.Call diverging ? diverging.Arguments : [t])
{
    var span = Where(argument);

    if (avoid.Contains(span) || span.To - span.From >= lexemes.Count) continue;

    if (others.FirstOrDefault(other => Where(other) == span) is not Node aligned) return span;

    if (Divergence(argument, aligned, avoid) is (int From, int To) deeper) return deeper;
}
```

Avoiding the exact pair is necessary: adding the same brackets twice would not
advance the search. Avoiding the subtree is not. Once the first pair fixes an
outer boundary, surviving readings can still disagree inside it, and the next
pair must be found by traversing that aligned argument.

Production reproduction:

```ronin
function wrap (x => Number) { return x; }
function print (x => Number) { return x; }
function print (x => Number) to (y => Number) { return x; }
function send (x => Number) { return x; }
function send (x => Number) to (y => Number) { return x; }
var a => Number;
var b => Number;
var a to b => Number;
var result = wrap print send a to b to a;
```

The three structural readings are:

```text
wrap(print(send-to(a to b, a)))
wrap(print-to(send(a to b), a))
wrap(print-to(send-to(a, b), a))
```

Their selecting sources are:

```ronin
wrap print (send a to b to a)
wrap print (send (a to b)) to a
wrap print (send (a) to b) to a
```

The first needs one pair and is the only repair currently returned. Each of the
other two needs an outer pair around `send a to b` to choose the `print-to`
boundary, followed by a pair inside it to select the corresponding `send`
reading.

For the second target, the search proceeds concretely as follows:

1. On the unmodified eight lexemes, `Divergence` returns span `(2, 6)`, the
   target argument `send a to b`.
2. Re-resolving `wrap print (send a to b) to a` leaves exactly two readings:
   `send(a to b)` and `send-to(a, b)` inside the same outer grouping.
3. The target's aligned first `print-to` argument still occupies `(2, 6)`, and
   the competitor has an argument over that same range. The correct next step
   is to recurse into those arguments and return `(3, 6)`, `a to b`.
4. Line 322 sees `(2, 6)` in `avoid` and continues before line 325 performs the
   alignment. No other argument differs, so `Divergence` returns null and the
   repair is dropped. The third target stops the same way before reaching `a`.

The comment at `Repair.cs:257-264` correctly says an existing pair must not be
added again, but the implementation applies that rule to traversal as well as
returning a candidate. The distinction is observable without any cap, budget,
or size boundary.

Direct resolution of all three manually bracketed sources returned `Resolved`;
after stripping repair groups, each result matched exactly one of the three
original target trees. All three corresponding full production files compiled
with zero findings. The unmodified production file produced one `Ambiguous`
finding with `Total = 3`, three displayed readings, `Repairs.Count = 1`, and
`Language.Actions.Count = 1`.

The new maintained tests are all one-pair cases. Existing multi-pair coverage
uses sibling disagreements, where operation recursion happens before this call
loop; it does not require descending into a target argument whose outer range
is already in `avoid`.

**Recommendation:** use `avoid` to prevent returning an identical pair, not to
prune recursion through an aligned argument. An aligned span already in
`avoid` must still be walked for a deeper disagreement. Maintain this exact
three-reading production source through both `Compilation` and
`Language.Actions`, asserting three distinct applicable repairs/actions with
pair counts one, two, and two, and zero findings after each is applied.

---

## Verification record

- `git diff --check a105f42..1ab4ce9` — passed.
- `dotnet restore Ronin.sln --locked-mode` — passed.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror` —
  passed with zero warnings and zero errors.
- Exact maintained Release coverage command — 1,191 passed; 100% line, branch,
  and method coverage for `Ronin` and `Ronin.Server`.
- `dotnet test Ronin.sln --no-restore --configuration Debug` — 1,191 passed.
- `dotnet list Ronin.sln package --vulnerable --include-transitive` — no known
  vulnerable direct or transitive packages in any project.
- Exact `FRESHAUDIT16` maintained cases — two one-pair compiler repairs and two
  one-pair editor actions for the repeated-`a` source; all applied files clean.
- Direct nested-repair Release probe — three readings and one repair; the search
  trace returned `(2, 6)` first, then returned no span for each target needing a
  nested pair.
- Manual structural verification — all three bracketings resolve uniquely and
  each stripped result matches exactly one original target.
- Production nested-repair probe — one ambiguity with three readings but one
  repair and one action; all three manually repaired full files compile with
  zero findings.
- The pre-existing dirty `docs/spec` edits and untracked handoff material were
  preserved. No temporary audit source remains.
