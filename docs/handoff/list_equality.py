#!/usr/bin/env python3
"""
list_equality.py -- finding 6 is bigger than "low / pessimization", because the
missing function is not a cutoff optimisation. It is «is».

IS-AND-EQUALITY.md settled this week that «is» on a list is VALUE equality, and
that lists and lookups are values while instances are entities. So the same
comparison the cutoff needs is the one the language operator needs. With
reference equality in place, «[1, 2] is [1, 2]» is FALSE -- a wrong answer, not
a pessimization.

Also checked here, and not in the finding: a lookup is UNORDERED (MATCH.md §4),
so lookup equality is a different function from list equality, and a shared
elementwise implementation gets one of them wrong.
"""

W = 78


# ---------------------------------------------------------------- the graph
class Node:
    def __init__(self, name, fn, deps):
        self.name, self.fn, self.deps = name, fn, deps
        self.value, self.evals = None, 0


class Graph:
    def __init__(self, equal):
        self.nodes, self.equal, self.clock = {}, equal, {}

    def add(self, name, fn, deps=()):
        n = Node(name, fn, list(deps))
        self.nodes[name] = n
        self.dirty(name)
        return n

    def source(self, name, value):
        n = self.add(name, lambda: value, ())
        n.value = value
        return n

    def set(self, name, value):
        self.nodes[name].fn = lambda: value
        self.recompute(name)

    def recompute(self, name):
        n = self.nodes[name]
        n.evals += 1
        new = n.fn()
        changed = not self.equal(new, n.value)
        n.value = new
        if changed:
            for m in self.nodes.values():
                if name in m.deps:
                    self.recompute(m.name)
        return changed

    def dirty(self, name):
        self.recompute(name)


REF = lambda a, b: a is b
STRUCT = lambda a, b: a == b


def scenario(equal, label):
    g = Graph(equal)
    g.source('tick', 0)
    g.add('items', lambda: [1], ['tick'])       # fresh list every recompute
    g.add('reader', lambda: len(g.nodes['items'].value), ['items'])
    base = g.nodes['reader'].evals
    for t in (1, 2, 3):
        g.set('tick', t)
    print(f'  {label:32} downstream evaluations after 3 ticks: '
          f'{g.nodes["reader"].evals - base}')


print('=' * W)
print('1. Reproducing the finding')
print('=' * W)
scenario(REF, 'reference equality (today)')
scenario(STRUCT, 'structural equality')
print('''
  Confirmed. A fresh «object[]» with identical content is a new reference
  every round, so cutoff never fires on any list literal.''')

print('=' * W)
print('2. The same missing function is «is»')
print('=' * W)
a, b = [1, 2], [1, 2]
print(f'  under reference equality   «[1,2] is [1,2]»  ->  {REF(a, b)}')
print(f'  under structural equality  «[1,2] is [1,2]»  ->  {STRUCT(a, b)}')
print('''
  IS-AND-EQUALITY.md: «is» is value equality, and lists are values. So the
  reference-equality path is not only defeating cutoff -- if the evaluator
  reaches the same comparison, it makes a language operator return the wrong
  answer, which is a correctness bug and not a low-severity one.

  This also removes one of the three recommended routes. "Explicitly exempt
  lists from cutoff" cannot be the whole answer, because structural equality
  still has to exist for «is». Once it exists, exempting cutoff is a
  performance choice rather than an avoidance -- and a different one.''')

print('=' * W)
print('3. A lookup is unordered, so it needs a DIFFERENT function')
print('=' * W)
L1, L2 = [('a', 1), ('b', 2)], [('b', 2), ('a', 1)]
print(f'  elementwise (list rules)     {L1} == {L2}  ->  {L1 == L2}')
print(f'  as unordered pairs           ->  {dict(L1) == dict(L2)}')
print('''
  MATCH.md §4: "a lookup is unordered, so arms have no fall-through and no
  first-match-wins." That was stated as a semantic property and it implies an
  equality: two lookups with the same associations in a different written
  order ARE the same lookup.

  A shared elementwise comparison gets this wrong, silently, and the symptom
  is a lookup-valued cell that never cuts off because the author wrote the
  arms in a different order somewhere. Worth settling in the same change,
  because reusing the list comparison is the obvious thing to do and it is
  the wrong thing.''')

print('=' * W)
print('4. When structural comparison pays -- it is arithmetic, not a judgement')
print('=' * W)
print('''  per round, with cutoff:     compare(n)  + (1 - hit) x downstream
           without cutoff:    downstream

  so cutoff pays when         compare(n) < hit x downstream

  compare(n) has EARLY EXIT, so its cost is n only when the lists are equal --
  which is exactly the case where the saving is banked. When they differ it
  usually exits at element 0.
''')
print('  downstream work is measured in the same units as one element compare')
print(f'\n  {"list length":>12} {"hit rate":>10} {"downstream":>12} {"cutoff pays?":>14}')
print('  ' + '-' * 54)
for n, hit, down in [(10, 0.97, 50), (100, 0.97, 5000), (1000, 0.97, 50000),
                     (10, 0.05, 50), (1000, 0.97, 500),
                     (100000, 0.97, 1000)]:
    pays = n < hit * down
    print(f'  {n:>12} {hit:>10.2f} {down:>12} {"yes" if pays else "no":>14}')
print('''
  The 0.97 is not invented: FAILUREMODES.md §2 measured 97% of recomputes
  producing an unchanged value in a mouse-move scenario. At that hit rate
  comparison loses only when the list is long AND the downstream is small,
  which is a narrow band.

  So: structural comparison with early exit, unconditionally, is the right
  default. A cached content hash is the fix for the case comparison loses --
  and it is available cheaply BECAUSE lists are immutable values, so the hash
  is computed once at construction and never invalidated. But note it buys
  nothing for the probe's own case: a flat list literal rebuilt every round
  pays O(n) to hash just as it would to compare. Hashing wins on NESTED
  structures, where child hashes are reused, and on long-lived lists compared
  repeatedly. Build it when a measurement asks for it, not before.''')

print('=' * W)
print('5. The route I would rule out')
print('=' * W)
print('''  "Represent immutable lists with stable identity" -- interning or
  hash-consing -- gives O(1) equality forever and should still be refused:

    * it needs a global table, which in an always-running environment is
      never collected and grows for the life of the session;
    * that table is a SYNCHRONISATION POINT, and THREADING.md deliberately
      designed the parallel section to have none -- thread-local arrays, a
      spin pool, no shared mutable structure in the hot path. A global intern
      table cuts straight across that, and contention on it would be worst
      exactly where the work is most parallel.

  The O(1) equality is real. The price is a shared mutable structure in a
  design whose threading story depends on not having one.''')
