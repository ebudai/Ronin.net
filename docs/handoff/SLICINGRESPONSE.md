# Three corrections accepted, one answer, and four dependencies the table is missing

The table read fine.

All three corrections stand. One of them is my recurring error and I want to name
the discipline rather than the instance. Then: **new document for E**, and four
edges I think the slice graph is missing.

---

## 1. §4 was a claim about code I cannot read — third time

> *"It cannot be live: there is no lookup value, no hash table, and `@` is
> integer indexing over lists only."*

Correct, and the sentence was indefensible as written. I had a **design
constraint** — `@` and `is` must share one key relation — and I promoted it to
*"probably live today, check this first, it is cheap and probably real."* That is
a finding, and I had no basis for one.

Same failure as `DIRECTION-PACKET.md` §4, where I claimed the existing detector
was sufficient without reading the detector, and it is the error class I named in
`CODE-REVIEW.md` before committing it twice more.

The discipline, since naming the instance clearly is not working: **I have no way
to observe the tree, so anything about its current state must be phrased as a
question, never as a severity.** "Does `@` use a default comparer?" is something I
can ask. "It probably does" is something I cannot know. If a sentence has a
severity in it, it must be about a design consequence, not about the code.

§4's content survives with the severity stripped: it is a constraint on E, not a
bug report.

## 2. No `==` to remove — and the argument gets cheaper

Accepted. `IS-AND-EQUALITY.md` §2 was written as if there were something to
migrate; there is not. The operator table is `+ - * / @ otherwise`, so it is a
decision **not to add** `==`.

The argument survives and improves: the case against `==` was that it exists only
to disambiguate from `=`, and that `=` already means something here (the
association separator). Never adding it costs nothing at all — no migration, no
deprecation, no dual spelling. That is strictly better than the case I made.

## 3. The R5 bill is prospective — which is the strategic point, not a caveat

> *"`is valid`, `y is x`, `to uppercase`, `time to live`, `a number`, `not found`
> are all accepted today, because `is`/`to`/`a`/`not` aren't patterns yet."*

Right, and it is worth making explicit what I only implied: **the corpus
measurement was always about names people will want to write, never about names
that exist.** There is no Ronin corpus to break. That makes this the cheapest
moment the bill will ever be.

And it generalises into a rule that should go in the registry, because it is
about every future operator and not just `is`:

> A word operator's R5/R7b bill is only payable **before programs exist**. After
> that, every added glue word is a breaking change to source that compiles today.

So the glue registry has a **closing date**. Not now — but "we can add word
operators later" is false, and someone will assume it is true unless the registry
says otherwise. I would put that sentence in the generated header.

His ordering constraint follows directly and I agree with it: **A must land with
or before B**, or the language spends a slice refusing names it currently accepts.

## 4. The answer: E gets a new document

`LOOKUP-EQUALITY.md` is a semantics document that *assumes* the value exists.
Folding construction and evaluation into it would make one document that is two,
and would hide exactly the dependency his table just made visible. Documents
should mirror the slice graph rather than cut across it.

But I would scope the new one wider than "the lookup value", because the gap he
found is wider than lookups:

```
  resolve «[ a = 1 ]»      -> NoParse
  resolve «[ a = 1 ] @ a»  -> NoParse
```

The resolver has no production for an association **at all**. So the missing
document is *square aggregates: resolution, evaluation, and runtime values* —
covering list and lookup together, because:

- finding 8's single-parse production and the resolver production are adjacent
  layers of the same work;
- finding 3's immutable list type is the runtime value on the list side, and the
  lookup value should be built beside it rather than after it;
- `@` by key and `@` by index are one operator that needs one story.

I will write that. Tell me if you would rather it wait until A+B is in, since it
is the largest of the outstanding pieces and it will read better against a
settled `is`.

## 5. Four edges the table is missing

**a. `match` depends on E.** `MATCH.md` declared `match` to be sugar for a lookup
indexed by a discriminant. No lookup value means no `match` runtime — so `match`
is blocked on E, and nothing currently records that.

**b. B's list case couples to findings 3 and 6.** `Builtin.Same` being structural
is what makes `is` on lists right, but cutoff uses `object.Equals`. If B registers
`is` as `Same` while cutoff keeps `object.Equals`, the language ships **two
comparisons that disagree** — `[1,2] is [1,2]` true, and the same two values
"changed" for cutoff. Either B and finding 6 share one function, or B ships
scalars only and lists wait. I would take the second: **B on scalars is clean and
unblocked; B on lists after finding 3.**

**c. C may be two things, and I made one of them sound cheaper than it is.** I
wrote that `type of x is number` "needs no new syntax at all". That is true of the
*grammar* and false of the machinery — it needs types to be comparable **values**,
which is more than `is a` needs. `is a` needs a runtime type tag and a type table;
`type of x is …` needs types in the value language. Those should be separate
rows, and only the first belongs in C.

**d. A's word set is registry-derived, not a list.** `R-art` generalised to
`R7b` — *no name may begin with a pattern-distinguishing word* — and today that
set is `{a, an, not}` **computed from the pattern table**. If A implements the
three words literally, the rule stops being true the moment the stdlib adds
`(_) is like (_)`. Generate the set; the blanket-vs-conditional decision then
becomes one predicate, as `ARTICLE-CONDITIONAL.md` §5 said.

## 6. Where that leaves the order

Not a rewrite of his table — the same slices with the edges above added:

| | |
|---|---|
| **A** R5′, R7b (generated set), R6b-for-`not` | unblocked; must precede or accompany B |
| **B₁** `is` / `is not` on **scalars** | A. Clean and small |
| **D** duplicate keys refused | unblocked; parallel with A+B₁ |
| **finding 3** immutable list runtime type | unblocked |
| **B₂** `is` on **lists** | finding 3 + finding 6 sharing one comparison |
| **E** square aggregates: resolution, evaluation, runtime values | D. Unblocks `match` |
| **C** `is a` / `is an` | B + type table |
| — | `type of x is …` — separate, later, needs types as values |

A + B₁ + D as the first slice, with finding 3 alongside if there is room.

Probes referenced: none new. This one is bookkeeping, and it should be.
