#!/usr/bin/env python3
"""
which_rules.py -- two corrections to SIMPLER-RULES.md, both of which change the
recommendation.

CORRECTION 1. The "0.00% ambiguous with the rules in force" arm did not measure
the rules. It generated names that avoided glue and anchor words ENTIRELY, which
is far stricter than R5′ (interior glue only) or R6b (leading word-content only).
«to uppercase», «is valid», «by sum» are all legal under the rules and were
excluded from that arm. Redone here with the actual predicates.

CORRECTION 2. "Ambiguity is an error, bracket it" is NOT a complete answer.
Brackets GROUP; they do not CLASSIFY. A bracketed span that is itself ambiguous
stays ambiguous, so some readings cannot be selected at all:

    print print price      «print price» as a NAME cannot be expressed --
                           «print (print price)» is ambiguous inside the bracket

That case is exactly what R6b refuses. So R6b is not preventing capture; it is
preventing PROGRAMS NOBODY CAN WRITE. Checked per rule below.
"""

import random

W = 78


def P(*specs):
    return [tuple(None if w == '_' else w for w in s.split()) for s in specs]


PATS = P('print _', 'sum of _', 'send _ to _', 'send _', 'item _ of _',
         'sort _ by _')
GLUE = {'to', 'of', 'by'}
WORDRUNS = [('print',), ('sum', 'of'), ('send',), ('item',), ('sort',)]


# ------------------------------------------------------------- rule predicates
def r5_interior(nm):
    return any(0 < i < len(nm) - 1 and w in GLUE for i, w in enumerate(nm))


def r5_allglue(nm):
    return all(w in GLUE for w in nm)


def r6b(nm):
    return any(len(r) < len(nm) and nm[:len(r)] == r for r in WORDRUNS)


RULES = [('R5′ interior', r5_interior), ('R5′ all-glue', r5_allglue),
         ('R6b leading', r6b)]


# ------------------------------------------------------------------ resolution
def parses(toks, names):
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
            return [(seg + ' ' + r).strip()
                    for r in M(pat, si + 1, i + 1, j)] if i < j and toks[i] == seg else []
        out, last = [], si == len(pat) - 1
        for sp in ([j] if last else range(i + 1, j + 1)):
            for a in E(i, sp):
                for r in M(pat, si + 1, sp, j):
                    out.append((a + ' ' + r).strip())
        return out

    return set(E(0, len(toks)))


def insertions(toks, k):
    if k == 0:
        yield toks
        return
    for i in range(len(toks)):
        for j in range(i + 1, len(toks) + 1):
            yield from insertions(toks[:i] + ['('] + toks[i:j] + [')'] + toks[j:],
                                  k - 1)


def all_selectable(src, names):
    rs = parses(src, names)
    if len(rs) < 2:
        return None
    got = set()
    for k in (1, 2):
        for cand in insertions(src, k):
            if len(cand) > len(src) + 6:
                continue
            c = parses(cand, names)
            if len(c) == 1:
                got |= c
        if rs <= got:
            return True
    return rs <= got


print('=' * W)
print('1. Which rules protect a reading that brackets cannot express?')
print('=' * W)
print('''  For each rule, names refused ONLY by that rule are admitted, and every
  ambiguity they create is checked for whether all its readings can be selected
  by bracketing.
''')
BASE = ['price', 'order', 'count', 'line', 'total']
VOCAB = BASE + sorted(GLUE) + ['print', 'sum', 'send', 'item', 'sort']
random.seed(5)


def gen(names, depth=0):
    if depth > 2 or random.random() < 0.45:
        return list(random.choice(sorted(names)))
    out = []
    for seg in random.choice(PATS):
        out += [seg] if seg is not None else gen(names, depth + 1)
    return out


for label, pred in RULES:
    others = [p for l, p in RULES if l != label]
    bad = []
    tested = amb = 0
    for _ in range(600):
        # a name this rule refuses and the others do not
        nm = tuple(random.choice(VOCAB) for _ in range(random.choice([2, 2, 3])))
        if not pred(nm) or any(o(nm) for o in others):
            continue
        names = {(w,) for w in BASE} | {nm}
        src = gen(names)
        if not (3 <= len(src) <= 8):
            continue
        tested += 1
        ok = all_selectable(src, names)
        if ok is None:
            continue
        amb += 1
        if not ok:
            bad.append((src, nm))
    verdict = 'ALL READINGS EXPRESSIBLE' if not bad else 'SOME READINGS INEXPRESSIBLE'
    print(f'  {label:16} ambiguous cases {amb:>4}   unrepairable {len(bad):>4}   {verdict}')
    for src, nm in bad[:2]:
        print(f'      «{" ".join(src)}»  with the name «{" ".join(nm)}»')
        for r in sorted(parses(src, {(w,) for w in BASE} | {nm})):
            print(f'          {r}')

print('''
  Brackets group; they do not classify. Wherever a name begins with a pattern's
  own words, the bracketed span is ambiguous in the same way the unbracketed one
  was, and the name reading cannot be reached at all.

  That is R6b's real job, and it is not the one I gave it. R6b does not prevent
  a capture that could otherwise be reported -- it prevents a program that
  cannot be written.''')

print('=' * W)
print('2. The ambiguity rate, with the ACTUAL rule predicates this time')
print('=' * W)


def legal(nm, rules):
    return not any(p(nm) for l, p in RULES if l in rules)


CONFIGS = [('all four in force', {'R5′ interior', 'R5′ all-glue', 'R6b leading'}),
           ('R6b only', {'R6b leading'}),
           ('none', set())]
for label, keep in CONFIGS:
    tot = amb = 0
    for trial in range(8):
        random.seed(200 + trial)
        names = {(w,) for w in BASE}
        tries = 0
        while len([x for x in names if len(x) > 1]) < 14 and tries < 4000:
            tries += 1
            nm = tuple(random.choice(VOCAB) for _ in range(random.choice([2, 2, 3])))
            if legal(nm, keep):
                names.add(nm)
        for _ in range(3000):
            src = gen(names)
            if not (2 <= len(src) <= 10):
                continue
            tot += 1
            if len(parses(src, names)) > 1:
                amb += 1
    print(f'  rules kept: {label:20} {amb:>5} / {tot:<6} = {100.0*amb/tot:>5.2f}% ambiguous')
print('''
  So the earlier 0.00% was an artefact of over-filtering the name set, and the
  honest comparison is between these rows -- not between 0% and 3%.''')
