#!/usr/bin/env python3
"""
rules_or_brackets.py -- does Budai's design let the rules be DELETED?

His algorithm: enumerate, filter, and if more than one survives it is an
ambiguity error with bracketing offered. That is a USE-SITE rule. R5′, R6b and
R7b are DECLARATION-SITE rules that stop the same statements from ever being
ambiguous.

So they are two ways to pay for the same thing, and the exchange rate is
measurable: how many statements need a bracket if the rules go away?

Statements are generated FROM the grammar (pick a pattern, fill its holes) so
the corpus is valid programs rather than random words. Two name sets:

    names avoid glue and anchor words       -- what the rules enforce
    names may contain them                  -- what dropping the rules allows
"""

import random

W = 78


def P(*specs):
    return [tuple(None if w == '_' else w for w in s.split()) for s in specs]


def count(words, names, patterns):
    n = len(words)
    C = [[0] * (n + 1) for _ in range(n + 1)]

    def match(pat, si, i, j):
        if si == len(pat):
            return 1 if i == j else 0
        seg = pat[si]
        if seg is not None:
            return match(pat, si + 1, i + 1, j) if i < j and words[i] == seg else 0
        tot, last = 0, si == len(pat) - 1
        for sp in ([j] if last else range(i + 1, j + 1)):
            if C[i][sp]:
                tot += C[i][sp] * match(pat, si + 1, sp, j)
        return tot

    for w in range(1, n + 1):
        for i in range(0, n - w + 1):
            j = i + w
            t = 1 if tuple(words[i:j]) in names else 0
            for p in patterns:
                t += match(p, 0, i, j)
            C[i][j] = t
    return C[0][n]


PATS = P('print _', 'sum of _', 'send _ to _', 'item _ of _',
         'total for _', 'sort _ by _')
BASE = ['order', 'price', 'base', 'total', 'customer', 'name', 'list', 'row',
        'count', 'item', 'date', 'tax', 'line', 'amount', 'id', 'code',
        'city', 'rate', 'net', 'due']
GLUE = {'to', 'of', 'for', 'by'}
ANCHOR = {'print', 'sum', 'send', 'item', 'total', 'sort'}


def make_names(nphrase, allow_collide):
    s = {(w,) for w in BASE}
    tries = 0
    while len([x for x in s if len(x) > 1]) < nphrase and tries < 8000:
        tries += 1
        k = random.choice([2, 2, 3])
        vocab = BASE + (sorted(GLUE | ANCHOR) if allow_collide else [])
        p = tuple(random.choice(vocab) for _ in range(k))
        if not allow_collide and any(w in GLUE or w in ANCHOR for w in p):
            continue
        s.add(p)
    return s


def gen_stmt(names, depth=0):
    if depth > 2 or random.random() < 0.4:
        return list(random.choice(sorted(names)))
    out = []
    for seg in random.choice(PATS):
        out += [seg] if seg is not None else gen_stmt(names, depth + 1)
    return out


print('=' * W)
print('What the declaration-time rules buy, in brackets')
print('=' * W)
print("""  Statements generated FROM the grammar, so the corpus is valid programs.
  Twelve independent name sets per configuration, because the answer turned out
  to depend a lot on WHICH colliding names exist -- a single draw gave 3.7% and
  another gave 0.0%, so one draw is not a measurement.
""")
tot = {False: [0, 0], True: [0, 0]}
per_trial = []
for trial in range(12):
    for collide in (False, True):
        random.seed(1000 + trial * 7 + (1 if collide else 0))
        NAMES = make_names(30, collide)
        t = a = 0
        for _ in range(8000):
            s = gen_stmt(NAMES)
            if not (2 <= len(s) <= 12):
                continue
            if count(s, NAMES, PATS) > 1:
                a += 1
            t += 1
        tot[collide][0] += t
        tot[collide][1] += a
        if collide:
            per_trial.append(100.0 * a / t)

print(f'  {"name set":>10}  {"ambiguous %":>12}   (rules deleted)')
for i, p in enumerate(per_trial):
    print(f'  {i:>10}  {p:>11.2f}%')
print()
for collide, label in ((False, 'rules in force'), (True, 'rules deleted ')):
    t, a = tot[collide]
    print(f'  {label}   {a:>6} / {t:<7} = {100.0*a/t:.2f}% of statements ambiguous')

print("""
  Zero out of sixty-seven thousand with the rules in force -- not "few ties",
  none. About 3% without them, with a spread from 0% to 7.6% depending on which
  colliding names a program happens to declare.

  So the exchange rate is:

      keep R5' + R6b + R7b     names are restricted; no statement ever needs a
                               bracket it did not ask for
      delete them              every name is legal, and roughly one statement
                               in thirty needs a bracket

  and the second column is Budai's original design exactly: enumerate, filter,
  offer brackets on what is left.

  One thing travels with the deletion and is easy to miss: MINIMUM LOOKUP goes
  too. Its job is to pick a winner when several derivations exist -- the same
  job the rules do, at the other end of the pipeline. If ambiguity is an error
  there is nothing left to pick, and "fewest lookups" stops being part of the
  language.

  The number is an order of magnitude, not a forecast: the corpus is synthetic
  and there is no Ronin code to measure instead.""")
