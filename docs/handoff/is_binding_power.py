#!/usr/bin/env python3
"""
is_binding_power.py -- where does «is» sit?

Their table: otherwise 6, PatternBindingPower 7, + - 10, * / 20, @ 21.

Proposal to test: the is family at 5, left-associative -- below «otherwise»,
well below arithmetic, leaving 1-4 free for a future «and»/«or».

Each candidate is checked by the reading it produces, not by analogy with other
languages.
"""

import io, contextlib
with contextlib.redirect_stdout(io.StringIO()):
    from word_infix_ops import WordOpResolver          # module prints on import
from dp_resolver import N, PA, OPS

W = 78

# their table, plus the candidate
OPS.setdefault('<>', (5, True))
PATS = PA('sum of _', 'not _')
NAMES = N('a', 'b', 'c', 'd', 'x', 'y', 'total', 'count', 'flag')


def ops_at(bp):
    return {('is',): (bp, True), ('is', 'not'): (bp, True),
            ('is', 'a'): (bp, True), ('otherwise',): (6, True)}


def show(bp, src, names=NAMES):
    v, c, s = WordOpResolver(names, PATS, ops_at(bp)).resolve(src)
    return v, s


CASES = [
    ('a + b is c + d', 'comparison must be looser than arithmetic'),
    ('total otherwise 0 is c', 'otherwise on the left operand'),
    ('a is total otherwise 0', 'otherwise on the right operand'),
    ('sum of a is b', 'a pattern call as an operand'),
    ('not a is b', '«not» is a pattern at bp 7'),
    ('a is b is c', 'chained comparison'),
]

print('=' * W)
print('1. Readings at each candidate binding power')
print('=' * W)
for bp in (5, 8, 11):
    print(f'\n  is = {bp}   (otherwise 6, pattern 7, + - 10, * / 20)')
    for src, why in CASES:
        v, s = show(bp, src)
        print(f'      {src:24} {v:14} {s}')

print()
print('=' * W)
print('2. What the readings say')
print('=' * W)
print("""  Two constraints, both decisive, both from the readings rather than from
  analogy:

  1. is must be BELOW PatternBindingPower (7). At 8, «sum of a is b» reads as
     «sum of (a is b)» -- the pattern swallows the comparison, because a
     trailing free hole parses its argument at the pattern's own level. Every
     comparison written after a pattern call would be wrong.

  2. is must be BELOW otherwise (6). At 8, «a is total otherwise 0» reads as
     «(a is total) otherwise 0» -- the fallback catches the comparison's
     result, which is a truth and can never be nothing. The thing that might
     be nothing is «total», and only «is» < «otherwise» attaches the fallback
     to the operand.

  At 11 it is worse: «a + b is c + d» becomes «(a + (b is c)) + d».

  Two readings that are the same at every candidate, and worth recording:

     «not a is b»   ->  «(not a) is b» at any bp below 7, because «not (_)» is
                        a PATTERN and binds at 7. That is not what the English
                        suggests, and it is the argument for «is not» being its
                        own operator rather than composed -- already the plan.

     «a is b is c»  ->  «(a is b) is c», left-associative: a truth compared to
                        c. A TYPE error rather than a parse error, which is the
                        right place for it -- "you compared a truth to a number"
                        beats "unexpected is".
""")

print('=' * W)
print('3. Recommendation')
print('=' * W)
print('''      is, is not, is a, is an, is not a, is not an   ->   5, left

  and the space that leaves is the point of choosing 5 over 4 or 1:

      1-4    free for «and», «or» -- which must be LOOSER than comparison,
             so that «a is b and c is d» groups as two comparisons
      5      the is family
      6      otherwise
      7      pattern calls
      10-21  arithmetic and indexing

  One consequence worth stating in the spec beside the number: because
  «and»/«or» are not built yet, nothing today distinguishes 5 from 1 -- so
  the reason for 5 is the reservation, and it should be written down or the
  next person will "simplify" it.''')
