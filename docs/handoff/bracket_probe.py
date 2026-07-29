#!/usr/bin/env python3
"""
bracket_probe.py -- the probe extension the programmer is blocked on.

ZERO-GLUE.md mechanism 3 claims a glue word sitting next to a bracket need not
be reserved, because a name is a word-only span and cannot straddle a bracket.
The claim is structural, but R5's blanket form is what the exhaustive search
actually verified, so the refinement needs its own run.

`ronin_grammar_probe.py` cannot express it: a pattern segment is a word or a
hole, and a hole matches an unbracketed expression. This adds a third segment
kind.

    HOLE     an unbracketed argument
    BHOLE    an argument that MUST be bracketed -- models « { … } »

and, in the fuzzer, a second reservation policy:

    blanket   every glue word is reserved                    (R5 as verified)
    refined   glue immediately preceded by a BHOLE is not    (the proposal)

Note one thing that falls out immediately: a BHOLE in leading position is NOT
left-recursive, because it must begin with «(». So bracket-delimited infix is
expressible where word infix is not -- which is the same reason mechanism 3
works at all.
"""

import re
import ronin_grammar_probe as P
from ronin_grammar_probe import Probe, Scope, HOLE, tokenize


class _BHole:
    __slots__ = ()
    def __repr__(self): return 'BHOLE'


class _THole:
    """A hole pinned to exactly ONE word token -- models the declaring hole of
    «for each (_) in (_)», where the loop variable is a single new name. This
    is the OTHER mechanism, and the one that would buy back «in»: it fixes the
    split point by construction rather than by blocking a straddle."""
    __slots__ = ()
    def __repr__(self): return 'THOLE'


BHOLE = _BHole()
THOLE = _THole()
HOLES = (HOLE, BHOLE, THOLE)


def pat_str(pat):
    out = []
    for s in pat:
        out.append('(_)' if s is HOLE else '{_}' if s is BHOLE
                   else '<_>' if s is THOLE else s)
    return ' '.join(out)


P.pat_str = pat_str


def _call_show(self):
    out, ai = [], 0
    for s in self.pat:
        if s in HOLES:
            out.append(self.args[ai].show())
            ai += 1
        else:
            out.append(s)
    return '[' + ' '.join(out) + ']'


P.Call.show = _call_show


def anchor_run(pat):
    """Words before the first hole of EITHER kind."""
    run = []
    for s in pat:
        if s in HOLES:
            break
        run.append(s)
    return tuple(run)


def glue(pat, policy='blanket'):
    """Literal segments after the first hole.

    refined: a segment is unreachable by any name -- and so needs no
    reservation -- when the hole before it cannot be straddled:

      BHOLE   a name is a word-only span and cannot contain a bracket
      THOLE   the hole is exactly one token, so the split point is fixed and
              no name can start earlier and run into the glue"""
    run = len(anchor_run(pat))
    out = set()
    for i in range(run, len(pat)):
        s = pat[i]
        if s in HOLES:
            continue
        if policy == 'refined' and i > 0 and pat[i - 1] in (BHOLE, THOLE):
            continue
        out.add(s)
    return out


class BProbe(Probe):
    def __init__(self, scope, **kw):
        for pat in scope.patterns:
            if pat and pat[0] is HOLE:
                raise ValueError(f'left-recursive pattern {pat_str(pat)!r}')
        # deliberately NOT rejecting a leading BHOLE: it must start with «(»,
        # so it consumes a token before recursing and is not left-recursive.
        self.scope = scope
        self.arg_mode = kw.get('arg_mode', 'expr')
        self.name_match = kw.get('name_match', 'all')
        self.depth_cap = kw.get('depth_cap', 40)

    def match_pattern(self, pat, si, toks, pos, depth):
        if si == len(pat):
            yield [], pos
            return
        seg = pat[si]
        if seg is THOLE:
            # exactly one word token, and it is a DECLARATION: it resolves
            # whatever is in scope, and costs one table operation
            if pos < len(toks) and re.match(r'[A-Za-z_]', toks[pos]):
                for rest, p2 in self.match_pattern(pat, si + 1, toks,
                                                   pos + 1, depth):
                    yield [P.Name((toks[pos],))] + rest, p2
            return
        if seg is BHOLE:
            if pos < len(toks) and toks[pos] == '(':
                for arg, p in self.exprs(toks, pos + 1, depth + 1):
                    if p < len(toks) and toks[p] == ')':
                        for rest, p2 in self.match_pattern(pat, si + 1, toks,
                                                           p + 1, depth):
                            yield [arg] + rest, p2
            return
        yield from super().match_pattern(pat, si, toks, pos, depth)


if __name__ == '__main__':
    W = 74
    def h(t): print('\n' + '=' * W + f'\n{t}\n' + '=' * W)
    def ok(l, c):
        print(f'  [{"PASS" if c else "FAIL"}] {l}'); return c
    res = []

    h('the extension works: a BHOLE only matches a bracketed argument')
    pat = ('send', BHOLE, 'to', HOLE)
    sc = Scope(names=frozenset({('a',), ('b',)}), patterns=frozenset({pat}))
    pr = BProbe(sc)
    for src in ['send ( a ) to b', 'send a to b']:
        v, w, parses = pr.resolve(src)
        print(f'  {src:22} -> {v:14} {parses}')
    res.append(ok('bracketed form parses', BProbe(sc).resolve('send ( a ) to b')[0] == 'OK'))
    res.append(ok('unbracketed form does not', BProbe(sc).resolve('send a to b')[0] == 'NO PARSE'))

    h('mechanism 3, directly: the capture that R5 exists to stop')
    # the capture needs BOTH patterns: the short one is what the long name
    # displaces. A single pattern cannot demonstrate it -- getting this wrong
    # is how I built a broken counterexample once before.
    names = frozenset({('a',), ('b',), ('a', 'to', 'b')})
    plain = frozenset({('send', HOLE, 'to', HOLE), ('send', HOLE)})
    brack = frozenset({('send', BHOLE, 'to', HOLE), ('send', BHOLE)})
    u = BProbe(Scope(names=names, patterns=plain)).resolve('send a to b')
    b = BProbe(Scope(names=names, patterns=brack)).resolve('send ( a ) to b')
    print('  «a to b» declared, «to» NOT reserved:')
    print(f'    unbracketed  send a to b        -> {u[0]:9} {u[1]}')
    print(f'    bracketed    send ( a ) to b    -> {b[0]:9} {b[1]}')
    res.append(ok('unbracketed: the name swallows the call',
                  u[1] and 'a to b' in str(u[1])))
    res.append(ok('bracketed: the swallowing reading does not exist',
                  b[1] and 'a to b' not in str(b[1])))

    h('glue sets under the two policies')
    for p in [('send', HOLE, 'to', HOLE), ('send', BHOLE, 'to', HOLE),
              ('if', BHOLE, 'then', BHOLE, 'otherwise', BHOLE)]:
        print(f'  {pat_str(p):40} blanket={sorted(glue(p))}  '
              f'refined={sorted(glue(p, "refined"))}')

    h('a leading BHOLE is legal where a leading HOLE is not')
    try:
        BProbe(Scope(names=frozenset({('a',)}), patterns=frozenset({(HOLE, 'x')})))
        res.append(ok('leading HOLE rejected', False))
    except ValueError:
        res.append(ok('leading HOLE rejected', True))
    try:
        BProbe(Scope(names=frozenset({('a',)}), patterns=frozenset({(BHOLE, 'x')})))
        res.append(ok('leading BHOLE accepted', True))
    except ValueError:
        res.append(ok('leading BHOLE accepted', False))

    print('\n' + '=' * W)
    print(f'  {sum(res)}/{len(res)} checks pass')
    print('=' * W)
