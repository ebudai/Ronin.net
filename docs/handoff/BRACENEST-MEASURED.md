# The brace-nest exponential — it moved to `[`, and one-parse-one-decision is not built

Measured after `60d3954`, on nests whose body fails late so every production has
to parse the whole thing before it can tell. Times are one compilation each.

```
                depth 4   depth 6   depth 8   depth 10   depth 12

BEFORE (list/lookup on braces)
  «{{{{ 1 2 }}}}»   13 ms     11 ms    170 ms    599 ms*    603 ms*

AFTER
  «{{{{ 1 2 }}}}»    0 ms      0 ms      0 ms      0 ms       0 ms
  «[[[[ 1 2 ]]]]»    0 ms      7 ms    130 ms    600 ms*    583 ms*

  * refused rather than parsed — MaxGroups cutting in, as designed
```

Control, a nest that succeeds at every level and so never backtracks:

```
  «{{{{ 1 }}}}»      0 ms      0 ms      0 ms      0 ms       0 ms
  «[[[[ 1 ]]]]»      0 ms      0 ms      0 ms      2 ms       9 ms
```

## What this says

**The brace side is fixed, completely.** A block nest is flat at every depth,
failing or succeeding. One production opens on `{` and it costs what one
production costs. That half of the decision delivered exactly what it promised.

**The bracket side inherited the curve unchanged.** 0 → 7 → 130 → 600 is the
same shape the brace side had, one bracket over, reaching `MaxGroups` at the same
depth. Nothing was removed; it was relocated.

## Why — and it is not what `BRACEDECISION.md` §2 assumes

> those are separated by whether the first element is an assignment — a
> discriminator *inside* the first element, so still one parse and one decision

That is the right design and it is **not what the parser does**.
`Temporary.Parse` tries `Lookup.Parse` and then `List.Parse`, each a full
aggregate parse of the whole nest, and `Association` inside a lookup's value is
the third. Three productions opened on `{` before; three open on `[` now.

So the measurement does not argue against the decision — the decision's *stated*
mechanism would remove the cost, and it was never built. Moving the spellings
was the easy half; the hard half is still outstanding and is now the only thing
standing between the language and a linear brace grammar.

## What it would take

Parse the first element **once**, as whichever shape is more general, then
dispatch:

- an `Association` is `value = value`, so a lookup element is a list element with
  more in it;
- so parse elements as "value, optionally `=` value", and decide the aggregate's
  kind from whether the first one had an `=`;
- a mixed aggregate — `[ 1, 2 = 3 ]` — becomes a finding, which it should be
  anyway and currently is not stated;
- `[ ]` needs its stated default, which `BRACEDECISION.md` §2 already flags
  (empty list).

That is a change to `Aggregate`'s element handling rather than to any rule, and
the measurement above is the test for it: the bracket row should go flat like the
brace row.

## What is protected meanwhile

`MaxGroups` still refuses rather than grinds — at depth 10 and 12 the file is
rejected with a finding rather than parsed slowly, which is what the bound is
for. The exponential is a performance defect on hostile input, not a
denial-of-service, and it was that before the move as well.
