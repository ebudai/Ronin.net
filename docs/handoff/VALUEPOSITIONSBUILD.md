# Consultation — before building (A): two premise corrections and the totality mechanism

> **Ledger** — `[R]` Consultation — before building (A): grammar reality and the totality mechanism
> supersedes: none
> superseded by: none

`(A)` is ruled and I am building it. A grammar map of the whole `Statement`/`Value`
hierarchy turned up two premises in `VALUEPOSITIONSRULING` that the code does not match,
and one mechanism fact — probing before building, as `TYPECHECKERHANDOFF` §0 asks. None
unwinds the ruling; each changes what I build. Proposed answers below; confirm or correct.

## §1 — two of the five constructs are not grammar nodes

§3 names five value positions my set missed. Three are real grammar productions and I will
cover them: a **`when`/`if`/`while` condition** (`Scope.Conditional<T>.Condition`), a
**`for each` iterable** (`Scope.Iterating.Iterable`), and — not in §3 but real — a **`when
changing` target** (`Scope.Reactive.Target`) and an **`association` `x = y`** (`Association`
`Destination`/`Origin`). The other two do **not exist**:

- **`wait until`** is not syntax. There is no `wait`/`until` keyword; `Test/Unit/Waiting.cs`
  documents that a `wait until` is compiled to `n+1` `when`s at the runtime `Graph` level. No
  grammar node to classify.
- **`match`** does not exist anywhere — no keyword, no node, only a forward-looking comment
  in `Values.cs`. Likewise **`if`-as-expression** is unbuilt (`if` is a statement; the
  value-level conditional today is the `otherwise` operator).

Also: **`return` is not a `Statement`** — it is a resolver builtin *pattern* (`return (_)`),
so a return answer is a resolved-tree `Node.Call` argument, caught by the same descent that
catches any call argument, not by a grammar statement case.

**Proposed:** classify the three-plus real constructs now. The totality gate (§2) is
precisely what forces `wait until`, `match`, and `if`-expression to be classified **when they
are built** — which is the ruling's whole point, and why their not-existing-yet does not
weaken it. I will note in the walk that they are intentionally future.

## §2 — "compiler-checked" is not achievable over the open hierarchy; the gate is a test

The ruling asks the position function to be total, **compiler-checked, no default arm**. In
C# that is not directly possible here: `Grammar.Statement`/`Value` is an **open** class
hierarchy (non-sealed), so a `switch` expression with no `_` arm emits `CS8509`
(*not exhaustive*) **always** — the compiler cannot prove no further subtype exists — which
under `-warnaserror` fails every build, covered or not. There is no language mechanism that
says "exhaustive over the concrete subtypes that exist."

The codebase's own totality gates are **tests**, not the compiler: the `FindingKind`
count-equality assertion (`Findings.cs`), the lexer's every-character test (`Literals.cs`),
the read-only-promise reflection over `Assembly.GetTypes()` (`Admission.cs`). So the
idiom-consistent realisation of §2 is:

- the classifier has an explicit arm per grammar kind and **`_ => throw`** (no silent
  default) — so an unclassified construct that reaches the checker fails loudly, not silently;
  **and**
- a **gate test** enumerates every concrete `Ronin.Grammar` node type
  (`typeof(Compilation).Assembly.GetTypes().Where(IsSyntax …)`, the existing `nodes` template
  at `Compilation.cs:1913`) and asserts each is classified — so a **new node type fails the
  build the moment it is added**, before anyone writes source that reaches the throw.

That is "an unclassified construct fails the build," achieved by the gate the codebase
already uses for exactly this, rather than a compiler feature C# does not have. **Proposed:**
build it this way. If by "compiler-checked" you meant something stronger (e.g. sealing the
grammar hierarchy so the compiler *can* check it — a much larger, separate change), say so
and I will scope that instead.

## §3 — the shape I will build (for your picture)

One walk, driven from the grammar statements, `Disagreeing` no longer emitting `ActionInValue`:

- a **statement classifier** (total, `_ => throw`) gives each statement its value-position
  roots — a datum's initializer, an association's two sides, a conditional's condition, an
  iterator's iterable, a reactive's target; a scope recurses its body; declarations/imports/
  errors have none; a **bare expression statement's root is not a value position** (a
  standalone action is performed — no carve-out needed, §6);
- each value-position root resolves to a `Node` tree; a **resolved-node descent** (total,
  `_ => throw`) walks its value positions — call arguments, operator operands, list elements,
  lookup keys and values, round-group parts (transparent) — reporting an action at each, and
  at the root where the grammar says the root is a value;
- both classifiers are covered by the enumerate-all-kinds gate test.

Acceptance targets: `REAUDIT79`'s controls, plus the three real constructs
(`when`/`if`/`for each` conditions with an action), plus the gate test itself.

Nothing is built yet; the branch is at the clean `bed1acc` baseline. This is the last thing
I want confirmed before a large, one-shot slice.
