# Scoping the semantic type checker — what is already ruled, and the handful that is not

> **Ledger** — `[R]` Reconstructs the already-ruled checker scope and asks Q1–Q7;
> **answered by `CHECKERSCOPINGRULINGS`** — build to that. Q1 confirmed with three
> refinements (`nothing : Optional(Variable)`, `Variable` carries a requirement set,
> `fast` never enters `Type`); Q2 closed; Q3 confirmed (a strong alias *is* opaque
> `Named`); Q4–Q7 were already ruled and are pointed to there. §2 carries inline
> `[V]`/`[R]` marks because its claims differ in status.
> answered by: CHECKERSCOPINGRULINGS
> supersedes: none
> superseded by: none

**From:** the successor, at `57f36e3` (type half signed off, `REAUDIT53`).
**Asks:** confirm the semantic type term and the unification model I have assembled
from existing rulings, and rule the seven open questions in §4. Nothing here is a
request to re-decide settled ground; §2 exists precisely so you do not have to.

---

## 0. Where we are

The **type half** is done and signed off. Annotation text resolves against an
expected kind; unknown and ambiguous annotations are findings at the site. What
does **not** yet exist is a *semantic type*: a successful resolution is computed
and then discarded, and a function signature keeps its parameter/return **spellings**
(string-encoded in `Declarations.Typed`, for duplicate classification), not a
resolved type. So every one of these compiles clean today, and each should be a
finding:

```ronin
var x => number = "text";
function f => number { return "text"; }
var xs => list of number = ["text"];
var m  => lookup text => number = [];      -- also builds a list, not an empty lookup
function f => number { 1; }                -- final value should sugar to a return
```

(Verified at `57f36e3`: all five clean. Also verified: value lookup literals
`[1 = 2]`, `[a = b]` now resolve, so `EAGGREGATES2` §1's "a lookup does not resolve
at all" is stale — the three-kind node landed since. Aggregate typing is reachable.)

This is `FRESHAUDIT21` finding 1, the one large stage still open.

## 1. The shape of the work, stated plainly

The checker is **a monomorphising inference engine**, not only a value-against-annotation
check. That is forced, not chosen: `GENERICS.md` §2 / `GENERICSII.md` §1 —
*"An array needs a concrete element type to be an array. Monomorphisation is
**forced**, not chosen … Erasure is therefore not available."* Omitting a
parameter type already **is** a generic (`GENERICSII.md` §3), and return/recursion
inference produces type variables that only monomorphisation grounds
(`RETURNANDLITERALS.md` §2, `MONOMORPHANDRETURN.md` §2). So the foundation and the
generic engine are not two projects with a clean seam; they share the inference
variable and the `(function, instantiation)` cache.

I propose we still **stage** it (§5), foundation first, but you should know going in
that the end state is an inference engine.

## 2. Already ruled — confirm, do not re-derive

Citations are on-disk filenames. Voice matters: `TYPEHALFRULINGS`, `REAUDIT47RULING`,
`FIVERULINGS`, `INSTANCEBINDING`, and `Test/Expiry.cs` speak as **verdicts**; the
`EAGGREGATES2`, `GENERICS*`, `OVERLOADS`, `DELEGATES`, `RETURNANDLITERALS`,
`RECURSIVERETURN`, `MONOMORPHANDRETURN` memos are **recommendations** except where
they say "forced"/"decided"/"taken". I mark `[V]` verdict, `[R]` recommendation.

**Type identity and unification**
- **No subtyping. Unification is equality.** `[V]` `TYPEHALFRULINGS.md` §4:
  *"Ronin has no subtyping … Unification is equality, so `text => number` unifies
  with `text => number` and with nothing else."* No joins, no meets, no variance.
- **`error` is a named, one-directional bottom.** `[R, but the checker rule]`
  `ERRORASVALUE.md` §2: *"`Error` is assignable to every type. No type is assignable
  to `Error`."* One-directional on purpose — *"If it matches in both directions it is
  a `dynamic`/`any` hole."* Named because `x is an Error` must be writable.
- **No union types.** `[R]` `ERRORASVALUE.md` §1 — `T | Error` is rejected;
  error is an ordinary value, not a union member. The checker builds no unions for
  error propagation.
- **`is` is value equality; `is a` is a type test.** `[R]` `ISANDEQUALITY.md`
  §1, §9 — the article is the namespace selector.

**The vocabulary the type term must represent**
- Scalars `number`, `text`, `truth`; the `error` bottom; constructors `list of (_)`,
  `optional (_)`, `lookup (_) => (_)`; the function type `(_) => (_)`. `[V]`
  `TYPEVOCABULARY.md` §2/§4, `TYPEHALFRULINGS.md` §4 (*"Ruling: in"* on the arrow).
- **One number type.** `[V]` `TYPEHALFDECISIONS.md` §1 / `TYPEHALFRULINGS.md` §1 —
  all numbers are `number`; `fast` is a **modifier**, a representation hint, not a
  seventh type. `true`/`false` are values of `truth` (`RETURNANDLITERALS.md` §3).
- **`type X;` names an opaque type.** `[V]` `TYPEVOCABULARY.md` §3 — declaration
  names, definition gives structure; a bare handle is admissible.
- **The action type has no spelling** and is inadmissible in a value position.
  `[V]` `TYPEVOCABULARY.md` §2, `FIVERULINGS.md` §2b.
- **One symbol table, kind is a filter** (already built). `[V]` `FIVERULINGS.md`
  §4, `TYPEVOCABULARY.md` §2. (`GENERICSII.md` §8a floats separating them — see Q4.)

**Aggregates** (`EAGGREGATES2.md`, with §8 superseded by `REAUDIT47RULING.md` §5)
- **Unify, not check.** `[R]` §7 — `[e1..en] -> list of T`, disagreement is an
  error naming both positions; `[k1=v1..] -> lookup of K V`; `[] -> list of ?`.
  The element that pins `T` may be the third one.
- **`[]` is the empty list; the empty lookup comes outward-in from the expected
  type.** `[R]` §5 — `var m => lookup text number = []` is the empty lookup; there is
  no second literal. The checker must override the parser's list-default when the
  expected type is a lookup.
- **A miss gives `nothing`; `m @ k : optional V`; optionals nest; list index
  out of range stays `Error`.** `[V]` `REAUDIT47RULING.md` §5. This settles the
  `Error`/`optional` fork that `EAGGREGATES2.md` §10 reserved for you as *"the one
  thing I still want from Budai before the type checker"* — §10 predates the ruling;
  I read it as closed, and I want that confirmation (Q2).

**Functions, returns, recursion, tail sugar**
- **Written `=>` declares; omitted infers from the body** — a concrete type, the
  action type, or a type variable. `[R]` `RETURNANDLITERALS.md` §2. Multiple `return`
  sites **unify**; one exit flavour per body; the legality check *is* the inference
  pass (§1b–§1c).
- **Recursion infers from the base case, unifying not checking, over the recursive
  group (SCC); the answer must be ground.** `[R]` `RECURSIVERETURN.md` §1–§3. **No
  annotation is mandated for any recursion class** — `MONOMORPHANDRETURN.md` §2
  withdraws the earlier "recursion needs a written return type"; monomorphisation
  dissolves polymorphic recursion, and nested-datatype recursion surfaces as a depth
  limit, *"not a type error."* Needs a recursion/instantiation **depth limit** before
  the first generic recursive function, and inference cached on `(function,
  instantiation)`.
- **Tail sugar `{ x } ≡ { return x; }`** — final statement only, never in a `when`
  body, `{ x; }` too; total *because* the action type is inadmissible in value
  position, so `print x` never sugars. `[V/R]` `TAILSUGAR.md`. A sugared tail is a
  return site and feeds the same unification.

**Generics** (`GENERICS.md`, `GENERICSII.md` — mostly `[R]`, monomorphisation `[forced]`)
- Type constructors are **patterns**, no angle brackets, zero reserved words.
  Omit a type and the parameter is generic (structural, implicit, monomorphised —
  "Zig's `anytype`"). The **inferred requirement-set is the interface**, checked at
  the call boundary before entering the body ("a C++20 concept, derived not declared").
  There are **no user-spellable type variables** (`GENERICSII.md` §9).

**Overloads** (`OVERLOADS.md` `[R]`, `Test/Expiry.cs` `[V]` on the split)
- A reading carries a **candidate set** per shape; narrow by admissible argument
  types; `0` → dead reading, `1` → resolved, `>1` → overload ambiguity — one pass,
  not a post-pass. `[R]` §1, §3.
- **The declaration-site refusal is a temporary stand-in.** `[V]` `Expiry.cs` —
  same-shape/**same** param types is a duplicate that **never** expires; same-shape/**different**
  param types is the approximation that expires into use-site ambiguity, and that
  needs an expression-level type ascription (Q7) that does not exist yet.

## 3. The proposed semantic type term

A discriminated type, structural-equality by shape, assembled from §2:

```
Type =
  | Scalar(number | text | truth)              -- the three ground scalars
  | Error                                       -- named bottom; ⊑ every type, nothing ⊑ it
  | Action                                      -- no spelling; inadmissible in value position
  | List(Type element)                          -- list of (_)
  | Optional(Type inner)                        -- optional (_); nests, optional(optional V) ≠ optional V
  | Lookup(Type key, Type value)                -- lookup (_) => (_)
  | Function(Type[] parameters, Type result)    -- (sig) => result; parameters may be empty
  | Named(SymbolId)                             -- a declared «type X»; opaque this pass (see Q3)
  | Variable(id)                                -- an inference variable; not user-spellable
```

Unification is equality, with three asymmetries: `Error` unifies with (is assignable
into) any type and the result is that type; `Variable` binds; `Action` never unifies
into a value position. `Named` unifies only with the same `Named` this pass. That is
the whole relation — no subtyping means there is nothing else to it.

**Please confirm this shape, or correct it.** It is the one artifact everything
downstream stores and compares.

## 4. Open — please rule

Ordered by how much they gate the work. Each carries my recommendation, as the type-half
memos did.

**Q1 (high) — the type term of §3.** Confirm the representation, especially: `error`
as a first-class `Error` case rather than a per-type flag; `Action` as a case rather
than "null return"; and `Named` opaque for this pass. *Recommendation: as written.*

**Q2 (high) — is the `Error`/`optional` fork closed?** `REAUDIT47RULING.md` §5 rules
miss → `optional V`, which is the shape the checker needs, yet `EAGGREGATES2.md` §10
still lists the fork as deciding "the checker's whole shape." I read §10 as predating
and superseded. *Recommendation: confirm closed; the checker is equality-unification
with an `Error` bottom and `optional` nesting, and no further fork remains.*

**Q3 (high) — `type X;` identity for this pass.** `TYPEVOCABULARY.md` calls `money`
a *"strong alias over number"* (§2) and `type X;` an opaque handle (§3). Under
no-subtyping these pull apart: an opaque `Named` unifies only with itself, but a
"strong alias" would have to unify with `number`. Structure (`type Car = Vehicle and
{…}`) and its base are already deferred to the `Algebra.Unresolved` walk-extension in
the ledger. *Recommendation: for this pass, every declared `type X` is opaque `Named`,
unifying only with itself; defer aliases-over-a-base and algebra structure **with**
the base-resolution item, so `money`-as-alias is a later ruling and not a blocker now.
Confirm that split.*

**Q4 (medium) — one symbol table or two, under generics.** The ratified position is
one table with a kind filter (`FIVERULINGS.md` §4), which is built. `GENERICSII.md`
§8a recommends separating type and value tables *"before R5/R6b are extended to types."*
*Recommendation: keep one table unless you want the separation now; if you do, it is
cheaper before the checker stores types than after. Tell me which.*

**Q5 (medium) — `Scope.Invoke`: compile-time or runtime?** Still owed from
`NEEDFROMDESIGN.md` §4 (`MONOMORPHANDRETURN.md` §5, `RETURNANDLITERALS.md` §4a). It
decides whether overload/call ambiguity is compile-time (and can carry a bracket/ascription
repair) or a runtime condition with nowhere for the repair to appear. *Recommendation:
compile-time, to keep ambiguity in the same reported-with-repairs channel the resolver
already uses — but it is your call and it gates the overload diagnostic.*

**Q6 (medium) — `optional`: modifier or constructor?** `NOTHING-ANALYSIS.md` §D says
`optional` is currently parsed/stored as a **modifier** (beside `compiled`/`shared`);
the type work reads `optional (_)` as a **constructor** and resolves it as one today.
*Recommendation: the constructor reading is operative and the checker should read only
that; confirm the modifier storage is dead so it can be removed rather than left to
disagree.*

**Q7 (low, deferrable) — is there an expression-level type ascription?** The
prerequisite for use-site overload resolution (`OVERLOADS.md` §4). Since use-site
overloading is itself deferred (declaration-site refusal stands), this can wait.
*Recommendation: defer with the overload expiry; note it so the ledger row and this
question travel together.*

Two consequences I am **not** asking you to rule, only flagging: every `number` column
must be able to hold an `Error`, so storage cannot be unboxed `double[]` and wants a
validity bitmap plus reason side-array (`ERRORASVALUE.md` §5) — a runtime rep decision,
expensive to revisit; and mid-session monomorphisation (a call at a new argument type
instantiates during a run, `GENERICSII.md` §1) is a real requirement of the always-running
environment that no doc has designed yet.

## 5. Proposed scope for this pass

**In:** the semantic type term; storing it on annotations and signatures; equality
unification with the `Error` bottom; initializer/return/assignment/call/operator
mismatch findings; aggregate element/key/value unification and outward-in `[]`;
function parameter/return capture and return-site inference; base-case-first recursion
over an SCC with a depth limit; tail sugar as a consumer of the typed tree; the
ledgered `Algebra.Unresolved` base-resolution and the `fast` target/duplicate checks.

**Deferred (named, with successors):** use-site overload resolution and the expiry of
the declaration-site refusal (waits on Q5/Q7); user-type **structure** beyond opaque
`Named` — aliases, `and`/`or` algebra, base assignability (waits on Q3's split);
declared generic constraints (inferred ones are in); `type of x` branching in generic
bodies; the storage-rep and mid-session-monomorphisation items above.

## 6. Proposed implementation order — each a gated commit with negative tests

An empty finding collection is the failure condition for every row; the source-level
negative test is the deliverable, not an afterthought.

1. **Type term + store it + base resolution.** Define §3; annotation resolution returns
   and stores a `Type`; signatures carry `Type`s beside the spelling. Add
   `Algebra.Unresolved` to the annotation walk (the ledger's "one more node, no other
   machinery"). *Test: undeclared base `type Car = Vehicle and {…}` becomes a finding.*
2. **Initializer & return mismatch.** Infer an expression's type, unify with the declared
   type. *Test: the five §0 witnesses become findings; `error`-bottom cases still pass.*
3. **Aggregate unification + outward-in `[]`.** *Test: `list of number = ["text"]`
   is a finding; `lookup … = []` builds the empty lookup, not a list.*
4. **Function capture + return inference + recursion.** Replace the spelling with the
   type where checking needs it; infer omitted returns by unifying return sites; solve
   recursion base-case-first over the SCC; add the instantiation depth limit and the
   `(function, instantiation)` cache. *Test: `factorial`/`collect` infer; a never-answering
   `loop` is refused for an unground answer.*
5. **Tail sugar.** Final value tail becomes a return site. *Test: `{ 1 }` answers; a bare
   value mid-body and a value tail in a `when` warn.*
6. **`fast` checks; overload expiry (only if Q5/Q7 land).** *Test: `fast truth` and
   `fast fast number` become findings; the expiry ledger rows that close are deleted, not
   left.*

Steps 4–6 are where the generic engine proper begins; if that proves large, it splits
into its own staged effort and 1–3 stand alone as the foundation.

## 7. What I need from you

Rule Q1–Q7 (Q7 may be "defer"). Everything else in §2 I will build to as written unless
you correct it. With Q1–Q3 answered I can start step 1 immediately; Q4–Q6 are needed by
steps 4 and 2 respectively, not by step 1.
