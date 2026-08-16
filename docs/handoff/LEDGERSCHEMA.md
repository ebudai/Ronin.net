# The ledger header — the schema, its legal values, and the parse contract

> **Ledger** — `[R]` Fixes the field names and legal values `LEDGERRULING` §7 requires
> before Pass 1, and the parse contract the generator reads. `LEDGERRULING` §6 made
> supersession a field; §1/§2/§3 gave the marker rules; `ANSWEREDEDGE` added the
> paired answer edge. P1–P3 below are the successor's resolution of three gaps the
> ruling left, open to the designer.
> supersedes: none
> superseded by: not yet checked

## The schema

Every design memo and ruling opens with a **ledger header**: a leading blockquote,
before the first prose paragraph, of exactly this shape.

```
> **Ledger** — `[V]` <one line: what this decides, or what it asks>
> answers: <memo(s)>          ┐ the answer edge — at most one side, present only
> answered by: <ruling>       ┘ when the document is in an answer relation
> supersedes: <docs/sections | none | not yet checked>
> superseded by: <docs | none | not yet checked>
```

The two supersession lines are always present; the answer line appears only when
the document asks or answers. Every fact is scannable without judgment — the whole
requirement `CHECKERSCOPINGRULINGS` §8 stated.

## The marker

Exactly two tokens, backtick-wrapped:

| marker | means | who may write it |
|---|---|---|
| `[V]` | **verdict** — the design binds this | the designer certifies it (§2) |
| `[R]` | **recommendation** — a proposal, not yet binding | any successor, freely |

- **A document's marker states its *strongest* claim** (§1). If any part binds it is
  `[V]`, and the one-liner names the part that does not. The costs are asymmetric: an
  `[R]` on a document that actually binds invites a later memo to overturn a settled
  decision; a `[V]` on a mostly-proposal costs a reader one sentence.
- **A marker is earned by a citable ruling event, not by tone** (§2). A `[V]` exists
  because a ruling document was written and relayed — that record is the evidence.
  **A document with no citable ruling event is `[R]` by default**, which makes the
  proposal pass safe in one direction: the designer only ever promotes `[R]→[V]`,
  never has to catch a wrong `[V]`, because a wrong `[V]` cannot be written without a
  citation that does not exist.

## The answer edge

A memo asks; a ruling answers. That relation is **paired and distinct from
supersession** (`ANSWEREDEDGE`), because the two tell a reader to do opposite
things:

| edge | what the reader does |
|---|---|
| `superseded by X` | read **X instead** — this is struck |
| `answered by X` | read **X as well** — X governs, and X is where the verdict is |

`answered by X` means **X governs** — uniformly whether the ruling confirmed,
modified, or rejected the memo. A reader who follows the edge gets the truth in
every case, so "answered and rejected" needs no fourth state; shades stay in the
one-liner. A memo or consultation package carries **`answered by: <ruling>`**; the
ruling carries **`answers: <memo(s)>`**; a document in no answer relation carries
neither.

The edge is **filled in Pass 1, not Pass 2** — the pairs are the relay record, not
archaeology, so a known pair is a stated fact and never a `not yet checked`. Moving
them into Pass 1 takes ~20 documents off Pass 2's worklist; Pass 2 keeps only the
genuine archaeology of which claim struck which.

## The supersession fields

Both fields are **lifecycle edges**, orthogonal to the marker — a `[V]` that has
been superseded *was* binding and is not now (§6). Legal values:

| value | meaning |
|---|---|
| `none` | walked, and nothing stands in this relation |
| `not yet checked` | **not walked** — Pass 2 has not reached this document |
| a reference list | `DOC`, or `DOC §n` for a section-level (partial) edge; comma-separated for several |

`none` and `not yet checked` are **different facts and never share a spelling** (§3):
an empty-looking field must not read as "nobody found anything" when it means
"nobody looked." Pass 1 writes `not yet checked` wherever it has not walked, and the
set of documents still reading `not yet checked` *is* Pass 2's worklist — generated,
not remembered.

A section qualifier marks a **partial** edge: `superseded by: REAUDIT47RULING §5
(§8, §10)` means those sections are superseded and the document otherwise stands, so
it still binds. A bare `DOC` with no qualifier is a whole-document supersession.

## What does *not* get a header

- **Audit reports** (`REAUDIT*`, `AUDIT*`, `FRESHAUDIT*`, `CODEREVIEW`) keep their
  native `**Audited:** / ## Result` form. The `[V]`/`[R]` axis is authority over the
  design; an audit *observes* — it carries **disposition**, not supersession (§4).
- **Tooling scripts** (`*.py`) — no prose to mark.

## Granularity

Header-level for every design document. **Inline `[V]`/`[R]` marks on individual
claims are required only where one document's claims genuinely differ in status and a
consumer must tell them apart** (§5) — which is why `SEMANTICCHECKERSCOPING` carries
them. Do not inline-mark single-decision memos.

## The parse contract (for the generator)

A ledger header is the leading blockquote whose first line matches
`^> \*\*Ledger\*\* — `. It runs over consecutive `>` lines until the first non-`>`
line. Within it:

- the **marker** is the first `` `[V]` `` / `` `[R]` `` token on the opening line;
- the **Ledger line** is everything from `— ` up to the first field line (it may
  soft-wrap over `>` continuations);
- **answers** / **answered by** are the values after `^> answers: ` / `^> answered by: `;
- **supersedes** is the value after `^> supersedes: `;
- **superseded by** is the value after `^> superseded by: `.

The generator's **first job is edge reciprocity** (`ANSWEREDEDGE` §3): every
`answered by: X` naming a document must be matched by an `answers:` on X naming it
back, and vice versa. A dangling or one-sided answer edge is a **defect**, as is a
design document (not an audit report or script) with no header, or one whose fields
are missing or hold an illegal value — the header's fact has a consumer, which is
what keeps it true (§7). Supersession reciprocity waits until Pass 2 fills those
fields.

## P1–P3 — three gaps the ruling left, resolved here

`LEDGERRULING` §6 gave the schema in a code block that differs in three details from
the fourteen headers already on disk (and from the ruling's own header). A generator
parser keys on these, so they are fixed here, as the successor's resolution, open to
the designer's adjustment:

| | gap | resolved |
|---|---|---|
| **P1** | marker delimiter — bare `[V]` in the §6 block, backtick-wrapped everywhere real | **backticks**: `` `[V]` ``. The block omits them inside a fence |
| **P2** | a kind word ("verdict"/"advisory") after the marker | **dropped**: the marker is the kind; the one-liner carries the genre |
| **P3** | `supersedes` / `superseded by` on one line joined by `\|`, or two | **two lines**, per the normative §6 block |
