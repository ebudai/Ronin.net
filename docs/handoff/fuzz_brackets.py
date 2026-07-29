#!/usr/bin/env python3
"""
Exhaustive tie search with bracket-delimited holes -- ZERO-GLUE mechanism 3.

The question: if a glue word is immediately preceded by a bracketed hole, may
it be left unreserved? The structural argument says yes (a name is a word-only
span and cannot straddle a bracket). R5's blanket form is what the original
search verified, so the refinement gets its own run.

SCOPE, stated up front because the last figure was quoted without one:

  * SINGLE-pattern scopes, not pairs. The refinement changes R5 (names against
    glue), which is a per-pattern property. R6 (pattern anchor prefixes) is
    untouched by it and was covered by the original run.
  * anchor-first patterns only. A leading BHOLE is expressible now -- it is not
    left-recursive, because it must consume «(» -- but admitting it is a
    separate R6 question, flagged at the end rather than answered here.
  * statements of 2-3 units, where a unit is a word or a bracketed word.
"""

import itertools
import time
from ronin_grammar_probe import Scope, HOLE
from bracket_probe import BProbe, BHOLE, THOLE, pat_str, anchor_run, glue

WORDS = ['a', 'b', 'to', 'of']
HOLES = [HOLE, BHOLE, THOLE]


def gen_patterns():
    """anchor [word] hole [word hole], every hole either kind."""
    out = set()
    for anchor in ['a', 'b']:
        for h in HOLES:
            out.add((anchor, h))
            for w in ['to', 'of', 'b']:
                out.add((anchor, w, h))
                for h2 in HOLES:
                    out.add((anchor, h, w, h2))
    return sorted(out, key=lambda p: (len(p), pat_str(p)))


def gen_names():
    out = {(w,) for w in WORDS}
    out |= {(w1, w2) for w1, w2 in itertools.product(WORDS, repeat=2)}
    return sorted(out)


def gen_statements(max_units=3):
    units = list(WORDS) + [f'( {w} )' for w in WORDS]
    out = []
    for n in range(2, max_units + 1):
        for c in itertools.product(units, repeat=n):
            out.append(' '.join(c))
    return out


def legal_names(names, pat, policy):
    res = glue(pat, policy)
    return frozenset(n for n in names
                     if not (len(n) > 1 and any(w in res for w in n)))


PATS = gen_patterns()
NAMES = gen_names()
STMTS = gen_statements()
NAME_SETS = list(itertools.combinations(NAMES, 2))

print('=' * 78)
print('BRACKET-DELIMITED HOLES: EXHAUSTIVE TIE SEARCH')
print('=' * 78)
print(f'  patterns          : {len(PATS)}')
print(f'  name pairs        : {len(NAME_SETS)}')
print(f'  statements        : {len(STMTS)}')
print(f'  scopes per policy : {len(PATS) * len(NAME_SETS)}')
print()

results = {}
for policy in ('blanket', 'refined'):
    t0 = time.time()
    ties, checked, skipped = [], 0, 0
    for pat in PATS:
        patset = frozenset({pat})
        for names in NAME_SETS:
            legal = legal_names(frozenset(names), pat, policy)
            if len(legal) < 2:
                skipped += 1
                continue
            pr = BProbe(Scope(names=legal, patterns=patset))
            for src in STMTS:
                checked += 1
                verdict, winners, parses = pr.resolve(src)
                if verdict == 'TIE -> ERROR':
                    ties.append((pat, legal, src, winners))
    results[policy] = (ties, checked, time.time() - t0)
    print(f'  {policy:8}  resolutions {checked:>9}   ties {len(ties):>5}   '
          f'{time.time() - t0:6.1f}s')

print()
blanket_ties, refined_ties = results['blanket'][0], results['refined'][0]
new = [t for t in refined_ties
       if (pat_str(t[0]), t[2]) not in {(pat_str(b[0]), b[2]) for b in blanket_ties}]
print(f'  ties present under refined but not under blanket: {len(new)}')
if new:
    print('\n  COUNTEREXAMPLES -- the refinement is UNSOUND as stated:')
    seen = set()
    for pat, names, src, winners in new:
        key = (pat_str(pat), src)
        if key in seen:
            continue
        seen.add(key)
        print(f'\n    pattern   {pat_str(pat)}')
        print(f'    names     {sorted(" ".join(n) for n in names)}')
        print(f'    statement {src}')
        for c, s in winners:
            print(f'      {c} lookups  {s}')
        if len(seen) >= 6:
            break
else:
    print('''
  None. Relaxing the reservation for glue that follows a bracketed hole
  introduces no tie that the blanket rule was preventing, over this space.''')

print()
print('=' * 78)
print('WHAT THIS DOES AND DOES NOT LICENSE')
print('=' * 78)
print(f'''  Verified: single-pattern scopes, anchor-first patterns, holes of both
  kinds, names of 1-2 words, statements of 2-3 units over {len(WORDS)} words with
  bracketed units admitted.

  NOT verified, and each needs its own run before anyone relies on it:

    * pattern PAIRS with bracketed holes -- R6 interaction
    * leading-BHOLE (bracket-delimited infix), which is newly expressible
      because it is not left-recursive; whether R6 should admit it is open
    * the pinned-declaring-hole variant, which is a different mechanism:
      it constrains a hole to ONE TOKEN rather than requiring brackets''')
