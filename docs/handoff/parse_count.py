#!/usr/bin/env python3
"""
parse_count.py -- "find all the possibilities, then let the type checker break
the ties". How many possibilities are there?

I expected this to refute the algorithm and it does not. My first run of this
file was also wrong in the other direction -- it counted zero for everything,
because I built statements no pattern could cover. Both are recorded because the
correction is the interesting part.

The real answer: statements have very few derivations, and the reason is
structural rather than lucky. Ronin has NO JUXTAPOSITION. Two adjacent names do
not compose into anything, so an unbracketed argument must be exactly ONE
expression covering exactly that span. The combinatorial segmentation people
fear from spaces-in-names never happens, because nothing combines the segments.
"""

import random

W = 78


def P(*specs):
    return [tuple(None if w == '_' else w for w in s.split()) for s in specs]


def count(words, names, patterns):
    """Every valid derivation of the span -- not the minimum-cost ones, all."""
    n = len(words)
    C = [[0] * (n + 1) for _ in range(n + 1)]

    def match(pat, si, i, j):
        if si == len(pat):
            return 1 if i == j else 0
        seg = pat[si]
        if seg is not None:
            return match(pat, si + 1, i + 1, j) if i < j and words[i] == seg else 0
        tot, last = 0, si == len(pat) - 1
        for sp in ([j] if last else range(i + 1, j + 1)):
            if C[i][sp]:
                tot += C[i][sp] * match(pat, si + 1, sp, j)
        return tot

    for w in range(1, n + 1):
        for i in range(0, n - w + 1):
            j = i + w
            t = 1 if tuple(words[i:j]) in names else 0
            for p in patterns:
                t += match(p, 0, i, j)
            C[i][j] = t
    return C[0][n]


PATS = P('print _', 'sum of _', 'send _ to _', 'item _ of _')
VOC = ['a', 'sum', 'of', 'to', 'send', 'item', 'b', 'print', 'c']


def dense(words):
    """EVERY contiguous run is a declared name. Maximally hostile -- no real
    program looks like this."""
    return {tuple(words[i:j])
            for i in range(len(words)) for j in range(i + 1, len(words) + 1)}


print('=' * W)
print('1. Worst-case derivation count, searched')
print('=' * W)
print('  4000 random statements per length, every substring a declared name\n')
print(f'  {"words":>6} {"max derivations":>17}   worst example')
print('  ' + '-' * 74)
random.seed(0)
for n in range(3, 13):
    mx, arg = 0, None
    for _ in range(4000):
        s = [random.choice(VOC) for _ in range(n)]
        c = count(s, dense(s), PATS)
        if c > mx:
            mx, arg = c, s
    print(f'  {n:>6} {mx:>17}   {" ".join(arg or [])}')
print('''
  Eighteen, at twelve words, with every substring in the program declared as a
  name. Not exponential -- barely superlinear.

  So the enumeration is affordable and my instinct that it would not be was
  wrong. Worth being precise about WHY, because the reason is a property that
  has to be protected rather than a happy accident:

      Ronin has no juxtaposition. «a b» is not an application, a product, or
      anything else -- so a hole is filled by exactly one atom or call covering
      exactly that span, and the number of ways to do that is bounded by how
      many names and patterns happen to fit, not by how many ways the words
      could be cut up.

  A language with juxtaposition -- Haskell-style application, or an implicit
  product -- would be exponential here, because every cut point would be a
  choice. Ronin is not, because there are no cut points to choose.

  That should go in the spec as a CONSTRAINT, not an observation: adding an
  adjacency operator later would take the resolver from near-linear to
  exponential, and nothing in the grammar would announce it.''')

print('=' * W)
print('2. Where the derivations that DO exist come from')
print('=' * W)
CASES = [
    ('item print of print of a of c', 'a pattern anchor usable as a name'),
    ('send sum of to to item to of to', 'a glue word usable as a name'),
]
for src, why in CASES:
    s = src.split()
    print(f'  {src:34} {count(s, dense(s), PATS):>3}   {why}')
print('''
  Both come from the same place: a word that is structure in one reading and
  spelling in another. Which is exactly what R5′, R6b and R7b regulate -- so
  those rules are not complexity piled on top of the design, they are what
  keeps this number at one for code anybody would actually write.''')

print('=' * W)
print('3. Can the type checker break the ties we have actually measured?')
print('=' * W)
TIES = [
    ('x is a number', 'truth', 'truth', False),
    ('send to to to to', 'nothing', 'nothing', False),
    ('sum of all things', 'number', 'number', False),
    ('send time to live', 'nothing', 'nothing', False),
    ('x is not x', 'truth', 'truth', False),
    ('sorted xs reversed', 'list', 'ERROR', True),
]
print(f'  {"statement":24} {"reading A":10} {"reading B":10} types decide?')
print('  ' + '-' * 62)
for src, ta, tb, dec in TIES:
    print(f'  {src:24} {ta:10} {tb:10} {"YES" if dec else "no"}')
print('''
  Five of six have the SAME TYPE on both sides, so a type filter cannot touch
  them. The one it breaks is the postfix case -- which is the example Budai
  raised months ago, and he was right about that one.

      minimum lookup   decides SEGMENTATION -- where a name starts and stops
      types            decide APPLICABILITY -- whether an operand fits

  Complementary, not alternative. Types alone leave every segmentation tie
  standing; lookup alone leaves the postfix family standing. What survives
  both is what genuinely needs a bracket.''')
