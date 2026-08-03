#!/usr/bin/env python3
"""
dp_bracket.py -- dp_resolver plus a bracket-delimited hole, to test the
prediction the programmer's root law makes.

His law:

    A name is one lookup. Any composite reading of the same span is at least
    two. So a name whose span equals a composite's span always wins, silently.

The law is stated over spans, not over patterns -- and that makes it PREDICTIVE.
A name is a word-only, contiguous span. So a composite is safe from capture
exactly when no name can cover it, and there are two ways to arrange that:

    (a) the composite's span is not word-only   -- it contains a bracket or a
                                                   symbol, which no name may
    (b) the split inside it is fixed            -- a one-token hole pins where
                                                   the name would have to end

(a) predicts a FOURTH free shape that «ZERO-GLUE.md» does not list: a glue word
that is FOLLOWED by a bracket-delimited hole should need no reservation, for the
same reason mechanism 3 works when it is PRECEDED by one. Testing it needs a
BHOLE in the DP resolver, which dp_resolver.py does not have.

This adds one:  BHOLE matches only a span that is exactly « ( ... ) ».
"""

from dp_resolver import (DPResolver, Cell, HOLE, lex, INF, MAXBP,
                         WORD, NUM, SYM, OPEN, CLOSE, N)


class _BHole:
    __slots__ = ()
    def __repr__(self): return '{_}'


class _THole:
    __slots__ = ()
    def __repr__(self): return '<_>'


BHOLE = _BHole()
THOLE = _THole()
HOLEKINDS = (HOLE, BHOLE, THOLE)


def PB(*specs):
    """« _ » free hole, « { » bracketed hole, « < » one-token hole."""
    out = []
    for s in specs:
        out.append(tuple(HOLE if w == '_' else BHOLE if w == '{'
                         else THOLE if w == '<' else w for w in s.split()))
    return frozenset(out)


def pstr(pat):
    return ' '.join('(_)' if s is HOLE else repr(s)
                    if s in (BHOLE, THOLE) else s for s in pat)


class BDPResolver(DPResolver):
    """Only `match` changes; everything else is inherited."""

    @staticmethod
    def _is_bracketed(t, i, j):
        if j - i < 2 or t[i][0] != OPEN or t[j - 1][0] != CLOSE:
            return False
        d = 0
        for k in range(i, j):
            if t[k][0] == OPEN:
                d += 1
            elif t[k][0] == CLOSE:
                d -= 1
                if d == 0 and k != j - 1:
                    return False
        return d == 0

    def match(self, pat, si, t, pos, end):
        if si == len(pat):
            if pos == end:
                yield 0, '', 1
            return
        seg = pat[si]
        last = si == len(pat) - 1

        if seg not in HOLEKINDS:
            if pos < end and t[pos] == (WORD, seg):
                for c, s, n in self.match(pat, si + 1, t, pos + 1, end):
                    yield c, (seg + ' ' + s).strip(), n
            return

        if seg is THOLE:                       # exactly one word token
            if pos < end and t[pos][0] == WORD:
                for c, s, n in self.match(pat, si + 1, t, pos + 1, end):
                    yield c, (t[pos][1] + ' ' + s).strip(), n
            return

        if seg is BHOLE:                       # exactly « ( ... ) »
            lo = end if last else pos + 2
            for split in ([end] if last else range(pos + 2, end + 1)):
                if not self._is_bracketed(t, pos, split):
                    continue
                arg = self.E[pos + 1][split - 1][0]
                if arg.cost == INF:
                    continue
                for c, s, n in self.match(pat, si + 1, t, split, end):
                    yield (arg.cost + self.bracket_cost + c,
                           ('⟨' + arg.show + '⟩ ' + s).strip(),
                           arg.count * n)
            return

        if last:                               # free trailing hole
            arg = self.E[pos][end][self.pattern_bp]
            if arg.cost < INF:
                yield arg.cost, arg.show, arg.count
            return
        for split in range(pos + 1, end + 1):  # free medial hole
            arg = self.E[pos][split][0]
            if arg.cost == INF:
                continue
            for c, s, n in self.match(pat, si + 1, t, split, end):
                yield arg.cost + c, (arg.show + ' ' + s).strip(), arg.count * n

    def atoms(self, t, i, j):
        """Same as the base, but a pattern is OPEN only if it ends in a FREE
        hole -- a trailing BHOLE or THOLE closes it."""
        ac, ao = self.Ac[i][j], self.Ao[i][j]
        if j - i == 1 and t[i][0] == NUM:
            ac.offer(0, t[i][1])
        if all(k == WORD for k, _ in t[i:j]):
            w = tuple(v for _, v in t[i:j])
            if w in self.names:
                ac.offer(1, '«' + ' '.join(w) + '»')
        if self._is_bracketed(t, i, j):
            inner = self.E[i + 1][j - 1][0]
            if inner.cost < INF:
                ac.offer(self.bracket_cost + inner.cost, '⟨' + inner.show + '⟩')
        for pat in self.patterns:
            target = ao if pat[-1] is HOLE else ac
            for cost, show, cnt in self.match(pat, 0, t, i, j):
                target.offer(1 + cost, show, cnt)
