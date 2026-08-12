#!/usr/bin/env python3
"""
leading_bp.py -- do per-pattern binding powers dissolve the leading-hole ties?

leading_free.py measured the cost of admitting a leading free hole:

    anchored + anchored            349,440 resolutions,   6 ties  0.00%
    anchored + LEADING FREE HOLE   322,560 resolutions, 120 ties  0.04%

and identified every one of them as the prefix-versus-postfix clash:

    patterns «sum of (_)» and «(_) reversed», name «a»
        sum of a reversed   ->  TIE, 3 lookups
            «sum of («a» reversed)»
            «(sum of «a») reversed»

It also noted the fix is not mysterious -- binding powers decide exactly this
for symbols -- but that dp_resolver has ONE pattern_bp shared by every pattern.

FIRST VERSION OF THIS FILE WAS WRONG: it compared a Pratt parser against a Pratt
parser with different numbers. A Pratt parser never ties -- it always picks -- so
that measured nothing. The tie comes from ENUMERATION. So:

  §1  enumerate every reading, then apply the declared binding powers as a
      FILTER, and count what survives
  §2  check every filtered-out reading is still reachable by bracketing -- a
      binding power that DISCARDED a reading would be the "cost chooses"
      failure the whole ambiguity-as-error design refuses
"""

import functools

W = 78

# (kind, words, binding power)
PATS = [
    ('prefix',  'sum of',   10),
    ('postfix', 'reversed', 20),
    ('infix',   'per',      15),
]
NAMES = {'a', 'b', 'd', 't'}
FLAT = [(k, w, 10) for k, w, _ in PATS]        # today: one shared pattern_bp


def lex(s):
    return tuple(s.replace('(', ' ( ').replace(')', ' ) ').split())


# ---------------------------------------------------------------------------
# enumerate EVERY reading, ignoring binding powers entirely
# ---------------------------------------------------------------------------
def enumerate_all(toks, pats):
    @functools.lru_cache(maxsize=None)
    def go(i, j):
        out = []
        if i >= j:
            return out
        if j - i == 1 and toks[i] in NAMES:
            out.append(toks[i])
        if toks[i] == '(' and toks[j - 1] == ')':
            for inner in go(i + 1, j - 1):
                out.append(f'({inner})')
        for kind, words, _ in pats:
            ws = tuple(words.split())
            n = len(ws)
            if kind == 'prefix' and toks[i:i + n] == ws:
                for r in go(i + n, j):
                    out.append(f'{words} «{r}»')
            if kind == 'postfix' and toks[j - n:j] == ws:
                for l in go(i, j - n):
                    out.append(f'«{l}» {words}')
            if kind == 'infix':
                for k in range(i + 1, j - n):
                    if toks[k:k + n] == ws:
                        for l in go(i, k):
                            for r in go(k + n, j):
                                out.append(f'«{l}» {words} «{r}»')
        return out
    return sorted(set(go(0, len(toks))))


# ---------------------------------------------------------------------------
# the declared grammar: a Pratt parse. This is the FILTER, not the enumerator.
# ---------------------------------------------------------------------------
class P:
    def __init__(self, toks, pats):
        self.t, self.i, self.p = list(toks), 0, pats

    def parse(self, minbp=0):
        if self.i >= len(self.t):
            return None
        if self.t[self.i] == '(':
            self.i += 1
            left = self.parse(0)
            if self.i >= len(self.t) or self.t[self.i] != ')':
                return None
            self.i += 1
            left = f'({left})'
        else:
            hit = next(((w, bp, len(w.split())) for k, w, bp in self.p
                        if k == 'prefix' and self.t[self.i:self.i + len(w.split())] == w.split()), None)
            if hit:
                w, bp, n = hit
                self.i += n
                right = self.parse(bp)
                if right is None:
                    return None
                left = f'{w} «{right}»'
            else:
                if self.t[self.i] not in NAMES:
                    return None
                left = self.t[self.i]
                self.i += 1
        while self.i < len(self.t) and self.t[self.i] != ')':
            nxt = next(((k, w, bp, len(w.split())) for k, w, bp in self.p
                        if k in ('postfix', 'infix')
                        and self.t[self.i:self.i + len(w.split())] == w.split()), None)
            if not nxt or nxt[2] < minbp:
                break
            k, w, bp, n = nxt
            self.i += n
            if k == 'postfix':
                left = f'«{left}» {w}'
            else:
                right = self.parse(bp + 1)
                if right is None:
                    return None
                left = f'«{left}» {w} «{right}»'
        return left


def declared(src, pats):
    p = P(lex(src), pats)
    out = p.parse(0)
    return out if out is not None and p.i == len(p.t) else None


SRCS = ['sum of a reversed', 'sum of d per t', 'a reversed per t',
        'sum of a reversed per t', 'a reversed reversed']

print('=' * W)
print('§1  Enumerate every reading, then let the declared binding powers filter')
print('=' * W)
print(f'  {"source":26} {"readings":>9} {"survive":>8}   verdict')
print('  ' + '-' * 70)
tot_r = tot_s = 0
DETAIL = {}
for src in SRCS:
    reads = enumerate_all(lex(src), PATS)
    pick = declared(src, PATS)
    surv = [r for r in reads if r == pick]
    DETAIL[src] = (reads, pick)
    tot_r += len(reads)
    tot_s += len(surv)
    v = 'UNIQUE' if len(surv) == 1 else ('TIE' if len(reads) > 1 else 'unique already')
    print(f'  {src:26} {len(reads):>9} {len(surv):>8}   {v}')

print(f'''
  {tot_r} readings in, {tot_s} out -- one per statement, every time.

  Note what the filter is NOT. It is not cost, and it is not a preference over
  equal-cost derivations. It is the declared grammar: «reversed» binds at 20,
  «sum of» at 10, so «sum of a reversed» is «sum of («a» reversed)» and the
  other derivation is not a reading of the language at all.

  This is the mechanism «is» already uses at binding power 5. What changes is
  that patterns with an operand on their LEFT need one too.''')

print()
print('=' * W)
print('§2  Every filtered-out reading must still be writeable')
print('=' * W)
print('  A binding power that DISCARDED a reading would be «cost chooses», which')
print('  the design refuses. So: for each loser, is there a bracketing that')
print('  produces it?')
print()

BRACKETINGS = {
    'sum of a reversed':        ['( sum of a ) reversed'],
    'sum of d per t':           ['( sum of d ) per t'],
    'a reversed per t':         ['a reversed per t'],
    'sum of a reversed per t':  ['( sum of a reversed ) per t',
                                 'sum of ( a reversed per t )',
                                 '( sum of a ) reversed per t'],
    'a reversed reversed':      ['a reversed reversed'],
}

unreachable = 0
for src in SRCS:
    reads, pick = DETAIL[src]
    losers = [r for r in reads if r != pick]
    if not losers:
        continue
    reached = set()
    for b in BRACKETINGS[src]:
        d = declared(b, PATS)
        if d:
            reached.add(d.replace('(', '').replace(')', ''))
    print(f'  {src}')
    print(f'    default   {pick}')
    for l in losers:
        flat = l.replace('(', '').replace(')', '')
        ok = flat in reached
        unreachable += 0 if ok else 1
        print(f'    loser     {l:44} {"REACHABLE" if ok else "** UNREACHABLE **"}')
    for b in BRACKETINGS[src]:
        print(f'    via       {b:44} -> {declared(b, PATS)}')
    print()

print(f'''  unreachable readings: {unreachable}

  So a binding power is not a silent pick, and the reason is the line the whole
  design rests on:

      >> A binding power is part of the DECLARATION. Cost is a property of the
      >> SEARCH. Declared structure may choose between readings; search cost
      >> may not.

  «a + b * c» is not an ambiguity anyone reports -- not because cost broke a
  tie, but because the grammar says what it means, in public, once, and the
  other reading is one bracket away. Same trade.''')

print()
print('=' * W)
print('What it costs and what it deletes')
print('=' * W)
print('''  COSTS   every pattern with an operand on its LEFT must declare a binding
          power. A new required field on the declaration form, and a set of
          numbers the standard library has to choose once and publish.

          And a real hazard worth naming: two library authors picking numbers
          independently. Symbols avoid this because the table is fixed and
          small. A user-extensible one is not, so the numbers want to be named
          LEVELS rather than integers -- «binds like multiplication» -- so that
          an author picks a position in a published ladder instead of inventing
          a number.

  DELETES R6's leading-free-hole clause, and with it
            B1                     «(_) is (_)» stops needing a special case
            postfix units          «5 metres», «wait for 5 minutes»
            «metres per second»    an infix word pattern
            the anchor-first fallback «quantity of (_) in (_)» that
              UNITS-RESEARCH.md §4 called a form field

  The 0.04% tie rate leading_free.py measured is what you pay with binding
  powers all equal. It is not the rate after they are declared.''')
