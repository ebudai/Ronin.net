#!/usr/bin/env python3
"""
bp_columns.py -- is "one column per binding power" intrinsic, or a keying choice?

The programmer measured the DP table at ~4.5 MB per binding-power level and asks
how many rungs a named ladder should have, chosen against that price. Before
answering the size question: is the price real? A resource argument that can be
engineered away should not be choosing a language's operator vocabulary.

FIRST VERSION OF THIS FILE WAS WRONG in a way worth keeping in the file. I
modelled scheme A as a lazy memo keyed by (span, minbp) and predicted its key
count would grow with the number of levels. It did not -- it was flat, because a
lazily memoised span is only ever reached with the minbps its parents actually
ask for. That refutes my model, not his measurement, and separating the two is
the useful result:

    the growth he measured is a property of EAGER ALLOCATION -- "the table
    carries a column per level the recurrences CAN ask for" -- and not of
    memoising by binding power at all.

So there are two independent savings, and they are worth pricing separately:

  §1  correctness -- does a one-column scheme compute the same parses?
  §2  eager vs lazy       -- allocate every column, or only reachable keys
  §3  keyed vs tagged     -- is the bp dimension needed at all
"""

W = 78

# Operators, some SHARING a level, so equal-precedence chains are genuinely
# ambiguous and "same parse set" is a real test rather than a formality.
OPS = {'+': 20, '-': 20, '*': 30, '/': 30, '<': 10, '>': 10}
LEVELS = sorted(set(OPS.values()))


def make_input(n, ops):
    letters = 'abcdefghijklmnopqrstuvwxyz'
    toks = []
    for i in range(n):
        toks.append(letters[i])
        if i < n - 1:
            toks.append(ops[i % len(ops)])
    return toks


# ---------------------------------------------------------------------------
# A: memo keyed by (span, minbp). Equal precedence left AMBIGUOUS on purpose.
# ---------------------------------------------------------------------------
def parse_A(toks):
    memo, visited = {}, set()

    def go(i, j, minbp):
        k = (i, j, minbp)
        if k in memo:
            return memo[k]
        visited.add(k)
        memo[k] = []
        out = []
        if j - i == 1:
            out.append(toks[i])
        for m in range(i + 1, j):
            w = toks[m]
            if w in OPS and OPS[w] >= minbp:
                bp = OPS[w]
                for l in go(i, m, bp):
                    for r in go(m + 1, j, bp):
                        out.append(f'({l} {w} {r})')
        memo[k] = out
        return out

    return set(go(0, len(toks), 0)), len(visited)


# ---------------------------------------------------------------------------
# B: one column per span; each derivation carries its own top binding power.
# ---------------------------------------------------------------------------
def parse_B(toks):
    memo = {}
    ATOM = 10 ** 6

    def go(i, j):
        if (i, j) in memo:
            return memo[(i, j)]
        memo[(i, j)] = []
        out = []
        if j - i == 1:
            out.append((toks[i], ATOM))
        for m in range(i + 1, j):
            w = toks[m]
            if w in OPS:
                bp = OPS[w]
                for l, lbp in go(i, m):
                    if lbp < bp:
                        continue
                    for r, rbp in go(m + 1, j):
                        if rbp < bp:
                            continue
                        out.append((f'({l} {w} {r})', bp))
        memo[(i, j)] = out
        return out

    return {t for t, _ in go(0, len(toks))}, len(memo)


print('=' * W)
print('§1  Do the two schemes compute the same parses?')
print('=' * W)
print(f'  {"tokens":>7} {"parses":>8}   A keys   B keys   identical?')
print('  ' + '-' * 60)
bad = 0
for n in (3, 4, 5, 6, 7):
    toks = make_input(n, list(OPS))
    sa, ka = parse_A(toks)
    sb, kb = parse_B(toks)
    ok = sa == sb
    bad += 0 if ok else 1
    print(f'  {len(toks):>7} {len(sa):>8} {ka:>8} {kb:>8}   '
          f'{"yes" if ok else "** NO **"}')

print(f'''
  mismatches: {bad}.  Ambiguity is genuine here -- «+» and «-» share a level, so
  a chain has Catalan-many readings and set equality is a real test.

  So the bp dimension is not carrying information. The top binding power of a
  derivation is one integer, and the derivation can hold it.''')

print()
print('=' * W)
print('§2  Where the growth actually comes from: EAGER vs LAZY')
print('=' * W)
print('''  His table "carries a column per level the recurrences CAN ask for" -- so it
  is allocated over the whole level set, not filled on demand. That is the
  growth, and it is separable from the keying.
''')
print(f'  {"tokens":>7} {"spans":>7} {"eager: spans x levels":>23} {"lazy A keys":>13}')
print('  ' + '-' * 60)
for n in (3, 5, 7):
    toks = make_input(n, list(OPS))
    t = len(toks)
    spans = t * (t + 1) // 2
    _, ka = parse_A(toks)
    print(f'  {t:>7} {spans:>7} {spans * len(LEVELS):>23} {ka:>13}')

print(f'''
  levels in this grammar: {len(LEVELS)}

  Lazy keying alone already collapses it, because a span is only ever reached
  with the minbps its parents actually ask for -- a small fraction of the level
  set. That is a saving available WITHOUT changing the parser's shape at all.''')

print()
print('=' * W)
print('§3  So how many rungs?')
print('=' * W)
print(f'''  Two independent reductions, in the order I would take them:

    1. allocate lazily             removes the "column per level the recurrences
                                   CAN ask for" -- levels stop being a
                                   multiplier on the whole table
    2. tag instead of key          removes the dimension entirely; §1 shows the
                                   answers are identical

  Either one alone makes a rung approximately free. So the ladder's size goes
  back to being a READABILITY question, which is the right question for it to be.

  The rungs a reader actually distinguishes, with what already exists marked:

      8  postfix / tightest        units, «(_) reversed»
      7  multiplicative
      6  additive
      5  comparison                «is» -- ALREADY RULED at 5
      4  range / interval
      3  logical and
      2  logical or
      1  loosest                   ascription «(x => Text)»

  Eight, and «is» at 5 lands in the right place without moving. That is a check
  on the ladder rather than a coincidence: 5 was chosen for its own reasons two
  documents before this one.

  For scale: C has 15 and is the standard example of too many. Haskell has 10.
  Smalltalk has one and is the standard example of too few. Eight is inside the
  range that works, and NAMED is what stops it drifting -- an author writes
  «binds like multiplication», never 70.''')
