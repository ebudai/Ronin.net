#!/usr/bin/env python3
"""
word_infix_ops.py -- the is family as OPERATORS, not patterns.

He is right that «(_) is (_)» cannot be a pattern: a leading free hole is
refused, and «otherwise» already lives in the operator table for exactly that
reason. Every probe I ran modelled the family as patterns, so two things need
checking rather than assuming:

  1. do the R5′ / R7b conclusions survive the move? The costs change -- a
     pattern call costs one lookup, an operator costs none -- so every
     comparison shifts, and "it shifts both sides equally" is precisely the
     kind of claim this project punishes;

  2. what does multi-word infix matching have to look like? He notes the scan
     is `Operators.TryGetValue(lexemes[k].Text)`, one lexeme. This is a
     reference model for the multi-word version, and it turns up a design
     decision: greedy-longest-match is NOT the right rule here.
"""

from dp_resolver import (DPResolver, HOLE, INF, MAXBP, WORD, NUM, SYM,
                         OPEN, CLOSE, N, PA)

W = 78


class WordOpResolver(DPResolver):
    """word_ops: {(w1, w2, ...): (bp, leftassoc)}

    Every operator run that matches at a position is OFFERED, and cost decides
    -- ties are counted, not broken. That is the language's rule; a greedy
    longest match would be a silent pick."""

    def __init__(self, names, patterns, word_ops, **kw):
        super().__init__(names, patterns, **kw)
        self.word_ops = word_ops
        self.maxrun = max((len(k) for k in word_ops), default=0)

    def expr(self, t, i, j, m):
        super().expr(t, i, j, m)
        cell = self.E[i][j][m]
        depth = 0
        for k in range(i, j):
            if t[k][0] == OPEN:
                depth += 1
                continue
            if t[k][0] == CLOSE:
                depth -= 1
                continue
            if depth or t[k][0] != WORD:
                continue
            for run in range(1, self.maxrun + 1):
                end = k + run
                if end >= j or not (i < k):
                    continue
                words = tuple(v for kind, v in t[k:end] if kind == WORD)
                if len(words) != run or words not in self.word_ops:
                    continue
                bp, leftassoc = self.word_ops[words]
                if bp < m:
                    continue
                lm = bp if leftassoc else bp + 1
                rm = bp + 1 if leftassoc else bp
                l, r = self.E[i][k][lm], self.E[end][j][rm]
                if l.cost < INF and r.cost < INF:
                    cell.offer(l.cost + r.cost,
                               f'({l.show} {" ".join(words)} {r.show})',
                               l.count * r.count)


IS_OPS = {('is',): (6, True), ('is', 'not'): (6, True),
          ('is', 'a'): (6, True), ('is', 'an'): (6, True),
          ('is', 'not', 'a'): (6, True), ('is', 'not', 'an'): (6, True)}
NOT_PAT = PA('not _')


def run(names, src, pats=NOT_PAT):
    v, c, s = WordOpResolver(names, pats, IS_OPS).resolve(src)
    return v, c, s


print('=' * W)
print('1. Do the conclusions survive the pattern -> operator move?')
print('=' * W)
BASE = N('x', 'y', 'valid', 'number')
CASES = [
    ('x is y', ('x', 'is', 'y'), 'name spans the operator -> capture'),
    ('x is a number', ('a', 'number'), 'article tie'),
    ('x is not x', ('not', 'x'), 'not-initial tie'),
    ('x is valid', ('is', 'valid'), 'edge glue -> must be UNCHANGED'),
]
for src, decl, why in CASES:
    bv, bc, bs = run(BASE, src)
    av, ac, asw = run(BASE | {decl}, src)
    tag = ('unchanged' if (bv, bs) == (av, asw)
           else 'CAPTURE' if av == 'OK' else av)
    print(f'  {src:20} + name «{" ".join(decl):12}»  '
          f'{bv:8}{bc} -> {av:14}{ac}   {tag}')
    print(f'      {why}')
print('''
  Same verdicts as the pattern model, at lower costs -- an operator is free, so
  every reading loses the pattern's lookup and the comparisons shift together.
  «is valid» stays legal; the three hazards stay hazards. R5′ and R7b carry
  over unchanged.''')

print('=' * W)
print('2. Multi-word matching: offer every run, do not match greedily')
print('=' * W)
for label, names in (('without «a number»', BASE),
                     ('with the name «a number»', BASE | {('a', 'number')})):
    v, c, s = run(names, 'x is not a number')
    print(f'  x is not a number   {label:26} {v:14} {c}  {s}')
print('''
  A greedy longest-match scan would silently take «is not a» in both rows and
  never report the second. Offering every run and letting cost decide produces
  the TIE -- which is the language's rule, and the thing R7b exists to prevent
  in the first place. So the multi-word scan is:

      at each position, for each run length that matches an operator, OFFER it
      -- do not commit to the longest.

  Greedy matching would hide exactly the defect R7b was derived from.''')

print('=' * W)
print('3. R7b\'s relation restated over operator word-runs')
print('=' * W)


def r7b_from_operators(ops):
    """His restatement: is -> is not -> is not a, first extra word is the
    R7b word. Formally: for runs P and Q with P a proper prefix of Q, the
    word at position len(P) is an R7b word."""
    out = set()
    for p in ops:
        for q in ops:
            if len(q) > len(p) and q[:len(p)] == p:
                out.add(q[len(p)])
    return out


print(f'  operators: {sorted(" ".join(k) for k in IS_OPS)}')
print(f'  R7b from prefix-extension = {sorted(r7b_from_operators(IS_OPS))}')
print('''
  Same answer, simpler mechanism, and it derives from Builtin.Operators which
  already generates Rules.Infix. His restatement is better than mine.''')

print('=' * W)
print('4. But do not DELETE the pattern half of the relation')
print('=' * W)
SUMPATS = PA('sum of _', 'sum of all _')
NAMES = N('things', 'x')
for extra, label in ((set(), 'without «all things»'),
                     ({('all', 'things')}, 'with the name «all things»')):
    v, c, s = DPResolver(NAMES | extra, SUMPATS).resolve('sum of all things')
    print(f'  sum of all things   {label:26} {v:14} {c}  {s}')
print('''
  «sum of all (_)» refines «sum of (_)» by inserting «all» at the start of its
  hole, and the tie appears exactly as it does for the operators. So the
  refinement relation over PATTERNS is still live -- it just does not apply to
  the is family, which is not a pattern family.

  R7b therefore has TWO sources feeding one set:

      operators   prefix-extension of a word run     is -> is not -> is not a
      patterns    insertion at the start of a hole   sum of (_) -> sum of all (_)

  Today the stdlib happens to have no such pattern pair, so an
  operators-only generator gives the right answer and will keep giving it until
  someone adds one. That is the "correct by coincidence" shape again -- cheap
  to avoid by generating from both tables now.''')
