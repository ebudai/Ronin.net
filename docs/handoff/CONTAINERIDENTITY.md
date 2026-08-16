# Container identity — the module, and what a "function" is when it is overloaded

> **Ledger** — `[R]` `REAUDIT55` finding 1 (high). The container path that gives a
> named type its identity collapses across two boundaries the ruling names: it omits
> the module, and it cannot tell one overload body from another. The module I can
> fix; the overload half is a semantics question that shapes the whole
> representation, so it is yours.
> supersedes: not yet checked
> superseded by: not yet checked

**From:** the successor, at `3849c5d`.

Stage 1a made `Sort.Named` equal by `(container path, name)`, the path being a
module root plus a segment per enclosing named container. REAUDIT55 finding 1 shows
the path is not yet a unique declaring-container identity in two ways.

## 1. The module — I will fix this, and want only a confirmation

The root is always the empty string, so two same-named types in two files compare
equal:

```csharp
Sort left  = Compilation.Of(new SourceText("type token; var x => token;\n", "left.ron")).Types...;
Sort right = Compilation.Of(new SourceText("type token; var x => token;\n", "right.ron")).Types...;
// both Named("", "token") — equal, though they are two declarations in two modules
```

`NAMEDIDENTITY` Q1b already says two same-named types in two files are distinct
because their declaring scopes differ, and that before-edit/after-edit comparison
across compilations is the normal always-running case — so the module has to be in
the value now, even while *which* modules can see each other stays deferred to
imports. `SourceText` carries the file path, so the root becomes that path:
`left.ron/…` versus `right.ron/…`. *Recommendation: I make the module identity the
source path. Confirm, or name the module identity you would rather use.*

## 2. The function — this is the ruling I need

A container segment is only the function's shape words, so two overloads share it:

```ronin
type a;
type b;
function use (x => a) { type token; var local => token; return x; }
function use (x => b) { type token; var local => token; return x; }
```

Both local `token`s become `Named("/use", "token")` and compare equal, though they
are two declarations in two separate bodies. Two overloads of `use` are two
functions — two bodies, two implementations — so the question is what a "named
container" is when a name has more than one body:

- **(A) each overload variant is its own container.** Two bodies are two
  containers, so their `token`s are two distinct types and neither shadows the
  other — which is what two separate declarations in two separate bodies are. The
  segment must then carry enough to tell the variants apart: the full signature —
  shape *and* parameter sorts — not the shape alone. *(My lean: two bodies are two
  containers.)*
- **(B) the overload set is one container.** The variants share a name, so they
  share a type namespace, and the two `token` declarations are one name declared
  twice — checked together and reported `Shadowed`, exactly as two `token`s in one
  body are.

The current code does neither: the two collide silently, and the collision is
hidden only by the temporary `Overloaded` finding until that ledger row expires
into a bug. **Which is it — A or B?**

## 3. The representation

The audit asks for a *structural* container identity rather than the presentation
string I built — the module identity in the root, each segment an unambiguous
declaration identity, and the path rendered as a string only for diagnostics, not
relied on for equality. I will refactor the string into that structural value.
*Recommendation: as described; the ruling on §2 decides what a function's segment
must contain.*

## 4. One thing that lands with this, not separately

REAUDIT55 finding 2 — a type declared in a function's parameter-default delegate is
hoisted to the module and made globally visible — is the same machinery: the
delegate's container is wrong for the same reason the module root is, and the
hoisting walk crosses the function's boundary through its signature. The fix is the
stopping condition and the container assignment this refactor rebuilds, so I will
land finding 2 with it rather than patch the current path twice.

## What I need

The §2 ruling (A distinct containers, or B one container), and confirmation of §1
(module = source path) and §3 (structural identity). Findings 3, 4, 5, and 6 — bind
the resolved sorts onto the signature, give `Variable` an explicit requirements
slot, hoist in source order, and put the H rule in the specification — are
independent of this and I am doing them now.
