# SUPERSEDED — see `AMBIGUITY-AS-ERROR.md` §3a

**Do not implement from this document.** Its three-row ownership table was wrong
and has been replaced by a four-row one in `AMBIGUITYASERROR.md` §3a. Every
other point it made has been folded into §3a and §8 of that document, so nothing
here is still live.

---

## What was wrong

The three-row table said a **particular** collision is always reported against
the source declaration. That loses declaration ordering when the rival pattern
was introduced *later*:

```ronin
var accounts => Number;
for each (bank account) in accounts {
    function index of bank (x => Number) { return x; }
    return index of bank account;
}
```

Renaming `bank account` fixes it — so the collision is particular — but the
pattern was declared afterwards and respelling *it* fixes it too. `SCOPING.md`'s
standing convention blames the later declaration, and my table pointed at the
one that was correct when it was written.

The replacement splits the particular row by what the rival is: a **built-in
operator** (only the name is actionable) versus a **pattern** (both are, so the
later declaration is blamed). See §3a for the full table and the invariant behind
it.

## What moved, and where

| point | now lives in |
|---|---|
| source/shadow suppression (the doubling he hit) | §3a, row 1 |
| universal vs particular ownership | §3a, rows 2–4 |
| `REAUDIT46` 2–3 as a prerequisite for removing the exemption | §8, steps 5–6 |
| why the exemption is diagnostics debt rather than soundness debt | §3a |
| the comment marking the open half | §3a |

## The process note this makes for itself

This is the third documentation drift on this thread — my spec sentence quoted
as an implementation property, a failing probe cited as proof, and now a
superseded table sitting alongside its own correction. Same shape every time: a
claim living in a second place, where it survives the fix.

> **One document owns each decision. Others link to it rather than restating
> it.**

Which is why this file is now a stub and not a synchronised copy: two tables
that agree today are two tables that disagree later, and nothing announces it
when they do.
