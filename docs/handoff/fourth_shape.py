#!/usr/bin/env python3
"""
fourth_shape.py -- does the root law predict a shape ZERO-GLUE.md missed?

«ZERO-GLUE.md» lists three free shapes: anchor-only, symbol separator, and a
glue word PRECEDED by a bracket-delimited hole. The programmer's root law is
stated over spans, so it should also cover a glue word FOLLOWED by one -- and a
glue word whose neighbouring hole is pinned to a single token.

Four spellings of the same operator, plus the loop, run against the declaration
that captures the free form.
"""

import itertools
from dp_bracket import BDPResolver, PB, pstr, BHOLE, THOLE
from dp_resolver import HOLE, N

W = 78


def res(names, pats, src):
    v, c, s = BDPResolver(names, pats).resolve(src)
    return v, c, s


print('=' * W)
print('1. Four spellings of «otherwise», against the name that captures it')
print('=' * W)
SPELLINGS = [('_ otherwise _', 'x otherwise y', 'x otherwise y'),
             ('_ otherwise {', 'x otherwise ( y )', 'x otherwise y'),
             ('{ otherwise _', '( x ) otherwise y', 'x otherwise y'),
             ('{ otherwise {', '( x ) otherwise ( y )', 'x otherwise y')]
base = N('x', 'y')
print(f'  {"pattern":22} {"source":22} {"before":>8} {"after":>8}  verdict')
print('  ' + '-' * 72)
for spec, src, decl in SPELLINGS:
    pats = PB(spec)
    bv, bc, bs = res(base, pats, src)
    av, ac, asw = res(base | {tuple(decl.split())}, pats, src)
    tag = ('n/a' if bv != 'OK' else
           'unchanged' if (bv, bs) == (av, asw) else 'CAPTURE')
    print(f'  {pstr(next(iter(pats))):22} {src:22} {bc:>8} {ac if av=="OK" else "-":>8}'
          f'  {tag}')
    if tag == 'CAPTURE':
        print(f'      {bs}   ->   {asw}')
print('''
  Only the fully-free spelling captures. The moment either operand must be
  bracketed, the composite's span stops being word-only and no name can cover
  it -- so «otherwise» would need no reservation. That is a FOURTH free shape:
  a glue word FOLLOWED by a bracket-delimited hole, not only preceded by one.''')
print()

print('=' * W)
print('2. The one-token hole -- and the degenerate-control trap, again')
print('=' * W)
print('''  Run WITHOUT a rival pattern first, because that is the mistake I made
  last time and it looks like a pass:
''')
base1 = N('item', 'list')
base2 = N('item', 'list', 'item in list')
NORIVAL = [('for each _ in _',), ('for each < in _',)]
RIVAL = [('for each _ in _', 'for each _'), ('for each < in _', 'for each _')]
for label, sets in (('no rival', NORIVAL), ('with rival «for each (_)»', RIVAL)):
    print(f'  {label}:')
    for spec in sets:
        pats = PB(*spec)
        bv, bc, bs = res(base1, pats, 'for each item in list')
        av, ac, asw = res(base2, pats, 'for each item in list')
        tag = ('n/a' if bv != 'OK' else
               'unchanged' if (bv, bs) == (av, asw) else 'CAPTURE')
        names = ' | '.join(sorted(pstr(p) for p in pats))
        print(f'    {names:40} {bv:9}->{av:9} {tag}')
        if tag == 'CAPTURE':
            print(f'        {bs}  ->  {asw}')
    print()
print('''  Without a rival, BOTH spellings pass and the run means nothing -- the
  literal «in» is mandatory, so a name spanning it has nowhere to go. Only
  with the rival does the difference appear, and it is not the difference I
  expected:

    free spelling    silent capture  -- «for each «item in list»», cost 2
                                        beats «for each «item» in «list»», 3
    pinned spelling  TIE -> ERROR    -- both readings cost 2, so the table
                                        counts two derivations and refuses

  So the one-token hole does NOT make the name harmless. It equalises the two
  costs, which converts a SILENT misreading into a LOUD one. That is a real
  improvement and a weaker claim than "«in» needs no reservation" -- the word
  is safe from silent capture, not from collision, and the program is rejected
  until it is bracketed.''')
print()

print('=' * W)
print('3. So the three "free shapes" are one law with three ways to satisfy it')
print('=' * W)
print('''
  A word inside a pattern needs reserving iff SOME contiguous word-only span
  covers a composite reading that includes it. Three ways to prevent that:

    symbol          the span cannot be word-only  -- a name has no symbols
    bracketed hole  the span cannot be word-only  -- a name has no brackets
                    ... on EITHER side of the word, which is the fourth shape

  and one weaker mechanism that does not prevent coverage but removes the
  silence:

    one-token hole  equalises the two costs       -- TIE, not capture

  Anchor-only patterns are NOT on this list. They are the case where the span
  IS coverable, which is exactly why R6b was needed. «ZERO-GLUE.md» has
  anchor-only in the free column; on this law it belongs in the other one,
  with the note that what it costs is a prefix rather than a word.
''')
