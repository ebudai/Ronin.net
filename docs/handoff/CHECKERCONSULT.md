# Consultation — starting the type-checker slices

> **Ledger** — `[R]` Consultation — starting the type-checker slices
> answered by: SLICEONETYPINGS
> supersedes: none
> superseded by: none

Four questions, ordered by what they block, each with **what the code actually
says** and my reading — but the language-meaning ones are yours to rule, not mine
to pick. Context: the approved program (`~/.claude/plans/…`, type-checker-first,
VS Code client first) reconfirmed the frontier is exactly two gaps — expressions
have no type, and overload selection is a stub gated behind ascription. Slice 1 is
expression/operation type-checking, and it needs Q1. Q2 completes the operators
but blocks only its own later slice. Q3 is doc hygiene you flagged. Q4 is two
tooling confirmations before I add a new toolchain.

The one fact under all of Q1/Q2: an `Operator` (`Compiler/Resolution/Resolver.cs:1593`)
carries **precedence and a runtime `Func` only — no operand or result types
anywhere**. The whole operator table is `Builtin.Operators`
(`Compiler/Runtime/Values.cs:134`): `+ - * /`, `is`, `otherwise`, `@`. Slice 1
gives each a *typer* (a third half, seeded from the same one definition as its
meaning and precedence), and the typers below are what I need ruled.

---

## Q1 — the operator typings for Slice 1: `+ - * /`, `is`, `@` — BLOCKS Slice 1

**`+ - * /` — I propose `number × number → number`.** All numbers are one `number`
type to the checker (`fast` is a modifier stored beside the type, never inside it —
`CHECKERSCOPINGRULINGS` Q1), so `/` is `number × number → number` like the rest;
the numeric tower being deferred does not give the checker a second number type.

- **Confirm number-only.** The runtime is `Arithmetic("+", (l,r) => l + r)`, which
  on dynamic values *could* concatenate text. Is `+` ever text concatenation, or is
  concatenation a named function and `+` strictly numeric? I am assuming strictly
  numeric (Ronin has no operator overloading), so `"a" + "b"` is an operand finding.

**`is` — I propose `T × T → truth`, operands must unify.** `is` is the language's
one equality (`Values.cs:163`), structural, no subtyping.

- **Confirm require-unify.** `1 is "text"` — a type error (the operands cannot
  unify), or always-answerable `false`? I lean type-error: with no subtyping a
  cross-type compare can never be true and is almost certainly a mistake, which the
  language's teaching stance would rather catch than silently answer `false`.
  `error` unifies with anything already (`Sort.Error.Same` is unconditionally true),
  so a value of the bottom type compared with anything stays clean.

**`@` — I read `REAUDIT47RULING` §5 as already ruling this. Confirm my reading:**
`lookup K V @ K → optional V` (a miss gives `nothing`), and `list of E @ number → E`
(out-of-range stays a runtime `Error`, typed `E`). Three sub-questions the ruling
does not obviously settle:

- Is the **key/index type checked** — `m @ k` requires `k` unify with `K`, and a
  list index unify with `number`? (I assume yes.)
- Is **`text` indexable** (`text @ number`)? If so, to what — `text`, an
  `optional text`, a character type? (I assume **not** indexable for now — a finding
  — unless you say otherwise.)
- `@` on a **non-indexable** left (`5 @ 1`, `truth @ 0`): a finding — the existing
  `TypeMismatch`, or a new "not indexable" kind? (Programmer's call on kind; I raise
  it only because it is new surface.)

## Q2 — `otherwise` typing — BLOCKS its own slice only (not Slice 1)

The nuanced one, so Slice 1 explicitly leaves `otherwise` inferring `null` (today's
behaviour) until you rule. `otherwise` is the one `Catches` operator
(`Values.cs:189`): the right side runs **only when the left "catches"** (is
`nothing`/error). The guard idiom is `m @ k otherwise default` — `optional V`
falling back to a `V`.

- **Admissible left, and the result.** Is it `optional T otherwise T → T` (unwrap
  the optional when it is present, else the fallback)? Or does the fallback preserve
  the optional (`optional T otherwise optional T → optional T`)? Or both, unified?
- **The exit case.** `sum otherwise return 0` — the right operand *exits* and yields
  no value (this is the live guard idiom the `Unreachable` work preserved). I read
  the result there as the **left's caught-out type** (the `T` of an `optional T`),
  the right contributing no type because it never produces a value. Confirm.
- **A non-failing left.** `5 otherwise 0` — the left is a plain `number`, nothing to
  catch, so the right is dead. Is that a type error (`otherwise` wants something
  that can be absent on its left), or allowed with the right unreachable? This
  rhymes with the `Unreachable` dead-code finding; it may deserve one too.

## Q3 — execute the ruled-but-unrun `§8` supersession pass? — BLOCKS doc consistency

`CHECKERSCOPINGRULINGS` §8 **ruled four doc edits that were never executed**: strike
`EAGGREGATES2` §10, strike `GENERICSII` §8a, strike `NOTHINGANALYSIS` §D's modifier
claim, and add the `FIVERULINGS` §3 pointer to `OVERLOADS` §4. Because they never
ran, `OPENDECISIONS` §3 still argues **for** a separate type-table that `Q4` /
`FIVERULINGS` §4 **withdrew** — the one live contradiction the ledger surfaces, and
exactly the "facts drift silently" theme you raised.

- **May I execute the §8 pass now**, as ruled? The sensitive part: `OPENDECISIONS`
  is a **live `[V]` verdict**, so striking its §3 edits a standing verdict. I would
  strike the separate-type-table prose, mark it superseded by `CHECKERSCOPINGRULINGS`
  Q4, and regenerate/verify `LEDGER.md` — but editing a `[V]` is yours to authorise,
  not mine to assume. Say the word and it is a clean, ruled housekeeping commit.

## Q4 — two tooling confirmations before Slice 0 — BLOCKS Slice 0 (low-stakes)

- **A new toolchain in a .NET repo.** The VS Code client is a small TypeScript
  package (`vscode-languageclient`) with its own npm build. I propose it lives in
  `clients/vscode/`, and the canonical `{ id, extension }` home is a data file the
  manifest's language contribution is generated from — mirroring the generated
  goldens (`docs/reference.md`, `docs/reserved-words.txt`). Acceptable, or would you
  rather the extension live elsewhere / the client be VS Code-config only?
- **Fixture filenames.** Tests pass `"Player.ron"` / `"P.ron"` to `SourceText`.
  These are **opaque in-memory identifiers** — the compiler derives a
  `ModuleIdentity` from the path and never reads the extension — so they are not the
  `.ronin` fact and the drift-gate should not treat them as a consumer. I propose
  **leaving them as-is** rather than a churny rename. Confirm, or you want them
  migrated to `.ronin` for uniformity?

---

## Summary

| # | question | blocks | my read (yours to rule) |
|---|---|---|---|
| Q1 | `+ - * /` : `number×number→number`; `is` : `T×T→truth`, require-unify; `@` : per `REAUDIT47RULING` §5, key/index checked, `text` not indexable | **Slice 1** | as stated; confirm number-only `+` and require-unify `is` |
| Q2 | `otherwise` : `optional T otherwise T → T`; exit-right yields left's caught type; non-failing left maybe a dead-code finding | its own slice | leave inferring `null` in Slice 1 until ruled |
| Q3 | run the ruled `CHECKERSCOPINGRULINGS §8` supersession pass, incl. striking `OPENDECISIONS §3` (a live `[V]`) | doc consistency | may I execute it? |
| Q4 | client in `clients/vscode/`, extension single-sourced from a generated data file; leave `*.ron` fixture ids as-is | Slice 0 | as stated; confirm |

Nothing is built against any of these yet. Slice 1 starts the moment Q1 lands.
