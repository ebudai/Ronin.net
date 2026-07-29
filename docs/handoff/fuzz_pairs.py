#!/usr/bin/env python3
"""Pattern-PAIR run for bracket-delimited holes: covers the R6 interaction the
single-pattern run in fuzz_brackets.py deliberately left out.

    python3 fuzz_pairs.py blanket
    python3 fuzz_pairs.py refined

Writes ties to fuzz_pairs_<policy>.txt so the two runs can be compared without
holding both in one process."""

import itertools
import sys
import time
from ronin_grammar_probe import Scope
from bracket_probe import BProbe, pat_str, anchor_run, glue
from fuzz_brackets import gen_patterns, gen_names, gen_statements

policy = sys.argv[1]
assert policy in ('blanket', 'refined')


def prefix_free(pats):
    runs = [anchor_run(p) for p in pats]
    for i, r1 in enumerate(runs):
        for j, r2 in enumerate(runs):
            if i != j and len(r1) < len(r2) and r2[:len(r1)] == r1:
                return False
    return True


def legal_names(names, pats, policy):
    res = set()
    for p in pats:
        res |= glue(p, policy)
    return frozenset(n for n in names
                     if not (len(n) > 1 and any(w in res for w in n)))


PATS = gen_patterns()
NAMES = gen_names()
STMTS = gen_statements(int(sys.argv[2]) if len(sys.argv) > 2 else 3)
NAME_SETS = list(itertools.combinations(NAMES, 2))
PAIRS = [c for c in itertools.combinations(PATS, 2) if prefix_free(c)]

t0 = time.time()
ties, checked = [], 0
for pats in PAIRS:
    patset = frozenset(pats)
    for names in NAME_SETS:
        legal = legal_names(frozenset(names), patset, policy)
        if len(legal) < 2:
            continue
        pr = BProbe(Scope(names=legal, patterns=patset))
        for src in STMTS:
            checked += 1
            v, winners, _ = pr.resolve(src)
            if v == 'TIE -> ERROR':
                ties.append((tuple(sorted(pat_str(p) for p in patset)), src,
                             tuple(sorted(' '.join(n) for n in legal))))

with open(f'fuzz_pairs_{policy}.txt', 'w') as f:
    f.write(f'policy {policy}\nunits {len(STMTS)}\npairs {len(PAIRS)}\nresolutions {checked}\n'
            f'ties {len(ties)}\nseconds {time.time() - t0:.1f}\n')
    for t in sorted(set(ties)):
        f.write(repr(t) + '\n')

print(f'{policy}: pairs {len(PAIRS)}  resolutions {checked}  ties {len(ties)}  '
      f'{time.time() - t0:.1f}s')
