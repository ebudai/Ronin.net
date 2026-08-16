# Generics — the "if" is already answered, and the shape is unusually constrained

> **Ledger** — `[R]` Generics — the "if" is already answered, and the shape is unusually constrained
> supersedes: not yet checked
> superseded by: not yet checked

Ronin has generics whether or not anyone designs them: `optional` is in the spec
as a keyword, and lists, lookups, intervals and delegates are all parameterised.
So the question is not *whether* but *what shape* — and three decisions already
taken narrow it a long way.

---

## 1. Type constructors should be patterns — measured

The type language does not need a new syntax. It needs the one that already
exists:

```
list of (_)            glue = {}      list of number            OK
optional (_)           glue = {}      list of big number        OK   ← read whole
lookup of {_} {_}      glue = {}      lookup of (number) (text) OK
lookup of (_) to (_)   glue = {to}    ← the expensive alternative
```

Anchor-first, so **zero reserved words**, and it nests:
`lookup of (number) (list of text)` resolves uniquely. A multi-word type name
(`big number`) is read whole, because that is what the resolver does with names.

Two consequences worth stating:

**No angle brackets.** `<T>` exists in C# and Java because their grammars have
no other way to attach a parameter to a name. Ronin's does. A type constructor is
just a pattern whose arguments happen to be types.

**One resolver, two languages.** `list of number` resolves by exactly the
machinery that resolves `sum of x` — minimum lookup, ties are errors, R5 and R6
apply. That means the type language inherits every property the value language
was verified for, rather than needing its own campaign.

**A two-parameter constructor wants bracketed holes**, because `lookup of (_) (_)`
with free holes has the adjacent-holes problem. `lookup of (number) (text)` costs
nothing; `lookup of number to text` costs the word `to`, which is among the most
expensive available.

### One question this raises immediately

`optional` is currently a **modifier keyword** (`lexical-structure.md:20`). If
type constructors are patterns, should it become the pattern `optional (_)`?
That would remove a keyword and leave one mechanism instead of two. I lean yes,
but it touches shipped code, so it is his call and not a free one.

## 2. The instance decision forces monomorphisation

This is the constraint that most needs stating before either is built.

`INSTANCEBINDING.md` settled **one cell per member, holding N values** — an
array. An array needs a concrete element type to be an array at all. So a
generic type instantiated at three element types is **three sets of arrays**, not
one set of boxed values.

**Erasure is therefore not available.** Java-style generics, where `list of
number` and `list of text` are the same runtime type, cannot coexist with
struct-of-arrays storage. Ronin's generics must be **monomorphised** — resolved
to concrete types at compile time, with a distinct layout per instantiation.

That is the C++/Rust answer rather than the Java one, and it comes with the usual
costs — code size per instantiation, and no runtime type parameters — and the
usual benefits, which here include keeping the vectorisation and the SoA win the
whole instance decision was made for.

It also means **a cell cannot hold a value of unresolved type.** Generics live
entirely in the compiler; the graph at runtime is monomorphic.

## 3. Priority: generic types now, generic declarations later

The audience decides this. A VB6-style programmer **uses** `list of number`
constantly and **declares** a generic function almost never — that is
container-author work, and the containers ship with the language.

So the two halves can be separated, and should be:

| | needed when | who writes it |
|---|---|---|
| parameterised **types** | immediately — the stdlib cannot be typed without them | everyone, constantly |
| generic **declarations** by users | much later | rarely, and by advanced users |

The stdlib's own generic functions — `first of`, `count of`, `sort by` — can be
built-in and typed internally, with no user-facing type-parameter syntax at all.
That gets you a long way before the hard part is needed.

**And the hard part is genuinely hard**, which is the second reason to defer it:
a user-declared generic function needs a way to say *the same type here and
here*, and every spelling I can think of either costs reserved words
(`for any element, function first of …`) or introduces implicit binding of an
undeclared name — which conflicts directly with no-shadowing and flat merged
symbol tables, where an undeclared name is an error by construction. I do not
have a good answer and would rather say so than invent one.

## 4. Two things Ronin can do that most languages cannot

**Variance can follow mutability, because mutability is already in the type.**
Covariance is unsound for mutable containers — that is the Java array hole. Ronin
distinguishes `var` from `let` at the declaration, so:

- a `let` list, being derived and unwritable, can be **covariant** safely
- a `var` list must be **invariant**

Most languages have to bolt variance annotations on (`out T`, `in T`) precisely
because they cannot see mutability in the type. Ronin can, and it should — this
is a case where an existing distinction pays for itself.

**Outward-in typing rules out global inference, and that is a feature.**
Hindley-Milner style inference produces error messages arbitrarily far from the
mistake, which is the one thing this language has consistently refused. Types
flowing outward-in means a generic instantiation is decided where it is written,
and a mismatch is reported there.

## 5. What I would not do yet

**Constraints and bounds.** `function total of (items => list of T where T has +)`
is where generics stop being cheap. For the target audience it is almost never
needed, and the stdlib can special-case its own. Adding a constraint language is
the single most reliable way to make a type system unreadable, and it can be
added later without breaking anything — whereas removing one cannot.

## 6. Summary of what needs deciding

| | |
|---|---|
| type constructors as patterns | **recommended** — measured, zero glue, nests |
| `optional` becomes `optional (_)` | leaning yes; touches shipped code |
| monomorphisation | **forced** by the instance decision, not a choice |
| variance follows `var`/`let` | recommended; unusually cheap here |
| user-declared generic functions | **defer** — no good spelling yet, and not needed for the audience |
| constraints | defer, and be slow about it |
