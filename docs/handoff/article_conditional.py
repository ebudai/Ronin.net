#!/usr/bin/env python3
"""
article_conditional.py -- can the article ban fire only where it must?

Budai: "wondering if we can get away with only banning it when it would create
ambiguity."

The tie is «x is a number» reading two ways. That needs TWO things at once:

    a VALUE name  «a number»
    a TYPE name   «number»                 -- the remainder, exactly

If «queue» is not a type, «a queue» threatens nothing. So the candidate rule is

    R-art′  a name may begin with «a»/«an» unless the REMAINDER is a type name

which is checkable at declaration with one probe of the type table.

Whether that is a good idea turns on a distinction this project has already
drawn once and should not blur: R5 and R6 are ALREADY conditional on the
pattern table (SCOPING.md: an inner pattern can invalidate an outer name, and
the inner declaration is the one refused). What was rejected in
GLUE-AS-WHOLE-NAMES.md §1 was conditionality on USE SITES -- legality depending
on whether some statement somewhere turns out ambiguous. R-art′ is the first
kind, not the second.

Measured here with a namespace-aware resolver, because with one shared table
the question cannot even be asked.
"""

import itertools
from dp_bracket import BDPResolver, PB, pstr
from dp_resolver import N, HOLE

W = 78


class NS(BDPResolver):
    """A type-test pattern resolves its trailing hole in the TYPE table."""

    def __init__(self, names, typenames, patterns, typepats, **kw):
        super().__init__(names, patterns, **kw)
        self.typenames = typenames
        self.typepats = typepats

    def match(self, pat, si, t, pos, end):
        if pat in self.typepats and si == len(pat) - 1:
            w = tuple(v for k, v in t[pos:end] if k == 'word')
            if len(w) == end - pos and w in self.typenames:
                yield 1, '«' + ' '.join(w) + '»ᵗ', 1
            return
        yield from super().match(pat, si, t, pos, end)


PATS = PB('_ is _', '_ is a _', '_ is not _', '_ is not a _')
TYPEPATS = frozenset(p for p in PATS if 'a' in p)
UNI = ['x', 'is', 'a', 'not', 'number']
VBASE = N('x')
TYPES = N('number')


def res(vals, src):
    v, c, s = NS(vals, TYPES, PATS, TYPEPATS).resolve(src)
    return v, s


print('=' * W)
print('1. The collision needs the remainder to be a TYPE')
print('=' * W)
for extra, label in (({('a', 'number')}, 'value name «a number»  (remainder IS a type)'),
                     ({('a', 'queue')}, 'value name «a queue»   (remainder is NOT a type)')):
    print(f'  with {label}:')
    for src in ['x is a number', 'x is not a number']:
        v, s = res(VBASE | extra, src)
        print(f'      {src:24} {v:14} {s}')
    print()

print('=' * W)
print('2. Exhaustive: is "remainder is a type" the exact trigger?')
print('=' * W)
SRCS = [s for k in range(1, 6) for s in itertools.product(UNI, repeat=k)]
before = {}
for s in SRCS:
    v, sh = res(VBASE, ' '.join(s))
    if v == 'OK':
        before[s] = sh
print(f'  {len(SRCS)} sources, {len(before)} of which parse before any new name\n')


def wordcontent(p):
    return tuple(x for x in p if x is not HOLE)


ANCHORONLY = [wordcontent(p) for p in PATS
              if p[-1] is HOLE and all(x is not HOLE for x in p[:-1])]
GLUE = {'is', 'not', 'a'}

rows = []
for k in range(2, 4):
    for c in itertools.product(UNI, repeat=k):
        if c in VBASE:
            continue
        vals = VBASE | {c}
        broke = None
        for s, bs in before.items():
            av, asw = res(vals, ' '.join(s))
            if av != 'OK' or asw != bs:
                broke = (s, bs, av, asw)
                break
        if not broke:
            continue
        interior = any(0 < i < len(c) - 1 and w in GLUE for i, w in enumerate(c))
        r6b = any(len(a) < len(c) and c[:len(a)] == a for a in ANCHORONLY)
        artblanket = c[0] in ('a', 'an')
        artcond = artblanket and c[1:] in TYPES
        rows.append((c, broke, interior, r6b, artblanket, artcond))

print(f'  {len(rows)} declarations break something. Attribution:\n')
print(f'  {"name":22} {"R5′ interior":>13} {"R6b":>6} '
      f'{"begins a/an":>12} {"remainder is a type":>21}')
print('  ' + '-' * 78)
for c, broke, interior, r6b, ab, ac in rows:
    print(f'  «{" ".join(c):20}» {str(interior):>13} {str(r6b):>6} '
          f'{str(ab):>12} {str(ac):>21}')

uncovered_blanket = [c for c, _, i, r, ab, ac in rows if not (i or r or ab)]
uncovered_cond = [c for c, _, i, r, ab, ac in rows if not (i or r or ac)]
print(f'''
  unexplained by  R5′ + R6b + R-art  (blanket):      {len(uncovered_blanket)}
  unexplained by  R5′ + R6b + R-art′ (conditional):  {len(uncovered_cond)}''')
for c in uncovered_cond[:6]:
    print(f'      «{" ".join(c)}»')

overkill = [c for c, _, i, r, ab, ac in rows if ab and not ac]
safe_blanket = []
for k in range(2, 4):
    for c in itertools.product(UNI, repeat=k):
        if c in VBASE or c[0] not in ('a', 'an'):
            continue
        if c in [r[0] for r in rows]:
            continue
        safe_blanket.append(c)
print(f'''
  names beginning «a» that break NOTHING and the blanket rule refuses anyway:
      {len(safe_blanket)}   e.g. {", ".join("«"+" ".join(c)+"»" for c in safe_blanket[:5])}''')

print(f'''
  [{"PASS" if not uncovered_cond else "FAIL"}] the conditional rule is exactly as strict as the ambiguity.

  So R-art can be narrowed to R-art′ with no loss, and the diagnostic gets
  BETTER rather than worse -- "«a number» cannot be declared: «number» is a
  type, so «x is a number» would have two readings" explains itself, where
  "no name may begin with «a»" does not.''')

print('=' * W)
print('3. The cost of going conditional, stated fairly')
print('=' * W)
print('''  R-art′ is conditional on the TYPE TABLE, which is the same kind of
  conditionality R5 and R6 already have -- SCOPING.md already says an inner
  pattern can invalidate an outer name and the inner declaration is refused.
  So this is not a new species of rule.

  What it does introduce is one more way a later declaration invalidates an
  earlier one:

      value name  «a queue»      legal today
      later:      type «queue»   -> now «x is a queue» has two readings

  Refuse the type declaration and name both sites, per the existing
  convention. Inside a module that is a rename and it is fine.

  ACROSS A MODULE BOUNDARY it is not, and this is worth flagging now: a
  library that adds an exported type can invalidate an importer's variable
  name. Under the blanket rule that could never happen, because the name was
  already illegal. So R-art′ hands one more case to the import differential
  check from MODULEMERGE.md §4 -- which is where it belongs, but it means the
  two decisions are coupled and should ship together.''')
