# The base-resolution item is not "no machinery" — how much algebra for this pass?

> **Ledger** — `[R]` Probes a premise in `Test/Expiry.cs` and `CHECKERSCOPINGRULINGS`
> §6 step 1 that the tree contradicts: how much algebra the base-resolution pass needs.
> supersedes: not yet checked
> superseded by: not yet checked

**From:** the successor, at `57f36e3`, starting step 1.

The ledger row and step 1's plan scope the base-resolution successor as *"the same
walk admitting one more node, `Algebra.Unresolved` beside `Type.Unresolved`, with no
other machinery."* I probed the parsed tree before building on that, and the "no
machinery" half does not hold.

## What the tree actually holds

Parsed at `57f36e3`, reading `Type.Algebra` off each declaration:

| source | `Algebra.Unresolved.Reference` | resolve it as a type (bases declared) |
|---|---|---|
| `type Truck = Vehicle;` | `«Vehicle»` | Resolved |
| `type Money = number;` | `«number»` | Resolved |
| `type Car = Vehicle and { … }` | `«Vehicle and»` | **NoParse** |
| `type U = A or B;` | `«A or B»` | **NoParse** |

`Algebra.Bases` and `Algebra.Unions` are **empty** in every case — the split into
bases and unions is not done; the whole run of words sits in one `Reference`.

So the `Reference` is the raw run *including the algebra operators*. The parser peels
the record `{ … }` off as the `Definition`, which leaves a **dangling `and`** in the
reference; and `A or B` is a two-name union the type resolver reads as one unknown
type. `and` and `or` are not reserved words and not in the type-mode operator table —
which is exactly what `TYPEHALFRULINGS` §3 says: *"the type-mode operator table,
currently empty … bases will introduce `and`/`or` with this follow-up."*

## Why it matters

Resolving the `Reference` as-is — the "one more node" fix — does two things:

- **correctly** flags an undeclared bare base: `type Truck = Vehicle;` with `Vehicle`
  undeclared is `NoParse`, so it becomes the `UnknownType` finding the row wants; but
- **falsely** flags every valid `and`/`or` algebra: `type Car = Vehicle and { … }`
  with `Vehicle` declared is `NoParse` too, because the reference is `«Vehicle and»`.
  That is a finding on a correct program — worse than the silent accept it replaces.

The row's premise and `§3`'s "operators arrive with this follow-up" are the same fact
seen from two sides: making the base resolve *is* the `and`/`or` operator work, and
that is machinery, not one more node.

## The options

**A. Bare single-name base only.** Resolve the reference only when it is one clean
type name — no `and`/`or` — and report an undeclared one; leave compound `and`/`or`
algebras silent as today. Closes `type X = Y;`, misses the common `Vehicle and
{record}`. No new machinery, no false finding, but the primary form stays unchecked.

**B. Split the algebra and resolve each name.** Separate the reference at `and`/`or`
into candidate type references, drop the empty part the record leaves behind, resolve
each against the type table, and report the unknowns. Handles `Vehicle and {record}`
and `A or B` without false findings. It does *not* populate `Bases`/`Unions` or build
algebra semantics — it is only the findings net. But it is the `and`/`or` work `§3`
anticipated, and it has to cope with the dangling `and`; whether that split belongs in
the operator table (your `§3` framing) or is done by the walk is your call.

**C. Defer the whole item.** Leave the ledger row; the silent accept stays this pass;
take base resolution up with the algebra-structure design (`Bases`/`Unions`, the alias
declaration syntax deferred under Q3), where the operators belong anyway.

## Recommendation

**B, as a findings-only net — or C.** A is unsatisfying because `Vehicle and {record}`
is the form the feature exists for, and leaving it silent half-does the job. B is the
real answer but is genuinely the `and`/`or` operator follow-up you named in `§3`, so if
you would rather that land as designed operators with `Bases`/`Unions` populated, then
C — defer — is cleaner than a findings-net that a later operator pass has to reconcile
with. What I want from you is which: a scoped findings-net now (B), or defer to the
algebra follow-up (C). The Type-term half of step 1 (the semantic type the annotation
resolver returns) is independent of this and I can build it either way.

## One line for the ledger, whichever way it goes

The current row says "one more node, no other machinery." That is the struck-sentence
pattern again: it will send the next successor down the same naive fix. It should say
what the probe found — the reference carries the operators, `Bases`/`Unions` are unpopulated,
and the fix is the `and`/`or` follow-up — with the ruling here as its successor.
