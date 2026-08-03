#!/usr/bin/env python3
"""Is «match <expr> [ arms ]» grammatically sound?

It is the same shape as «if (_) {_}»: a FREE hole followed by a BRACKETED one.
`if_expression.py` measured that shape -- the bracket fixes where the free hole
must stop, so the construct is determinate in extent and its glue set is empty.
This confirms it for the match spelling and for a multi-word discriminant.
"""

from bracket_probe import BProbe, BHOLE, pat_str, glue
from ronin_grammar_probe import Scope, HOLE

W = 74
MATCH = ('match', HOLE, BHOLE)
IFPAT = ('if', HOLE, BHOLE)

print('=' * W)
print('1. What the shape costs')
print('=' * W)
for p in (MATCH, IFPAT):
    print(f'  {pat_str(p):26} glue blanket={sorted(glue(p))!s:8} '
          f'refined={sorted(glue(p, "refined"))}')
print('''
  Empty either way. «match» is an anchor and the arms are bracketed, so the
  construct reserves no words at all -- the same result «if» got.''')

print('=' * W)
print('2. Is the discriminant determinate?')
print('=' * W)
NAMES = frozenset({('y',), ('type', 'of', 'y'), ('a',), ('b',), ('a', 'b')})
sc = Scope(names=NAMES, patterns=frozenset({MATCH}))
ok = True
for src in ['match y ( a )',
            'match type of y ( a )',
            'match a b ( a )',
            'match a + b ( a )']:
    v, w, parses = BProbe(sc).resolve(src)
    print(f'  {src:28} -> {v:9} {w}')
    ok &= (v == 'OK')
print(f'''
  [{"PASS" if ok else "FAIL"}] every form resolves uniquely

  The discriminant is a free hole, but the arms are not: the bracket fixes
  where it must stop. So a multi-word discriminant — «type of y» — is read
  whole, and so is one containing an operator.''')
