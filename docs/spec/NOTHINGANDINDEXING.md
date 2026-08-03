# `nothing` and `@` — the gap is real, and one of them was never written down

Budai asked which documents define these. Checked, and the honest answer is that
one is a **proposal I let later documents treat as settled**, and the other has
**no document at all** — it was decided in conversation months ago and never
written. Both are my fault, and the second is load-bearing for four recent
documents.

---

# 1. `nothing` — decided long ago, specified nowhere

`optional` **is** in the spec — `lexical-structure.md:20` lists it as a keyword
and `grammatical-structure.md:6` has it as a modifier. Its companion value never
arrived: every occurrence of "nothing" in `docs/spec/` is the English word in
prose.

So the programmer is right to say he has no definition. There is not one. What
follows consolidates decisions taken in conversation, marked by provenance so he
can see which are settled and which are recent consequences.

## 1.1 Settled earlier, never written

- **`nothing` is a value, not a null pointer.** It is the built-in constant for
  "no value", chosen over `null` deliberately.
- **`optional T` is the type** that admits it: `var x => optional number` holds a
  number or `nothing`.
- **An optional parameter defaults to `nothing`** when the caller omits it.
- **`otherwise` catches both `nothing` and `Error`.** One operator for both,
  with `if (x is error)` kept for the rare case that must distinguish them.
- **`nothing` does not propagate silently through arithmetic.** `x + 1` where
  `x` is `nothing` is an **Error**, not `nothing`. This is the important one: it
  is what stops a missing value from quietly becoming a wrong answer three
  computations later.

## 1.2 Consequences added by recent documents

These follow from the above, and each is load-bearing for something already
proposed:

- **A lookup miss yields `nothing`** — `MATCH.md`. This is what makes
  `] otherwise 0` not be match syntax.
- **`if c { a }` with no alternative is `optional T`**, and is `nothing` when `c`
  is false — `IFASEXPRESSION.md` §3. This is what makes
  `if c { a } otherwise { b }` the existing postfix operator rather than a new
  form.
- **Exhaustiveness is the absence of `nothing`** — `MATCH.md` §3. Arms covering
  every case give `T`; arms missing one give `optional T`, so omitting
  `otherwise` is a type error rather than a separate analysis.

**Nothing above is new.** But three of the last four design documents rest on it
and it exists only as prose in those documents, which is exactly the shape this
project keeps finding and correcting. It should go into the spec beside
`optional`, not into the handoff folder.

---

# 2. `@` — a lean, not a decision, and later documents pretended otherwise

`INTERVALSANDINDEXING.md` §1 is the only place it appears, and what it actually
says is:

> I lean `@`, on the grounds that misreading a line as a comment is a worse first
> impression than losing a sigil we have no plan for. **But it is close**, and
> the 1-based argument for `#` is real.

That is a recommendation awaiting a decision. `MATCH.md` and `MATCHNAMED.md` then
used `@` as though it were settled — `car garages @ the datsun` — which is how a
proposal becomes a fact without anyone deciding it. Same failure as the
left-recursion comment, one week later.

**So this needs a call, not a document.** The choice, restated:

| | reads as | risk |
|---|---|---|
| `list @ 4` | "list at 4" | `@` is the conventional annotation sigil; taken if Ronin ever wants annotations |
| `list # 4` | "list number 4" — exactly right for 1-based | `#` starts a comment in shell, Python, Ruby and Make, so a newcomer's eye may read the rest of the line as dead |

What *is* settled and can go to the programmer regardless: indexing moves off
`[ ]` to a **symbol**, because any word-spelled indexer would make its glue a
reserved word and end `RESERVED (0)`; the symbol binds tighter than arithmetic,
so `list @ 4 + 1` is `(list @ 4) + 1`; and lists are 1-based with closed
intervals, which is a coherent pair.

---

# 3. What to send

| for | send |
|---|---|
| `nothing` | **this document**, §1. There is no other. It belongs in `docs/spec/` beside `optional` rather than in the handoff folder |
| `@` | `INTERVALSANDINDEXING.md`, with §2 above attached so it is read as an open choice. The symbol-not-a-word part is decided; the character is not |

And a note for the supersession table: `MATCH.md` and `MATCHNAMED.md` use `@`
illustratively. If the character changes they are unaffected in substance — the
argument is about indexing, not about the glyph.
