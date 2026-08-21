# A nullary `function f` carries no signature — where should it, and is a bare `f` a call?

> **Ledger** — `[R]` A no-argument function registers as a bare value NAME, with no signature: its return type — written or inferred — is stored nowhere and read nowhere, so `REAUDIT63` finding 4's action witness, and value no-arg functions generally, cannot be checked. Asks whether a bare nullary reference is a call or the function value, and where a nullary function's signature should live.
> supersedes: none
> superseded by: none

**From:** the successor, at `3e5f5af`, actioning `REAUDIT63`. Findings 1–3 and 5
are cut; finding 4's action-sort inference is cut and fires for a parameterized
action; its own witness references the action by a bare no-arg name, and that is
what this is about.

Finding 4 wants `var x => number = f`, with `function f { return; }`, to report
that `f` answers with no value and cannot stand where a value is wanted. I made
the inference construct the action sort and refuse it in a value position — and it
does, for `f 5`. **The bare no-arg witness stays silent, and the reason is not the
action sort. A nullary function has no signature at all.**

## §0 — what is already settled

- **`function f ()` is ill-formed; `function f` is the nullary form.** `EMPTYBRACKETS`
  (`[V]`) rejected the empty-bracket spelling, so a no-argument function is written
  with no brackets, and its identifier — `f` — is a plain name, no holes.
- **A nullary pattern reserves a name.** `Declarations.cs:366`–`370`: a nullary
  supplied thing reserves its spelling so a bare `return` cannot read two ways. The
  same shape a nullary user function has.
- So a nullary function is, by construction, **name-shaped**. What follows from that
  in the checker is what has no answer.

## §1 — the finding: a nullary function is a bare value name

Measured at `3e5f5af`. A nullary function's return type is stored nowhere and read
nowhere. Every one of these compiles with no finding:

```ronin
function f { return 5; }         var x => text   = f;    -- f is a number, x is text
function f => number { }         var x => text   = f;    -- written return, same
function f { return; }           var x => number = f;    -- finding 4's witness; f is an action
function f { return 5; }
function g (n => number) { return f; }                   -- g does not infer from f either
```

Instrumented, the initializer's resolver produces a `Node.Name` for `f`, and the
checker's `Overloads` table and its inferred-answer map are **both empty** — no
signature for `f` is in either. The parameterized control, `function f (n => number)
{ return 5; }` with `var x => text = f 5`, reports the mismatch: a shaped function
is in `Overloads`, inferred and read; a nullary one is not.

## §2 — the mechanism: `TryPattern` fails, so it is filed as a value

`Declare` routes a member by its identifier (`Declarations.cs:406`):

```csharp
if (member.Identifier.TryPattern(out var pattern, out var blocks) is false)
{
    Cell(member);          // :419 — the value-declaration path
    return;
}
// :447 — else: Overloads[pattern].Add(new Signature(... Returned(function) ...))
```

A nullary identifier has no holes, so `TryPattern` fails, and `f` never reaches the
function registration that files a `Signature` — the return type — in `Overloads`.
It reaches `Cell`, whose last arm (`:491`–`500`) files any non-datum, non-type
member as a bare value name:

```csharp
Symbols.WithNames(member is Grammar.Type ? SymbolKind.Type : SymbolKind.Value, name);
```

So `function f` is filed exactly as `type x;` is — *"a name you can use and cannot
construct"*, the comment there. The `Grammar.Function` it actually is, and the
return type it carries, are dropped on the floor. Nothing downstream can read what
was never written down.

## §3 — the design question underneath

Two things have no answer in the code, and the second is a decision, not a gap.

**Where a nullary function's signature should live.** It is name-shaped, so it
reserves a spelling through `Cell`, like a nullary pattern. But it is a callable
with a return type, and a callable's signature lives in `Overloads`, keyed by its
pattern. The two are not reconciled: today it takes the name half and loses the
signature half.

**Whether a bare `f` is a call or the function value** — and this is the one I
cannot decide from the code. The language has delegates, so a function CAN be a
value. So `var x => number = f`:

- if `f` is a **call**, `x` is `f`'s answer — `number`, or the action for finding 4's
  `f`, which no value type admits; or
- if `f` is the **function value**, `x` is `f` itself — an `() => number`, and
  assigning it to `number` is a different mismatch.

`REAUDIT63` finding 4 assumes the first: `f`'s inferred sort *is* `Action`, admitted
in no value position. But `EMPTYBRACKETS` removed `f ()`, so there is no distinct
call spelling to tell a call from a reference by — which is exactly why a nullary
pattern had to reserve its name. If a bare `f` is the call, there is no way to name
the function value; if it is the value, there is no way to call a nullary function
at all. One of those is the intended reading, and it decides what the checker reads
`f` as before any of the storage question matters.

## §4 — what I need

**Q1 — is a bare nullary reference a call or the function value?** For a nullary
`function f => T`, is `f` in a value position `f`'s answer (a `T`, or the action
type where it answers with none), or the function itself (a `() => T`)? If there is
a separate call spelling I have missed, name it. Finding 4 reads it as a call; I need
that confirmed or corrected, because it decides what `Inferred` makes of `f`.

**Q2 — where does a nullary function's signature live?** It is filed as a bare value
name today, its return type lost. Should a nullary function register its `Signature`
in `Overloads` under its nullary pattern — the same table shaped functions use, so
the return-inference and reading paths carry it unchanged — while keeping the name
reservation it already has? Or does the signature belong on the value name itself, as
the sort that name resolves to? The first reuses the machinery I have; the second
makes `f` a typed value, which only fits if Q1 says `f` is the function value.

**Q3 — does finding 4 then follow?** If Q1 is "call" and Q2 files the signature so a
reference can read it, then a no-value nullary function has the action sort as its
answer, and `var x => number = f` is the `ActionInValue` finding I already built —
with no change to the action machinery, only to what a nullary reference resolves to.
Confirm, so I am not solving finding 4 twice.

## §5 — what I do with each answer

- **Q1 = call, Q2 = Overloads:** I file a nullary function's `Signature` in
  `Overloads` under its `[f]` pattern beside the name reservation, and read a nullary
  reference the way a call is read (`Returned`). Finding 4's witness fires, and value
  no-arg functions check everywhere — the whole gap closes on the machinery already
  built.
- **Q1 = function value:** the reference is a `() => T`, not a call; I make
  `Inferred` give a nullary name that function sort, and finding 4 becomes a
  different finding (a function value assigned to a scalar), which I would want you to
  confirm the shape of, since the action type would then never arise from a bare `f`.
- **Q2 = on the value name / a new home:** I wire the signature there instead, once
  you say which.

The five findings are cut and green regardless; this is the one witness that reads a
nullary function's sort, and a nullary function does not have one to read yet.
