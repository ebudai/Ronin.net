#!/usr/bin/env python3
"""
reserve_cost.py -- what the five rulings cost in reserved names.

Same corpus method as glue_cost.py: harvest every identifier from a large
Python source tree, split camelCase / snake_case into word runs, and ask how
many multi-word identifiers a proposed anchor-only pattern would refuse.

An anchor-only pattern «w (_)» costs: no name may BEGIN with w.
A type name «w» costs: no name may BE w  (one table -- see §4).

Measured for: return, truth, nothing, optional, and the type-name set.
"""

import os, re, sys, collections

IDENT = re.compile(r'[A-Za-z_][A-Za-z0-9_]*')
CAMEL = re.compile(r'[A-Z]?[a-z0-9]+|[A-Z]+(?![a-z])')
W = 78


def words(ident):
    out = []
    for part in ident.split('_'):
        if part:
            out.extend(m.group().lower() for m in CAMEL.finditer(part))
    return out


ROOTS = [p for p in ['/usr/lib/python3.10', '/usr/local/lib/python3.11/dist-packages',
                     '/root/.local/lib/python3.11/site-packages',
                     '/usr/lib/python3.11'] if os.path.isdir(p)]

seen = set()
files = 0
for root in ROOTS:
    for dirpath, _, names in os.walk(root):
        for n in names:
            if not n.endswith('.py'):
                continue
            try:
                src = open(os.path.join(dirpath, n), 'r',
                           encoding='utf-8', errors='ignore').read()
            except OSError:
                continue
            files += 1
            for m in IDENT.finditer(src):
                seen.add(m.group())

allnames = {i: words(i) for i in seen}
multi = {i: w for i, w in allnames.items() if len(w) > 1}
single = {i: w for i, w in allnames.items() if len(w) == 1}

print('=' * W)
print('Corpus')
print('=' * W)
print(f'  roots            {len(ROOTS)}   {", ".join(ROOTS)}')
print(f'  files            {files}')
print(f'  identifiers      {len(allnames)}')
print(f'  multi-word       {len(multi)}')
print(f'  single-word      {len(single)}')
print()

# ---------------------------------------------------------------------------
print('=' * W)
print('1. Anchor-only patterns: no name may BEGIN with the anchor')
print('=' * W)
print(f'  {"anchor":14} {"names refused":>14} {"% of multi-word":>16}   examples')
print('  ' + '-' * 70)
ANCHORS = ['return', 'result', 'give', 'answer',
           'optional', 'maybe', 'perhaps',
           'old', 'previous', 'wait']
for a in ANCHORS:
    hits = [i for i, w in multi.items() if w[0] == a]
    ex = ', '.join(sorted(hits, key=len)[:3])
    print(f'  {a:14} {len(hits):>14} {100*len(hits)/len(multi):>15.3f}%   {ex[:34]}')

# ---------------------------------------------------------------------------
print()
print('=' * W)
print('2. Type names in ONE table: no name may BE the type name')
print('=' * W)
print('  (a whole-name collision, not a prefix one -- much cheaper)')
print()
print(f'  {"type name":14} {"exact collisions":>17} {"as first word":>15}   verdict')
print('  ' + '-' * 68)
TYPES = ['truth', 'nothing', 'number', 'text', 'list', 'lookup', 'optional']
for t in TYPES:
    exact = [i for i, w in allnames.items() if w == [t]]
    first = [i for i, w in multi.items() if w[0] == t]
    v = 'free' if not exact else f'{len(exact)} name(s) collide'
    print(f'  {t:14} {len(exact):>17} {len(first):>15}   {v}')

print(f'''
  Whole-name reservation is the cheap kind. A TYPE is a name, not a pattern, so
  it reserves only its own spelling -- «number» -- and leaves «number of
  items», «text buffer», «list head» untouched. That is the second column, and
  it is the column that would have been reserved had types been PATTERNS.''')

# ---------------------------------------------------------------------------
print()
print('=' * W)
print('3. What a SEPARATE type table would buy')
print('=' * W)
print('  It buys back exactly the exact-collision column above: the names that')
print('  are spelled the same as a type and used as values.')
print()
tot = 0
for t in TYPES:
    exact = [i for i, w in allnames.items() if w == [t]]
    tot += len(exact)
    if exact:
        print(f'  {t:14} {", ".join(sorted(exact)[:6])}')
print(f'''
  total identifiers recovered by a second table : {tot}
  as a share of all identifiers                 : {100*tot/len(allnames):.4f}%

  That is the entire prize. Against it: every name rule (R5', R6b, R7b, the
  self-ambiguity rule) has to be run twice and kept in step, and «type of x»
  makes a TYPE flow through a VALUE position -- so the two tables meet anyway,
  in the one place where a collision is unrepairable.''')

# ---------------------------------------------------------------------------
print()
print('=' * W)
print('4. «return» compared to the reservations already taken')
print('=' * W)
BENCH = [('old', 'ruled: pattern, accepted'), ('wait', 'ruled: accepted'),
         ('return', 'THIS RULING'), ('optional', 'THIS RULING'),
         ('previous', 'the cheaper alternative to old')]
rows = []
for a, note in BENCH:
    hits = [i for i, w in multi.items() if w[0] == a]
    rows.append((a, len(hits), 100*len(hits)/len(multi), note))
rows.sort(key=lambda r: -r[2])
print(f'  {"anchor":12} {"refused":>9} {"%":>9}   note')
print('  ' + '-' * 66)
for a, n, p, note in rows:
    print(f'  {a:12} {n:>9} {p:>8.3f}%   {note}')
