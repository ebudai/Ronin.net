#!/usr/bin/env python3
"""
«if» as an expression with brace-delimited blocks, checked against the probe.

Budai wants Rust's shape: «if c { x }» where a block's value is its final
expression, so «if» replaces a ternary instead of inventing one.

The grammar question is what the pattern costs. Compare:

    if (_) then (_) otherwise (_)     glue = {then, otherwise}   two words
    if (_) {_}                        glue = {}                  nothing

and whether a free condition hole followed immediately by a braced block is
determinate -- i.e. whether the condition can run past the «{».
"""

from ronin_grammar_probe import Scope, HOLE
from bracket_probe import BProbe, BHOLE, pat_str, glue

W = 74
def h(t): print('\n' + '=' * W + f'\n{t}\n' + '=' * W)
def ok(l, c):
    print(f'  [{"PASS" if c else "FAIL"}] {l}'); return c
res = []

WORDY = ('if', HOLE, 'then', HOLE, 'otherwise', HOLE)
BRACED = ('if', HOLE, BHOLE)

h('1. What each spelling costs')
for p in (WORDY, BRACED, ('if', HOLE, BHOLE, 'otherwise', BHOLE)):
    print(f'  {pat_str(p):38} glue blanket={sorted(glue(p))!s:24} '
          f'refined={sorted(glue(p, "refined"))}')
res.append(ok('«if (_) {_}» reserves nothing under either policy',
              glue(BRACED) == set()))
res.append(ok('the word-glue spelling costs two words',
              glue(WORDY) == {'then', 'otherwise'}))
print('''
  Note the third line: even «if (_) {_} otherwise {_}» costs nothing under the
  refined rule, because «otherwise» sits between two braced blocks and no name
  can straddle a bracket. So the braced shape is free either way.''')

h('2. Is the condition determinate? Can it run past the brace?')
NAMES = frozenset({('a',), ('b',), ('c',), ('a', 'b')})
sc = Scope(names=NAMES, patterns=frozenset({BRACED}))
for src in ['if a ( c )', 'if a b ( c )', 'if a + b ( c )']:
    v, w, parses = BProbe(sc).resolve(src)
    print(f'  {src:22} -> {v:9} {w}')
    print(f'      all parses: {parses}')
res.append(ok('single-word condition, unique',
              BProbe(sc).resolve('if a ( c )')[0] == 'OK'))
res.append(ok('multi-word condition, unique',
              BProbe(sc).resolve('if a b ( c )')[0] == 'OK'))
res.append(ok('operator in the condition, unique',
              BProbe(sc).resolve('if a + b ( c )')[0] == 'OK'))
print('''
  The condition is a free hole, but the block that follows it is not: the
  bracket fixes where the condition must stop. A free hole followed by a
  bracketed hole is therefore determinate in extent even though a free hole
  alone is not -- which is the same property that makes the braced shape free.''')

h('3. «if» nested in the condition of another «if»')
v, w, parses = BProbe(sc).resolve('if if a ( b ) ( c )')
print(f'  if if a ( b ) ( c )   -> {v:9} {w}')
res.append(ok('legal and unique, if unlovely', v == 'OK'))
print('''
  Grammatically fine. This is a formatter problem, not a grammar problem --
  bracket the inner one and it reads. Worth a lint, not a rule.''')

h('4. The composition worth noticing')
print('''  «if c { a }» with no alternative has no value when c is false. The
  language already has «nothing» and «optional T» for exactly that, so:

      if c { a }                  optional T
      if c { a } otherwise { b }  T

  and the second is not a new form -- it is the POSTFIX «otherwise» the
  language already has for catching nothing and error, applied to an optional.
  One «otherwise», one meaning: "when the left side produced nothing, use this
  instead." The conditional and the error-handling uses become the same
  operator rather than two words that happen to rhyme.

  That also means «else» should not exist. One word.''')

print('\n' + '=' * W)
print(f'  {sum(res)}/{len(res)} checks pass')
print('=' * W)
