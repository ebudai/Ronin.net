#!/usr/bin/env python3
"""
Budai's proposal: disambiguate word infix left-to-right, leftmost binds first.

`WHYSYMBOLINFIX.md` said this needed per-pattern binding powers and a
restructured table. That was wrong, and it was wrong because the demonstration
behind it was running on a resolver bug -- «a + b + c» did not parse, so equal
precedence looked unwritable when it was only unwritable *there*.

With associativity fixed, equal precedence is fine. So this asks the same
question of word patterns, using the machinery that already exists:

    a LEADING hole is parsed at pattern_bp        -> admits equal precedence
    a TRAILING hole is parsed at pattern_bp + 1   -> forbids it

which is exactly precedence climbing's left-associative rule, applied to the
word layer instead of the symbol layer, with ONE shared level rather than a
number per pattern.
"""

import dp_resolver as D
from dp_resolver import DPResolver, N, PA, HOLE

W = 74
def h(t): print('\n' + '=' * W + f'\n{t}\n' + '=' * W)
def ok(l, c):
    print(f'  [{"PASS" if c else "FAIL"}] {l}'); return c
res = []


class LeftAssoc(DPResolver):
    """Word patterns, left-associative at one shared level."""

    def match(self, pat, si, t, pos, end):
        if si == len(pat):
            if pos == end:
                yield 0, '', 1
            return
        seg = pat[si]
        if seg is not HOLE:
            if pos < end and t[pos] == (D.WORD, seg):
                for c, s, n in self.match(pat, si + 1, t, pos + 1, end):
                    yield c, (seg + ' ' + s).strip(), n
            return

        leading = si == 0
        trailing = si == len(pat) - 1

        if trailing:
            # right operand of a left-associative form: forbid equal level
            m = self.pattern_bp + 1 if pat[0] is HOLE else self.pattern_bp
            arg = self.E[pos][end][m]
            if arg.cost < D.INF:
                yield arg.cost, f'({arg.show})' if pat[0] is HOLE else arg.show, arg.count
            return

        # a medial or LEADING hole. Leading holes of a left-associative form
        # admit their own level, so a chain can grow leftwards.
        m = self.pattern_bp if leading else 0
        for split in range(pos + 1, end + 1):
            arg = self.E[pos][split][m]
            if arg.cost == D.INF:
                continue
            for c, s, n in self.match(pat, si + 1, t, split, end):
                shown = f'({arg.show})' if leading else arg.show
                yield arg.cost + c, (shown + ' ' + s).strip(), arg.count * n


NAMES = N('a', 'b', 'c', 'd', 'xs')
INFIX = PA('_ to _', '_ of _')
MIXED = PA('sorted _', '_ reversed')

h('1. Word infix, one shared level, left-associative')
for src in ['a to b', 'a to b of c', 'a of b to c', 'a to b of c to d']:
    v, cost, show = LeftAssoc(NAMES, INFIX, pattern_bp=7).resolve(src)
    print(f'  {src:22} -> {v:12} {cost}  {show}')
    res.append(ok(f'«{src}» is unique', v == 'OK'))

h('2. The same statement under the shipped rule, for comparison')
for src in ['a to b of c']:
    v, cost, show = DPResolver(NAMES, INFIX, pattern_bp=7).resolve(src)
    print(f'  {src:22} -> {v:12} {cost}  {show}')

h('3. Does it also settle prefix versus postfix?')
for src in ['sorted xs reversed', 'xs reversed', 'sorted xs']:
    v, cost, show = LeftAssoc(NAMES, MIXED, pattern_bp=7).resolve(src)
    print(f'  {src:22} -> {v:12} {cost}  {show}')

print('''
  «sorted xs reversed» is the case POSTFIXPATTERNS.md called a tie. Under a
  left-associative word layer it is not a tie -- it has an answer, and the
  answer should be the one a reader gets scanning left to right.''')

print('\n' + '=' * W)
print(f'  {sum(res)}/{len(res)} checks pass')
print('=' * W)
