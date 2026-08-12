# The descriptor slice — three answers, and one of them corrects the ruling

`Injection` as the precedent is the right read, and better than the one I gave:
*three hand-written copies, and a real injector left out of the registry would
have kept it green.* That is the same failure with a sharper example than any of
mine. Fourth time.

The three questions, and **§2 is me correcting something I specified badly.**

---

## 1. `stop` — the law already decided this, and it is not a docs question

Take the two cases apart, because they have different answers and only one of
them is about documentation.

**If `stop` is writable in source today**, then it is a word that participates in
parsing and is not in the table the name rules run over — which is
`FIVE-RULINGS.md` §0 exactly, and the same defect as `return`-as-a-keyword:

```
  «stop» not in the table   ->  a user may declare the name «stop»
                                or «stop the clock», and nothing refuses it
                            ->  the capture is found at the use site, if at all
```

So it must become an entry regardless of the reference. That is not a
consequence of wanting a `see also` target; the `see also` target is a
consequence of it. Cost is a whole-name reservation only — `stop` is nullary, so
it reserves its own spelling and nothing else: **5 exact collisions in a
460,030-identifier corpus**, and `stop word`, `stop loss`, `stop time` all stay
legal.

**If `stop` is not writable in source yet** — if `Graph.Stop` is reachable only
from the runtime and no source form exists — then there is nothing to describe,
the entry waits for the slice that exposes it, and **`see also` should simply not
be written until both ends exist.** A checked reference with one end missing is
the check working, not the check being wrong.

Either way, no wider ladder is needed for this.

### But the ladder question is real, and it has an answer

*What belongs in the reference?* **Everything a user can write.** Builtin
patterns, operators, types, literals. And under `FIVE-RULINGS.md` §4 that set is
already supposed to be **one table with a kind on each entry** — so
`IReadOnlyList<Pattern>` beside `IReadOnlyList<string>` is not a starting point
the reference has to work around. It is the fragmentation §4 already ruled
against, showing up in a second place.

Which reframes the slice, in a way that makes it worth more than it costs:

> **A descriptor list with `Builtins` and `Truths` derived from it *is* the first
> concrete form of the one-table ruling.** The `kind` field and the `summary`
> field want to live on the same record, and building the record once serves
> both.

Worth knowing before writing it, because it changes what the type is called and
what else it will be asked to carry. Not a reason to widen the slice now — the
kind field can arrive later on a record that already exists, which is much
cheaper than a second record.

## 2. One file, not N — and I specified this badly

Straight correction: *"one page per entry, plus an index"* was a layout decision
stated as though it were a structural one. It is not. **The reference is
generated, so its file layout is a rendering choice and reversible at any time.**
Paying N goldens now buys reversibility that is already free.

**One `reference.md`, one golden, the same discipline as `reserved-words.txt`.**

- Zero new machinery — the registry test already compares a generated string to a
  committed file, and this is a second string through the same path.
- One diff, in context. A change to `return`'s summary shows up next to `stop`'s,
  which is where you want to read it, because the two are written to point at
  each other.
- N goldens means adding an entry adds a file, and a *forgotten* file is a silent
  gap — the exact failure class this slice exists to close.

Revisit when the docs are published and per-entry URLs start to matter. That is a
generator change and a test change, and nothing upstream of it moves.

## 3. Required by the *type*, not by a test — where that is possible

Your reading is right that a test is this repo's enforcement mechanism, and I
mean it literally where it can be literal. The two failures hiding under "no
summary" have different best answers:

| failure | enforce with |
|---|---|
| an entry with **no** summary | **the type** — make it a required constructor parameter. You cannot add an entry without one, and there is nothing to remember |
| a summary that is empty, whitespace, or a placeholder | **a test** — the type cannot see that |
| `see also` naming an entry that does not exist | **a test** — a cross-entry invariant, which no single record can check |

So: required-by-construction where the type reaches, tested where it does not.
That is strictly cheaper than testing both, and it is the same principle as the
descriptor itself — **make the wrong state unrepresentable before you make it
detectable.** A test tells you the entry is missing a summary; a required
parameter means the thought never occurs.

The gate stays a test, and it is the two rows above rather than three.

## 4. Summary

| | |
|---|---|
| §5 as an assertion, with the message changed | good — and recording the reasoning where it sits is the part that keeps it true |
| `Injection` as precedent | better example than mine; *"a real injector left out of the registry would have kept it green"* is the whole argument in one line |
| `stop` | **if writable in source, it must be an entry anyway** — `FIVE-RULINGS.md` §0, not a docs consequence. 5 exact collisions, cheap |
| `stop` if not yet writable | no entry, and **do not write the `see also`** until both ends exist |
| the ladder | everything writable. Which is §4's one table — so **the descriptor list is that table's first form**, and `kind` belongs on the same record later |
| one page per entry | **withdrawn.** I specified a rendering choice as a structural one |
| the artefact | **one `reference.md`, one golden**, same path as `reserved-words.txt`. Split when published docs need URLs |
| "fails the build" | **the type** for a missing summary — required constructor parameter, free, unforgettable |
| the gate | **a test** for empty/placeholder summaries and for `see also` resolving. Two rows, not three |
