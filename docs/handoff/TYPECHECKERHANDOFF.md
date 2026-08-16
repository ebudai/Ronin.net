# Handoff — the type checker, mid-slice

> **Ledger** — `[R]` Handoff — the type checker, mid-slice
> supersedes: none
> superseded by: none

**State:** branch `resolver-and-symbol-separation` at `89d1c88`, pushed, working
tree clean. 1,226 tests, 100% line/branch/method.

---

## 0. The gate battery — every commit passes all of it

```
  dotnet restore --locked-mode
  dotnet build --no-restore --configuration Release -warnaserror
  dotnet test  --no-build --configuration Release /p:CollectCoverage=true \
      /p:CoverletOutputFormat=cobertura /p:Threshold=100 \
      /p:ThresholdType=line%2Cbranch /p:ThresholdStat=total
  dotnet test  Ronin.sln --no-restore --configuration Debug
  dotnet format Ronin.sln --verify-no-changes --include <changed files>
  git diff --check
```

Coverage is asserted in **Release**, which counts branches differently from
Debug. `dotnet format` reports pre-existing `IDE1006` warnings on some test
fixtures; those are not yours and the Release build is the gate that matters.

And the working rules, which the audits enforce:

- **Probe before believing.** Three times this session a premise in a design
  document was wrong, and each time a five-minute probe caught it before the code
  was built on it. `EAGGREGATES` §0 was withdrawn because of one.
- **Sabotage every fix.** Break it, watch the guarding test fail, restore. A fix
  whose test passes when the fix is removed is not guarded — this caught a
  redundant branch and a too-weak assertion in one session.
- **Delete rather than defend.** Unreachable or redundant code is removed, not
  tested. The 100% gate is what keeps that honest.
- **Relay design questions.** Do not pick. Every question relayed this session
  came back with an answer that changed the code.

## 1. What has landed, in order

| commit | |
|---|---|
| `db0746b` | type names lowercased — 369 lines, pure rename, no production file |
| `9f43dda` | `SymbolTable` entries carry a `SymbolKind`. Behaviour-neutral |
| `e9b656e` | `type x;` records kind `Type` |
| `edaacc0` | value lookups **narrow** on kind — a type is not mentionable in an expression |
| `4bceb79` | the `=>` token renamed `Returns` → `Arrow` (three jobs, named for one) |
| `c82223e` | the prelude, less the arrow: `number`, `text`, `truth`, `error`, `list of (_)`, `optional (_)` |
| `5d907f5` | three type names the rename missed, behind modifiers; `Supplied` test |
| `89d1c88` | a symbol may be a pattern segment — `lookup (_) => (_)` is an ordinary pattern |

## 2. What is next — step 4, the type half

Annotations resolve with an **expected kind**, and unknown or ambiguous type
annotations become findings **at the annotation**.

- `Type.Unresolved` carries a `Reference` — a run of words awaiting meaning,
  exactly like a statement. Resolve it through the existing resolver.
- The candidate set narrows on kind by the same pass that narrows on type
  (`FIVE-RULINGS` §4). The value side is already written — `SymbolTable.Known`
  and `Callable` filter to `SymbolKind.Value`; the type side is the same filter
  read the other way.
- `Compilation` deliberately does not walk type annotations today, and says so:
  *"types resolve against a table that does not exist yet, and reading them
  against the wrong one is worse than not reading them at all."* That comment is
  the thing being closed.
- One finding per mistake, at the site — not deferred, and not once per use.

**Then the fixture sweep, as its own commit**, so the findings are a visible list
rather than mixed into the change that causes them. Sized already:

```
  money, shared money, compiled money    12   a strong alias, not a primitive
                                              -> «number», or a local «type money;»
  whole number                            1   everything is «number» now
  Dog, Car                                2   user types — check they are declared
```

Everything else in the fixtures resolves against the prelude.

## 3. Open, and needed before step 4 finishes

**`fast number`.** Ruled that all numbers are `number` and context decides the
representation, with `fast number` the one exception, for `/fp:fast` on a single
variable. Two spellings, and it changes what the table holds:

- a seventh prelude **type name**, so `fast number` and `number` are two names
  sharing a word — costs no reserved word;
- or **`fast` as a modifier** on `number`, like the `shared`/`compiled` modifiers
  the fixtures already use — costs a reserved word, and keeps ONE number type
  with a representation hint attached.

The semantics as stated — one number type, representation chosen by context —
argue for the modifier, since the checker then never sees two number types. But
`Modifiers` is a fixed keyword set, so it is a reserved word either way it is
spelled. Not the programmer's call.

## 4. Traps this session paid for — do not rediscover them

- **A pattern's segments must be words the lexer produces.** `list of` is TWO
  segments; `for each` is one, because it is a composite keyword. The `Pattern`
  constructor says so, loudly, on first construction.
- **`Truths` takes every nullary supply.** Adding type names without filtering it
  on kind silently made them truth literals — a wrong answer, not a missing one.
  There is a test named after it. The other two derivations (`Whole`, `Builtins`)
  deliberately DO include type entries: a type is supplied, so declaring one is
  refused, and `list of (_)` is a shape nobody may redeclare.
- **The doc goldens are generated, not written.** `docs/reference.md` from
  `Manual.Of(SymbolTable.Supplies)` and `docs/reserved-words.txt` from
  `Glue.Registry(SymbolTable.Builtins)`. Regenerate with a throwaway test that
  writes both files, run it, delete it. Never hand-edit them.
- **The documentation gate catches a doc comment inserted between a summary and
  its type.** Adding `SymbolKind` above `SymbolTable` left the class undocumented
  and the enum described twice. Put a new type ABOVE the preceding summary.
- **A rename regex anchored on `=>` misses a type behind a modifier.** Three
  `=> reactive Number` survived the case rename, and the symmetry proof said the
  diff was pure — because those lines were never touched at all. A clean proof of
  the wrong property.
- **`reactive` is legacy syntax.** It is `let` vs `var` now. But
  `Declarations.cs` still branches on `datum.Modifiers.Is<Reactive>()` beside
  `Mutability is Let`, so the modifier is live in the compiler, not only in
  fixtures. Separate cleanup, not part of the checker.

## 5. Further out, already ruled

- **`TAIL-SUGAR`** — `{ x }` ≡ `{ return x; }`, only the final statement, never in
  a `when` body, and `{ x; }` sugars too. It needs the checker first: its
  totality argument is that the action type is inadmissible in a value position,
  so `print x` cannot sugar — which requires knowing declared return types.
- **`Function.Returns` is parsed and dropped.** No reader anywhere. Capturing
  declared parameter and return types is what `TAIL-SUGAR` and E §7 both wait on.
- **`LOOKUP-ARROW-RULED` §2** — `m => lookup text => number => truth` must be an
  ambiguity error offering all three bracketings, and §3 forbids resolving it
  with a binding power. A maintained-test target once annotations resolve.
- **E §5 and §7** — expected-type `[]` and aggregate unification. Recorded in the
  expiry ledger at `Test/Expiry.cs` with their successor.
