#!/usr/bin/env python3
"""
Leading holes: is the programmer's LeadingHole rule stricter than it needs to be?

He is right that it might be. R6 refuses any pattern beginning with a hole. A
BHOLE in leading position is not left-recursive -- it must consume «(» before
recursing -- so «{_} otherwise {_}» is expressible where «(_) otherwise (_)» is
not, and the current rule refuses both.

The claim to test: R6's real subject is not "does the pattern start with a
word" but "how far can matching proceed DETERMINISTICALLY". Words match
deterministically. A BHOLE does too -- its extent is fixed by bracket matching.
A THOLE does -- exactly one token. Only a free HOLE is indeterminate, because
its extent is unmarked.

    determinate prefix = the leading run of segments up to the first free HOLE

R6 generalises to: determinate prefixes must be prefix-free. That subsumes the
anchor-run rule (identical when no BHOLE/THOLE is present) and gives a
principled answer for leading holes instead of a blanket refusal.

Three admission policies, run against each other:

    strict    no leading hole of any kind            (the rule as implemented)
    bracket   leading BHOLE allowed; HOLE, THOLE not
    loose     leading BHOLE and THOLE allowed

    python3 fuzz_leading.py <policy> [max_units]
"""

import itertools
import sys
import time
from ronin_grammar_probe import Scope, HOLE
from bracket_probe import BProbe, BHOLE, THOLE, pat_str, glue

WORDS = ['a', 'b', 'to', 'of']
HOLES = [HOLE, BHOLE, THOLE]
FREE = (HOLE,)

policy = sys.argv[1] if len(sys.argv) > 1 else 'bracket'
MAXU = int(sys.argv[2]) if len(sys.argv) > 2 else 2
ALLOWED_LEADING = {'strict': (), 'bracket': (BHOLE,), 'loose': (BHOLE, THOLE)}[policy]


def determinate_prefix(pat):
    """Leading segments up to the first FREE hole. Words, BHOLEs and THOLEs are
    all determinate; only an unbracketed hole is not."""
    run = []
    for s in pat:
        if s in FREE:
            break
        run.append(s)
    return tuple(run)


def prefix_free(pats):
    runs = [determinate_prefix(p) for p in pats]
    for i, r1 in enumerate(runs):
        for j, r2 in enumerate(runs):
            if i != j and len(r1) < len(r2) and r2[:len(r1)] == r1:
                return False
    return True


def admissible(pat):
    if pat[0] in HOLES and pat[0] not in ALLOWED_LEADING:
        return False
    return True


def gen_patterns():
    out = set()
    for anchor in ['a', 'b']:
        for h in HOLES:
            out.add((anchor, h))
            for w in ['to', 'of', 'b']:
                out.add((anchor, w, h))
                for h2 in HOLES:
                    out.add((anchor, h, w, h2))
    for h in HOLES:                                  # leading-hole shapes
        for w in ['to', 'of', 'b']:
            out.add((h, w))
            for h2 in HOLES:
                out.add((h, w, h2))
    return sorted([p for p in out if admissible(p)],
                  key=lambda p: (len(p), pat_str(p)))


def gen_names():
    out = {(w,) for w in WORDS}
    out |= {(w1, w2) for w1, w2 in itertools.product(WORDS, repeat=2)}
    return sorted(out)


def gen_statements(max_units):
    units = list(WORDS) + [f'( {w} )' for w in WORDS]
    return [' '.join(c) for n in range(2, max_units + 1)
            for c in itertools.product(units, repeat=n)]


def legal_names(names, pats):
    res = set()
    for p in pats:
        res |= glue(p, 'refined')
    return frozenset(n for n in names
                     if not (len(n) > 1 and any(w in res for w in n)))


PATS = gen_patterns()
NAMES = gen_names()
STMTS = gen_statements(MAXU)
NAME_SETS = list(itertools.combinations(NAMES, 2))
LEADING = [p for p in PATS if p[0] in HOLES]
PAIRS = [c for c in itertools.combinations(PATS, 2)
         if prefix_free(c) and (not LEADING or any(p[0] in HOLES for p in c))]

t0 = time.time()
ties, checked, rejected = [], 0, 0
for pats in PAIRS:
    patset = frozenset(pats)
    for names in NAME_SETS:
        legal = legal_names(frozenset(names), patset)
        if len(legal) < 2:
            continue
        try:
            pr = BProbe(Scope(names=legal, patterns=patset))
        except ValueError:
            rejected += 1
            continue
        for src in STMTS:
            checked += 1
            v, winners, _ = pr.resolve(src)
            if v == 'TIE -> ERROR':
                ties.append((tuple(sorted(pat_str(p) for p in patset)), src,
                             tuple(sorted(' '.join(n) for n in legal)), winners))

print(f'policy {policy:8} patterns {len(PATS):4} (leading-hole {len(LEADING):3})  '
      f'pairs {len(PAIRS):5}  statements {len(STMTS):4}')
print(f'  resolutions {checked:>10}   ties {len(ties):>5}   {time.time()-t0:6.1f}s')

if ties:
    print('\n  COUNTEREXAMPLES (first 5 distinct pattern/statement pairs):')
    seen = set()
    for pstrs, src, names, winners in ties:
        key = (pstrs, src)
        if key in seen:
            continue
        seen.add(key)
        print(f'\n    patterns  {list(pstrs)}')
        print(f'    names     {list(names)}')
        print(f'    statement {src}')
        for w in winners:
            print(f'      {w}')
        if len(seen) >= 5:
            break
