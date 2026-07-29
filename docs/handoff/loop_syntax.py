#!/usr/bin/env python3
"""
Loop syntax: is `for each bank in banks` safe given names may contain «in»?

The programmer implemented `iterate banks => bank` because a name containing
«in» looked like it would make the split point ambiguous. That instinct is
right about the mechanism and wrong about the outcome, because R5 already
covers it -- but only if R5 is actually enforced, and R5's price is that «in»
becomes reserved inside multi-word names.

Run this before deciding, because the interesting failure is NOT a tie. It is
a strictly-cheaper wrong reading, which is silent.

Modelling note: the loop variable is a DECLARING hole, so it always resolves
regardless of what is in scope. That is modelled by putting the candidate loop
variable in the name set -- which is the honest thing to do, and it makes the
competing reading STRONGER, not weaker.
"""

from dp_resolver import DPResolver, N, PA

W = 78
def h(t): print('\n' + '=' * W + f'\n{t}\n' + '=' * W)
def ok(label, cond):
    print(f'  [{"PASS" if cond else "FAIL"}] {label}'); return cond

results = []

PATS = PA('for each _ in _', 'count of _')

# =====================================================================
h('1. The ordinary case is unique, with or without the rule')

r = DPResolver(N('bank', 'banks'), PATS, pattern_bp=7).resolve('for each bank in banks')
print(f'  for each bank in banks   ->  {r[0]}, {r[1]} lookups\n      {r[2]}')
results.append(ok('unique', r[0] == 'OK'))

# =====================================================================
h('2. Two «in»s: the hazard is SILENT, not a tie')

SRC = 'for each order in transit in count of banks'

# «order in transit» is the loop variable -- a declaration, so always available.
# «transit in count of banks» is a name someone declared elsewhere, for another
# purpose, possibly in another file.
no_r5 = N('order', 'transit', 'banks',
          'order in transit',                 # declaring hole
          'transit in count of banks')        # an innocent name elsewhere

r = DPResolver(no_r5, PATS, pattern_bp=7).resolve(SRC)
print(f'  R5 OFF   {SRC}\n           -> {r[0]}, {r[1]} lookups\n           {r[2]}')

# what the author meant: loop variable «order in transit», collection «count of banks»
intended = DPResolver(N('order', 'transit', 'banks', 'order in transit'),
                      PATS, pattern_bp=7).resolve(SRC)
print(f'\n  intended (that name not declared)\n           -> {intended[0]}, '
      f'{intended[1]} lookups\n           {intended[2]}')

results.append(ok('with the extra name it still parses -- no tie, no error',
                  r[0] == 'OK'))
results.append(ok('and it parses to a DIFFERENT program (silent capture)',
                  r[2] != intended[2]))
results.append(ok('the wrong reading is strictly cheaper, so nothing flags it',
                  r[1] < intended[1]))
print(f'''
  Exactly the «send hello to alice» shape from RONIN-GRAMMAR R5: a longer name
  swallows a call, costs fewer lookups, and wins outright. Declaring a name in
  another file silently rewrites a loop that already worked.''')

# =====================================================================
h('3. With R5 enforced, the hazard is not reachable')

print('''  R5: a multi-word name may not contain any word appearing after the first
  hole of an in-scope pattern.

      for each (_) in (_)      anchor = «for each»     glue = {in}

  So BOTH competitors are rejected at their declaration sites:
      «transit in count of banks»   rejected -- multi-word, contains «in»
      «order in transit»            rejected -- and it is the loop variable,
                                    so the error lands on the loop itself''')

r5_on = N('order', 'transit', 'banks')
r = DPResolver(r5_on, PATS, pattern_bp=7).resolve(SRC)
print(f'\n  R5 ON    {SRC}\n           -> {r[0]}')
results.append(ok('no reading at all -- the statement is unwritable, correctly',
                  r[0] == 'NO PARSE'))

# =====================================================================
h('4. Why uniqueness is structural, not lucky')

print('''  Under R5, the only «in» that can appear in a loop header is the pattern's
  own glue:

    - the loop variable is a declared name; multi-word ones cannot contain «in»
    - the collection is an expression; a name in it cannot contain «in»
    - the only pattern with «in» after a hole is «for each (_) in (_)» itself,
      and that needs its own «for each» anchor to appear

  One «in» means one split point means one reading. It is not that the
  competing readings tie and get caught -- there are no competing readings.''')

cases = [
    ('for each bank in banks',                    N('bank', 'banks')),
    ('for each open order in banks',              N('open order', 'banks')),
    ('for each bank in count of banks',           N('bank', 'banks')),
    ('for each bank in (count of banks)',         N('bank', 'banks')),
]
allok = True
for src, names in cases:
    r = DPResolver(names, PATS, pattern_bp=7).resolve(src)
    print(f'  {src:42} {r[0]:10} {r[1]} lookups')
    allok &= (r[0] == 'OK')
results.append(ok('every well-formed loop header resolves uniquely', allok))

# =====================================================================
h('5. What R5 costs, stated plainly')

victims = ['in flight order', 'logged in user', 'built in defaults',
           'in progress tasks', 'in memory cache', 'in stock items',
           'opt in list', 'sign in token', 'check in time']
print('  Multi-word names that become illegal the moment «for each (_) in (_)»')
print('  is in scope -- which, as a builtin, is everywhere:\n')
for v in victims:
    print(f'      {v}')
print(f'''
  {len(victims)} plausible names, all with decent renames («pending tasks»,
  «current user», «default settings»). But note the shape of the cost: it is
  paid by EVERY program, forever, so that ONE pattern can read well.

  Single-word «in» is still legal -- R5 only examines multi-word names. Worth
  a test either way.''')

# =====================================================================
h('6. The general rule this is an instance of')

print('''  Glue words are reserved words. The standard library's glue set IS the
  language's reserved-word list, and it grows every time a pattern with a word
  after a hole is added -- retroactively invalidating user names.

  The lever: a pattern whose words ALL precede its first hole has an EMPTY
  glue set and reserves nothing.

      sum of (_)                 anchor = «sum of»            glue = {}
      count of (_)               anchor = «count of»          glue = {}
      compute total for (_)      anchor = «compute total for» glue = {}
      for each (_) in (_)        anchor = «for each»          glue = {in}
      send (_) to (_)            anchor = «send»              glue = {to}
      repeat (_) times           anchor = «repeat»            glue = {times}

  So the stdlib rule is: put the words FIRST. Reach for word glue only where
  readability genuinely demands the interleaving, and treat each one as a
  deliberate reserved-word decision with a review, not as a style choice.''')

# =====================================================================
h('7. R6: «for each» forecloses any other pattern starting with «for»')

print('''  R6 is a leading-run prefix rule checked at scope entry, so it rejects
  «for (_)» beside «for each (_) in (_)» without looking at any statement.
  The resolver cannot demonstrate that; it runs after the check. So instead:
  can a competing reading be built at all, if R6 let the pair through?''')

R6PATS = PA('for each _ in _', 'for _')
tries = [
    ('for each bank in banks',  N('bank', 'banks')),
    ('for each bank in banks',  N('bank', 'banks', 'each bank')),
    ('for each bank',           N('bank', 'each bank')),
]
for src, names in tries:
    r = DPResolver(names, R6PATS, pattern_bp=7).resolve(src)
    extra = sorted(' '.join(n) for n in names)
    print(f'  {src:26} names={extra}\n      -> {r[0]:8} {r[2]}')

r = DPResolver(N('bank', 'banks', 'each bank'), R6PATS, pattern_bp=7).resolve(
    'for each bank in banks')
results.append(ok('no witness: «for (_)» cannot swallow a loop header under R5',
                  r[0] == 'OK'))
print('''
  It cannot -- because swallowing would need a name spanning «... in ...», and
  R5 has already banned those. So R6's rejection of the pair is CONSERVATIVE
  here, not load-bearing.

  Implement R6 as stated anyway: blanket, cheap, checked once at scope entry.
  But record that «for each» does not foreclose a future «for (_)» on
  ambiguity grounds -- only on R6's conservatism, which is a rule that could be
  refined later if that spelling is ever wanted. Do not let the two facts get
  conflated into "the loop syntax cost us «for»".''')

print('\n' + '=' * W)
print(f'  {sum(results)}/{len(results)} checks pass')
print('=' * W)
