#!/usr/bin/env python3
"""
return_arity.py -- two questions the programmer's NEEDFROMDESIGN raises.

§1  Can «return» and «return (_)» coexist as two builtin patterns without
    creating an ambiguity? They are prefix-related, which R6's prefix-free
    clause used to forbid and no longer does. Prefix-related is not the same as
    ambiguous, and the difference is measurable rather than arguable.

§2  What do the candidate truth literals cost? They are NULLARY entries -- names,
    not patterns -- so they reserve their own spelling and nothing else. Same
    corpus method as reserve_cost.py.
"""

import os, re
from dp_resolver import DPResolver, N, PA

W = 78
print('=' * W)
print('§1  «return» and «return (_)» as two builtins')
print('=' * W)

PATS = PA('return _', 'return', 'sum of _')
NAMES = N('x', 'value', 'ready')

for src in ['return', 'return x', 'return sum of x', 'return value']:
    v, c, s = DPResolver(NAMES, PATS).resolve(src)
    print(f'  {src:20} -> {v:10} cost={c}')

print(f'''
  No tie anywhere. The reason is the reason we keep relying on: there is NO
  JUXTAPOSITION, so bare «return» followed by «x» is not a composition -- it is
  simply not a reading. «return x» can only be the one-hole pattern, and
  «return» alone can only be the nullary one. Prefix-related, never ambiguous.

  So the arity split is mechanically free. It is a NAMING question, not a
  parsing one.''')

# ---------------------------------------------------------------------------
print()
print('=' * W)
print('§2  Truth literals -- whole-name reservation cost')
print('=' * W)

IDENT = re.compile(r'[A-Za-z_][A-Za-z0-9_]*')
CAMEL = re.compile(r'[A-Z]?[a-z0-9]+|[A-Z]+(?![a-z])')


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
            for m in IDENT.finditer(src):
                seen.add(m.group())

allnames = {i: words(i) for i in seen}
multi = {i: w for i, w in allnames.items() if len(w) > 1}

print(f'  corpus: {len(allnames)} identifiers, {len(multi)} multi-word')
print()
print(f'  {"literal":10} {"exact":>7} {"1st word":>9} {"any word":>9}   note')
print('  ' + '-' * 64)
CAND = [('true', 'matches the type name «truth»'),
        ('false', 'matches the type name «truth»'),
        ('yes', 'reads well in prose, badly after «is»'),
        ('no', 'also a very common English word'),
        ('on', 'state, not truth'),
        ('off', 'state, not truth'),
        ('done', 'candidate for the valueless exit, not a literal'),
        ('stop', 'already taken by the runtime -- disarms a «when»')]
for lit, note in CAND:
    exact = sum(1 for w in allnames.values() if w == [lit])
    first = sum(1 for w in multi.values() if w[0] == lit)
    anyw = sum(1 for w in multi.values() if lit in w)
    print(f'  {lit:10} {exact:>7} {first:>9} {anyw:>9}   {note}')

print(f'''
  Only the «exact» column is spent by a literal, because a nullary entry is a
  NAME and a name reserves its own spelling. The other two columns are what it
  would have cost as a PATTERN, and are shown only to make that concrete:
  «true» as a literal leaves «true positive», «true north» alone.

  Every candidate is affordable. This is a readability choice with no budget
  attached, which is the good kind.''')
