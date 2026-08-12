#!/usr/bin/env python3
"""
self_ambiguous.py -- a third correction, and it collapses two rules into one.

which_rules.py reported "R5′ interior -- ALL READINGS EXPRESSIBLE" on ZERO
ambiguous cases. Zero cases proves nothing; the generator never produced one,
because that arm's GLUE set was pattern glue and «Infixes(names)» is about
OPERATOR words, which it never tested. Another degenerate control.

Operator words behave differently from pattern glue, and the difference is the
whole story:

    name «a to b»    the span «a to b» has ONE reading (the name). Brackets can
                     select it:  «send (a to b)»            -> REPAIRABLE
    name «x is y»    the span «x is y» ALSO reads as a comparison, so bracketing
                     it changes nothing                     -> UNREPAIRABLE
    name «send price» the span also reads as a call         -> UNREPAIRABLE

So the test is not about glue, or anchors, or refining words. It is:

    A NAME IS SAFE IF ITS OWN SPAN HAS NO OTHER READING.

which is one rule where we had two, and it explains why the other two go.
"""

W = 78


def P(*specs):
    return [tuple(None if w == '_' else w for w in s.split()) for s in specs]


PATS = P('print _', 'send _ to _', 'send _', 'sum of _')
WORDOPS = {('is',)}


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
        for k in range(i + 1, j - 1):          # word infix operators
            if (toks[k],) in WORDOPS and toks[k] not in '()':
                d = sum(1 if t == '(' else -1 if t == ')' else 0
                        for t in toks[i:k])
                if d != 0:
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


def insertions(toks, k):
    if k == 0:
        yield toks
        return
    for i in range(len(toks)):
        for j in range(i + 1, len(toks) + 1):
            yield from insertions(toks[:i] + ['('] + toks[i:j] + [')'] + toks[j:],
                                  k - 1)


BASE = {('a',), ('b',), ('x',), ('y',), ('price',), ('total',)}

CASES = [
    ('Glue(names) -- interior pattern glue', ('a', 'to', 'b'), 'send a to b'),
    ('Glue(names) -- all glue', ('to', 'to'), 'send to to to to'),
    ('Infixes(names) -- operator word', ('x', 'is', 'y'), 'x is y'),
    ('Shadowing/R6b -- leading pattern words', ('send', 'price'), 'send send price'),
    ('R7b -- leading refining word (no rival here)', ('a', 'total'), 'print a total'),
]

print('=' * W)
print('Is the name the only reading of its own span?')
print('=' * W)
print(f'  {"rule":42} {"self-ambiguous":>15} {"repairable":>12}')
print('  ' + '-' * 72)
rows = []
for label, nm, src in CASES:
    names = BASE | {nm}
    self_amb = len(parses(list(nm), names)) > 1
    stoks = src.split()
    rs = parses(stoks, names)
    if len(rs) < 2:
        rows.append((label, self_amb, None))
        print(f'  {label:42} {str(self_amb):>15} {"(not ambiguous)":>12}')
        continue
    got = set()
    for k in (1, 2):
        for cand in insertions(stoks, k):
            if len(cand) > len(stoks) + 6:
                continue
            c = parses(cand, names)
            if len(c) == 1:
                got |= c
        if rs <= got:
            break
    rep = rs <= got
    rows.append((label, self_amb, rep))
    print(f'  {label:42} {str(self_amb):>15} {str(rep):>12}')

ok = all(r is None or (r is not sa) for _, sa, r in rows)
print(f'''
  [{"PASS" if ok else "FAIL"}] unrepairable exactly when the name's own span is ambiguous

  Which is the rule, and it is one rule rather than two:

      A name may be declared only if its own token span has no other reading.

  «Shadowing(names)» (R6b) and «Infixes(names)» are both special cases of it --
  a name beginning with a pattern's words, and a name spanning an infix
  operator, are the two ways a span reads as something else. «Glue(names)» and
  «Refining(names)» are not: those names ARE the only reading of themselves,
  and the ambiguity they cause is elsewhere in the statement, where a bracket
  can reach it.

  So the revised scope is one deletion and one replacement, not three
  deletions:

      delete   Glue(names), Refining(names)
      replace  Shadowing(names) + Infixes(names)  ->  one self-ambiguity check

  and the replacement is cheaper to state, cheaper to test, and does not need
  the glue registry, the anchor runs, or the refinement relation at all.''')
