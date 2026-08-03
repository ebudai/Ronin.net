#!/usr/bin/env python3
"""
anchor_rule_shape.py -- how narrow can the second law be?

anchor_prefix.py showed R5 misses a class: a name that begins with a pattern's
anchor run. The blanket repair -- "no name may begin with any anchor run" --
would kill «item count», «sort order», «round trip», «filter text», which are
ordinary names in a language sold on readability. So: measure the smallest
rule that still closes it.

The claim to test:

    a name N is capturable iff some pattern's ENTIRE WORD CONTENT is a proper
    prefix of N

which is only true of patterns shaped « w1 .. wk (_) » -- all words, then one
free trailing hole. Any pattern with glue needs that glue word inside N, and
R5 already refuses that. Any pattern with a bracketed hole cannot be shadowed
by a name at all, because a name is a word-only span.

If the claim holds, «item count» survives «item (_) of (_)» and only the
anchor-only patterns cost anything.
"""

import itertools
from dp_resolver import DPResolver, N, PA, HOLE

W = 78


def words_only_open(pat):
    """Entire word content is a prefix and there is exactly one trailing free
    hole: « w1 .. wk (_) »."""
    return pat[-1] is HOLE and all(s is not HOLE for s in pat[:-1])


def wordcontent(pat):
    return tuple(s for s in pat if s is not HOLE)


def sweep(universe, base_names, patterns, maxsrc=4, maxname=3):
    pats = PA(*patterns)
    base = frozenset(tuple(x.split()) for x in base_names)
    glue = set()
    for p in pats:
        seen = False
        for s in p:
            if s is HOLE:
                seen = True
            elif seen:
                glue.add(s)
    narrow = [wordcontent(p) for p in pats if words_only_open(p)]

    srcs = [s for k in range(1, maxsrc + 1)
            for s in itertools.product(universe, repeat=k)]

    def res(names, s):
        v, c, sh = DPResolver(names, pats).resolve(' '.join(s))
        return v, sh

    before = {s: res(base, s) for s in srcs}
    cands = [c for k in range(1, maxname + 1)
             for c in itertools.product(universe, repeat=k) if c not in base]

    caps, by_r5, by_narrow, unexplained, overkill = 0, 0, 0, [], []
    for c in cands:
        r5 = len(c) > 1 and any(w in glue for w in c)
        nr = any(len(a) < len(c) and c[:len(a)] == a for a in narrow)
        names = base | {c}
        dangerous = False
        for s in srcs:
            bv, bs = before[s]
            if bv != 'OK':
                continue
            av, asw = res(names, s)
            if av == 'OK' and bs == asw:
                continue
            dangerous = True
            caps += 1
            if r5:
                by_r5 += 1
            elif nr:
                by_narrow += 1
            else:
                unexplained.append((c, s, bs, asw))
        if nr and not dangerous and not r5:
            overkill.append(c)
    return caps, by_r5, by_narrow, unexplained, overkill, narrow, sorted(glue)


CONFIGS = [
    ('item (_) of (_)  — glue-bearing, so no anchor-only pattern',
     ['item', 'of', 'count', 'a'], ['a', 'count'], ['item _ of _']),
    ('item (_) of (_)  +  count of (_)',
     ['item', 'of', 'count', 'a'], ['a'], ['item _ of _', 'count of _']),
    ('print (_)  — the expensive shape',
     ['print', 'a', 'job', 'queue'], ['a', 'job', 'queue'], ['print _']),
    ('sort (_) by (_)  — glue «by», anchor «sort»',
     ['sort', 'by', 'a', 'order'], ['a', 'order'], ['sort _ by _']),
    ('sum of (_)  +  (_) otherwise (_)',
     ['sum', 'of', 'x', 'otherwise'], ['x'], ['sum of _', '_ otherwise _']),
]

print('=' * W)
print('Is "entire word content is a proper prefix" the exact law?')
print('=' * W)
allbad, allover = [], []
for title, uni, bn, ps in CONFIGS:
    caps, r5, nr, bad, over, narrow, g = sweep(uni, bn, ps)
    allbad += bad
    allover += [(title, o) for o in over]
    print(f'\n  {title}')
    print(f'      glue={g}   anchor-only patterns={[" ".join(a) for a in narrow]}')
    print(f'      captures={caps:3}  by R5={r5:3}  by narrow rule={nr:3}  '
          f'unexplained={len(bad)}  refused-but-harmless={len(over)}')
    for c in over[:4]:
        print(f'        refused but never captured: «{" ".join(c)}»')

print()
print(f'  [{"PASS" if not allbad else "FAIL"}] narrow rule + R5 '
      f'{"closes every silent capture" if not allbad else "leaves cases open"}')
for c, s, bs, asw in allbad[:8]:
    print(f'      «{" ".join(c)}» on «{" ".join(s)}»: {bs} -> {asw}')

print()
print('=' * W)
print('The names the narrow rule saves')
print('=' * W)
SAFE = [('item count', 'item (_) of (_)'), ('sort order', 'sort (_) by (_)'),
        ('round trip', 'round (_) to (_) places'),
        ('filter text', 'filter (_) where (_)'),
        ('send queue', 'send (_) to (_)'), ('join key', 'join (_) with (_)'),
        ('split point', 'split (_) on (_)')]
print('  legal under the narrow rule, illegal under a blanket anchor ban:\n')
for n, p in SAFE:
    print(f'    «{n:12}»   rival pattern {p:26} has glue -> R5 territory')
print('''
  illegal either way, because the pattern is anchor-only:

    «print job»  «print queue»  «broadcast list»  «while loop»
    «rounded value»  «sum of squares»  «first of month»  «wait until dawn»

  The single-word ones are the whole cost: «print ...», «broadcast ...»,
  «while ...», «rounded ...». Every other anchor-only pattern in the seed
  registry is two words («sum of», «count of», «first of», «wait until»),
  and a name beginning with those two words is already unusual.''')
print()
