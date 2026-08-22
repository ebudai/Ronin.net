# Re-audit 68 — name collision closes, but same-run nested returns are missed

> **Ledger** — `[A]` Audit of `d908c25..acb0aea`, requested by
> `FORAUDIT68`. REAUDIT67's multi-word-name witness is repaired. **Not signed
> off:** one adjacent medium-severity finding remains—the word-run guard assumes
> every call starts a word run, but an unparenthesized pattern argument can be a
> nested `return (_)` inside that same run.
> supersedes: none
> superseded by: none

## Audit result

The requested repair holds. A medial `return` inside the declared names
`customer return policy` and `annual return summary` no longer marks an
unresolved enclosing statement as `Answering`. The original failing witness,
its resolved control, and the equivalent name without `return` all produce the
expected `Unanswered`. The prior high-severity silent contradiction is closed.

The new boundary is not equivalent to the resolver's expression structure,
however. A name is a maximal word run, but an expression is not: a pattern's
hole can consume another pattern call without parentheses. Thus `send return 5`
is resolved as `send (return 5)` even though the `return` anchor follows the word
`send`. The resolved `Called` walk sees that nested answer. When its value is
unknown, the tree disappears and the lexical fallback rejects the same anchor
solely because its preceding lexeme is a word.

## Finding 1 — medium — an unresolved unparenthesized nested return is misreported as no return

**Locations:** `Compiler/Compilation.cs:516-532`, where `Answering` requires the
anchor to start a word run; `Compiler/Compilation.cs:926-940`, where a false flag
allows `Unanswered`; `Compiler/Compilation.cs:1150-1164`, whose resolved-tree
walk detects returns at every depth; and `Compiler/Resolution/Resolver.cs:833-843`,
where a pattern's trailing hole consumes an expression extending to the end of
the span.

The new condition includes:

```csharp
k is 0 || lexemes[k - 1].Kind is not LexemeKind.Word
```

That distinguishes `customer return policy` from a parenthesized
`send (return nope)`, but it also excludes the valid unparenthesized composition
`send return nope`. Pattern nesting supplies an expression boundary that is not
a lexical word-run boundary.

### Witness

```ronin
function send (x) { return x; }
function f => number { send return nope; }
```

**Actual:** one `Unanswered` at `f`.

**Expected under the maintained cascade policy:** no `Unanswered`, matching
direct `return nope` and parenthesized `send (return nope)`. The body
syntactically attempts a value-return; only that value fails to resolve.

The resolved control demonstrates that this is accepted language syntax rather
than a speculative token interpretation:

```ronin
function send (x) { return x; }
function f => number { send return 5; }
```

It compiles cleanly. Because the lexical `Answering` flag is false for this
source too, the clean result comes through the resolved tree and its nested
`SymbolTable.Answer` call. Replacing only `5` with `nope` removes that tree and
changes the compiler's account of the same expression structure.

The severity is medium, as in REAUDIT66: this is an inaccurate extra diagnostic
on already unresolved source, rather than a missed contradiction in otherwise
resolved source. It nevertheless violates the explicit cascade policy and makes
diagnostics depend on whether a nested return's value resolves.

### Repair direction and regression coverage

Preserve the name protection from REAUDIT67, but do not equate expression starts
with starts of lexical word runs. The classifier needs enough grammatical or
partial-resolution evidence to distinguish a declared multi-word name from a
pattern anchor beginning inside another pattern's argument. A flat adjacency
scan plus either “every anchor” or “word-run-start anchors” cannot represent both
cases.

Maintain all current controls and add at least:

- resolved `send return 5`, proving unparenthesized nested calls remain legal;
- unresolved `send return nope`, matching the cascade result of
  `send (return nope)`;
- the same two cases nested another level without parentheses; and
- the repaired declared-name witness, ensuring the solution does not restore
  REAUDIT67's false positive.

## Disposition of REAUDIT67

| Prior finding | Reassessment |
|---|---|
| `return` inside a legal multi-word name suppressed `Unanswered` | **Closed.** The original witness and requested causal controls now pass. The replacement word-run boundary introduces the distinct false negative above. |

## Verification performed

- Reviewed `FORAUDIT68` and the production/test diff for
  `d908c25..acb0aea`.
- Locked restore: clean.
- Debug and Release builds with `-warnaserror`: clean, zero warnings and errors.
- Maintained Release and Debug suites: `1332` passed, `0` failed in each.
- Release coverage gate: `100%` line and `100%` branch for `Ronin` and
  `Ronin.Server`.
- Changed-file `dotnet format --verify-no-changes`: clean.
- `git diff --check`: clean before this report.
- The failing unresolved witness and resolved control were executed as temporary
  xUnit integration tests; the probe file was removed after capture.

The requested high-severity finding is closed and the repair protects legal
multi-word names. Signoff should wait until the unresolved path also recognises
valid unparenthesized nested return calls.
