#!/usr/bin/env python3
"""
Adversarial verification. Hand-picked cases prove nothing about cases nobody
thought of, so: enumerate small scopes and statements exhaustively and search
for a surviving tie under

    minimum-lookup  +  non-leading-segment reservation  +  expr-extent args

Any tie found is a counterexample: a statement the compiler must reject even
though every name in it was legally declared.
"""

import itertools
from ronin_grammar_probe import Probe, Scope, HOLE, pat_str

WORDS = ['a', 'b', 'to', 'of']


def reserved_nonleading(patterns):
    """FIX: 'non-leading' must be judged by POSITION, not by word value.
    The first version excluded any segment equal to the anchor word, so in
    pattern «b b (_)» the second 'b' was wrongly treated as leading and left
    unreserved. Found by the fuzzer, not by inspection."""
    out = set()
    for p in patterns:
        run = 0
        while run < len(p) and p[run] is not HOLE:
            run += 1              # the anchor run: words before the first hole
        out |= {s for s in p[run:] if s is not HOLE}
    return out


def anchor_run(p):
    run = []
    for s in p:
        if s is HOLE:
            break
        run.append(s)
    return tuple(run)


def prefix_free(patterns):
    """The fuzzer's other finding: «b (_)» and «b b (_)» tie on 'b b b a' with
    no name involved at all -- b(bb(a)) and bb(b(a)) both cost 3. No naming
    rule can fix that; it is a property of the pattern set. Require anchor
    runs to be prefix-free."""
    runs = [anchor_run(p) for p in patterns]
    for i, r1 in enumerate(runs):
        for j, r2 in enumerate(runs):
            if i != j and len(r1) < len(r2) and r2[:len(r1)] == r1:
                return False, (r1, r2)
    return True, None


def legal_names(names, patterns):
    res = reserved_nonleading(patterns)
    return frozenset(n for n in names
                     if not (len(n) > 1 and any(w in res for w in n)))


def gen_patterns():
    """All patterns of shape: anchor [word] hole [word hole], <= 4 segments."""
    pats = set()
    for anchor in ['a', 'b']:
        pats.add((anchor, HOLE))
        for w in ['to', 'of', 'b']:
            pats.add((anchor, w, HOLE))
            pats.add((anchor, HOLE, w, HOLE))
    return sorted(pats, key=lambda p: (len(p), pat_str(p)))


def gen_names():
    out = set()
    for w in WORDS:
        out.add((w,))
    for w1, w2 in itertools.product(WORDS, repeat=2):
        out.add((w1, w2))
    return sorted(out)


ALL_PATS = gen_patterns()
ALL_NAMES = gen_names()

ties = []
checked = 0
statements = [' '.join(c) for n in (2, 3, 4)
              for c in itertools.product(WORDS, repeat=n)]

NAME_SETS = list(itertools.combinations(ALL_NAMES, 2))

rejected_patsets = 0
for pats in itertools.combinations(ALL_PATS, 2):
    patset = frozenset(pats)
    ok, clash = prefix_free(patset)
    if not ok:
        rejected_patsets += 1
        continue
    for names in NAME_SETS:
        legal = legal_names(frozenset(names), patset)
        if len(legal) < 2:
            continue
        try:
            pr = Probe(Scope(names=legal, patterns=patset))
        except ValueError:
            continue
        for src in statements:
            checked += 1
            verdict, winners, parses = pr.resolve(src)
            if verdict == 'TIE -> ERROR':
                ties.append((patset, legal, src, winners))

print('=' * 78)
print('EXHAUSTIVE SEARCH FOR SURVIVING TIES')
print('=' * 78)
print(f'  pattern pairs   : {len(list(itertools.combinations(ALL_PATS, 2)))}')
print(f'  rejected (anchor not prefix-free): {rejected_patsets}')
print(f'  statements/scope: {len(statements)}')
print(f'  resolutions run : {checked}')
print(f'  ties found      : {len(ties)}')
print()

if ties:
    print('COUNTEREXAMPLES (first 6 distinct):')
    seen = set()
    shown = 0
    for patset, names, src, winners in ties:
        key = (tuple(sorted(pat_str(p) for p in patset)), src)
        if key in seen:
            continue
        seen.add(key)
        shown += 1
        print(f'\n  patterns : {", ".join(sorted(pat_str(p) for p in patset))}')
        print(f'  names    : {", ".join(sorted(" ".join(n) for n in names))}')
        print(f'  statement: {src}')
        for w in winners:
            print(f'      {w}')
        if shown >= 6:
            break
else:
    print('  none -- the rule set is complete over this space.')

# ---------------------------------------------------------------- arg extent
print()
print('=' * 78)
print('ARGUMENT EXTENT: does an unbracketed arg need to span an operator?')
print('=' * 78)
sc = Scope(names=frozenset({('a',), ('b',), ('c',)}),
           patterns=frozenset({('send', HOLE, 'to', HOLE)}))
for mode in ('atom', 'expr'):
    v, w, p = Probe(sc, arg_mode=mode).resolve('send a + b to c')
    print(f'  arg_mode={mode:5} "send a + b to c"  ->  {v}'
          f'{"   " + w[0] if w else ""}')
