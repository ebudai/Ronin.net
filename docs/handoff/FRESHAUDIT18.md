# Fresh audit 18 — synthetic brackets corrupt the extents used to align later arguments

**Re-audited:** `1ab4ce9..181c572`, the commit addressing
`FRESHAUDIT17`.

**Result:** no sign-off. The exact `FRESHAUDIT17` reproduction is fixed and
maintained: `wrap print send a to b to a` now receives all three repairs and
editor actions, with pair counts one, two, and two. An aligned argument is
correctly traversed even when its own range is already in `avoid`.

The range walk depends on node extents from each re-resolved candidate, but the
synthetic bracket lexemes used for those resolutions have the default source
position `(Offset = 0, Length = 0)`. When an open-ended call ends at a synthetic
closing bracket, the resolver derives an impossible extent for that call. A
real trace produced a `print` call with length `-5` and its enclosing `wrap`
call with length `0`. `Where` then fails to align that call with the same target
argument, adds a surplus outer bracket, and cannot reach the remaining inner
disagreement.

A ten-lexeme production expression demonstrates the result:
`wrap print wrap send send a to b to a`. It has nine total readings, five
displayed, and a verified repair for every displayed target. Compilation
publishes four repairs and the editor exposes four actions. The dropped target
has a three-pair repair that resolves uniquely and compiles cleanly.

This is one high-severity finding. All maintained gates are green: locked
restore, warning-as-error Release build, all 1,193 tests in Debug, all 1,193
tests in the exact Release coverage gate, 100% line/branch/method coverage for
`Ronin` and `Ronin.Server`, and the transitive NuGet vulnerability audit.

The deliberately open `FRESHAUDIT8` findings 6 and 7 remain outside this
re-audit and are not counted again. The programmer's accepted K-fold cost
residual and decision not to maintain the twenty-child case are also not
findings here. This reproduction has ten lexemes and its missing answer needs
three pairs; neither the work budget nor the resolver ceiling is approached.

No production, maintained test, or existing documentation file was changed
during this re-audit. This file is the only repository artifact added.

---

## Disposition of `FRESHAUDIT17`

| prior finding | re-audit result |
|---|---|
| 1. `avoid` prevents recursion into an aligned argument that already has an outer bracket | **Closed on the exact reproduction; deeper growth loses alignment because synthetic brackets corrupt enclosing extents.** The maintained two-level case now exposes all three repairs/actions. In the three-level case, the walk does traverse added pairs, but its range comparison no longer recognizes an enclosing call as aligned after that call ends at a synthetic bracket; see finding 1. |

---

## 1. Re-resolving positionless brackets produces invalid call extents and drops a three-pair repair

**Severity: high — a legal ten-lexeme production expression displays five
readings but offers only four repairs and four code actions, although all five
displayed targets have verified selecting bracketings. The missing edit
resolves uniquely, structurally matches its target, and compiles cleanly.**

The repair walk now aligns arguments by the source lexeme range returned from
`Where` (`Compiler/Resolution/Repair.cs:306-344`). For competitor trees, those
nodes come from resolving `Bracketed(spans)` after every added pair.

`Bracketed` creates each synthetic token without source coordinates at
`Repair.cs:368-380`:

```csharp
bracketed.Add(new Lexeme(LexemeKind.Close, ")"));
bracketed.Add(new Lexeme(LexemeKind.Open, "("));
```

`Lexeme` defaults both `Offset` and `Length` to zero
(`Compiler/Resolution/Resolver.cs:1031-1040`). The resolver records a node's
extent from the first and last lexeme of its span, with

```csharp
last.Offset + last.Length - first.Offset
```

at `Resolver.cs:793-798`. Therefore an open-ended call whose trailing argument
ends in a synthetic close receives a length calculated from source position
zero rather than from the original boundary.

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
var b to a => Number;
var result = wrap print wrap send send a to b to a;
```

The resolver counts nine readings and retains five for display. Four receive
repairs. The missing displayed tree is structurally:

```text
wrap(print(wrap(send-to(send(a to b), a))))
```

Its selecting source is:

```ronin
wrap print (wrap send (send (a to b)) to a)
```

That source has three nested pairs, resolves uniquely, and its stripped tree
matches exactly the omitted displayed target. The corresponding full production
file compiles with zero findings.

The failed search trace, using zero-based half-open lexeme spans, is:

1. From the original ambiguity, add `(4, 8)` around the inner `send a to b`.
2. Re-resolve, then add `(2, 10)` around `wrap send (send a to b) to a` to
   eliminate another surviving outer reading.
3. Re-resolving those two pairs leaves exactly the desired `send(a to b)` and
   its `send-to(a, b)` competitor. The correct next span is `(5, 8)`, `a to b`.
4. The synthetic close for `(2, 10)` is the final lexeme of the open-ended
   `print` call. The resulting competitor tree contains:

   ```text
   wrap call:  Offset = 0, Length = 0
   print call: Offset = 5, Length = -5
   ```

5. The original target's `print ...` argument occupies `(1, 10)`. `Where` on
   the competitor's corrupted `print` call reduces it to a different range, so
   line 324 finds no aligned argument and lines 332-336 return the surplus
   `(1, 10)` pair instead of descending to `(5, 8)`.
6. After that surplus pair is added, the same corrupted inner `print` extent
   remains. `(1, 10)` is now in `avoid`, so no span is returned and the search
   stops with the target unrepaired.

This is not a merely stale diagnostic coordinate on a synthetic internal tree.
The current algorithm makes those coordinates its structural alignment key,
so the invalid metadata changes which repairs exist at the public compiler and
editor boundaries.

A deterministic generated nested-call probe found the same symptom in five of
the first 290 ambiguous statements it encountered: displayed alternatives
without corresponding repairs. The production case above is one of those and
was then reduced and traced independently.

The maintained `FRESHAUDIT17` case needs only two pairs. Its second pair is
found before an enclosing open-ended call whose argument ends at a synthetic
close must itself be aligned as another call's argument. That keeps it green
while the next nesting level fails.

**Recommendation:** make the alignment key stable across repair re-resolution.
Either give synthetic bracket lexemes boundary coordinates that preserve the
source extents of enclosing nodes, or carry original lexeme ranges independently
of extents recalculated from synthetic endpoints. Maintain this exact
three-pair source through `Compilation` and `Language.Actions`, asserting five
distinct repairs/actions with pair counts one, two, two, two, and three, and
zero findings after each edit is applied. A direct invariant that every node
used by `Where` has a non-negative, source-contained extent would guard the
underlying metadata failure rather than only this parse shape.

---

## Verification record

- `git diff --check 1ab4ce9..181c572` — passed.
- `dotnet restore Ronin.sln --locked-mode` — passed.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror` —
  passed with zero warnings and zero errors.
- Exact maintained Release coverage command — 1,193 passed; 100% line, branch,
  and method coverage for `Ronin` and `Ronin.Server`.
- `dotnet test Ronin.sln --no-restore --configuration Debug` — 1,193 passed.
- `dotnet list Ronin.sln package --vulnerable --include-transitive` — no known
  vulnerable direct or transitive packages in any project.
- Exact `FRESHAUDIT17` maintained case — three compiler repairs and three
  editor actions, with pair counts one, two, and two; all applied files clean.
- Generated nested-call repair probe — five incomplete repair sets among the
  first 290 ambiguous statements encountered.
- Direct ten-lexeme trace — nine total/five displayed readings, four repairs;
  the search added `(4, 8)`, `(2, 10)`, then the surplus `(1, 10)`, and stopped.
- Metadata inspection after the second pair — re-resolved `print` length `-5`
  and enclosing `wrap` length `0`.
- Manual structural verification — the three-pair source resolves uniquely and
  its stripped tree matches exactly the missing displayed target.
- Production probe — one ambiguity with nine total/five displayed readings but
  four repairs and four actions; the manually repaired full file compiles with
  zero findings.
- The pre-existing dirty `docs/spec` edits and untracked handoff material were
  preserved. No temporary audit source remains.
