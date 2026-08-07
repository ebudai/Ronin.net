# What I need to build the next slices

Three questions and one note, each with what the code actually says rather than
what I remember it saying. Ordered by what they block.

Everything in `FIVE-RULINGS.md` that is not listed here is either buildable now
(§1, §5 — starting on them) or waits on the type checker (§2a, §3, §4).

---

## 1. What is `return` inside a `when` body? — blocks §1's follow-on only

`FIVE-RULINGS.md` §1 asks whether `Graph.Return` means the function's value or a
graph node's. It is the second, and there is a sharper problem underneath.

```csharp
// Graph.cs
public void Return()
{
    if (firing is null) throw new InvalidOperationException("«return» is only meaningful inside a body");

    returned = true;
}
```

Guarded on `firing`, per-firing, and sitting beside `stop` which disarms the
`when`. So it is **early exit from a run**, and — the part that matters — **it
takes no value.**

`return (_)` takes one. So these are not one operation wearing one word; they are
two operations wearing one word:

| | takes a value | means |
|---|---|---|
| `return (_)` — the ruling | yes | this is the function's answer |
| `Graph.Return` — exists | no | end this run of this `when` body |

§1 notes that bare `return` — the zero-hole pattern — is now available because
R6's prefix-free clause went. **I think that is the runtime's existing
operation**, and that `return (_)` and `return` are two builtins rather than one
with an optional argument. But that is a reading, not a ruling, and the case that
needs deciding is `return (_)` written *inside* a `when` body, where there is no
function to answer for.

This does not block §1. `return (_)` as a builtin pattern is well defined
whatever the answer, and I am building it. It blocks knowing whether bare
`return` should join it in the same commit.

## 2. How is an action distinguished from a function? — ANSWERED, recorded here for the trail

`FIVE-RULINGS.md` §2b asks whether the declaration form already distinguishes
them, and says the ruling costs one enum case if it does. It does not, and the
reason is a rule already taken.

There **is** a slot. `Grammar.Function.Returns` is a `Type`, parsed after `=>`,
and all three of these compile today with no findings:

```
function twice (x => Number) => Number { return x; }     Returns = declared
function twice (x => Number)            { return x; }     Returns = <none>
function shout (x => Number)            { }               Returns = <none>
```

But omission is already spoken for. `GENERICS-II.md` §3 ruled *omit the type and
it is generic*, and the parser's own comment insists the distinction it draws is
between a type **declared** and a type **missing**, not between declared and
none:

> A consumed «=>» commits. Leaving the type optional afterwards made
> «function f => {}» a function with no return type rather than one missing it.

So under the convention as it stands, `function shout (x) { }` is a function
whose return type is *inferred*, not an action. Reading the empty slot as "action"
would give the return position the opposite meaning to the parameter position,
which is the sort of asymmetry someone hits in week one.

**Ruled by Budai while this was being written:** *an inferred return type is not
generic if it does not depend on any type going in.* So the empty slot means
INFERRED rather than either generic or action, and the action type is what
inference **produces** for a body that never answers — not a slot, and not an
exception to omission-≡-generic. The asymmetry I was worried about does not
arise, because the return position is not doing omission-≡-generic at all.

Which interlocks with §1 above: whether a body ever answers is only decidable
once `return (_)` exists, so the inference has a prerequisite and it is the
slice I am building now. Recorded rather than deleted because that dependency is
the sort of thing that gets rediscovered.

## 3. Truth literals — not blocking, but needed sooner than §2a suggests

§2a sets the literals aside as "a separate small decision" that should not hold
`truth` up. Agreed for the type. But the moment `truth` exists, the tagging
criterion needs fixtures that *declare* something as one, and a fixture cannot
initialise a `truth` without a literal for it. So the literals are not on the
critical path for the type and are on it for the tests that prove the type did
what it was for.

Not a question, a scheduling note: they arrive together or the second half is
untestable.

---

## Two things from the code that bear on assumptions in the rulings

**`Overloads` was already a candidate set, and the runtime already anticipated
narrowing.** `OVERLOADS.md` §3 flagged "whether declaration lookup currently
returns at-most-one by construction" as something it could not see. It never did:
`Declarations.Overloads` maps a shape to a *list*, and `Scope.Invoke` contains

```csharp
if (overloads.Count > 1) return new Error($"«{pattern}» is ambiguous after type filtering");
```

written before the question was asked. The candidate set is not a missing field;
a resolved call reaches every declaration it could mean through its shape, which
is now pinned by a test.

**Type annotations were being read against the value table.** Found while
building the editor slice, and fixed. A type is a reference too, so the walk that
resolves statements was resolving `=> Number` as a value expression — mostly a
no-reading nobody reports, but where an annotation's words happened to be
ambiguous *as values* it reported an ambiguity about a **type**, quoting readings
that could not be written at that position.

This bears on §4. One table with kinds is ruled, and the compiler is currently
walking annotations with nowhere correct to send them — so the kind field is not
only about rules running once; it is what gives type-position resolution a
correct answer at all. The prune I added is a stop-gap that stops the wrong
answer, not a route to the right one.
