#!/usr/bin/env python3
"""
injected_and_repair.py -- the auditor's two gaps.

GAP 1  the rule must apply to INJECTED names, and the diagnostic must be owned
       by whoever can act on it.
GAP 2  repair_complete.py FAILS as written, because it does not filter names by
       the new rule. A document citing a failing probe is the exact "claim
       outlives its evidence" shape, so it is fixed here rather than explained.

Both are answered by one observation: whether a collision is UNIVERSAL over the
injected hole or PARTICULAR to one filling.
"""

import random

W = 78


def P(*specs):
    return [tuple(None if w == '_' else w for w in s.split()) for s in specs]


WORDOPS = {'is'}


# --------------------------------------------------------------- resolution
def parses(toks, names, pats):
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
        for pat in pats:
            out.extend(M(pat, 0, i, j))
        for k in range(i + 1, j - 1):
            if toks[k] in WORDOPS:
                d = sum(1 if t == '(' else -1 if t == ')' else 0 for t in toks[i:k])
                if d:
                    continue
                for a in E(i, k):
                    for b in E(k + 1, j):
                        out.append(f'({a} {toks[k]} {b})')
        return out

    def M(pat, si, i, j):
        if si == len(pat):
            return [''] if i == j else []
        seg = pat[si]
        if seg is not None:
            return [(seg + ' ' + r).strip()
                    for r in M(pat, si + 1, i + 1, j)] if i < j and toks[i] == seg else []
        out, last = [], si == len(pat) - 1
        for sp in ([j] if last else range(i + 1, j + 1)):
            for a in E(i, sp):
                for r in M(pat, si + 1, sp, j):
                    out.append((a + ' ' + r).strip())
        return out

    return set(E(0, len(toks)))


def self_ambiguous(nm, pats):
    """PESSIMISTIC: any word run could be a name, so the check does not depend
    on what else happens to be declared."""
    n = len(nm)
    C = [[0] * (n + 1) for _ in range(n + 1)]

    def M(pat, si, i, j):
        if si == len(pat):
            return 1 if i == j else 0
        seg = pat[si]
        if seg is not None:
            return M(pat, si + 1, i + 1, j) if i < j and nm[i] == seg else 0
        tot, last = 0, si == len(pat) - 1
        for sp in ([j] if last else range(i + 1, j + 1)):
            if C[i][sp]:
                tot += C[i][sp] * M(pat, si + 1, sp, j)
        return tot

    for w in range(1, n + 1):
        for i in range(0, n - w + 1):
            j = i + w
            t = 1                                   # any run may be a name
            for p in pats:
                t += M(p, 0, i, j)
            for k in range(i + 1, j - 1):
                if nm[k] in WORDOPS and C[i][k] and C[k + 1][j]:
                    t += C[i][k] * C[k + 1][j]
            C[i][j] = t
    return C[0][n] > 1


# ============================================================ GAP 1
print('=' * W)
print('1. Injected names, and who owns the diagnostic')
print('=' * W)
print('''  An injected name has a fixed prefix and a hole filled by a user name --
  «index of» + the loop variable. So its collision is either UNIVERSAL over that
  hole or PARTICULAR to one filling, and that decides who can act:

      universal   no loop variable avoids it  -> blame the PATTERN, once
      particular  this loop variable causes it -> blame the DECLARATION, per site

  The test is one line: substitute a fresh, otherwise-unused word for the hole
  and re-run the check.
''')
FRESH = ('qqq',)
CASES = [
    ('index of (_) exists',
     P('index of _', 'print _'), [('bank',), ('is', 'valid'), ('bank', 'account')]),
    ('index of bank (_) exists',
     P('index of bank _', 'print _'), [('bank',), ('is', 'valid'), ('bank', 'account')]),
]
for label, pats, loopvars in CASES:
    universal = self_ambiguous(('index', 'of') + FRESH, pats)
    print(f'  {label}')
    print(f'      fresh filling «index of qqq» self-ambiguous: {universal}'
          f'   -> {"UNIVERSAL, blame the pattern once" if universal else "particular, blame per declaration"}')
    for lv in loopvars:
        nm = ('index', 'of') + lv
        print(f'          injected «{" ".join(nm):24}» self-ambiguous: '
              f'{self_ambiguous(nm, pats)}')
    print()
print('''  «index of is valid» is refused in BOTH configurations, because «is» is an
  operator word inside the injected span -- which is the case an InjectedBy
  exemption would let through, and the reason the rule must be stated over
  written AND injected names.

  And the two blame rules fall out of the same test rather than being asserted:
  «index of (_)» collides for every filling, so renaming a loop variable cannot
  help and one diagnostic against the pattern is the whole truth; «index of bank
  (_)» collides only for fillings starting «bank», so the loop variable is
  actionable and the later declaration is blamed, per site.''')

# ============================================================ GAP 2
print('=' * W)
print('2. Repair completeness -- EXHAUSTIVE, with the new rule applied')
print('=' * W)
import itertools

PATS = P('print _', 'send _ to _', 'send _', 'sum of _')
VOCAB = ['a', 'b', 'to', 'send', 'print']
CAND = [('a',), ('b',), ('a', 'to', 'b'), ('to', 'to'), ('b', 'to', 'a'),
        ('to', 'a'), ('send', 'a'), ('a', 'is', 'b')]

NAMES, refused = set(), []
for nm in CAND:
    (refused.append(nm) if self_ambiguous(nm, PATS) else NAMES.add(nm))
print(f'  refused by the rule : {[" ".join(n) for n in refused]}')
print(f'  admitted            : {sorted(" ".join(n) for n in NAMES)}')
print()
print('  The admitted set deliberately keeps everything Glue(names) used to')
print('  refuse -- interior glue (a to b) and the all-glue name (to to) -- so')
print('  the property is tested on exactly the names the deletion lets through.')
print()


def insertions(toks, k):
    if k == 0:
        yield toks
        return
    for i in range(len(toks)):
        for j in range(i + 1, len(toks) + 1):
            yield from insertions(toks[:i] + ['('] + toks[i:j] + [')'] + toks[j:], k - 1)


checked = bad = 0
misses = []
total = sum(len(VOCAB) ** k for k in range(2, 7))
for n in range(2, 7):
    for tup in itertools.product(VOCAB, repeat=n):
        s = list(tup)
        rs = parses(s, NAMES, PATS)
        if len(rs) < 2:
            continue
        checked += 1
        got = set()
        for k in (1, 2):
            for cand in insertions(s, k):
                if len(cand) > len(s) + 6:
                    continue
                c = parses(cand, NAMES, PATS)
                if len(c) == 1:
                    got |= c
            if rs <= got:
                break
        if not (rs <= got):
            bad += 1
            misses.append((s, sorted(rs - got)))

print(f'  every statement of length 2..6 : {total} candidates')
print(f'  ambiguous                      : {checked}')
print(f'  with an unreachable reading    : {bad}')
for s, m in misses[:5]:
    print(f'      {" ".join(s)} cannot express {m}')
print()
print(f'  [{"PASS" if not bad else "FAIL"}] every reading of every ambiguous statement is selectable')
print()
print('  Exhaustive rather than sampled: the earlier sampled run found only two')
print('  ambiguous statements, and two is not a property test. This enumerates')
print('  the whole space at these lengths, so the claim is over all of it.')
