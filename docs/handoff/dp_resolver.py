#!/usr/bin/env python3
"""
dp_resolver.py -- the architecture your 2022 retrospective was pointing at.

You said the cute mistake was making a phrase ONE ENTITY instead of letting
each word be a node. That is exactly right, and here is why it blew up:
treating a phrase as an entity forces the parser to COMMIT to a phrase
boundary while it is still parsing. Every commitment can be wrong, so you need
backtracking, and backtracking over half-built phrase entities is the crazy
situation.

Let each word be a node and a phrase stops being an entity. It becomes a SPAN
-- a pair of indices. You never commit; you score. And the scoring function is
already yours:

    resolve a statement = the minimum-cost segmentation of a word sequence
                          into spans, where each span resolving to a symbol
                          costs one lookup

which is a shortest path on a DAG, solved bottom-up. No backtracking. Ties are
two paths of equal cost, which the table counts for free -- so "ties are an
error" needs no extra machinery.

    E[i][j][m] = cheapest expression over tokens i..j-1 parsed at minimum
                 binding power m

The third index is what makes precedence work. A word-pattern with an
unbracketed trailing argument returns at level `pattern_bp`, so it may only be
an operand where m <= pattern_bp -- precisely the precedence-climbing rule,
expressed as a table constraint instead of control flow.
"""

from dataclasses import dataclass
import re

HOLE = None
INF = float('inf')

OPS = {'+': (10, True), '-': (10, True), '*': (20, True), '/': (20, True),
       '<>': (5, True), '|>': (3, True)}
MAXBP = 30

WORD, NUM, SYM, OPEN, CLOSE = 'word', 'num', 'sym', 'open', 'close'
_RX = [(re.compile(r'\s+'), None), (re.compile(r'\d+(?:\.\d+)?'), NUM),
       (re.compile(r'[A-Za-z_][A-Za-z0-9_]*'), WORD),
       (re.compile(r'[+\-*/<>=!&|^~%@$:.?]+'), SYM)]


def lex(src):
    out, i = [], 0
    while i < len(src):
        for rx, kind in _RX:
            m = rx.match(src, i)
            if m:
                if kind:
                    out.append((kind, m.group()))
                i = m.end()
                break
        else:
            if src[i] in '()':
                out.append((OPEN if src[i] == '(' else CLOSE, src[i]))
                i += 1
            else:
                raise SyntaxError(f'stray {src[i]!r}')
    return out


class Cell:
    """Holds the cheapest cost for a span and HOW MANY derivations achieve it.
    The count must propagate through merges, or a tie in a subspan silently
    disappears when the subspan is used by a larger one."""

    __slots__ = ('cost', 'derivs')

    def __init__(self):
        self.cost = INF
        self.derivs = {}          # rendering -> number of derivations

    @property
    def count(self):
        return sum(self.derivs.values())

    @property
    def show(self):
        return next(iter(self.derivs), '')

    def offer(self, cost, show, count=1):
        if cost < self.cost:
            self.cost, self.derivs = cost, {show: count}
        elif cost == self.cost and cost != INF:
            self.derivs[show] = self.derivs.get(show, 0) + count

    def merge(self, other):
        if other.cost == INF:
            return
        for show, cnt in other.derivs.items():
            self.offer(other.cost, show, cnt)


class DPResolver:
    def __init__(self, names, patterns, pattern_bp=7, bracket_cost=1):
        self.names = names
        self.patterns = patterns
        self.pattern_bp = pattern_bp
        self.bracket_cost = bracket_cost

    def resolve(self, src):
        t = lex(src)
        n = len(t)
        # Aclosed: atoms that are complete in themselves (literal, name,
        #          bracketed group, pattern ending in a word)
        # Aopen  : pattern calls ending in an unbracketed trailing argument.
        #          These return at level pattern_bp.
        self.Ac = [[Cell() for _ in range(n + 1)] for _ in range(n + 1)]
        self.Ao = [[Cell() for _ in range(n + 1)] for _ in range(n + 1)]
        self.E = [[[Cell() for _ in range(MAXBP + 2)]
                   for _ in range(n + 1)] for _ in range(n + 1)]

        for width in range(1, n + 1):
            for i in range(0, n - width + 1):
                j = i + width
                self.atoms(t, i, j)
                for m in range(MAXBP + 1, -1, -1):
                    self.expr(t, i, j, m)

        top = self.E[0][n][0]
        if top.cost == INF:
            return 'NO PARSE', 0, ''
        return ('TIE -> ERROR' if top.count > 1 else 'OK'), top.cost, top.show

    def atoms(self, t, i, j):
        ac, ao = self.Ac[i][j], self.Ao[i][j]

        if j - i == 1 and t[i][0] == NUM:
            ac.offer(0, t[i][1])

        if all(k == WORD for k, _ in t[i:j]):
            w = tuple(v for _, v in t[i:j])
            if w in self.names:
                ac.offer(1, '«' + ' '.join(w) + '»')

        # a bracketed substatement costs one lookup regardless of size, and it
        # is CLOSED -- which is what makes «(compute total for a) + b» work
        if j - i >= 2 and t[i][0] == OPEN and t[j - 1][0] == CLOSE:
            inner = self.E[i + 1][j - 1][0]
            if inner.cost < INF:
                ac.offer(self.bracket_cost + inner.cost, '⟨' + inner.show + '⟩')

        for pat in self.patterns:
            target = ao if pat[-1] is HOLE else ac
            for cost, show, cnt in self.match(pat, 0, t, i, j):
                target.offer(1 + cost, show, cnt)

    def match(self, pat, si, t, pos, end):
        if si == len(pat):
            if pos == end:
                yield 0, '', 1
            return
        seg = pat[si]
        if seg is not HOLE:
            if pos < end and t[pos] == (WORD, seg):
                for c, s, n in self.match(pat, si + 1, t, pos + 1, end):
                    yield c, (seg + ' ' + s).strip(), n
            return
        if si == len(pat) - 1:
            # trailing argument: reaches the end of the span, parsed at the
            # pattern's own binding power
            arg = self.E[pos][end][self.pattern_bp]
            if arg.cost < INF:
                yield arg.cost, arg.show, arg.count
            return
        for split in range(pos + 1, end + 1):
            arg = self.E[pos][split][0]       # medial args cross any operator
            if arg.cost == INF:
                continue
            for c, s, n in self.match(pat, si + 1, t, split, end):
                yield arg.cost + c, (arg.show + ' ' + s).strip(), arg.count * n

    def expr(self, t, i, j, m):
        cell = self.E[i][j][m]
        cell.merge(self.Ac[i][j])
        # an open pattern call returns at level pattern_bp: usable only where
        # the required minimum binding power is no greater
        if m <= self.pattern_bp:
            cell.merge(self.Ao[i][j])

        depth = 0
        for k in range(i, j):
            if t[k][0] == OPEN:
                depth += 1
            elif t[k][0] == CLOSE:
                depth -= 1
            elif depth == 0 and t[k][0] == SYM and t[k][1] in OPS and i < k < j - 1:
                bp, leftassoc = OPS[t[k][1]]
                if bp < m:
                    continue
                lm = bp + 1 if leftassoc else bp
                rm = bp + 1 if leftassoc else bp
                l, r = self.E[i][k][lm], self.E[k + 1][j][rm]
                if l.cost < INF and r.cost < INF:
                    cell.offer(l.cost + r.cost,
                               f'({l.show} {t[k][1]} {r.show})',
                               l.count * r.count)


def N(*s):
    return frozenset(tuple(x.split()) for x in s)


def PA(*s):
    return frozenset(tuple(HOLE if w == '_' else w for w in x.split()) for x in s)
