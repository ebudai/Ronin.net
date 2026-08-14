# The check came out the other way — the allowance is harmless

**From:** the successor. You asked me to run the §1 check before acting, and it
decides everything: **the modifier reading of `var hidden cost => number` is not
well-formed.** So the allowance is harmless, §2–§4 do not apply, and finding 2
needs no change. Your old clone had the two-reading problem; the current grammar
has already closed it. C is done; a word on D at the end.

---

## 1. The check — measured, not reasoned

The question was: could `hidden` be consumed by `Modifiers.Parse` and `cost` be
the name — *a hidden variable called `cost`*? Parsing the source and reading the
tree back:

```
  var hidden cost => number;      Datum  name = «hidden cost»   modifiers = []
  hidden cost => number;          Datum  name = «hidden cost»   modifiers = []
  var reactive score => number;   Datum  name = «reactive score» modifiers = []
```

`hidden` and `reactive` are part of the **name**, and no reading makes them a
modifier. The grammatical reason is exactly the one your §1 asked for: **a datum
has no modifier position before its identifier.** Every `Modifiers.Parse` call in
the grammar is one of two shapes —

- **after `=>`**, on the type (`Datum.cs:70`, `Function.cs:38`) — «var x =>
  compiled money»; or
- **before a production-announcing keyword** (`Type.cs:28` before «type»,
  `Scope.cs` before «if»/«while»/«when»/«for each»).

There is no `<modifier> <name>` production and no `<mutability> <modifier>
<name>` one. So `Modifiers.Parse` cannot apply where a name begins, the
"a hidden variable named cost" reading cannot be produced, and there is nothing
for the parser to pick between. `Test/Unit/Loops.cs:90` asserts the only reading
there is, not one of two.

That the modifier reading is unreachable is *why* it is harmless, which is the
opposite of the `wait time` shape: there, both readings were well-formed and one
was silently chosen; here, only one is well-formed at all.

**So, per your own §1: A is "reserve nothing", and B and D go with it.** No
`SymbolKind.Reserved`, no `Name.Parse` refusal, no registry change. The
`fast` census row (2c) already landed and stands on its own — a keyword the lexer
knows belongs in the census whatever else is true.

**One caveat I owe you back:** this is the DATUM head, which is your example and
the audit's. A modifier does sit before a name-bearing *keyword* elsewhere —
«hidden type Box», where the while-loop that lets «var ready if needed» through
would also read «hidden type Box» as one name beside the `hidden`-modified type.
I did not chase it because it is not finding 2 and not a datum, but if "where may
a modifier appear" gets its own slice (your D), that is the first stone to turn.

## 2. C — the ledger row is in

`fast`'s target and duplicate validation is checker work, for the reason you
give: `fast truth` is wrong because `fast` qualifies a *number* occurrence, and
knowing it resolved to `truth` is the resolved semantic type finding 1 does not
have. It is in `Test/Expiry.cs` now, beside the base gap, as the second **lenient**
row —

```
  gap                            approximates      becomes
  «fast» on a non-number, and    no check at all   a target/duplicate check at the
  duplicated «fast», compile                       typed occurrence (finding 1's
  cleanly                                          checker)
```

— with its successor named, so it is a deletion target rather than a rediscovery.

## 3. D — noted, and it is a real slice, but not this one

Modifier **placement and meaning** — `fast if true`, `hidden while`,
`fast type box` accepting a modifier that means nothing there — is the general
question you named: *where may a modifier appear, and what does it mean there.*
It is real: `Modifiers.Parse` validates the placement of no modifier for any
production. But with §1 come out the harmless way, it is not urgent and not
`fast`'s to carry, and it wants the one write-up you describe — which modifiers
apply to which productions, what each means, and what happens where one means
nothing — rather than a patch here. I will raise it as its own document when it
is timely; flagging it now so it is on the board and not in my head.

## 4. Summary

| | |
|---|---|
| §1 check | **not well-formed** — a datum has no modifier slot before its identifier; measured, `hidden`/`reactive`/`fast` are name words there |
| finding 2 | **no change.** The allowance is harmless because the modifier reading is unreachable, not because a pick is acceptable. Your old clone differed |
| A / B | **reserve nothing** — no `Reserved` kind, no `Name.Parse` refusal, no registry entry. 2c census stands alone |
| C | **done** — expiry-ledger row for `fast` target/duplicate validation, successor named |
| D | **a real future slice**, not this one, and not `fast`'s — modifier placement is unvalidated for every modifier. Raised, not patched |
| «hidden type Box» | the one modifier-before-a-keyword case I did not chase; the first stone for D |
