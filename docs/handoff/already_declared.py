#!/usr/bin/env python3
"""
already_declared.py -- Budai: "isn't the self-ambiguity rule just Ronin's
analogue of «symbol already declared»?"

Stronger than an analogue. Checked here:

  1. plain duplicate declaration is the ONE-WORD CASE of it, so the two are one
     rule and not two;
  2. but the EXACT form is order-dependent -- a name legal today can be made
     self-ambiguous by an unrelated declaration tomorrow;
  3. the PESSIMISTIC form (assume every hole can be filled) is order-independent
     and comes out exactly equal to «Shadowing(names)» + «Infixes(names)».

So the unification is real and it buys the spec sentence and the diagnostic, not
the code -- which is worth knowing before it is sold as a simplification of the
implementation.
"""

W = 78


def P(*specs):
    return [tuple(None if w == '_' else w for w in s.split()) for s in specs]


PATS = P('print _', 'send _ to _', 'send _', 'sum of _', 'count of _')
WORDOPS = {'is'}


def readings(toks, names, pessimistic=False):
    """Number of ways this exact span reads. Under `pessimistic`, ANY word run
    may be a name -- which is what makes the check order-independent."""
    n = len(toks)
    C = [[0] * (n + 1) for _ in range(n + 1)]

    def M(pat, si, i, j):
        if si == len(pat):
            return 1 if i == j else 0
        seg = pat[si]
        if seg is not None:
            return M(pat, si + 1, i + 1, j) if i < j and toks[i] == seg else 0
        tot, last = 0, si == len(pat) - 1
        for sp in ([j] if last else range(i + 1, j + 1)):
            if C[i][sp]:
                tot += C[i][sp] * M(pat, si + 1, sp, j)
        return tot

    for w in range(1, n + 1):
        for i in range(0, n - w + 1):
            j = i + w
            t = 0
            if pessimistic:
                t += 1                                  # any run could be a name
            elif tuple(toks[i:j]) in names:
                t += 1
            for p in PATS:
                t += M(p, 0, i, j)
            for k in range(i + 1, j - 1):
                if toks[k] in WORDOPS and C[i][k] and C[k+1][j]:
                    t += C[i][k] * C[k + 1][j]
            C[i][j] = t
    return C[0][n]


print('=' * W)
print('1. Duplicate declaration is the one-word case')
print('=' * W)
NAMES = {('price',), ('items',), ('a',), ('b',)}
print('''  A second «var price» declares a second symbol for a span that already
  reads. Counting readings of the span rather than comparing symbols:
''')
for nm, note in ((('price',), 'already declared -> span already reads'),
                 (('unseen',), 'fresh -> span does not read yet')):
    r = readings(list(nm), NAMES)
    print(f'      «{" ".join(nm):8}» readings of its own span = {r}   {note}')
print('''
  So "symbol already declared" and "this span already reads" are the same
  check at different arities. One rule, and the message picks its wording from
  WHAT the other reading is:

      another name     -> «price» is already declared at line 12
      a pattern call   -> «send price» already reads as «send «price»»''')

print('=' * W)
print('2. The exact form is order-dependent')
print('=' * W)
for extra, label in ((set(), 'without «squares»'), ({('squares',)}, 'with «squares»')):
    r = readings(['sum', 'of', 'squares'], NAMES | extra)
    print(f'  «sum of squares»  {label:20} readings of its own span = {r}')
print('''
  Legal today, self-ambiguous tomorrow -- and the convention refuses the
  declaration that arrives SECOND, which here means refusing «var squares», a
  far better name than «sum of squares», over a collision its author never saw.
  That is the same worst-shape diagnostic R7b's conditionality ran into.''')

print('=' * W)
print('3. The pessimistic form is order-independent, and equals the two rules')
print('=' * W)


def wordcontent(p):
    return tuple(s for s in p if s is not None)


def r6b(nm):
    return any(len(w) < len(nm) and nm[:len(w)] == w
               for w in (wordcontent(p) for p in PATS))


def infixes(nm):
    return any(0 < i < len(nm) - 1 and x in WORDOPS for i, x in enumerate(nm))


CANDS = [('price',), ('send', 'price'), ('sum', 'of', 'squares'),
         ('x', 'is', 'y'), ('a', 'to', 'b'), ('to', 'to'),
         ('a', 'number'), ('count', 'of', 'items'), ('order', 'total')]
print(f'  {"candidate name":22} {"pessimistic":>12} {"R6b or Infixes":>16}  agree')
print('  ' + '-' * 60)
agree = True
for nm in CANDS:
    pess = readings(list(nm), NAMES, pessimistic=True) > 1
    old = r6b(nm) or infixes(nm)
    agree &= pess == old
    print(f'  «{" ".join(nm):20}» {str(pess):>12} {str(old):>16}  '
          f'{"yes" if pess == old else "NO"}')
print(f'''
  [{"PASS" if agree else "FAIL"}] pessimistic self-ambiguity == Shadowing(names) + Infixes(names)

  So the unification is real and it is the right way to STATE the rule -- one
  sentence, self-explaining, and it subsumes duplicate declaration. But the
  implementation does not shrink: the exact version cannot be used because it is
  order-dependent, and the pessimistic version is precisely the two checks
  already written.

  The win is the spec sentence and the diagnostic, not the code. Worth saying
  plainly, because "one rule replaces two" reads like a deletion and is not.''')
