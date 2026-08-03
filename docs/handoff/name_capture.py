#!/usr/bin/env python3
"""
name_capture.py -- when does DECLARING A NAME change an existing program?

The programmer withdrew a finding: declaring «nothing found» does not re-read
«x otherwise nothing», because the text that changed was NO PARSE before.
Invalid becoming valid is not a hazard. He is right, and he then proposed a
narrower law:

    a declared name that SPANS an operator takes that operator; a name that
    sits BESIDE one takes nothing.

That is R5's shape. But it was proposed from two examples, and this project's
recurring failure is a law stated from examples and then relied on. So:
enumerate every declaration over a small universe, every source over the same
universe, and classify every transition.

    UNCHANGED    parsed before, parses the same after
    EXTENSION    NO PARSE before, parses after            (harmless: monotone)
    CAPTURE      parsed before, parses DIFFERENTLY after  (silent -- the hazard)
    BREAK        parsed before, TIE or NO PARSE after     (loud, but breaking)

Then the law is tested as a predicate, not asserted:  does CAPTURE occur if and
only if the declared name contains a glue word?
"""

import itertools
import sys
import time
from dp_resolver import DPResolver, N, PA, HOLE

W = 78


def run(title, universe, base_names, patterns, glue_words, maxsrc=4, maxname=3):
    print('=' * W)
    print(title)
    print('=' * W)

    pats = PA(*patterns)
    base = frozenset(tuple(x.split()) for x in base_names)

    srcs = []
    for k in range(1, maxsrc + 1):
        srcs.extend(itertools.product(universe, repeat=k))

    def verdict(names, toks):
        v, c, s = DPResolver(names, pats).resolve(' '.join(toks))
        return v, s

    before = {s: verdict(base, s) for s in srcs}

    cands = []
    for k in range(2, maxname + 1):
        for c in itertools.product(universe, repeat=k):
            if c not in base:
                cands.append(c)

    tally = {'UNCHANGED': 0, 'EXTENSION': 0, 'CAPTURE': 0, 'BREAK': 0,
             'WAS-ERROR': 0}
    captures, breaks = [], []

    t0 = time.time()
    for c in cands:
        names = base | {c}
        has_glue = any(w in glue_words for w in c)
        for s in srcs:
            bv, bs = before[s]
            av, asw = verdict(names, s)
            if bv == 'TIE -> ERROR':
                tally['WAS-ERROR'] += 1
            elif bv == 'NO PARSE':
                tally['EXTENSION' if av != 'NO PARSE' else 'UNCHANGED'] += 1
            elif av == 'OK' and bs == asw:
                tally['UNCHANGED'] += 1
            elif av == 'OK':
                tally['CAPTURE'] += 1
                captures.append((c, s, bs, asw, has_glue))
            else:
                tally['BREAK'] += 1
                breaks.append((c, s, bs, av, has_glue))

    dt = time.time() - t0
    print(f'  {len(cands)} candidate declarations x {len(srcs)} sources '
          f'= {len(cands) * len(srcs)} transitions   ({dt:.1f}s)\n')
    for k in ('UNCHANGED', 'EXTENSION', 'CAPTURE', 'BREAK', 'WAS-ERROR'):
        print(f'    {k:12} {tally[k]:>8}')
    print()

    # -- the law, as a predicate -----------------------------------------
    print(f'  LAW UNDER TEST: a declaration is dangerous iff it contains a')
    print(f'  glue word {sorted(glue_words)}\n')
    bad = [x for x in captures + breaks if not x[-1]]
    good = [x for x in captures + breaks if x[-1]]
    print(f'    dangerous transitions with    a glue word in the name: {len(good)}')
    print(f'    dangerous transitions WITHOUT a glue word in the name: {len(bad)}')
    print(f'\n  [{"PASS" if not bad else "FAIL"}] '
          f'{"every capture/break is explained by R5" if not bad else "R5 does NOT cover every case"}\n')

    if bad:
        print('  UNEXPLAINED (first 12):')
        for c, s, bs, av, _ in bad[:12]:
            print(f'    declare «{" ".join(c)}»  on  {" ".join(s)}')
            print(f'        before: {bs}')
            print(f'        after : {av}')
        print()

    if captures:
        print('  SAMPLE CAPTURES (first 6, all glue-bearing):')
        seen = set()
        shown = 0
        for c, s, bs, asw, _ in captures:
            if c in seen:
                continue
            seen.add(c)
            print(f'    declare «{" ".join(c)}»  on  {" ".join(s)}')
            print(f'        before: {bs}')
            print(f'        after : {asw}')
            shown += 1
            if shown == 6:
                break
        print()
    return tally, bad


run('1. «otherwise» -- a word operator with a LEADING free hole',
    universe=['a', 'b', 'nothing', 'found', 'otherwise'],
    base_names=['a', 'b', 'nothing'],
    patterns=['_ otherwise _'],
    glue_words={'otherwise'})

run('2. Generality check -- a MEDIAL glue word in an anchor-first pattern',
    universe=['a', 'b', 'send', 'to', 'x'],
    base_names=['a', 'b', 'x'],
    patterns=['send _ to _'],
    glue_words={'to'})

print('=' * W)
print('3. The specific claim: is «nothing found» safe?')
print('=' * W)
pats = PA('_ otherwise _')
base = N('a', 'nothing')
after = N('a', 'nothing', 'nothing found')
for src in ['a otherwise nothing',
            'a otherwise nothing found',
            'nothing found',
            'nothing otherwise a']:
    bv, bc, bs = DPResolver(base, pats).resolve(src)
    av, ac, asw = DPResolver(after, pats).resolve(src)
    mark = 'unchanged' if (bv, bs) == (av, asw) else (
        'EXTENSION' if bv == 'NO PARSE' else 'CAPTURE')
    print(f'  {src:30} {bv:12} -> {av:12}   {mark}')
print()

print('=' * W)
print('4. And the case R5 exists for: a name that SPANS the operator')
print('=' * W)
after2 = N('a', 'nothing', 'a otherwise nothing')
for src in ['a otherwise nothing']:
    bv, bc, bs = DPResolver(base, pats).resolve(src)
    av, ac, asw = DPResolver(after2, pats).resolve(src)
    print(f'  {src}')
    print(f'    before declaring «a otherwise nothing»: {bv:12} {bs}')
    print(f'    after                                 : {av:12} {asw}')
print()
