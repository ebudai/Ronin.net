# Recursive return types — you were right, and three amendments that keep it working

> **Ledger** — `[V]` Recursive return types infer from the base case — ruled. The checker rule it implies is a recommendation.
> supersedes: none
> superseded by: MONOMORPHANDRETURN §2 (§4)

**Right call.** "Recursion needs a written return type" was over-refusing in the
same direction as the previous five, and I should have measured before I wrote
it. That is six for six now, and the pattern is no longer a coincidence — it is
a bias I should correct for rather than keep discovering.

Worth saying how the checking went, because it is the point: I predicted three
places the base-case rule would break, and **the first run of the probe refuted
all three.** The rule is more robust than I expected. What follows are the three
things it does need, none of which bring the annotation back.

---

## 1. Unify the recursive sites — do not merely check them

This is the amendment that matters, because the difference is invisible in the
sentence "infer it from the base case".

```
  function      BASE + CHECK           BASE + UNIFY            same?
  factorial     number                 number                  yes
  collect       list of ?              list of number          ** NO **
  find first    optional of ?          optional of number      ** NO **
```

`collect` starts `if n <= 0 { return empty list }`. The base case's own type is
**under-determined** — `list of ?` — and the thing that pins the element type is
the *recursive* site. If the recursive sites are validated against the base, the
function publishes `list of ?`. If they are unified in, it publishes `list of
number`.

An empty accumulator is how a large share of recursive functions begin, so this
is the common case rather than a corner.

> **State the rule as: the answer type is what all the return sites agree on,
> found by solving the base case first.** Base-case-first is then an *ordering*
> that makes the common case terminate in one pass — which is exactly why it is
> a good idea — rather than a different rule that only looks at one site.

## 2. Require the answer to be ground when solving finishes

```
  function loop (x) { return loop (x) }

  base + unify      REFUSED -- no base case
  naive unify-all   OK, answer = ?          <-- accepts it
```

A plain solve *succeeds* here with the answer variable still unbound, and an
unbound answer is not an answer. Your base-case formulation gets the rejection
for free; a solver that generalises it needs one closing check:

> **When the constraints are solved, the answer must be ground. An unsolved
> answer variable means the function never answers.**

And note which diagnostic each rule produces. Mine said *"please write a return
type"* — for a function that cannot work, where writing one would not have
helped. Yours says *"no return here is independent of the call itself"*, which
names the actual defect. The better error is an argument for the ruling, not a
detail of it.

## 3. Say "the recursive group", not "the function"

```
  function f (n)  { return g (n) }
  function g (n)  { if n <= 0 { return 0 }
                    return f (n - 1) }

  f alone            REFUSED -- no base case
  g alone            OK -- number
  the group together OK -- f: number, g: number
```

Taken alone, `f` has no return site independent of the recursion, so a
per-function rule refuses a program that is perfectly well typed — and refuses it
depending on which function the compiler happens to reach first, which is the
worst kind of rule.

Solving the mutually-recursive group together fixes it, and the group is
something the compiler already computes to order everything else. So this is a
wording change to the rule and a `foreach` over an SCC in the implementation,
not new machinery.

## 4. The one residue

**Polymorphic recursion** — a function that calls itself at a *different* type —
cannot be inferred; it is undecidable in general. It is rare, most working
programmers never write one, and it is the only place an annotation should be
demanded. When it comes up the message can say so specifically.

That is a much smaller rule than the one I proposed, and it fails closed with a
comprehensible reason instead of taxing every recursive function in the language.

## 5. Summary

| | |
|---|---|
| requiring an annotation for recursion | **wrong** — over-refusing, and I should have measured first |
| infer from the base case | **right**, and more robust than I predicted |
| but the recursive sites must **unify**, not be checked | otherwise `return empty list` publishes `list of ?` |
| and the answer must be **ground** when solving ends | otherwise a function that never answers is accepted |
| and it is the recursive **group**, not the function | otherwise mutual recursion fails, order-dependently |
| the only annotation that stays required | **polymorphic recursion** |

Probe: `recursive_infer.py`.
