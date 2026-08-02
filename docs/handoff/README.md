# What this folder is — and what to read instead

**This is correspondence, not specification.**

It is a chronological record of design proposals, audit findings, and answers.
Later documents override earlier ones **silently**: a proposal that was accepted,
amended, or abandoned reads exactly the same as one that still stands. Several
of the documents here describe language rules that no longer exist.

## Read these instead

| for | read |
|---|---|
| what the language is | `docs/spec/` |
| how to write it | `docs/guide/` |
| what is reserved | `docs/reserved-words.txt` — generated, and gated by a test |
| what the compiler does | the code, and `Test/` — every rule below is a test |
| *why* a rule is what it is | this folder, with the table below |

The spec and guide are maintained. This folder is not, and is not meant to be.

## What has been superseded

Read the right-hand column before quoting the left.

| document | status |
|---|---|
| `WHENANDWAIT.md` §1 | type scope **amended** — `WHENTYPESCOPE.md` §1. Module scope only; type scope is designed and blocked on instances |
| `WHENANDWAIT.md` §4 | `stop` **superseded twice** — `WHENTYPESCOPE.md` §3, then `DIRECTIONPACKET.md` §2 |
| `WHENANDWAIT.md` §5.3 | restart-versus-ignore **deleted** — `CHAINACTIVATIONS.md` §3. Runs are counted; there is no policy |
| `WHENANDWAIT.md` §7 | cutoff on `Recompute` was **already implemented** when written |
| `STOPALL.md` | **superseded entirely** — `DIRECTIONPACKET.md` §2. There is no `stop all` |
| `CHAINACTIVATIONS.md` §3 | stands, except `stop` — see `DIRECTIONPACKET.md` §2 |
| `DIRECTIONPACKET.md` §4 | **refuted by measurement** — `QUEUEDEPTH.md` §2. The round limit did not catch accumulation |
| `ACCUMULATIONBOUND.md` §1 | premise **refuted** — `QUEUEDEPTH.md` §2. A deep queue could not drain |
| `WAITSEMANTICS.md` §2 | naming **obsolete** — nothing generated is typed, so the names are reports |
| `EMPTYBRACKETS.md` §2 | `(_)` as source syntax **retracted** — `UNDERSCORE.md`. `_` is not in the language |
| `GLUEREGISTRY.txt` | a seed study, **superseded** by the generated `docs/reserved-words.txt` |
| `IFASEXPRESSION.md` | **not started** — no code corresponds to it |

## The settled `when` model, in one place

Because it is spread across six documents and was summarised wrongly once:

- a `when` is **module-scoped**; inside a type it is refused by name, pending
  instances
- a `when` body may **wait**, and `n` waits compile to `n + 1` `when`s and `n`
  **counts**
- **runs are counted**, not held to one at a time — there is no restart, no
  ignore, and no value an author has to name
- a wait is **level**: one whose condition is already true proceeds
- **`return`** ends one run and leaves the `when` armed
- **`stop`** disarms the `when` — there are **two** words, not three
- runs are taken **one per round**, and rounds that consume — or defer — a run
  the step inherited do not count against the round limit
- accumulation is watched by **draining, not depth**, over an adjustable window

All of it is in `docs/spec/grammatical-structure.md` §4.5.5, and all of it is
tested in `Test/Unit/Waiting.cs`.
