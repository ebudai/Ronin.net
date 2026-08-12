#!/usr/bin/env python3
"""
dont_do_that.py -- how much of the self-ambiguity rule survives a type checker?

Budai's position: maximum composition, and most of what we worry about is
"don't do that" territory. He has been right every time he pushed on this --
«time to live», «is valid», «to uppercase», the whole prefix-free clause.

The self-ambiguity rule is the one place I have pushed back, on the grounds that
brackets GROUP and do not CLASSIFY, so those names remove a program from the
language rather than merely making one confusing.

But brackets are not the only disambiguator. TYPES are, and filtering by
well-typedness is not a silent pick -- it is elimination, which is exactly the
tie-break Budai proposed for the original design. So the question is how much of
the rule the type checker takes back.

Modelled here: each reading carries a type, each position demands one, and a
reading that cannot be that type is eliminated. Not a preference -- a filter.
"""

W = 78

# reading -> (kind, type)   'nothing' = an action, usable only as a statement
CASES = [
    ('send price',
     [('send «price»', 'call', 'nothing'),
      ('«send price»', 'name', 'number')],
     'a name colliding with an ACTION pattern'),
    ('print job',
     [('print «job»', 'call', 'nothing'),
      ('«print job»', 'name', 'text')],
     'a name colliding with an ACTION pattern'),
    ('sum of items',
     [('sum of «items»', 'call', 'number'),
      ('«sum of items»', 'name', 'number')],
     'a name colliding with a VALUE pattern, same type'),
    ('x is y',
     [('(«x» is «y»)', 'operator', 'truth'),
      ('«x is y»', 'name', 'truth')],
     'a name spanning an operator, and itself a truth'),
    ('x is y',
     [('(«x» is «y»)', 'operator', 'truth'),
      ('«x is y»', 'name', 'number')],
     'the same name, but a number'),
]

POSITIONS = [('statement', {'nothing'}), ('value', {'number', 'text', 'truth'})]

print('=' * W)
print('What a type filter recovers')
print('=' * W)
print(f'  {"statement":16} {"position":10} {"survivors":>10}   verdict')
print('  ' + '-' * 68)
recovered = residual = 0
for src, readings, why in CASES:
    for pos, admits in POSITIONS:
        live = [r for r in readings if r[2] in admits]
        v = ('UNIQUE' if len(live) == 1 else
             'ambiguous' if len(live) > 1 else 'no reading')
        print(f'  {src:16} {pos:10} {len(live):>10}   {v}'
              f'{"  <- " + live[0][0] if len(live) == 1 else ""}')
    print(f'  {"":16} {why}')
    print()
    if all(len([r for r in readings if r[2] in a]) <= 1 for _, a in POSITIONS):
        recovered += 1
    else:
        residual += 1

print(f'''  recovered by the filter : {recovered} of {len(CASES)}
  still ambiguous         : {residual}

  The split is not arbitrary. A name collides with a pattern either way, but:

      ACTION pattern   the call is «nothing» and the name is a value, so the
                       position decides -- «send price» as a statement is the
                       call, as a value it is the name. Both legal, no rule
                       needed.
      VALUE pattern    both readings are the same type in the same position, so
                       nothing eliminates either.

  And that lands almost exactly where Budai wants it. The names the current rule
  costs -- «wait time», «send queue», «print job», «sort order» -- collide with
  ACTION patterns, and the filter takes all of them back. What stays refused is
  a name that duplicates a VALUE pattern's own computation, like «sum of items»
  beside «sum of (_)» -- a name you would rarely write, because the pattern
  already computes it.

  So the honest statement is not "the rule is wrong" and not "the rule is
  needed". It is:

      the self-ambiguity rule is a PRE-TYPE-CHECKER approximation, and it should
      shrink to «a name may not have another reading of the same type in the
      same position» when the type checker lands.

  That is a much narrower rule, it may be close to empty in practice, and the
  cases it keeps are the ones where "don't do that" genuinely cannot help --
  because there is nothing for the compiler or the reader to go on.''')
