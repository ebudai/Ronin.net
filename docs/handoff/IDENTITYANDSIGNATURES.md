# Named-type identity, and where signature sorts bind — two rulings before I build

> **Ledger** — `[R]` Actions the two architectural findings of `REAUDIT54` (no
> sign-off). Findings 2 and 4 are already fixed (`53a3825`); these two touch
> declaration identity and the overload ledger, so I want the design settled before
> the plumbing.
> answered by: NAMEDIDENTITY
> supersedes: none
> superseded by: none

**From:** the successor, at `53a3825`.

`REAUDIT54` withheld sign-off on four findings. Two were mine to just fix and are
done: the `Action` and `Variable` cases now exist, and an over-limit annotation
reports rather than vanishing. The other two are architectural, and each has a
decision inside it the rulings do not quite reach.

## 1. Named-type identity — what a declaration's identity IS, and how far it reaches

**The bug** (`REAUDIT54` finding 1, high). `Sort.Named` uses the spelling as
identity, so two distinct opaque types with the same name compare equal. Probed:

```ronin
function left  { type token; var x => token; }
function right { type token; var y => token; }
```

compiles clean, and the two retained `Named("token")` sorts compare **equal** —
though neither declaration is in the other's scope and they are distinct opaque
types. Q3 already rules the semantics: an opaque named type unifies only with the
**same declaration**. So `Named` must carry a declaration identity, not a spelling.
This is latent today (nothing consumes `Sort` equality yet) but becomes wrong the
moment unification, overload filtering, or a monomorphisation cache key does.

**What is available.** Every declaration already records its source span —
`Declared(name, span)`, `Declarations.cs:405`, including `type X;` — and inherited
declarations merge into a scope with their spans. So a stable per-declaration
identity exists without new bookkeeping: **the declaring span.** Within one
compilation it is unique per declaration; across files it differs by file, so it
distinguishes cross-module declarations too. Two *separate compilations* of the
same source coincide, but nothing ever compares types across compilations, so that
is not a case that arises.

**Q1a — is the declaring span the identity?** *Recommendation: yes.* It
distinguishes the sibling scopes above and, being file-plus-offset, the
cross-module case the audit asked about; it needs no new field. The alternative is
a monotonic symbol id assigned at declaration, which is more machinery for the same
distinction. Confirm the span, or name the id you would rather have.

**Q1b — how far does identity reach now?** The audit asked for "preferably a
cross-compilation/module identity test." *Recommendation: build within-compilation
identity now (the span), and defer the import question — which declaration is
visible where — to the module design, since no imports exist to test against.* The
span already makes two same-named types in two files distinct; what modules add is
which of them a third file sees, and that is theirs to rule. Confirm the deferral.

**Q1c — through the resolved name, or beside it?** The audit's phrasing is "carry
identity from the symbol table through the resolved name into the semantic type" —
i.e. onto `Node.Name`. That also fixes value-name identity, which overload
resolution will want. But `Node.Name` is the resolver's derivation identity, shared
with repair and the ambiguity machinery, so changing it is a core change with a
wide blast radius. The lighter path is `Sort.Of` reading the declaring span from
the scope's declarations for a named type only — types, low blast radius, and it
fixes finding 1 exactly. *Recommendation: the lighter path now; promote it onto
`Node.Name` when value-name identity is actually consumed.* Tell me if you would
rather pay for the core change once.

## 2. Signature sorts — step 1 as ruled, or step 2 by a superseding ruling

**The bug** (`REAUDIT54` finding 3, medium). A signature still carries its
parameter **spellings**, not resolved sorts (`Declarations.Signature.Types` is a
block of strings), and duplicate classification length-prefixes those strings.
Probed:

```ronin
function use (x => number) { }
function use (x => (number)) { }
```

reports one `Overloaded` and no `DuplicateSignature`, because the spellings differ —
though both annotations resolve to the same `Sort.Scalar("number")`. Under equality
unification these are the **same** signature, and the distinction matters to the
ledger: the `Overloaded` refusal is recorded to expire into use-site selection,
while a true `DuplicateSignature` must survive. So classifying by spelling puts a
declaration in the expiring bucket that belongs in the permanent one.

**What the fix is.** Bind each resolved parameter and return sort to its owning
signature, keep the spelling beside it for presentation, and classify duplicates by
sort. Same parameter sorts → `DuplicateSignature` (never expires); different →
`Overloaded` (expires). That is exactly the `Test/Expiry.cs` partition, so the fix
aligns the classifier with the ledger rather than fighting it.

**The decision.** `SEMANTICCHECKERSCOPING` §5/§6 put "signatures carry `Type`s
beside the spelling" in **step 1**, and §9 said take that order as written — so the
governing order is step 1. But the audit expressly invited a superseding ruling if
you would rather this land in step 2, whose stated content is initializer and
return mismatch. It is genuinely adjacent to both: it is a storage-and-identity
change (step 1's theme) that first pays off in classification (overload's theme).

**Q2 — step 1 now, or step 2 by a superseding ruling?** *Recommendation: step 1,
now.* It is a present, observable misclassification, not a future integration
preference, and the fix is small and ledger-aligned. But it is your order to
change; if step 2 is where you want it, say so and I will record the superseding
ruling and leave the spelling classifier until then.

## What I need

Q1a (span as identity), Q1b (defer the import reach), Q1c (lighter path vs core),
and Q2 (step 1 or step 2). With those I implement both findings and return the
whole of `REAUDIT54` for sign-off. Nothing here is built yet; the working tree is
the two-and-four fix and this memo.
