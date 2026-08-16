# Ronin — where it stands, and what's in the box

> **Ledger** — `[R]` Ronin — where it stands, and what's in the box
> supersedes: not yet checked
> superseded by: not yet checked

## Where the project actually is

`Ronin.net`, surveyed from the clone:

| | |
|---|---|
| Last commit | **2023-11-16**, "small refactor (.net 8)" |
| Active period | 2023-10-08 → 2023-11-16 |
| Tests | **257** — 161 unit, 95 failure, 1 integration |
| Target | net8.0 (Compiler + Test) |
| TODOs in `Compiler/` | exactly one, about date literal digits |

So it's **late 2023**, not 2022 — you had a solid five weeks on it. The commit
log reads like someone who knew what they were doing: "lexer and parser is 100%
line covered", "full coverage restored", "all tests passing" three separate
times.

**What's finished:** the lexer, and it's good. `Word`, `Symbol`,
`Symbol.Special`, `Punctuation`, `Keyword`, `Literal`, `Trivium`, dates, text,
numbers, comments. 161 unit tests and 95 deliberate-failure tests behind it.

**What's half-built:** `Compiler/Semantics/` — started 2023-11-05 and stopped
eleven days later. `Analyzer.cs` is 12 lines. `Types.cs` got to 182.

**Where it stopped:** `Identifier.Resolve`. The outer method builds index
permutations; the inner one has an empty loop body:

```csharp
foreach (var permutation in permutations)
{
    if (IsValid(permutation, reference.Length) is false) continue;

}
return Resolution.From(resolutions);
```

**One thing you'll want to know before you start:** `Word.Lex` already stops at
symbols and punctuation. Symbols are *already* their own lexeme class in your
lexer — the decision you made this week is implemented. It was `Name.Parse` that
re-merged them:

```csharp
while (parser.Token is Word or Symbol and not Punctuation) parser.Advance();
```

Delete `or Symbol` from that condition and the lexer already does the right
thing. The change is smaller than either of us assumed.

**Housekeeping:** there's a stray `Ronin.csproj` at the repo root targeting
net6.0 that isn't in `Ronin.sln`. It'll break a bare `dotnet build`. Delete it.

---

## The hot reloader

Not in either repo — `Ronin` and `Ronin.net` contain no hot-reload code, and
the only mention anywhere is the word "hot-reloadable" in the README goals. It
was a different project.

Since you framed it as a from-scratch exercise: **you landed on both halves of
the modern approach.**

*Keeping the metadata alive and diffing it* is what .NET's own Hot Reload does
— it computes metadata/IL deltas against a baseline and applies them through
`MetadataUpdater.ApplyUpdate`. You reinvented the delta model.

*Copying over anything that didn't change* is React Fast Refresh's
state-preservation heuristic: keep the state when the shape is unchanged,
discard and re-init when it isn't. You reinvented that too, independently, and
it's the part most homegrown attempts get wrong by trying to preserve
everything.

What reading would have added, and what to fold in if you rebuild it:

- **A quiescent point.** Erlang's key idea: don't swap whenever the edit
  arrives, swap at a defined boundary where nothing is mid-evaluation. Without
  this you eventually hit a half-migrated object graph.
- **Two versions live at once.** Erlang keeps old and new module versions
  running so in-flight work finishes against the code it started with.
- **An explicit schema-change taxonomy.** Add a field → use the initializer.
  Remove → drop. Rename → genuinely ambiguous, ask. Change type → needs a
  mapping. Your "copy what didn't change" handles the first two implicitly and
  silently does the wrong thing on the third.

And the thing that makes this much easier in Ronin than in whatever you built it
for: `let` values never need migrating, only recomputing. The migration surface
is just the `var` sources — 23% of nodes on the sample graph, versus every
object on the heap in a conventional runtime.

---

## The port

`Resolver.cs` → `Compiler/Resolution/` (or wherever you like; namespace is
`Ronin.Compiler`)
`Resolutions.cs` → `Test/Unit/`

**It has never been compiled.** No .NET in my sandbox. I read it carefully and
fixed three things I caught by inspection — a `Dictionary` being relied on for
insertion order (it isn't, in .NET), a constructor-overload ambiguity, and a
public xunit method exposing an internal enum (CS0051) — but assume there's a
fourth. It's a faithful transliteration of the Python that *has* been tested, so
if the tests pass, the algorithm is right.

**What's in it:**

- `Resolver` — the DP. `E[i, j, m]` = cheapest expression over tokens `i..j-1`
  at minimum binding power `m`. Third index carries precedence: an open pattern
  call returns at `PatternBindingPower` and is only available where
  `m <= PatternBindingPower`.
- `SymbolTable.Validate()` — the two scope-wide rules that exhaustive search
  turned up: anchor runs must be prefix-free, and multi-word names may not
  contain pattern glue.
- `Pattern` — rejects hole-initial patterns at construction (left recursion).
- `Lexeme.Split` — a standalone splitter so the resolver is testable without
  your lexer. Production input should come from `Lexer` instead; that adapter is
  the one piece I didn't write, since it depends on how you want to walk the
  `Token` linked list.

**16 table-driven cases plus 5 rule tests**, with expectations transcribed from
the verified Python. The tie cases compare reading *sets*, not order, so they
don't depend on hash iteration order.

**First thing to run:**

```
dotnet build Ronin.sln
dotnet test Test/Test.csproj
```

That gives you the 2023 baseline before you touch anything. Then drop the two
files in and run again.

---

## When you pick it back up

The order that keeps the frontend from eating another five weeks:

1. Write ten Ronin programs by hand. No compiler. Find the warts where they're
   free to find.
2. Drop in the resolver, delete `Identifier.Resolve` and the `or Symbol` in
   `Name.Parse`, run the 257 tests.
3. Tree-walking reactive interpreter — first program that actually runs.
4. Avalonia host: watch the file, re-derive, show values. The VB6 loop.

Step 1 is the cheap one and it's the one we keep skipping.
