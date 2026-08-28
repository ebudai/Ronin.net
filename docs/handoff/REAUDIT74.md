# Re-audit 74 — the strict-argument finding skips lookup keys

> **Ledger** — `[A]` Audit of `eccead6..232fcb8`, requested by
> `FORAUDIT74`. The per-callable `Unanswered` correction and its rationale are
> correct, and the new `Unreachable` finding works for direct arguments, nested
> calls, list values, and calls below operation operands. **Not signed off:** the
> strict-group walk visits values only, so a value-return in a lookup key can
> still make its enclosing call unreachable with no finding.

## Audit result

The amendment's first two items are sound. `Returns` already partitions
unresolved readings by callable owner, the added nested-delegate control pins
that boundary, and the comment now records the whole-body precondition that
justifies suppressing `Unanswered`.

The new resolved-tree walk also handles its maintained witnesses correctly:
direct and nested call arguments fire, a statement-level return does not, list
values preserve strict context, and operation operands reset it so
`otherwise return` remains a live guard. The walk does not, however, traverse
all parts of a group. A lookup evaluates both each key and each value, while
`Reach` descends only `part.Value`. The missing key arm leaves a valid strict
argument form silent.

## Finding 1 — high — a value-return in a lookup key leaves the dead call unreported

**Location:** `Compiler/Compilation.cs`, `Reach`, in the `Node.Group` arm that
recurses through `group.Parts` using only `part.Value`.

### Witness

```ronin
function send (x) { return x; }
function f => number { send [return 5 = 1]; }
```

The source resolves. At runtime, constructing the lookup argument evaluates its
key before `send` can run; evaluating `return 5` exits `f`, so `send` is
unreachable under exactly the amendment's strict-argument rule.

**Actual:** the command-line compiler reports `0 with problems`. It emits
neither `Unreachable` nor `Unanswered`: the return-site walk correctly sees the
lookup key and concludes that `f` carries a value, while the new dead-call walk
does not see the same key.

**Expected:** one `Unreachable` for the enclosing `send` call (on the return
site under FORAUDIT74's original anchoring, or on the call under
FINDINGCOMPOSITION's subsequent span ruling).

The asymmetry is visible in the adjacent control:

```ronin
function f => number { send [1 = return 5]; }
```

That value-position form does report `Unreachable`, because `part.Value` is the
one half `Reach` currently visits.

### Repair direction and regression coverage

For a lookup group reached as a call argument, carry the same strict/enclosing
call context through both `part.Key` and `part.Value`, in their evaluation
order. The group kind should decide whether a key exists, as `Node.Group.Whole`
and the evaluator already do; ordinary groups and lists continue to visit only
their values.

Add maintained controls for both lookup positions, including at least the
silent key witness above. A nested call in a key, such as
`send [send return 5 = 1]`, is useful to pin that the key is traversed rather
than merely special-cased for a direct return.

## Verification performed

- Reviewed `FORAUDIT74`, `UNRESOLVEDRETURNAMENDMENT`, and the production/test
  diff for `eccead6..232fcb8`.
- Reproduced the lookup-key miss and the lookup-value control through the public
  command-line compiler; temporary source files were removed.
- Locked restore: clean.
- Debug and Release builds with `-warnaserror`: clean, zero warnings and errors.
- Maintained Debug and Release suites: `1352` passed, `0` failed in each.
- Release coverage gate: `100%` line and `100%` branch for `Ronin` and
  `Ronin.Server`.
- Generated-ledger check and `git diff --check`: clean.

FORAUDIT74 is not fully signed off. Its callable-boundary correction and the
main `Unreachable` shape hold, but the strict aggregate traversal is incomplete.
