# Re-audit 59 — Registered membership and inherited overload sets

**Audited:** `213a7ee..e9579aa`, principally `e9579aa`, against `REAUDIT58`,
`REAUDIT57`, `SCOPING`, `SCOPEIDENTITYRULING`, and
`CONTAINERIDENTITYRULING`.

**Date:** 2026-08-16

## Result

**Not signed off. One high-severity and one medium-severity finding remain.**

`REAUDIT58` itself is correctly repaired. Named bodies now join a shared table by
the exact registered `Pattern`, established through the owning signature's source
span. A structurally refused function is independent, so its local types no longer
enter a valid same-word overload set; the original witness now reports both
`EmptyHole` and `UnknownType`, and the valid signature retains a null parameter sort.
Type bodies likewise bypass function-overload classification.

The adjacent inherited-scope path is not correct. `Declarations` deliberately
merges enclosing overload signatures inward, but `Compilation.Scope` decides
whether to classify from the number of **local bodies**. One local declaration of
an inherited pattern therefore takes the singleton fast path and is never classified
with the inherited signature. Exact duplicates and current type-selectable overloads
both compile clean.

When two local bodies do trigger the shared path, the inherited signatures are
classified, but only after `Declarations.Of` has re-resolved them against the inner
container's type table. That discards the owning sort stored on the inherited
signature and can collapse two distinct H-identified named types into one permanent
duplicate.

---

## Findings

### 1. High — a single local declaration of an inherited pattern bypasses classification

An inner `Declarations` copies every enclosing overload list
(`Compiler/Grammar/Declarations.cs:89-105`), then adds local declarations to the
same list. Thus a locally declared `use (_)` and an enclosing `use (_)` are one
visible candidate set in the inner scope, as the flat inward-visibility design
requires.

`Compilation.Scope`, however, constructs `sets` only from bodies physically present
in the current statement list (`Compiler/Compilation.cs:211-239`). Its singleton
branch tests `bodies.Count`, recurses the one local body, and continues without
calling `Classify` (`:245-262`). It never asks how many signatures are in
`declared.Overloads[pattern]`.

The permanent duplicate witness compiles with no findings:

```ronin
function use (x => number) { return x; }
function outer {
    function use (y => number) { return y; }
}
```

Inside `outer`, the same shape with the same semantic parameter sort is declared
twice. No argument type can choose between those declarations, so this is
`DuplicateSignature`, the non-expiring rule. Current `Compilation.Findings` is
empty.

The distinct-sort control also compiles clean:

```ronin
function use (x => number) { return x; }
function outer {
    function use (y => text) { return y; }
}
```

Under the current pre-selection phase this is `Overloaded`; the refusal is ledgered
to expire only when type-directed selection exists. It is absent too.

This is not uncertainty about the classifier's answer. Building the same outer and
inner `Declarations` directly and invoking the existing `Classify` produces
`DuplicateSignature` for the first pair and `Overloaded` for the second. The full
compilation path simply never invokes it.

Classify whenever the current scope contributes a registered declaration to a
pattern whose visible signature set has more than one member, not only when the
current statement list contains several bodies. The local body's signature must be
resolved in its owning table first; then classification can include the already
resolved inherited candidates. Do not classify purely inherited sets again in every
child scope—local membership is the trigger, visible signature count is the set.

Maintain both witnesses. The duplicate case is essential because it guards the
permanent safety rule; the number/text control guards the temporary rule and its
eventual expiry boundary.

### 2. Medium — inherited signatures are rebound to the inner overload container

`Declarations.Of` copies inherited signatures with their stored sorts, but then
immediately maps **every** signature through `declarations.Resolved`
(`Compiler/Grammar/Declarations.cs:94,119-126`). The resolver belongs to the new
inner table. Inherited parameter and return spellings are therefore interpreted
again as though they had been written in the inner scope, contrary to the signature
store's purpose and the comments on `Sorted`: the checker and classifier are meant
to read the semantic sorts already resolved at the declaration's owner, not resolve
the words again.

The error becomes observable when two local bodies cause the shared classifier to
run:

```ronin
function use (x => token) {
    type token;
    return x;
}

function outer {
    function use (x => token) {
        type token;
        return x;
    }

    function use (x => number) { return x; }
}
```

The first `token` is `Named(module/use, token)`. The inner one is
`Named(module/outer/use, token)`. `SCOPEIDENTITYRULING` makes those different sorts,
and `number` is a third sort. The visible set therefore has three distinct groups:
under the current phase it should produce only an `Overloaded` finding with count
three.

Current behavior reports:

- a false `DuplicateSignature` joining the module-level and inner `token`
  declarations; and
- an `Overloaded` finding over only two groups.

The inherited spelling `token` was rebound through the inner shared table, turning
its correct `Named(module/use, token)` into `Named(module/outer/use, token)`. This is
the identity collapse the named-container work was designed to prevent, arriving
through re-resolution rather than equality.

The complementary `A, A, B` witness is wrong in the other direction: an inherited
outer `token` plus two equivalent inner `token` spellings currently produces one
three-site duplicate and no overload. Correct classification is a duplicate for the
two inner declarations **and** an overload between their group and the distinct
outer type.

Preserve the semantic sorts on inherited signatures. Resolve only signatures
introduced by the current declaration owner, and for a B overload container resolve
only that exact registered body's signatures against its shared type table. A
combined visible candidate set may span several named containers; classification
must compare each candidate's stored owning sorts, never reinterpret all spellings
through whichever container is currently being entered.

Maintain the three-distinct-groups witness and the inherited-distinct plus
inner-duplicate control. Assert diagnostic kinds, group counts, and related sites so
a later implementation cannot obtain the right count by joining the wrong
declarations.

---

## Disposition of `REAUDIT58`

| Prior finding | Reassessment |
|---|---|
| rejected function body participates in a valid overload container | **Closed.** Membership is keyed by the registered pattern found through the owner span. A refused function is recursed independently; it does not donate types, suppress `UnknownType`, or populate the valid signature's sort. The maintained test asserts both diagnostics and the null stored sort. |

## What the implementation gets right

- Registered bodies with the same structural `Pattern` still share one B container,
  including body and ancillary types.
- Same rendered words are no longer treated as proof of overload-set membership.
- A structurally refused function remains available for diagnostics inside its own
  body without contaminating a registered sibling.
- Type bodies and refused owners take an independent named-scope path and never
  reach `Classify` with a null or unrelated pattern.
- The former cross-body valid-source behaviors remain intact: shared type visibility,
  cross-signature resolution, cross-body uniqueness, and equivalent-spelling
  duplicate classification all passed targeted controls.
- The implementation removes the previous late nullable pattern search; a shared
  set carries a non-null registered pattern by construction.

## Verification record

Temporary audit probes were removed from the worktree before this report was
written.

- Inspected the complete `213a7ee..e9579aa` production and test diff. No newer
  designer ruling was present in the handoff folder.
- Re-ran the exact `REAUDIT58` witness through `Compilation.Of`: it now reports
  `EmptyHole` and `UnknownType`, and the registered signature's parameter sort is
  null.
- Probed registered overloads alongside refused same-word functions, independent
  type bodies, nested overload sets, inherited singleton duplicates, inherited
  singleton distinct sorts, and inherited named types combined with two local
  bodies.
- Confirmed the missed singleton answers by invoking `Declarations.Classify`
  directly: `DuplicateSignature` for number/number and `Overloaded` for number/text.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror`:
  **passed**, 0 warnings and 0 errors.
- Release coverage gate from `.github/workflows/build.yml`:
  **1,281 passed, 0 failed, 0 skipped**; `Ronin` and `Ronin.Server` each report
  **100% line, 100% branch, and 100% method coverage**.
- `git diff --check` and `git show --check HEAD`: **clean**.

The only worktree additions visible before and after the audit are the existing
untracked handoff documents plus this report; no production or maintained test file
was changed by the audit.
