#!/usr/bin/env python3
"""
aggregate_depth.py -- two things document E has to get right, measured.

Read off the tree at 1b1f788 rather than described:

  Runtime/List.cs   a list carries its own Depth, refuses past Deep = 256 AT
                    ADMISSION, refuses cycles, and reuses an already-admitted
                    child so a DAG stays a DAG. The comment says why the limit
                    is at construction: "a value the runtime accepts must be one
                    it can compare honestly".

  there is no Runtime/Lookup.cs, and «grep Association|Origin» over Resolution
  and Runtime finds nothing. Node.Group carries «Collection: bool» and a flat
  «Parts» -- so a lookup literal parses, gets its duplicate-key check, and then
  LOSES ITS KEYS. Evaluator turns any collection group into List.Admit(...).

So E adds a second aggregate beside a finished one. Two questions that decide the
shape:

  §1  if list depth and lookup depth are counted separately, can a nest that
      alternates them exceed the limit while both counters stay legal?
  §2  does a key that is itself an aggregate break equality, and does
      canonicalising at construction fix it?
"""

W = 78
DEEP = 8            # scaled down from 256 so the counterexample is printable


# --- values: ('list', [items]) | ('lookup', [(key, value)]) | a scalar --------
def L(*items):    return ('list', list(items))
def K(*pairs):    return ('lookup', list(pairs))


def alternating(n):
    """A nest that alternates list and lookup, n layers deep."""
    v = 0
    for i in range(n):
        v = L(v) if i % 2 == 0 else K(('k', v))
    return v


def depth_shared(v):
    """One measure over both kinds -- what List.cs does, extended."""
    if not isinstance(v, tuple):
        return 0
    kind, body = v
    if kind == 'list':
        return 1 + max((depth_shared(x) for x in body), default=0)
    return 1 + max((max(depth_shared(k), depth_shared(x)) for k, x in body), default=0)


def depth_per_kind(v, kind_wanted):
    """A counter that only counts its own kind -- the tempting cheap version."""
    if not isinstance(v, tuple):
        return 0
    kind, body = v
    children = ([x for x in body] if kind == 'list'
                else [y for pair in body for y in pair])
    inner = max((depth_per_kind(c, kind_wanted) for c in children), default=0)
    return inner + (1 if kind == kind_wanted else 0)


print('=' * W)
print(f'§1  Depth: one shared measure, or one per kind?   (limit = {DEEP})')
print('=' * W)
print(f'  {"layers":>7} {"shared":>7} {"list ctr":>9} {"lookup ctr":>11}   '
      f'{"shared says":>12}  {"per-kind says":>14}')
print('  ' + '-' * 74)
bypass = []
for n in range(1, 2 * DEEP + 3):
    v = alternating(n)
    s = depth_shared(v)
    dl = depth_per_kind(v, 'list')
    dk = depth_per_kind(v, 'lookup')
    sv = 'REFUSE' if s > DEEP else 'admit'
    pv = 'REFUSE' if (dl > DEEP or dk > DEEP) else 'admit'
    if sv != pv:
        bypass.append((n, s, dl, dk))
    if n <= 4 or n >= 2 * DEEP - 2 or sv != pv:
        print(f'  {n:>7} {s:>7} {dl:>9} {dk:>11}   {sv:>12}  {pv:>14}'
              f'{"   <- BYPASS" if sv != pv else ""}')

print(f'''
  disagreements: {len(bypass)}

  A per-kind counter admits a value {2 * DEEP} layers deep while neither of its two
  counters exceeds {DEEP}. Alternating halves every counter, so the real bound is
  the limit times the number of kinds -- and a third aggregate would raise it
  again.

  That matters because of WHY the limit exists. List.cs puts it at admission so
  that "a value the runtime accepts must be one it can compare honestly", and
  names cutoff, «changes», «old» and «is» as the askers. A value admitted at
  {2 * DEEP} deep is one the comparison was sized to refuse.

      >> ONE depth measure across every aggregate kind, carried on the value,
      >> and a KEY counts toward it exactly as an element does.

  «Fits» already asks the question again on reuse because depth is a property of
  the whole value. Same reason, one kind further out.''')


# ---------------------------------------------------------------------------
# §2  a key that is itself an aggregate
# ---------------------------------------------------------------------------
def canonical(v):
    """The form a key is stored in: total, and computed once at construction."""
    if not isinstance(v, tuple):
        return ('s', v)
    kind, body = v
    if kind == 'list':
        return ('l', tuple(canonical(x) for x in body))
    # a lookup is unordered, so its canonical form is its pairs SORTED
    return ('k', tuple(sorted((canonical(k), canonical(x)) for k, x in body)))


def structural_capped(a, b, budget):
    """Comparing structurally with a budget -- the version List.cs refused."""
    if budget <= 0:
        return True                      # ran out; call them equal
    if isinstance(a, tuple) != isinstance(b, tuple):
        return False
    if not isinstance(a, tuple):
        return a == b
    if a[0] != b[0] or len(a[1]) != len(b[1]):
        return False
    if a[0] == 'list':
        return all(structural_capped(x, y, budget - 1) for x, y in zip(a[1], b[1]))
    return all(structural_capped(k1, k2, budget - 1) and structural_capped(v1, v2, budget - 1)
               for (k1, v1), (k2, v2) in zip(a[1], b[1]))


print()
print('=' * W)
print('§2  Keys that are aggregates')
print('=' * W)

# same lookup, written with its pairs in two orders, used as a KEY
key_a = K(('x', 1), ('y', 2))
key_b = K(('y', 2), ('x', 1))
outer_a = K((key_a, 'hit'))
outer_b = K((key_b, 'hit'))

print(f'''  A lookup is unordered, so these two are the same key written two ways:

      [ x = 1, y = 2 ]   and   [ y = 2, x = 1 ]

  used as the key of an outer lookup.

  canonical form equal      : {canonical(key_a) == canonical(key_b)}
  so the outer lookups are  : {"EQUAL" if canonical(outer_a) == canonical(outer_b) else "UNEQUAL"}
  and the outer lookup has  : {"ONE key" if canonical(key_a) == canonical(key_b) else "TWO keys"}

  Which is the finding: written in the parser those are two DISTINCT spellings,
  so Collection's «Identity» -- length-prefixed token text -- calls them
  different keys and admits both. They are one key. So the parser's duplicate
  check answers the SPELLED question and the runtime still owes the VALUE
  question, exactly as Collection.Element's own comment says.

      >> a lookup canonicalises its keys AT CONSTRUCTION, and two entries whose
      >> keys canonicalise alike are refused there -- the same shape as TooDeep,
      >> and for the same reason.''')

# and the reason it cannot be a capped structural comparison instead
deep_a = alternating(2 * DEEP)
deep_b = alternating(2 * DEEP)
# make them differ only past the budget
def bury(v, at):
    if at == 0:
        return L(999)
    kind, body = v
    if kind == 'list':
        return ('list', [bury(body[0], at - 1)])
    return ('lookup', [(body[0][0], bury(body[0][1], at - 1))])

deep_c = bury(deep_a, 2 * DEEP - 1)
print(f'''
  And why the refusal cannot be replaced by a capped comparison, which is the
  cheaper thing someone will reach for:

  two values differing only below the budget
      capped comparison (budget {DEEP}) : {"EQUAL -- WRONG" if structural_capped(deep_a, deep_c, DEEP) else "unequal"}
      canonical forms                : {"equal" if canonical(deep_a) == canonical(deep_c) else "unequal -- correct"}

  A cap makes two unequal values compare equal, which is not an equivalence and
  is observable through cutoff and «old». List.cs already refuses this for lists;
  a lookup inherits the argument rather than re-deciding it.''')
