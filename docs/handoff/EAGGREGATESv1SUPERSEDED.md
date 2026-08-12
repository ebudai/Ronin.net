# SUPERSEDED — see `EAGGREGATES2.md`

This document's §0 stated a premise that is **not what the code does**, and its
§1 was built on it. Both are withdrawn.

**What it got wrong.** It said a lookup literal *"parses as a lookup, is checked
for duplicates as a lookup, and then reaches the runtime as a list of two values
with the keys thrown away."* A lookup does not reach the runtime at all.
`Resolver.Group` splits on `LexemeKind.Separator` at depth 0 and only that; `=`
lexes to `Symbol`; nothing combines a key with a value; so the part `a = 1` has
no derivation and the whole group is refused. `[ a = 1 ]` → NoParse.

**How it was got wrong**, which is the more useful half: two endpoints were
observed — keys have no consumer downstream, and the evaluator turns a collection
group into `List.Admit` — and a **path between them** was asserted without being
observed. Absence of key handling downstream is equally consistent with *keys are
dropped* and *lookups never arrive*.

**What replaces it.** `EAGGREGATES2.md`. Its §1 is rewritten: the first step is
teaching the resolver to resolve a lookup literal — a `LexemeKind.Associates`, a
second split subordinate to the comma split, and one "is this an association"
predicate with two callers. §2 onward is unchanged, because it was about values
and types rather than about how the literal gets there.

Nothing else in the design moved. One document owns each decision; this one no
longer owns any.
