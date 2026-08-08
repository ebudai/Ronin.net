# What I need to build the next slices

Three questions and one note, each with what the code actually says rather than
what I remember it saying. Ordered by what they block.

Everything in `FIVE-RULINGS.md` that is not listed here is either buildable now
(§1, §5 — starting on them) or waits on the type checker (§2a, §3, §4).

---

## 1. What is `return` inside a `when` body? — ANSWERED and BUILT

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

**Answered in `RETURN-AND-LITERALS.md` §1 and built.** Bare `return` is the
runtime's operation; the two are one concept at two arities; `return (_)` in a
`when` is refused with the message that document wrote, verbatim. A body has one
exit flavour and mixing is refused — which is the legality half of the walk whose
other half is the inference, and the collection is written once with its second
reader named in the code.

`done` is withdrawn per `MONOMORPH-AND-RETURN.md` §4; the valueless exit is bare
`return`. See §6 for the one loose end that leaves.

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

## 3. Truth literals — RULED as `true` / `false`, and next on my list

§2a sets the literals aside as "a separate small decision" that should not hold
`truth` up. Agreed for the type. But the moment `truth` exists, the tagging
criterion needs fixtures that *declare* something as one, and a fixture cannot
initialise a `truth` without a literal for it. So the literals are not on the
critical path for the type and are on it for the tests that prove the type did
what it was for.

Not a question, a scheduling note: they arrive together or the second half is
untestable. `RETURN-AND-LITERALS.md` §3 ruled `true`/`false` on exactly that
ground, so this is closed and is what I build next.

## 4. Recursion — settled, and one word of mine was wrong

`RECURSIVE-RETURN.md` and `MONOMORPH-AND-RETURN.md` both land, and the second
supersedes the first's §4. Recording the corrected form here because the version
in the first draft of this document is the one that loses information.

**«check» was the wrong verb, and it was mine.** I wrote *infer from the returns
that do not depend on the function, then CHECK the recursive ones against the
result*. Measured, that publishes `list of ?` for any function beginning
`return empty list`, because the base case is under-determined and the site that
pins the element type is the recursive one. The recursive sites must
**contribute** information, not be validated against what the base case already
knew. An empty accumulator is how a large share of recursive functions start, so
that is the common case rather than a corner.

Stated correctly:

> The answer type is what all the return sites agree on, found by solving the
> base case first. Base-case-first is an ORDERING that makes the common case
> terminate in one pass, not a different rule that looks at one site.

Two more, both from `RECURSIVE-RETURN.md` and neither mine to have got right:
the answer must be **ground** when solving finishes, or `function loop (x) {
return loop (x) }` is accepted with the answer unbound; and it is the recursive
**group** rather than the function, or mutual recursion is refused depending on
which of two functions the compiler reaches first.

**And the residue is empty.** `RECURSIVE-RETURN.md` §4 kept polymorphic
recursion as the one case needing an annotation; `MONOMORPH-AND-RETURN.md` §2
measures that away — finite polymorphic recursion instantiates in two steps and
the monomorphiser does not notice, and the infinite case surfaces as *"this
instantiates forever"* from the depth limit rather than as a type error. So
`RETURN-AND-LITERALS.md` §4 is withdrawn and nothing replaces it.

I will take the depth limit as a prerequisite of the first generic recursive
function rather than a follow-up, per §3 of that document: without it the failure
is a hang, and a compiler that hangs on a keystroke in an always-running editor
is worse than one that reports something.

## 5. `Scope.Invoke` is RUNTIME — the answer that did not reach you

`MONOMORPH-AND-RETURN.md` closes with this still outstanding. It was answered in
conversation and not written down, which is my mistake and exactly the drift this
channel exists to stop.

```csharp
// Evaluator.cs — the only call site
=> scope.Invoke(graph,
                call.Pattern,
                [.. call.Arguments.Select(argument => Argument(graph, argument, insideLet))],
                insideLet);
```

A `Graph`, and arguments that have already been evaluated. So
`«{pattern}» is ambiguous after type filtering` fires **when the call executes**,
which is `OVERLOADS.md` §4a's fear exactly: a runtime failure on a program the
editor called fine, with nowhere for a selectable repair to appear.

Two things soften it. It cannot fire from real source today at all, because
`Overloaded` refuses the declaration at compile time — the only way to reach it
is a hand-built `Scope`, which is how it is covered. And it was written as an
anticipation rather than as a mechanism, so nothing depends on it being there.

What follows: the narrowing belongs in the compile-time filter, and this check
then becomes either a belt-and-braces assertion or dead — and dead is deleted
here rather than tested. Worth deciding deliberately when the filter lands,
because deleting it removes the only place that currently names the condition.

## 6. The `return` / `stop` sentences — one word changed, and nowhere to put them

**«Disarm» is the wrong verb**, and it is the one thing in
`MONOMORPH-AND-RETURN.md` §4's otherwise-good pair that is not what happens.
`Graph.Stop` says so itself:

> And it REMOVES the node rather than disabling it. A stopped «when» that lingers
> still costs an edge walk and still counts toward cascades, and "stopped" that
> is not gone is the same leak the placement rule exists to prevent.

Budai's wording is the same: *`return` exits the current iteration, `stop` means
remove this event.* Disarm reads as reversible and nothing can re-arm a `when`,
so the pair these sentences exist to separate is not helped by describing one of
them loosely. The diagnostic now says *end this firing and leave the «when» in
place, or «stop» to remove it*, and the reference entries should match:

> **`return`** — ends the current body. In a function that answers, write
> `return (the answer)`. In an action or a `when` body there is nothing to
> answer, so write `return` on its own. In a `when`, this ends the current firing
> and leaves the `when` in place; to remove it, see `stop`.

> **`stop`** — removes this `when`, so it does not fire again and stops costing
> anything. To end only the current firing and leave it in place, see `return`.

**But there is nowhere to put them.** `docs/guide` is a single `README.md` and
`docs/spec` has no per-keyword reference. Tell me where they belong and I will
write them; I would rather ask than invent a documentation structure.

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
