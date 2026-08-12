#!/usr/bin/env python3
"""
miss_result.py -- what does a lookup miss give? Settling a contradiction between
two of my own documents.

  EAGGREGATES2 §8              Error. Because if a miss gives «nothing», then
                               «lookup of K (optional V)» cannot distinguish
                               ABSENT from PRESENT-AND-NOTHING.

  NOTHING-AND-INDEXING §1      nothing. Because MATCH.md rests on it twice:
                               «] otherwise 0» is the ordinary operator rather
                               than match syntax, and EXHAUSTIVENESS is the
                               absence of nothing -- arms covering every case
                               give «T», arms missing one give «optional T».

The second has two dependents and the first has one objection, so the objection
is what has to be tested. It rests on an assumption I did not state:

    that «optional (optional V)» collapses to «optional V».

FIVE-RULINGS §5 made «optional (_)» an ordinary generic type CONSTRUCTOR, formed
like «list of (_)». Constructors nest. So the assumption is false, and this
enumerates what each policy can actually observe.
"""

W = 78
ABSENT = ('absent',)
NOTHING = ('nothing',)


def probe(policy, stored, nests):
    """What does «m [ k ]» yield, and can the caller tell the two cases apart?"""
    if stored is ABSENT:
        return 'Error' if policy == 'error' else ('nothing' if not nests else 'nothing')
    # the key IS present and its value is the nothing constant
    if policy == 'error':
        return 'nothing'
    return 'nothing' if not nests else 'present (nothing)'


print('=' * W)
print('«m [ k ]» where m : lookup of K (optional V)')
print('=' * W)
print(f'  {"policy":34} {"key absent":>18} {"present, = nothing":>20}   tell apart?')
print('  ' + '-' * 76)
ROWS = [
    ('miss -> Error',                       'error', False),
    ('miss -> nothing, optionals COLLAPSE', 'nothing', False),
    ('miss -> nothing, optionals NEST',     'nothing', True),
]
for label, policy, nests in ROWS:
    a = probe(policy, ABSENT, nests)
    b = probe(policy, NOTHING, nests)
    print(f'  {label:34} {a:>18} {b:>20}   {"YES" if a != b else "no"}')

print('''
  So the objection in EAGGREGATES2 §8 holds against exactly one of the three --
  the middle row, which is not the design. «optional (_)» is a type constructor
  formed the same way «list of (_)» is, so «optional (optional V)» is a distinct
  type in the same way «list of (list of V)» is, and the ABSENT case is nothing
  at the outer level while the present-and-nothing case is a present value that
  happens to be nothing.

  My §8 argument assumed a collapsing optional, which is what languages get when
  «optional» is a union with an absorbing null. Ronin's is not one.''')

print()
print('=' * W)
print('What the two policies do to everything else that is settled')
print('=' * W)
DEPS = [
    ('«] otherwise 0» is the ordinary operator',
     'works -- otherwise catches nothing',
     'works -- otherwise catches BOTH nothing and Error'),
    ('exhaustiveness = absence of nothing',
     'works -- a missing arm makes the type «optional T»',
     'BREAKS -- the type would be «T» and an Error is not in it, so '
     'exhaustiveness needs a separate analysis'),
    ('«nothing» does not propagate through arithmetic',
     'works, and STATICALLY -- «m [ k ] + 1» is a type error, because '
     '«optional V» is not «V»',
     'works, but at RUNTIME -- «m [ k ] + 1» type-checks and fails when it runs'),
    ('cutoff can compare the result',
     'works -- nothing is an ordinary value',
     'works -- Errors are equal by reason'),
]
print(f'  {"depends on":42} {"miss -> nothing":>16}')
print('  ' + '-' * 76)
for dep, withnothing, witherror in DEPS:
    print(f'  {dep}')
    print(f'      nothing : {withnothing}')
    print(f'      Error   : {witherror}')
    print()

print('''  The third row is the one that changes my mind rather than merely surviving.

  If «m [ k ]» is typed «optional V», then forgetting to handle a miss is a
  COMPILE-TIME type error -- «optional V» is not «V», so «m [ k ] + 1» does not
  check. Under Error it type-checks and fails at run time.

      >> a miss gives «nothing», and «m [ k ]» is typed «optional V»
      >> -- which is strictly stronger than Error, not weaker

  EAGGREGATES2 §8's table is wrong twice: the result type is «optional V» rather
  than «V», and that is what makes «nothing» the right answer instead of the
  compromise it looked like.''')

print()
print('=' * W)
print('And the case that is NOT the same: a list index out of range')
print('=' * W)
print('''  A missing key is DATA -- a question about a table that has an honest answer.
  An index past the end of a list is a MISTAKE -- there is no sense in which the
  fifth element of a three-element list is absent-but-askable.

  Typing «xs @ i» as «optional T» would put an «otherwise» on every list index
  in the language to pay for a case that is a bug wherever it happens.

      >> list index out of range  -> Error
      >> lookup miss              -> nothing, and the index is «optional V»

  Different because the two failures are different in kind, and that reason
  belongs in both reference entries so the split does not read as arbitrary.''')
