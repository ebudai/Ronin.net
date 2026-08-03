#!/usr/bin/env python3
"""
name_capture2.py -- the first run's control was degenerate; this fixes it.

Run 2 of name_capture.py found ZERO captures for the medial glue word «to» in
«send (_) to (_)». That is not evidence that medial glue is safe -- it is an
artefact: with no shorter pattern «send (_)» in scope, the literal «to» is
mandatory, so a name spanning it has nowhere to go. R5's hazard needs a rival
reading to exist.

This run supplies the rival. Two configurations:

    A   «send (_) to (_)» AND «send (_)»      -- the classic R5 shape
    B   «print (_)» AND «(_) otherwise (_)»   -- a prefix pattern with a free
                                                 hole sitting NEXT TO the
                                                 operator, which is the exact
                                                 configuration «nothing found»
                                                 lives in

Single-word declarations are included this time, so «otherwise» itself as a
name is covered.
"""

import itertools
import time
from dp_resolver import DPResolver, N, PA

W = 78


def run(title, universe, base_names, patterns, glue_words,
        maxsrc=4, minname=1, maxname=3):
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

    cands = [c for k in range(minname, maxname + 1)
             for c in itertools.product(universe, repeat=k) if c not in base]

    tally = {'UNCHANGED': 0, 'EXTENSION': 0, 'CAPTURE': 0, 'BREAK': 0,
             'WAS-ERROR': 0}
    captures, breaks = [], []
    t0 = time.time()
    for c in cands:
        names = base | {c}
        hg = any(w in glue_words for w in c)
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
                captures.append((c, s, bs, asw, hg))
            else:
                tally['BREAK'] += 1
                breaks.append((c, s, bs, av + ' ' + asw, hg))
    dt = time.time() - t0

    print(f'  {len(cands)} declarations x {len(srcs)} sources = '
          f'{len(cands)*len(srcs)} transitions   ({dt:.1f}s)\n')
    for k in ('UNCHANGED', 'EXTENSION', 'CAPTURE', 'BREAK', 'WAS-ERROR'):
        print(f'    {k:12} {tally[k]:>8}')

    bad = [x for x in captures + breaks if not x[-1]]
    good = [x for x in captures + breaks if x[-1]]
    print(f'\n  dangerous WITH    a glue word {sorted(glue_words)} in the name: {len(good)}')
    print(f'  dangerous WITHOUT a glue word in the name                : {len(bad)}')
    print(f'  [{"PASS" if not bad else "FAIL"}] R5 (reserve glue inside names) '
          f'{"covers every case" if not bad else "does NOT cover every case"}\n')

    if bad:
        print('  UNEXPLAINED (first 10):')
        for c, s, bs, asw, _ in bad[:10]:
            print(f'    declare «{" ".join(c)}» on «{" ".join(s)}»')
            print(f'        before: {bs}')
            print(f'        after : {asw}')
        print()

    seen, shown = set(), 0
    if captures:
        print('  SAMPLE CAPTURES:')
        for c, s, bs, asw, _ in captures:
            if c in seen:
                continue
            seen.add(c)
            print(f'    declare «{" ".join(c)}» on «{" ".join(s)}»')
            print(f'        before: {bs}')
            print(f'        after : {asw}')
            shown += 1
            if shown == 5:
                break
        print()
    return bad


run('A. The classic R5 shape -- «send (_) to (_)» WITH a rival «send (_)»',
    universe=['a', 'b', 'send', 'to'],
    base_names=['a', 'b'],
    patterns=['send _ to _', 'send _'],
    glue_words={'to'})

run('B. A free-hole prefix pattern BESIDE the operator '
    '-- «nothing found»\'s actual habitat',
    universe=['a', 'nothing', 'found', 'otherwise', 'print'],
    base_names=['a', 'nothing'],
    patterns=['print _', '_ otherwise _'],
    glue_words={'otherwise'})
