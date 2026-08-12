#!/usr/bin/env python3
"""
is_article.py -- correcting glue_position2.py §2, which tested nothing.

There I wrote «_ is a <» through PA(), which only maps «_» to a hole -- so «<»
stayed a LITERAL WORD segment and the pattern could never match. The "fix"
printed a pass because the pinned pattern was inert, not because pinning works.
Exactly the degenerate-control failure this project keeps producing, so it is
re-run here with the resolver that actually has a one-token hole.

The real question underneath: «x is a number» has two readings the moment a
NAME «a number» exists, because R5′ (edge glue is legal) admits it. Which fix
removes the tie?

    pinning     «(_) is a <_>» -- depends entirely on whether a pinned type
                reference costs a lookup, which is a cost-model choice
    namespaces  the right operand of «is a» is a TYPE, resolved in the type
                table; a VALUE name «a number» is not a candidate at all
"""

from dp_bracket import BDPResolver, PB, pstr
from dp_resolver import N, HOLE

W = 78


class NS(BDPResolver):
    """Namespace-aware: a pattern may declare that a hole resolves in the TYPE
    table rather than the value table. Modelled by giving the resolver two name
    sets and marking which patterns are type tests."""

    def __init__(self, names, typenames, patterns, typepats, **kw):
        super().__init__(names, patterns, **kw)
        self.typenames = typenames
        self.typepats = typepats

    def match(self, pat, si, t, pos, end):
        if pat in self.typepats and si == len(pat) - 1:
            # trailing hole of a type test: resolve in the TYPE table only
            w = tuple(v for k, v in t[pos:end] if k == 'word')
            if len(w) == end - pos and w in self.typenames:
                yield 1, '«' + ' '.join(w) + '»ᵗ', 1
            return
        yield from super().match(pat, si, t, pos, end)


def run(label, names, typenames, pats, typepats, srcs):
    print(f'  {label}')
    for src in srcs:
        r = NS(names, typenames, pats, typepats)
        v, c, s = r.resolve(src)
        print(f'      {src:26} {v:14} {c}  {s}')
    print()


VALS = N('x', 'number', 'text')
VALS_TRAP = VALS | {('a', 'number')}
TYPES = N('number', 'text', 'big number')

print('=' * W)
print('1. The tie, with a real one-token hole this time')
print('=' * W)
FREE = PB('_ is _', '_ is not _', '_ is a _', '_ is not a _')
PIN = PB('_ is _', '_ is not _', '_ is a <', '_ is not a <')
for p in sorted(PIN, key=str):
    print(f'      {pstr(p)}')
print()
run('free article hole, no trap name', VALS, TYPES, FREE, set(),
    ['x is a number', 'x is not a number'])
run('free article hole, WITH the name «a number»', VALS_TRAP, TYPES, FREE,
    set(), ['x is a number', 'x is not a number'])
run('PINNED article hole, WITH the name «a number»', VALS_TRAP, TYPES, PIN,
    set(), ['x is a number', 'x is not a number'])
print('''  Pinning does remove the tie -- but only because a one-token hole in
  this resolver costs ZERO lookups, the same as a loop's declaring hole. A
  pinned hole here is a type REFERENCE, not a declaration, so it ought to cost
  one. Make it cost one and both readings are 3 again and the tie returns.

  So pinning is not a fix, it is a bet on a cost-model decision that has not
  been made and should not be made to serve this.''')

print('=' * W)
print('2. Namespaces remove it without betting on anything')
print('=' * W)
TYPEPATS = frozenset(p for p in FREE if 'a' in p)
run('type-position hole resolved in the TYPE table', VALS_TRAP, TYPES, FREE,
    TYPEPATS, ['x is a number', 'x is not a number', 'x is a big number',
               'x is number'])
print('''  «a number» is a VALUE name, so it is not a candidate for a type hole at
  all and the rival reading never exists. «ᵗ» marks a type-table lookup.

  Note «x is a big number» resolves: a multi-word TYPE name is read whole,
  which a one-token hole would have refused. So namespaces are not merely
  the safer fix, they are the more capable one.''')

print('=' * W)
print('3. And the article is doing real work, not decoration')
print('=' * W)
print('''  «x is number» and «x is a number» are DIFFERENT QUESTIONS:

      x is number         is the value of x equal to the value «number»
      x is a number       is the type of x the type «number»

  With separate tables there is no other way to tell which side of the
  language the right operand comes from -- the operator has to say it. So
  «a»/«an» is the namespace selector, and that is why it cannot be dropped
  even though it reads like grammar sugar.''')
