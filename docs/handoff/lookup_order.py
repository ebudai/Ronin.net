#!/usr/bin/env python3
"""
lookup_order.py -- REAUDIT47 findings 1 and 2 have one cause, and it is a
decision of mine that the implementation did not take.

E-AGGREGATES §6 ruled: "Insertion order is preserved for iteration and ignored
for equality. Two lookups may therefore be equal and iterate differently."

The implementation instead CANONICALISES the stored order by sorting, which is
what FRESHAUDIT20 finding 1 asked for. Everything REAUDIT47 reports follows from
that one choice:

  finding 1   sorting needs a total order COMPATIBLE with equality --
              «Compare(a,b) == 0 iff Same(a,b)» -- and there is no way to derive
              one for an arbitrary host object, so it falls back to ToString()
  finding 2   the comparer recurses through aggregates with no memo, so sorting
              re-walks shared DAGs exponentially

So the question is not "how do we fix the comparer". It is: does anything
actually NEED a total order?

  §1  what each consumer of a lookup needs -- an order, or only an equality
  §2  the two duplicate-detection strategies, on the audit's own witnesses
  §3  what it costs
"""

W = 78

print('=' * W)
print('§1  Which consumers need an ORDER, and which need only EQUALITY?')
print('=' * W)
CONSUMERS = [
    ('lookup equality  «a is b»',      'equality', 'same key set, same values'),
    ('duplicate keys at admission',    'equality', 'is this key already present'),
    ('cutoff / «old» / «changes»',     'equality', 'did the value change'),
    ('indexing  «m @ k»',              'equality', 'find the key equal to k'),
    ('hashing for fast indexing',      'equality', 'a hash consistent with equality; '
                                                   'order-insensitive combine'),
    ('printing / display',             'neither',  'wants DETERMINISM, and insertion '
                                                   'order is deterministic'),
    ('iteration  «for each»',          'neither',  'wants reproducibility, same reason'),
]
print(f'  {"consumer":34} {"needs":>10}   why')
print('  ' + '-' * 74)
for c, n, why in CONSUMERS:
    print(f'  {c:34} {n:>10}   {why}')

print(f'''
  Nothing needs a total order. Zero of {len(CONSUMERS)}.

  And the two requirements are not the same size. An EQUALITY over admitted
  values is derivable structurally -- it is written and it works. A TOTAL ORDER
  must order across every kind AND within every kind, forever, including kinds
  nobody has added yet. Finding 1 is not a bug in the comparer; it is that
  obligation coming due.''')

# ---------------------------------------------------------------------------
print()
print('=' * W)
print("§2  The audit's own witnesses, under both strategies")
print('=' * W)


class Host:
    """A host value the runtime does not recognise: kind 8, ordered by ToString."""
    def __init__(self, shown, identity):
        self.shown, self.identity = shown, identity

    def __repr__(self):
        return self.shown


def same(a, b):
    """Language equality -- structural, and it does NOT consult display text."""
    if isinstance(a, Host) or isinstance(b, Host):
        return isinstance(a, Host) and isinstance(b, Host) and a.identity == b.identity
    if isinstance(a, tuple) != isinstance(b, tuple):
        return False
    if not isinstance(a, tuple):
        return type(a) is type(b) and a == b
    return len(a) == len(b) and all(same(x, y) for x, y in zip(a, b))


def compare_by_text(a, b):
    """The implemented fallback: ordinal comparison of display text."""
    x, y = str(a), str(b)
    return (x > y) - (x < y)


def admit_by_sorting(entries):
    """Sort, then scan ADJACENT pairs for duplicates -- the implemented path."""
    ordered = sorted(entries, key=_CmpKey)
    # the duplicate scan asks LANGUAGE equality, but only of ADJACENT pairs --
    # which is sound only if the sort really put equal keys together
    for i in range(1, len(ordered)):
        if same(ordered[i - 1][0], ordered[i][0]):
            return 'REFUSED duplicate'
    return [k for k, _ in ordered]


import functools


@functools.total_ordering
class _CmpKey:
    def __init__(self, entry):
        self.k = entry[0]

    def __lt__(self, other):
        return compare_by_text(self.k, other.k) < 0

    def __eq__(self, other):
        return compare_by_text(self.k, other.k) == 0


def admit_by_equality(entries):
    """Keep insertion order; check each new key against those already taken."""
    taken = []
    for k, _ in entries:
        if any(same(k, seen) for seen in taken):
            return 'REFUSED duplicate'
        taken.append(k)
    return taken


A = Host('same', 'A')      # two UNEQUAL keys that print alike
B = Host('same', 'B')
P = Host('a', 'P')         # two EQUAL keys that print differently...
Q = Host('c', 'P')
R = Host('b', 'R')         # ...separated by an unequal one

CASES = [
    ('equal maps written in opposite orders',
     [(A, 1), (B, 2)], [(B, 2), (A, 1)], 'the two admissions must agree'),
    ('equal keys separated by an unequal one',
     [(P, 1), (R, 2), (Q, 3)], None, 'P and Q are ONE key -- must be refused'),
]

print(f'  {"witness":42} {"by sorting":>16} {"by equality":>16}')
print('  ' + '-' * 76)
for label, one, two, note in CASES:
    s1, e1 = admit_by_sorting(one), admit_by_equality(one)
    if two is None:
        ok_s = s1 == 'REFUSED duplicate'
        ok_e = e1 == 'REFUSED duplicate'
        print(f'  {label:42} {("refused" if ok_s else "ADMITTED"):>16} '
              f'{("refused" if ok_e else "ADMITTED"):>16}')
    else:
        s2, e2 = admit_by_sorting(two), admit_by_equality(two)
        # SORTING buys sequence equality over the canonical order, so that is
        # what it must be judged by -- comparing display text here would be
        # judging the defect with the defect.
        agree_s = (not isinstance(s1, str) and not isinstance(s2, str)
                   and len(s1) == len(s2)
                   and all(same(x, y) for x, y in zip(s1, s2)))
        # EQUALITY-based storage keeps insertion order and compares as a SET
        agree_e = (len(e1) == len(e2)
                   and all(any(same(k, j) for j in e2) for k in e1))
        print(f'  {label:42} {("agree" if agree_s else "DISAGREE"):>16} '
              f'{("agree" if agree_e else "DISAGREE"):>16}')
    print(f'  {"":42} {note}')

print('''
  Both of the audit's high-severity witnesses are properties of SORTING. Neither
  strategy is cleverer than the other about host values -- equality is simply not
  asked to invent an order it cannot have.''')

# ---------------------------------------------------------------------------
print()
print('=' * W)
print('§3  Finding 2, which is the same cause counted differently')
print('=' * W)


def dag(depth):
    v = ('leaf',)
    for _ in range(depth):
        v = (v, v)                     # ONE child, referenced twice
    return v


walks = 0


def compare_no_memo(a, b):
    global walks
    walks += 1
    if isinstance(a, tuple) and isinstance(b, tuple):
        for x, y in zip(a, b):
            r = compare_no_memo(x, y)
            if r:
                return r
        return 0
    return (a > b) - (a < b)


print(f'  {"depth":>6} {"comparer walks (no memo)":>26} {"equality with memo":>21}')
print('  ' + '-' * 60)
for d in (4, 8, 12):
    walks = 0
    compare_no_memo(dag(d), dag(d))
    memo = set()

    def same_memo(a, b):
        if (id(a), id(b)) in memo:
            return True
        memo.add((id(a), id(b)))
        if isinstance(a, tuple) and isinstance(b, tuple):
            return all(same_memo(x, y) for x, y in zip(a, b))
        return a == b
    same_memo(dag(d), dag(d))
    print(f'  {d:>6} {walks:>26} {len(memo):>21}')

print('''
  The audit measured 8,192 leaf renderings at depth 12 and the model reproduces
  the doubling. Two remedies exist:

    give the comparer its own memo   the audit's recommendation -- correct, and
                                     it keeps the total-order obligation alive
    delete the comparer              nothing needed it (§1), and finding 1 goes
                                     with it

  The second is smaller and closes both findings by removing their cause.''')

print()
print('=' * W)
print('Conclusion')
print('=' * W)
print('''  E-AGGREGATES §6 already ruled insertion order for iteration and equality
  that ignores order. The implementation canonicalised instead, because
  FRESHAUDIT20 finding 1 treated "equal lookups iterate differently" as a defect.

  It is not a defect; it is the trade §6 named, and it was named for exactly the
  reason finding 1 has now demonstrated: a canonical order needs a total order
  over every admissible key, and no such order is derivable.

      >> Drop canonical ordering. Store insertion order. Detect duplicates by
      >> EQUALITY against the keys already taken, not by adjacency after a sort.

  Findings 1 and 2 both close, and the comparer -- with its ToString fallback,
  its null rule and its missing memo -- is deleted rather than repaired.''')
