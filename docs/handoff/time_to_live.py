#!/usr/bin/env python3
"""
time_to_live.py -- can «time to live» be a legal name?

Budai's ask: it should be legal, and bracketing at the use site is an
acceptable price. Worth taking seriously rather than defending the blanket
rule, because two things I have been assuming turn out to be false.

  1. Interior glue is NOT dangerous on its own. «send (_) to (_)» alone cannot
     be captured by «time to live» -- the capture needs a SHORTER SIBLING
     pattern («send (_)») for the name to be an argument of. Without it there
     is no rival reading and the name is harmless.

  2. Where the sibling does exist, the capture is real -- but "silent" is doing
     less work than I have been giving it. Ronin already requires the reader to
     know the symbol table; that is what spaces-in-names means. A reader who
     knows «time to live» is in scope reads «send time to live» the same way
     minimum lookup does.

What is actually left is the AT-A-DISTANCE case: a declaration added later
changes a statement written earlier. And that has an instrument already
designed for it, one level up.
"""

import itertools
from dp_resolver import DPResolver, N, PA

W = 78
TTL = ('time', 'to', 'live')


def res(names, pats, src):
    v, c, s = DPResolver(names, pats).resolve(src)
    return v, c, s


print('=' * W)
print('1. Interior glue needs a shorter sibling to be dangerous')
print('=' * W)
BASE = N('time', 'live', 'message', 'server')
CONFIGS = [
    ('send (_) to (_)            alone', PA('send _ to _')),
    ('send (_) to (_) | send (_)      ', PA('send _ to _', 'send _')),
]
for label, pats in CONFIGS:
    print(f'  {label}')
    for src in ('send time to live', 'send message to server'):
        bv, bc, bs = res(BASE, pats, src)
        av, ac, asw = res(BASE | {TTL}, pats, src)
        tag = ('unchanged' if (bv, bs) == (av, asw)
               else 'CAPTURE' if av == 'OK' else av)
        print(f'      {src:24} {bv:9}{bc} -> {av:9}{ac}   {tag}')
    print()
print('''  Without the sibling there is nothing for «time to live» to be an
  argument OF, so the only reading is the two-argument call and the name is
  harmless. That is the whole hazard condition, and it is checkable against the
  pattern table at declaration time -- the same species of conditionality
  already accepted for R7b's article.

  Note also what is untouched in BOTH rows: «send message to server» does not
  care that «time to live» exists. The collision is not "«to» is unusable in
  names" -- it is "this exact phrase is now also a name".''')

print('=' * W)
print('2. Where the sibling exists: what is actually lost')
print('=' * W)
PATS = PA('send _ to _', 'send _')
for names, label in ((BASE, 'without the name'), (BASE | {TTL}, 'with the name')):
    for src in ('send time to live', 'send ( time ) to ( live )',
                'send ( time to live )'):
        v, c, s = res(names, PATS, src)
        print(f'  {label:18} {src:28} {v:9} {c}  {s}')
    print()
print('''  Both readings stay reachable by bracketing, in both directions. So the
  price Budai offered to pay is available and sufficient -- what he cannot get
  by bracketing is the UNBRACKETED form meaning the call, and that is the thing
  being traded away.''')

print('=' * W)
print('3. The at-a-distance case, and the instrument for it')
print('=' * W)
print('''  The residual hazard is not the reading -- it is the EDIT. Someone adds
  «var time to live» in an outer scope and a statement written earlier changes
  meaning with no diagnostic.

  MODULE-MERGE.md §4 already specified the instrument for exactly this shape,
  one level up:

      "an import may not change the reading of any statement already in the
       importing module"

  Applied to declarations rather than imports it is the same check:

      a DECLARATION may not change the reading of any statement already in
      its scope
''')
before = {}
STMTS = ['send time to live', 'send message to server', 'send time',
         'send ( time ) to ( live )']
for s in STMTS:
    v, c, sh = res(BASE, PATS, s)
    if v == 'OK':
        before[s] = sh
print(f'  declaring «time to live» -- checking {len(before)} statements in scope:')
changed = []
for s, bs in before.items():
    av, ac, asw = res(BASE | {TTL}, PATS, s)
    if (av, asw) != ('OK', bs):
        changed.append((s, bs, asw))
        print(f'      REJECT  «{s}»')
        print(f'              was: {bs}')
        print(f'              now: {asw}')
    else:
        print(f'      ok      «{s}»')
print(f'''
  So «time to live» is declarable, and it is refused only in a scope that
  already contains a statement it would re-read -- with the statement named, so
  the fix is a bracket on ONE line rather than a rename of the variable.

  Cost: the check is per-declaration over the scope, and it only has to look at
  statements containing the name's token run. A token index makes that cheap,
  and the always-running environment is the reason it is affordable at all.''')

print('=' * W)
print('4. What still has to be refused unconditionally')
print('=' * W)
ALLGLUE = PA('send _ to _')
for extra, label in ((set(), 'base'),
                     ({('to',)}, '+ «to»'),
                     ({('to',), ('to', 'to')}, '+ «to» and «to to»')):
    v, c, s = res(N('a', 'b') | extra, ALLGLUE, 'send to to to to')
    print(f'  send to to to to   {label:22} {v:14} {c}  {s}')
print('''
  The all-glue clause fires with NO sibling pattern -- the two readings are two
  placements of the same literal, so there is no shorter form to blame and no
  edit to point at. That one stays blanket, and it is a good demonstration that
  "make it conditional" is not a general answer, only the right answer where
  the hazard has a condition.''')
