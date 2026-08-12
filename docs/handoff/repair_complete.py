#!/usr/bin/env python3
"""
repair_complete.py -- the property the delete branch rests on.

If ambiguity becomes an error whose fix is "bracket it", then EVERY reading of
an ambiguous statement must be reachable by SOME bracketing. If one is not, the
programmer is handed an error with no fix and a meaning they cannot express --
which is worse than the silent capture we are removing.

The old backtracking probe asserted this for single-bracket insertion. Under the
delete branch it stops being a nice property and becomes load-bearing, so it is
checked here over generated ambiguous statements, with up to two insertions.
"""

import itertools
import random

W = 78


def P(*specs):
    return [tuple(None if w == '_' else w for w in s.split()) for s in specs]


PATS = P('print _', 'sum of _', 'send _ to _', 'item _ of _',
         'total for _', 'sort _ by _')


def enumerate_parses(toks, names):
    """All parse trees of toks. Trees render canonically -- brackets do not
    appear, so a bracketed source and its unbracketed reading compare equal."""
    n = len(toks)
    memo = {}

    def bracketed(i, j):
        if j - i < 2 or toks[i] != '(' or toks[j - 1] != ')':
            return False
        d = 0
        for k in range(i, j):
            if toks[k] == '(':
                d += 1
            elif toks[k] == ')':
                d -= 1
                if d == 0 and k != j - 1:
                    return False
        return d == 0

    def E(i, j):
        if (i, j) in memo:
            return memo[(i, j)]
        memo[(i, j)] = out = []
        if all(t not in '()' for t in toks[i:j]) and tuple(toks[i:j]) in names:
            out.append('«' + ' '.join(toks[i:j]) + '»')
        if bracketed(i, j):
            out.extend(E(i + 1, j - 1))
        for pat in PATS:
            out.extend(M(pat, 0, i, j))
        return out

    def M(pat, si, i, j):
        if si == len(pat):
            return [''] if i == j else []
        seg = pat[si]
        if seg is not None:
            if i < j and toks[i] == seg:
                return [(seg + ' ' + r).strip() for r in M(pat, si + 1, i + 1, j)]
            return []
        out, last = [], si == len(pat) - 1
        for sp in ([j] if last else range(i + 1, j + 1)):
            for a in E(i, sp):
                for r in M(pat, si + 1, sp, j):
                    out.append((a + ' ' + r).strip())
        return out

    return E(0, n)


def insertions(toks, k):
    """All ways to add k bracket pairs around spans."""
    if k == 0:
        yield toks
        return
    for i in range(len(toks)):
        for j in range(i + 1, len(toks) + 1):
            cand = toks[:i] + ['('] + toks[i:j] + [')'] + toks[j:]
            yield from insertions(cand, k - 1)


random.seed(3)
BASE = ['order', 'price', 'total', 'count', 'item', 'line', 'rate']
GLUE = ['to', 'of', 'for', 'by']
ANCHOR = ['print', 'sum', 'send', 'item', 'total', 'sort']


def make_names():
    s = {(w,) for w in BASE}
    vocab = BASE + GLUE + ANCHOR
    while len([x for x in s if len(x) > 1]) < 14:
        k = random.choice([2, 2, 3])
        s.add(tuple(random.choice(vocab) for _ in range(k)))
    return s


def gen(names, depth=0):
    if depth > 2 or random.random() < 0.45:
        return list(random.choice(sorted(names)))
    out = []
    for seg in random.choice(PATS):
        out += [seg] if seg is not None else gen(names, depth + 1)
    return out


print('=' * W)
print('Is every reading of an ambiguous statement reachable by bracketing?')
print('=' * W)
checked = unreachable = 0
examples, misses = [], []
for _ in range(400):
    NAMES = make_names()
    src = gen(NAMES)
    if not (3 <= len(src) <= 9):
        continue
    readings = set(enumerate_parses(src, NAMES))
    if len(readings) < 2:
        continue
    reachable = set()
    for k in (1, 2):
        for cand in insertions(src, k):
            if len(cand) > len(src) + 6:
                continue
            rs = set(enumerate_parses(cand, NAMES))
            if len(rs) == 1:
                reachable |= rs
        if reachable >= readings:
            break
    checked += 1
    if not (readings <= reachable):
        unreachable += 1
        misses.append((src, readings - reachable, readings))
    elif len(examples) < 3:
        examples.append((src, sorted(readings)))

print(f'  ambiguous statements checked : {checked}')
print(f'  with an unreachable reading  : {unreachable}')
print()
for src, rs in examples:
    print(f'  «{" ".join(src)}»')
    for r in rs:
        print(f'      {r}')
    print()
if misses:
    print('  UNREACHABLE:')
    for src, miss, allr in misses[:3]:
        print(f'    «{" ".join(src)}»  cannot express: {sorted(miss)}')
print(f'''  [{"PASS" if not unreachable else "FAIL"}] every reading is selectable by at most two bracket pairs

  Which is what makes "ambiguity is an error, bracket it" a complete answer
  rather than a dead end. It has to stay a property test, not a one-off check:
  a future pattern shape could break it, and the symptom would be a program
  nobody can write.''')
