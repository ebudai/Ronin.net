#!/usr/bin/env python3
"""
lookup_equality.py -- specifying what LISTEQUALITY.md §3 only flagged.

Last time I said list equality and lookup equality are two different functions
and that lookup equality should be settled "in the same change". That raised the
question without answering it. Answering it turns up two things I had not
looked at:

  1. order-insensitive EQUALITY plus order-sensitive ITERATION makes CUTOFF
     unsound -- it suppresses a change a downstream «for each» can see;
  2. fixing that by canonicalising at construction collapses lookup equality
     back into LIST equality, so it is one function after all, applied to a
     canonical form. That is a revision of what I told him.

Also probed: «@» and equality must share one key relation, and a hash-backed
table with a default comparer breaks that for structural keys.
"""

W = 78


def freeze(v):
    if isinstance(v, list):
        return ('list', tuple(freeze(x) for x in v))
    return ('atom', v)


def rank(k):
    """A total order over value kinds, then within a kind. Enough for a
    canonical form; the real one needs a case per runtime type."""
    kind, payload = k
    if kind == 'atom':
        return (0, type(payload).__name__, str(payload))
    return (1, '', tuple(str(rank(x)) for x in payload))


class Lookup:
    """canonical=True sorts the associations at construction."""

    def __init__(self, pairs, canonical):
        items = [(freeze(k), v) for k, v in pairs]
        keys = [k for k, _ in items]
        if len(set(keys)) != len(keys):
            raise ValueError('duplicate keys')
        self.items = sorted(items, key=lambda kv: rank(kv[0])) if canonical \
            else items
        self.canonical = canonical

    def __eq__(self, other):
        if not isinstance(other, Lookup):
            return False
        if self.canonical:
            return self.items == other.items          # elementwise: LIST rules
        return dict(self.items) == dict(other.items)   # order-insensitive

    def keys_in_order(self):
        return [k[1] for k, _ in self.items]

    def at(self, key, comparer):
        fk = freeze(key)
        for k, v in self.items:
            if comparer(k, fk):
                return v
        return 'nothing'


print('=' * W)
print('1. Order-insensitive equality + insertion-order iteration breaks cutoff')
print('=' * W)
a = Lookup([('a', 1), ('b', 2)], canonical=False)
b = Lookup([('b', 2), ('a', 1)], canonical=False)
print(f'  «[a=1,b=2]» is «[b=2,a=1]»            -> {a == b}   (unordered: correct)')
print(f'  iteration order of the first          -> {a.keys_in_order()}')
print(f'  iteration order of the second         -> {b.keys_in_order()}')
print(f'''
  So a lookup-valued «let» that recomputes from {a.keys_in_order()} to
  {b.keys_in_order()} is "unchanged" by equality, cutoff fires, nothing
  downstream re-runs -- and a downstream «for each» would have produced a
  DIFFERENT order. Cutoff has suppressed an observable change.

  This is not a cutoff bug. It is the cost of an equality that ignores
  something the program can still see.''')

print('=' * W)
print('2. Canonicalising at construction removes it -- and collapses two')
print('   functions into one')
print('=' * W)
ca = Lookup([('a', 1), ('b', 2)], canonical=True)
cb = Lookup([('b', 2), ('a', 1)], canonical=True)
print(f'  «[a=1,b=2]» is «[b=2,a=1]»            -> {ca == cb}')
print(f'  iteration order of the first          -> {ca.keys_in_order()}')
print(f'  iteration order of the second         -> {cb.keys_in_order()}')
print('''
  Equal lookups now iterate identically, so cutoff can never hide a difference,
  and the comparison is ELEMENTWISE on the canonical form -- the same function
  lists use.

  That revises what I sent last time. "Lookup equality is a different function"
  is true only if you compare as-written. Canonicalise at construction and it
  is the list comparison applied to a canonical form, which is less code and
  strictly safer.

  What it costs: one O(n log n) sort per lookup, paid once because lookups are
  immutable; a total order over key kinds; and the written order is not
  recoverable. For a map that is not a loss -- and «match» arms were already
  unordered by design.''')

print('=' * W)
print('3. Prerequisite: duplicate keys must be refused first')
print('=' * W)
try:
    Lookup([('a', 1), ('a', 2)], canonical=True)
    print('  duplicates accepted')
except ValueError:
    print('  duplicates refused at construction')
print('''
  MATCH.md §6b left "duplicate keys in a lookup literal are a finding" OPEN.
  It cannot stay open, because lookup equality is not well defined until it is
  closed: with duplicates admitted, is «[a=1, a=2]» equal to «[a=2, a=1]»?
  Either answer is defensible, which is the definition of a coin toss.

  Refuse duplicates and a lookup is a genuine map -- "same keys, same value at
  each key" -- and canonical order is unique. This is a blocking dependency,
  not a related item.''')

print('=' * W)
print('4. «@» and equality must share ONE key relation')
print('=' * W)
STRUCTURAL = lambda x, y: x == y
REFERENCE = lambda x, y: x is y
tbl = Lookup([([1, 2], 'x'), ('k', 'y')], canonical=True)
probe = [1, 2]
for name, cmp in (('structural', STRUCTURAL), ('reference/default', REFERENCE)):
    print(f'  «table @ [1,2]» with a {name:18} key comparer -> '
          f'{tbl.at(probe, cmp)!r}')
print('''
  A lookup backed by a hash table with the host's DEFAULT comparer takes the
  reference row: a structural key that «is» a key in the table is not found by
  «@». That is finding 6 one level down, and it is probably live today for any
  list-valued or lookup-valued key.

  The constraint to write into the spec: «@» finds the association whose key
  «is» the index. One relation, used by indexing, by equality, and by the
  duplicate-key check -- and if a hash is used to accelerate it, the hash must
  be a function of the same structural content, or the acceleration silently
  disagrees with the language.''')
