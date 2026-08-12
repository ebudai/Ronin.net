#!/usr/bin/env python3
"""
glue_as_name.py -- does R5′ narrow the pattern-glue rule too, and does
GlueAsName survive it?

His three shapes, and one thing to check before choosing between them: R5′ says
"no multi-word name may contain a glue word INTERIORLY". A one-word name has no
interior. But neither does a name made ENTIRELY of glue words -- «to to» has
«to» at index 0 and index 1, both edges -- and that is a case the rule as stated
admits without anyone deciding to.

Also checked: what R5′ does to the shadow-injection route GlueAsName originally
came from, because if that route stops firing the rule loses its capture
justification and becomes purely a legibility rule -- which is his question,
answered by measurement rather than by preference.
"""

import itertools
from dp_resolver import DPResolver, N, PA, HOLE

W = 78
PATS = PA('send _ to _', 'send _', 'print _')
GLUE = {'to'}


def res(names, src, pats=PATS):
    v, c, s = DPResolver(names, pats).resolve(src)
    return v, c, s


print('=' * W)
print('1. Does a ONE-WORD name equal to a glue word capture anything?')
print('=' * W)
BASE = N('a', 'b')
SRCS = [s for k in range(1, 5)
        for s in itertools.product(['a', 'b', 'send', 'to', 'print'], repeat=k)]
before = {}
for s in SRCS:
    v, c, sh = res(BASE, ' '.join(s))
    if v == 'OK':
        before[s] = sh
hits = []
for s, bs in before.items():
    av, ac, asw = res(BASE | {('to',)}, ' '.join(s))
    if av != 'OK' or asw != bs:
        hits.append((s, bs, av, asw))
print(f'  {len(before)} statements parse before declaring «to»')
print(f'  statements that change when «to» is declared: {len(hits)}')
for s, bs, av, asw in hits[:6]:
    print(f'      «{" ".join(s)}»: {bs} -> {av} {asw}')
print('''
  So GlueAsName is not a capture rule. A one-word name cannot straddle the
  literal it would have to cover. His read is right, and it is the reason the
  two findings needed separating rather than sharing Offender.''')

print('=' * W)
print('2. But a name made ENTIRELY of glue words has no interior either')
print('=' * W)
print('  «to to» -- glue at index 0 and index 1, both EDGES, so R5′ admits it.\n')
for extra, label in ((set(), 'base'),
                     ({('to',)}, '+ «to»'),
                     ({('to',), ('to', 'to')}, '+ «to» and «to to»')):
    for src in ('send to to to', 'send to to to to'):
        v, c, sh = res(BASE | extra, src)
        print(f'  {src:18} {label:22} {v:14} {c}  {sh}')
    print()
print('''  The four-token form does NOT tie -- I predicted it would and it does
  not, because placing the literal at the last position leaves the second hole
  empty. It takes five tokens for both placements to be viable:

      send «to» to «to to»      literal at position 2
      send «to to» to «to»      literal at position 3

  both at cost 3, so the statement becomes unwritable. Neither name contains
  glue interiorly, so R5′ as written admits both.

  This is not GlueAsName's job and it is not GlueInName's either. It is a gap
  in R5′ that neither of us stated:

      R5′ (as sent)   no multi-word name may contain a glue word INTERIORLY
      R5′ (repaired)  ... and no name may consist WHOLLY of glue words

  The second clause covers «to» and «to to» in one line, which means
  GlueAsName is the one-word case of a rule that has to exist anyway.''')

print('=' * W)
print('3. What R5′ does to the shadow-injection route')
print('=' * W)
print('''  SCOPING.md: «var seconds» injects «old seconds», which is multi-word, so
  R5 examines it and rejects it on glue «seconds» from «every (_) seconds».
  GLUE-AS-WHOLE-NAMES.md §1 pointed out that this had already reserved every
  glue word against every single-word REACTIVE name, invisibly.

  Under R5′:
''')
for name in (('old', 'seconds'), ('old', 'anything'), ('seconds',),
             ('old', 'x', 'seconds')):
    glue_like = {'seconds', 'old'}
    interior = [i for i, w in enumerate(name)
                if w in glue_like and 0 < i < len(name) - 1]
    allglue = all(w in glue_like for w in name)
    verdict = ('refused (interior)' if interior else
               'refused (wholly glue)' if allglue else 'ADMITTED')
    print(f'      «{" ".join(name):18}» {verdict}')
print('''
  «old seconds» is edge-glue, so R5′ admits it and the shadow route stops
  firing. That is the answer to his question: after R5′, GlueAsName has no
  capture justification left behind it -- the thing that used to back it was
  the shadow, and the shadow is now legal.

  It also closes something for free. GLUE-AS-WHOLE-NAMES.md §2 flagged that if
  any pattern used «old» as glue, EVERY reactive declaration in scope would
  produce a diagnostic about an injected name the author cannot rename -- "the
  worst diagnostic outcome in the language". Under R5′, «old anything» is
  edge-glue and admitted, so that hole closes without anyone aiming at it.''')

print('=' * W)
print('4. Where that leaves the three shapes')
print('=' * W)
print('''  shape 1  keep GlueAsName blanket for one-word names, narrow the
           multi-word containment                       <- his preference
  shape 2  narrow both, lose the legibility finding
  shape 3  leave glue blanket, «to uppercase» stays refused

  Shape 3 is out: ISANDEQUALITY.md §4 said «to uppercase» becomes legal and
  «time to live» stays refused, and that was the measured point of the
  narrowing -- 61% of the bill.

  Shape 2 is out for a reason §1 above did not supply and §2 does: the
  one-word case is not a separate legibility rule at all once you notice
  «to to». It is the degenerate case of "no name may consist wholly of glue
  words", which R5′ needs regardless. Deleting it would leave the two-word
  all-glue name admitted and «send to to to» unwritable.

  So shape 1, with the reason restated: GlueAsName survives not because
  legibility is worth a rule, but because it is one instance of a clause R5′
  was missing.''')
