#!/usr/bin/env python3
"""
recursive_infer.py -- is "infer the recursive function's type from the base case"
enough?

Budai's ruling, against my "recursion needs a written return type". I predicted
three things about where the base-case rule would break; the first run refuted
all three, so this version tests what actually separates the policies.

A type is ('con', name, [args]) or ('var', name). A function's answer type is a
variable, so a return site "mentions" that variable exactly when its type depends
on the recursive call.

  §1  base-case-first, and whether the recursive sites are UNIFIED or CHECKED
  §2  the function that never answers
  §3  mutual recursion
"""

W = 78

def V(n): return ('var', n)
def C(n, *a): return ('con', n, list(a))

def walk(t, s):
    while t[0] == 'var' and t[1] in s:
        t = s[t[1]]
    return t

def show(t, s=None):
    s = s or {}
    t = walk(t, s)
    if t[0] == 'var':
        return '?' + t[1]
    return t[1] + ('' if not t[2] else ' of ' + ', '.join(show(a, s) for a in t[2]))

def occurs(v, t, s):
    t = walk(t, s)
    return t[1] == v if t[0] == 'var' else any(occurs(v, a, s) for a in t[2])

def unify(a, b, s):
    a, b = walk(a, s), walk(b, s)
    if a == b:
        return s
    if a[0] == 'var':
        return None if occurs(a[1], b, s) else {**s, a[1]: b}
    if b[0] == 'var':
        return unify(b, a, s)
    if a[1] != b[1] or len(a[2]) != len(b[2]):
        return None
    for x, y in zip(a[2], b[2]):
        s = unify(x, y, s)
        if s is None:
            return None
    return s

def ground(t, s):
    """Is the type fully determined -- no variable left anywhere?"""
    t = walk(t, s)
    return False if t[0] == 'var' else all(ground(a, s) for a in t[2])

def mentions(v, t):
    return t[1] == v if t[0] == 'var' else any(mentions(v, a) for a in t[2])


NUM, TXT = C('number'), C('text')

# ---------------------------------------------------------------------------
# §1  base-case-first: UNIFY the remaining sites, or CHECK them?
# ---------------------------------------------------------------------------
CASES = [
    ('factorial', [NUM, NUM],
     'base «return 1»; recursive «return n * factorial (n-1)»'),
    ('collect',   [C('list', V('e')), C('list', NUM)],
     'base «return empty list» -- the element type is NOT pinned by the base'),
    ('find first', [C('optional', V('e')), C('optional', NUM)],
     'base «return nothing» -- same shape, through optional'),
    ('disagreeing', [NUM, TXT],
     'two independent sites that do not agree -- must be refused'),
]

def base_unify(sites):
    indep = [t for t in sites if not mentions('R', t)]
    if not indep:
        return ('REFUSED', 'no base case')
    s = {'R': indep[0]}
    for t in sites:
        s2 = unify(t, V('R'), s)
        if s2 is None:
            return ('REFUSED', 'sites disagree')
        s = s2
    return ('OK', show(V('R'), s))

def base_check(sites):
    """The tempting cheap version: fix the answer from the base, then merely
    CHECK the rest against it without letting them contribute information."""
    indep = [t for t in sites if not mentions('R', t)]
    if not indep:
        return ('REFUSED', 'no base case')
    answer = indep[0]
    for t in sites:
        if unify(t, answer, {}) is None:
            return ('REFUSED', f'{show(t)} != {show(answer)}')
    return ('OK', show(answer))          # <- published WITHOUT the extra info

print('=' * W)
print('§1  base-case-first -- does it matter whether the recursive sites unify?')
print('=' * W)
print(f'  {"function":13} {"BASE + CHECK":<22} {"BASE + UNIFY":<22}  same?')
print('  ' + '-' * 70)
for name, sites, why in CASES:
    b, u = base_check(sites), base_unify(sites)
    f = lambda r: r[1] if r[0] == 'OK' else 'refused: ' + r[1]
    same = 'yes' if f(b) == f(u) else '** NO **'
    print(f'  {name:13} {f(b):<22} {f(u):<22}  {same}')
    print(f'  {"":13} {why}')
print('''
  «collect» and «find first» are the whole finding. The base case is «return
  empty list» / «return nothing», whose own type is UNDER-DETERMINED, and the
  thing that pins the element type is the RECURSIVE site. Checking against the
  base publishes the loose type; unifying recovers «list of number».

  So the rule works -- but only in the form where the recursive sites CONTRIBUTE
  information rather than being validated against the base. That distinction is
  invisible in the statement "infer it from the base case" and is the difference
  between the rule working and not.''')

# ---------------------------------------------------------------------------
# §2  the function that never answers
# ---------------------------------------------------------------------------
print()
print('=' * W)
print('§2  «function loop (x) { return loop (x) }»')
print('=' * W)
sites = [V('R')]
s = {}
for t in sites:
    s = unify(V('R'), t, s)
print(f'''  sites            [ ?R ]        every return depends on the call itself
  base + unify     {base_unify(sites)[0]} -- {base_unify(sites)[1]}
  naive unify-all  OK -- answer = {show(V('R'), s)}     <-- ACCEPTS IT

  A plain solve succeeds with the answer variable still unbound, and an unbound
  answer is not an answer. So the solver needs one closing check that a
  base-case rule gets for free:

      >> when the constraints are solved, the answer type must be GROUND. An
      >> unsolved answer variable means the function never answers.

  ground(answer) after solving : {ground(V('R'), s)}

  And the diagnostic that falls out -- "no return here is independent of the
  call itself" -- names the actual defect, which "please write a return type"
  never did. My rule would have made the user annotate a function that cannot
  work.''')

# ---------------------------------------------------------------------------
# §3  mutual recursion
# ---------------------------------------------------------------------------
print()
print('=' * W)
print('§3  mutual recursion -- f answers with g, g has the base case')
print('=' * W)
GROUP = {'F': [V('G')], 'G': [NUM, V('F')]}
print('''  function f (n)  { return g (n) }                sites: [ ?G ]
  function g (n)  { if n <= 0 { return 0 }
                    return f (n - 1) }            sites: [ number, ?F ]
''')

def solve_one(name, group):
    sites = group[name]
    indep = [t for t in sites if not any(mentions(k, t) for k in group)]
    return ('OK', show(indep[0])) if indep else ('REFUSED', 'no base case')

def solve_group(group):
    s = {}
    for _ in range(len(group) + 1):          # iterate to a fixpoint
        for name, sites in group.items():
            for t in sites:
                s2 = unify(V(name), t, s)
                if s2 is None:
                    return ('REFUSED', 'sites disagree')
                s = s2
    if not all(ground(V(n), s) for n in group):
        return ('REFUSED', 'some function never answers')
    return ('OK', {n: show(V(n), s) for n in group})

print(f'''  one at a time, f first : {solve_one('F', GROUP)}
  one at a time, g first : {solve_one('G', GROUP)}
  the group together     : {solve_group(GROUP)}

  Taken alone, f has no site independent of the group, so a per-function base
  case rule refuses a program that is perfectly well typed. Solving the
  recursive GROUP together -- which is what a compiler already computes to order
  anything else -- gets «number» for both.

  Not a reason to reject the ruling. A reason to say "the recursive group" where
  the ruling says "the function".''')

print()
print('=' * W)
print('Verdict')
print('=' * W)
print('''  Budai's ruling is right, and my "recursion needs a written return type" was
  over-refusing in the same direction as the previous five. Three amendments,
  all mechanical, none reintroducing the annotation:

    1. UNIFY the recursive sites, do not merely check them against the base --
       otherwise «return empty list» publishes «list of ?»          (§1)
    2. require the answer to be GROUND when solving finishes -- otherwise a
       function that never answers is accepted                      (§2)
    3. say "the recursive GROUP", not "the function"                (§3)

  The one residue that genuinely cannot be inferred is POLYMORPHIC recursion --
  a function calling itself at a different type. That is undecidable in general,
  it is rare, and it is the only place an annotation should be demanded.''')
