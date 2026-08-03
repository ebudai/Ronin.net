#!/usr/bin/env python3
"""
anchor_prefix.py -- the second law, which nobody has written down.

name_capture2.py run B found 14 silent captures that R5 does not explain. All
of them have one shape:

    declare «print a»     «print a»  reads as the NAME, not the call

A name costs 1 lookup. A pattern call costs 1 + its arguments, so at least 2.
So whenever a declared name's tokens BEGIN with a pattern's anchor run and the
remainder is a parseable argument, the name wins on minimum lookup -- silently,
because both readings are valid.

R5 does not fire: the anchor run is not glue. R6 does not fire: it compares
patterns with patterns. This is pattern-vs-NAME, and it is the case where the
pattern's glue set is EMPTY -- exactly the anchor-only patterns that
«RESERVED (0)» was celebrating.

Tested here:
    1. the capture, isolated
    2. the direction -- is a name that is a PREFIX of an anchor run also unsafe?
    3. whether R5 + "no name may have an anchor run as a proper prefix"
       is complete over an exhaustive sweep
    4. what the rule actually costs, in names
"""

import itertools
import time
from dp_resolver import DPResolver, N, PA

W = 78


def show(names, pats, src):
    v, c, s = DPResolver(names, pats).resolve(src)
    return v, c, s


print('=' * W)
print('1. The capture, isolated')
print('=' * W)
P1 = PA('print _', 'sum of _')
BASE = N('a', 'queue')
for decl, src in [('print a', 'print a'),
                  ('sum of a', 'sum of a'),
                  ('print queue', 'print queue')]:
    bv, bc, bs = show(BASE, P1, src)
    av, ac, asw = show(BASE | {tuple(decl.split())}, P1, src)
    tag = 'unchanged' if (bv, bs) == (av, asw) else 'CAPTURE'
    print(f'  declare «{decl:10}»  source «{src:12}»')
    print(f'      before: {bv:8} {bc} lookups  {bs}')
    print(f'      after : {av:8} {ac} lookups  {asw}    <- {tag}')
print()

print('=' * W)
print('2. Which direction is unsafe?')
print('=' * W)
print('  anchor run «sum of».  Is a name that the anchor run PREFIXES unsafe,')
print('  or a name that PREFIXES the anchor run, or both?\n')
for decl, src in [('sum of x', 'sum of x'),     # anchor run prefixes the name
                  ('sum', 'sum of x'),          # name prefixes the anchor run
                  ('of x', 'sum of x')]:        # name is the tail
    b = show(N('x'), PA('sum of _'), src)
    a = show(N('x') | {tuple(decl.split())}, PA('sum of _'), src)
    tag = 'unchanged' if b[0::2] == a[0::2] else 'CAPTURE'
    print(f'  declare «{decl:9}»  {b[0]:9} {b[2]:22} -> {a[0]:9} {a[2]:22} {tag}')
print('''
  Only the first. A name the anchor run prefixes is a rival reading of the
  whole call; a name that merely starts with the same word is not, because
  the rest of the pattern still has to match and nothing else can absorb it.''')
print()

print('=' * W)
print('3. Exhaustive: is R5 + the anchor-prefix rule complete?')
print('=' * W)


def anchor_run(pat):
    out = []
    for s in pat:
        if s is None:
            break
        out.append(s)
    return tuple(out)


def sweep(universe, base_names, patterns, maxsrc=4, maxname=3):
    pats = PA(*patterns)
    base = frozenset(tuple(x.split()) for x in base_names)
    runs = [anchor_run(p) for p in pats]
    glue = set()
    for p in pats:
        seen_hole = False
        for s in p:
            if s is None:
                seen_hole = True
            elif seen_hole:
                glue.add(s)

    srcs = [s for k in range(1, maxsrc + 1)
            for s in itertools.product(universe, repeat=k)]
    before = {s: show(base, pats, ' '.join(s))[0::2] for s in srcs}
    cands = [c for k in range(1, maxname + 1)
             for c in itertools.product(universe, repeat=k) if c not in base]

    r5_only, both, unexplained, captures = 0, 0, [], 0
    for c in cands:
        hits_r5 = len(c) > 1 and any(w in glue for w in c)
        hits_ap = any(len(r) < len(c) and c[:len(r)] == r for r in runs)
        names = base | {c}
        for s in srcs:
            bv, bs = before[s]
            if bv != 'OK':
                continue
            av, asw = show(names, pats, ' '.join(s))[0::2]
            if av == 'OK' and bs == asw:
                continue
            captures += 1
            if hits_r5:
                r5_only += 1
            elif hits_ap:
                both += 1
            else:
                unexplained.append((c, s, bs, asw))
    return captures, r5_only, both, unexplained, sorted(glue), runs


CONFIGS = [
    ('print (_) | (_) otherwise (_)',
     ['a', 'nothing', 'found', 'otherwise', 'print'], ['a', 'nothing'],
     ['print _', '_ otherwise _']),
    ('send (_) to (_) | send (_)',
     ['a', 'b', 'send', 'to'], ['a', 'b'], ['send _ to _', 'send _']),
    ('sum of (_) | (_) otherwise (_)',
     ['x', 'sum', 'of', 'otherwise'], ['x'], ['sum of _', '_ otherwise _']),
]
t0 = time.time()
allbad = []
for title, uni, bn, ps in CONFIGS:
    cap, r5, ap, bad, g, runs = sweep(uni, bn, ps)
    allbad += bad
    print(f'  {title}')
    print(f'      glue={g}  anchor runs={[" ".join(r) for r in runs]}')
    print(f'      captures={cap:4}   explained by R5={r5:4}   '
          f'by anchor-prefix={ap:4}   unexplained={len(bad)}')
print(f'\n  ({time.time()-t0:.1f}s)')
print(f'\n  [{"PASS" if not allbad else "FAIL"}] the two rules together '
      f'{"explain every silent capture" if not allbad else "leave cases open"}')
for c, s, bs, asw in allbad[:8]:
    print(f'      declare «{" ".join(c)}» on «{" ".join(s)}»: {bs} -> {asw}')
print()

print('=' * W)
print('4. What the rule costs')
print('=' * W)
print('''  Glue reserves a WORD everywhere:      «to» kills  time to live,
                                        due to date, path to file, ...
  An anchor run reserves a PREFIX only: «sum of» kills only names that
                                        literally begin «sum of ...».

  So the price is far lower, but it is not zero, and «RESERVED (0)» does
  not currently count it. Names blocked by each, over the seed registry:''')
runs = ['print', 'sum of', 'count of', 'average of', 'first of', 'last of',
        'length of', 'item', 'sort', 'filter', 'send', 'broadcast', 'join',
        'split', 'round', 'rounded', 'while', 'if', 'repeat', 'for each']
print()
for r in ['sum of', 'send', 'print', 'for each']:
    print(f'    «{r}» blocks names beginning «{r} ...»  '
          f'— e.g. «{r} total», «{r} count»')
print(f'''
  The dangerous ones are the SHORT single-word anchors -- «send», «print»,
  «sort», «filter», «round», «item» -- because those are exactly the words a
  programmer reaches for at the head of a variable name: «send queue»,
  «print job», «sort order», «filter text», «round trip», «item count».
  «round trip» and «item count» are ordinary names and this rule kills them.''')
print()
