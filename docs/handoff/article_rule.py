#!/usr/bin/env python3
"""
article_rule.py -- §2 of is_article.py was wrong too, and the right rule is
much smaller than either fix I proposed.

Namespaces do NOT remove the tie. The rival reading is not a type lookup, it is
the plain «(_) is (_)» with the VALUE name «a number» on the right:

    x is a «number»ᵗ     type test, 3 lookups
    x is «a number»      value comparison, 3 lookups

Separate tables cannot help, because both readings are already in the tables
they belong to. So the question is which names create the tie -- and the answer
is narrow enough to be its own rule.
"""

import collections
import itertools
import os
import re
import sys
from dp_resolver import DPResolver, N, PA

W = 78


def res(names, pats, src):
    v, c, s = DPResolver(names, pats).resolve(src)
    return v, s


print('=' * W)
print('1. Which names break a type test?')
print('=' * W)
PATS = PA('_ is _', '_ is a _', '_ is not _', '_ is not a _')
UNI = ['x', 'is', 'a', 'not', 'number']
BASE = N('x', 'number')
SRCS = [s for k in range(1, 5) for s in itertools.product(UNI, repeat=k)]
before = {s: res(BASE, PATS, ' '.join(s)) for s in SRCS}

startsA, other = [], []
for k in range(2, 4):
    for c in itertools.product(UNI, repeat=k):
        if c in BASE:
            continue
        names = BASE | {c}
        broke = None
        for s in SRCS:
            bv, bs = before[s]
            if bv != 'OK':
                continue
            av, asw = res(names, PATS, ' '.join(s))
            if av != 'OK' or bs != asw:
                broke = (s, bs, av, asw)
                break
        if broke:
            (startsA if c[0] in ('a', 'an') else other).append((c, broke))

print(f'  names that break something and BEGIN with an article: {len(startsA)}')
for c, (s, bs, av, asw) in startsA[:4]:
    print(f'      «{" ".join(c)}» on «{" ".join(s)}»: {bs} -> {av}')
print(f'\n  names that break something and do NOT begin with an article: '
      f'{len(other)}')
for c, (s, bs, av, asw) in other[:8]:
    print(f'      «{" ".join(c)}» on «{" ".join(s)}»: {bs} -> {av} {asw}')
print()

interior = [c for c, _ in other
            if any(0 < i < len(c) - 1 and w in ('is', 'not', 'a')
                   for i, w in enumerate(c))]
print(f'  of those {len(other)}, explained by R5′ (interior glue): '
      f'{len(interior)}')
left = [c for c, _ in other if c not in interior]
print(f'  unexplained: {len(left)}')
for c in left[:6]:
    print(f'      «{" ".join(c)}»')
print(f'''
  So the full rule set for «is» is three narrow rules, not one blanket one:

      R5′   no multi-word name may contain «is»/«not» INTERIORLY
      R6b   no name may begin with a pattern's whole word content
      R-art no name may BEGIN with «a» or «an»
''')

# ------------------------------------------------------------------ corpus
IDENT = re.compile(r'[A-Za-z_][A-Za-z0-9_]*')
CAMEL = re.compile(r'[A-Z]?[a-z0-9]+|[A-Z]+(?![a-z])')


def words(i):
    out = []
    for part in i.split('_'):
        if part:
            out.extend(m.group().lower() for m in CAMEL.finditer(part))
    return out


print('=' * W)
print('2. What each of the three costs, on the corpus')
print('=' * W)
seen = set()
for root in (sys.argv[1:] or ['/usr/lib/python3.10', '/usr/lib/python3',
                              '/root/.cache/uv', '/usr/local/lib']):
    for dp, _, ns in os.walk(root):
        for n in ns:
            if n.endswith('.py'):
                try:
                    src = open(os.path.join(dp, n), 'r', encoding='utf-8',
                               errors='ignore').read()
                except OSError:
                    continue
                for m in IDENT.finditer(src):
                    seen.add(m.group())
multi = {i: w for i in seen if len(w := words(i)) > 1}
total = len(multi)


def count(pred):
    return sum(1 for w in multi.values() if pred(w))


rows = [
    ('R5  blanket «is»', lambda w: 'is' in w),
    ('R5′ interior «is»', lambda w: any(0 < i < len(w) - 1 and x == 'is'
                                        for i, x in enumerate(w))),
    ('R5  blanket «not»', lambda w: 'not' in w),
    ('R5′ interior «not»', lambda w: any(0 < i < len(w) - 1 and x == 'not'
                                         for i, x in enumerate(w))),
    ('R5  blanket «a»', lambda w: 'a' in w),
    ('R-art  begins «a»/«an»', lambda w: w[0] in ('a', 'an')),
    ('R6b  begins «not»', lambda w: w[0] == 'not'),
]
print(f'  {total} distinct multi-word identifiers\n')
print(f'  {"rule":26} {"kills":>8} {"share":>9}')
print('  ' + '-' * 46)
for label, pred in rows:
    c = count(pred)
    print(f'  {label:26} {c:>8} {100.0*c/total:>8.3f}%')

blanket = count(lambda w: any(x in w for x in ('is', 'not', 'a')))
narrow = count(lambda w: (any(0 < i < len(w) - 1 and x in ('is', 'not')
                              for i, x in enumerate(w))
                          or w[0] in ('a', 'an', 'not')))
print(f'\n  {"TOTAL blanket (is/not/a anywhere)":34} {blanket:>7} '
      f'{100.0*blanket/total:>8.3f}%')
print(f'  {"TOTAL narrow (R5′ + R6b + R-art)":34} {narrow:>7} '
      f'{100.0*narrow/total:>8.3f}%')
print(f'  {"reduction":34} {100.0*(blanket-narrow)/blanket:>7.0f}%')

print('\n  still refused under the narrow rules — a sample:')
bad = [i for i, w in multi.items()
       if any(0 < k < len(w) - 1 and x in ('is', 'not')
              for k, x in enumerate(w)) or w[0] in ('a', 'an', 'not')]
print('      ' + ', '.join(sorted((b for b in bad if '_' in b), key=len)[:10]))
print('''
  Which is the point worth making to Budai: after the narrowing, what is
  still refused is what a human reader would also misparse. «y is x» reads
  as a comparison; «is valid» does not.''')
