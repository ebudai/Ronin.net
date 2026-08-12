#!/usr/bin/env python3
"""
glue_position2.py -- run 1's counterexample was R6b's, not R5's. And the
narrowing has a cost run 1 did not look for.

Run 1 flagged «not x» as an edge-glue name that captures. It does -- but not as
an infix capture:

    not «x»   ->   «not x»

«not (_)» is an ANCHOR-ONLY prefix pattern, so this is R6b, which we already
adopted. The two rules cover disjoint shapes and the classification has to
respect that or R5′ gets blamed for R6b's work.

Re-run with R6b-refused names excluded. Then look for what run 1 did not: the
narrowing legalises edge-glue names like «a number», and «(_) is not a (_)»
beside «(_) is not (_)» may then TIE on «x is not a number».
"""

import collections
import itertools
from dp_resolver import DPResolver, N, PA, HOLE

W = 78


def res(names, pats, src):
    v, c, s = DPResolver(names, pats).resolve(src)
    return v, c, s


def wordcontent(p):
    return tuple(s for s in p if s is not HOLE)


def anchoronly(pats):
    return [wordcontent(p) for p in pats
            if p[-1] is HOLE and all(s is not HOLE for s in p[:-1])]


print('=' * W)
print('1. R5′ re-tested, with R6b-refused names excluded')
print('=' * W)


def sweep(universe, base_names, patterns, glue, maxsrc=4, maxname=3):
    pats = PA(*patterns)
    base = frozenset(tuple(x.split()) for x in base_names)
    runs = anchoronly(pats)
    srcs = [s for k in range(1, maxsrc + 1)
            for s in itertools.product(universe, repeat=k)]
    before = {s: res(base, pats, ' '.join(s))[0::2] for s in srcs}
    buckets, danger, ex = collections.Counter(), collections.Counter(), {}
    for k in range(2, maxname + 1):
        for c in itertools.product(universe, repeat=k):
            if c in base:
                continue
            if any(len(a) < len(c) and c[:len(a)] == a for a in runs):
                continue                       # already refused by R6b
            pos = [i for i, w in enumerate(c) if w in glue]
            if not pos:
                continue
            key = ('interior' if any(0 < i < len(c) - 1 for i in pos)
                   else 'edge only')
            buckets[key] += 1
            names = base | {c}
            for s in srcs:
                bv, bs = before[s]
                if bv != 'OK':
                    continue
                av, asw = res(names, pats, ' '.join(s))[0::2]
                if av != 'OK' or bs != asw:
                    danger[key] += 1
                    ex.setdefault(key, (c, s, bs, asw, av))
                    break
    return buckets, danger, ex


CONFIGS = [
    ('(_) is (_)  + rival «check (_)»',
     ['x', 'y', 'is', 'check'], ['x', 'y'], ['_ is _', 'check _'], {'is'}),
    ('(_) is (_) | (_) is not (_) | not (_)',
     ['x', 'y', 'is', 'not'], ['x', 'y'],
     ['_ is _', '_ is not _', 'not _'], {'is', 'not'}),
    ('send (_) to (_) | send (_)',
     ['a', 'b', 'send', 'to'], ['a', 'b'], ['send _ to _', 'send _'], {'to'}),
]
bad = False
for title, uni, bn, ps, g in CONFIGS:
    buckets, danger, ex = sweep(uni, bn, ps, g)
    print(f'\n  {title}')
    for key in ('edge only', 'interior'):
        print(f'      glue {key:10}  names {buckets[key]:4}   '
              f'dangerous {danger[key]:4}')
        if danger[key]:
            c, s, bs, asw, av = ex[key]
            tag = 'COUNTEREXAMPLE' if key == 'edge only' else 'e.g.'
            if key == 'edge only':
                bad = True
            print(f'        {tag} «{" ".join(c)}» on «{" ".join(s)}»: '
                  f'{bs} -> {av} {asw}')
print(f'''
  [{"PASS" if not bad else "FAIL"}] with R6b doing its own work, edge glue never captures.

      R5   (today)      no multi-word name may CONTAIN a glue word
      R5′  (narrowed)   no multi-word name may contain a glue word INTERIORLY
      R6b  (unchanged)  no name may begin with a pattern's whole word content
''')

print('=' * W)
print('2. The cost the narrowing introduces: «a» is an article AND a name word')
print('=' * W)
PATS = PA('_ is _', '_ is not _', '_ is a _', '_ is not a _')
print('  patterns: (_) is (_) | (_) is not (_) | (_) is a (_) | (_) is not a (_)')
print('  glue = {is, not, a}\n')
for extra, label in ((set(), 'without the name «a number»'),
                     ({('a', 'number')}, 'with the name «a number» (legal under R5′)')):
    names = N('x', 'number', 'text') | extra
    print(f'  {label}:')
    for src in ['x is a number', 'x is not a number', 'x is number',
                'x is not ( a number )']:
        v, c, s = res(names, PATS, src)
        print(f'      {src:26} {v:14} {c}  {s}')
    print()
print('''  «a number» is edge-glue, so R5′ admits it -- and then «x is a number»
  has two readings at the same cost. That is a TIE, so it is loud and
  bracket-repairable, not a silent capture. But it means declaring a name
  beginning «a » is a landmine for every type test in scope.

  Cheap fix, and it costs nothing else: treat the ARTICLE position as pinned.
  «(_) is a <_>» makes the type name exactly one token, so «a number» has no
  span to occupy.''')
PINNED = PA('_ is _', '_ is not _', '_ is a <', '_ is not a <')
names = N('x', 'number', 'text') | {('a', 'number')}
print('\n  with «(_) is a <_>» (one-token type name):')
for src in ['x is a number', 'x is not a number']:
    v, c, s = res(names, PINNED, src)
    print(f'      {src:26} {v:14} {c}  {s}')
print()

print('=' * W)
print('3. Does «is not» beat «is» + «not» without being ambiguous?')
print('=' * W)
P3 = PA('_ is _', '_ is not _', 'not _')
names = N('x', 'y')
for src in ['x is not y', 'x is ( not y )', 'not x']:
    v, c, s = res(names, P3, src)
    print(f'  {src:22} {v:14} {c} lookups   {s}')
print('''
  «x is not y» reads as inequality: one pattern and two names beats one
  pattern, one prefix call and two names. The negation-of-y reading is still
  reachable by bracketing, which is the standard repair.''')
