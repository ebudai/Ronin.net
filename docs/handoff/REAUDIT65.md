# Re-audit 65 — the direct repairs hold, but the ASCII boundary hangs and the return check remains bypassable

> **Ledger** — `[A]` Audit of `26a6923..740cb18`, requested by
> `FORAUDIT65`. **Not signed off:** two high-severity findings and one medium.
> REAUDIT64's direct nullary, written-return, and evaluator witnesses are repaired,
> but the ASCII numeral change leaves the full lexer unable to consume rejected
> Unicode digits, the written-return diagnostic is suppressed by any unrelated
> unresolved reading, and a refused nullary declaration is still installed as the
> resolver's authoritative call.
> supersedes: none
> superseded by: none

## Audit result

The central nullary repair is sound for accepted declarations: a bare nullary
reference now resolves once to `Node.Call`, so dependency edges, recursive-group
detection, type inference, composite-keyword names, and runtime invocation all
read the same node. The four original REAUDIT64 witnesses pass. A written
value-return function with a fully resolved bare-return or fall-through body now
reports `Unanswered`. An ASCII numeric token no longer reaches the evaluator's
former Unicode parse mismatch.

I cannot sign off the range. The last ASCII commit tests `Literal.Lex` directly
but not the full lexer loop; the rejected digit is consumed by no token and hangs
the compiler. The `Unanswered` cascade guard is body-wide instead of limited to
an unresolved attempted value return, allowing an unrelated unknown statement to
erase the finding and leave a contradictory signature clean. Finally, nullary
registration proceeds after `Cell` refuses a declaration, allowing invalid source
to alter the semantic table used by otherwise valid references.

## Finding 1 — high — rejecting a Unicode digit as numeric leaves the lexer in a non-progress loop

**Locations:** `Compiler/Lexicon/Literal.cs:90-117`,
`Compiler/Lexicon/Word.cs:11-24`, and `Compiler/Lexer.cs:12-28`.

`NUMERALALPHABET` correctly rules source numerals to ASCII `0-9`, and `Numeric`
now rejects Arabic-Indic `١`. But the fallback `Word.Lex` still rejects every
`char.IsDigit`, including that same character. It is neither punctuation nor a
Unicode symbol, so every lexer in the chain returns null without advancing the
cursor. `Lexer.Lex` then repeats its `while` loop at the same position forever.

### Witness

```csharp
Lexer lexer = new("١");
lexer.Lex();
```

**Actual:** the filtered test did not return within an eight-second timeout. The
loop is statically non-progressing: `cursor` remains zero and the loop condition
remains true. Any source that reaches such a digit outside a complete token the
date lexer accepts has the same problem, including an ASCII prefix followed by a
Unicode digit.

**Expected:** the full lexer consumes every input character and terminates. This
is also the explicit lexical-analysis invariant: every character belongs to
exactly one token and the lexer does not produce errors.

The maintained test at `Test/Unit/Literals.cs:102-115` calls only
`Literal.Lex`, so `Assert.Null` proves the numeral decision while bypassing the
required fallback/progress path. Add a full `Lexer.Lex` regression—and preferably
a compilation-level termination regression—for a lone Unicode digit, one after
an ASCII numeral, and mixed scripts.

The repair must make the lexer chain exhaustive after narrowing `Numeric`.
Aligning `Word`'s leading-digit exclusion with the ruled ASCII alphabet is one
possible classification; an explicit fallback token is another if Unicode digits
must not be names. Whichever classification is chosen, rejected-as-numeric cannot
mean rejected-by-every-token.

This replaces REAUDIT64 finding 4's evaluator exception with an earlier and more
severe front-end hang. The evaluator path itself is closed for tokens that really
are `Numeric`.

## Finding 2 — high — any unrelated unresolved reading suppresses `Unanswered`

**Location:** `Compiler/Compilation.cs:907-920`.

The repair correctly detects that a written `=> T` with no value-carrying return
contradicts its signature. To avoid stacking that finding on an unresolved
`return nope`, it suppresses `Unanswered` when **any** reading owned by the
function fails to resolve:

```csharp
if (checks.Where(check => ReferenceEquals(check.Owner, function))
          .SelectMany(check => check.Read)
          .Any(reading => reading.Resolution.TryTree(out _) is false))
    yield break;
```

That condition is much wider than the reason for it. An unrelated unknown
statement cannot carry a value out of the function, but it disables the check.

### Witness

```ronin
function f => number { nope; return; }
```

**Actual:** zero findings.

**Expected:** `Unanswered` at `f`. The body has a definite bare return and no
attempted `return (_)`; `nope` cannot satisfy the declared answer.

The implementation comment says the unresolved name has “its own finding,” but
value-side `NoParse` currently has no such diagnostic—the maintained suite itself
expects `function f => number { return nope; }` to be clean—so the broad guard can
turn the whole function into a clean compilation, not merely reduce a diagnostic
cascade.

Carry enough information to distinguish an unresolved reading that syntactically
may be `return (_)` from unrelated unresolved body text. For example, record the
return intent/arity beside `Reading` before resolution or restrict the guard to a
failed reference whose canonical form can be the answer pattern. Maintain:

- the witness above, with the unrelated reading before and after the bare return;
- the same case in a transparent nested block;
- a fully empty and a fully bare-return body;
- a valid value return; and
- the intended unresolved `return nope` control, if suppressing that one remains
  the policy.

The two direct REAUDIT64 finding-3 witnesses are closed, but the same semantic
contradiction remains silently reachable through this guard.

## Finding 3 — medium — a nullary declaration refused by `Cell` is nevertheless installed as a call

**Locations:** `Compiler/Grammar/Declarations.cs:406-453` and
`Compiler/Grammar/Declarations.cs:494-505`; the resulting call takes precedence at
`Compiler/Resolution/Resolver.cs:227-241`.

The name-shaped route calls `Cell(member)`, but `Cell` returns `void`. When
`Refused(name, span)` rejects a supplied or already-declared name, control returns
only from `Cell`; `Declare` continues. If no overload of that shape exists, it
still adds the rejected function to `Overloads` and `Symbols.WithNullary`.

Because the resolver now deliberately offers a nullary call in place of a name,
the refused declaration becomes authoritative over the declaration or supplied
literal that caused its refusal.

### Witness A — a refused function replaces a supplied truth literal

```ronin
function true { return 5; }
var x => truth = true;
```

**Actual:** `Supplied` plus a `TypeMismatch` saying the second `true` is a
`number`. The invalid function changed `true` from the supplied truth literal into
its nullary call.

**Expected:** the function is refused and does not enter any semantic table;
`true` in the initializer remains the supplied truth literal. The declaration
finding should not mutate the meaning of later source.

### Witness B — a refused function replaces an existing datum

```ronin
var f => text;
function f { return 5; }
var x => number = f;
```

**Actual:** only `Shadowed`; `f` in the initializer resolves to the rejected
function's number-returning call, hiding the `text`-versus-`number` mismatch.

**Expected:** the rejected function has no effect. If later checks continue after
the declaration finding, `f` remains the datum and the mismatch remains visible.

This is medium rather than high because both sources already contain a declaration
finding, but it leaves the partial semantic model used by diagnostics and the
editor internally inconsistent. Make `Cell` report whether it installed the
member (or preflight the name before either half is written), and file the
signature/nullary entry only on success. Maintain both declaration orders, an
existing datum/type, and supplied literals such as `true` and `nothing`.

## Disposition of REAUDIT64

| Prior finding | Reassessment |
|---|---|
| 1. nullary names were calls only in one checker helper | **Closed for accepted declarations.** Resolver output is now one `Node.Call`; ordering, recursion, and runtime invocation pass. Rejected declarations can still poison the nullary table (new finding 3). |
| 2. `Split(' ')` crashed composite-keyword nullaries | **Closed.** The bridge is removed and the registered canonical `Pattern` reaches the resolver. Composite and whitespace-normalised witnesses pass. |
| 3. a written value return admitted a valueless body | **Direct witnesses closed, not complete.** Fully resolved bare-return and fall-through bodies report `Unanswered`; any unrelated unresolved reading bypasses it (new finding 2). |
| 4. a lexer-classified numeric could throw in evaluation | **Evaluator mismatch closed; front-end regressed.** Accepted numerics are ASCII and parse invariantly, but rejected Unicode digits can no longer be consumed by the full lexer (new finding 1). |

## Verification performed

- Reviewed the production diff for `26a6923..740cb18`, `FORAUDIT65`, and
  `NUMERALALPHABET`.
- Debug and Release builds with `-warnaserror`: clean, zero warnings and errors.
- Maintained Release suite: `1330` passed, `0` failed.
- Release coverage gate: `100%` line and `100%` branch for `Ronin` and
  `Ronin.Server` using the workflow's `%2C`-escaped threshold argument.
- `git diff --check`: clean before this report.
- All three findings were executed as temporary xUnit probes against the real
  paths. The lexer witness was isolated under an external timeout. All temporary
  probe files were removed after capture.

The green gate and the four claimed direct repairs are confirmed. Signoff should
wait for the two high findings and the rejected-declaration contamination to be
repaired and maintained.
