#!/usr/bin/env python3
"""
overload_alternatives.py -- are overloads resolver alternatives, or a later pass?

The programmer's question, made measurable. Three candidate policies:

  A  SEPARATE       the resolver enumerates readings; overload selection runs
                    afterwards, on the reading that won. So while readings are
                    being eliminated, an overloaded pattern's parameter type is
                    NOT A SINGLE TYPE and cannot be used to eliminate anything.

  B  SPLIT          every overload declaration is its own derivation.
                    Call.Alike compares the declaration rather than the shape.

  C  CANDIDATE SET  one derivation per SHAPE, carrying the set of declarations
                    that shape could bind. The type filter narrows the set.
                    Empty set -> the derivation dies, which feeds ordinary
                    reading elimination. Set of one -> resolved. Set of two or
                    more -> an overload ambiguity, which is a different
                    diagnostic from a bracketing ambiguity.

Two things are measured:
  §1  what each policy can ELIMINATE  -- does separating the passes cost the
      resolver information it needs for its own job?
  §2  what each policy COSTS in derivations.
"""

W = 78
line = lambda: print('-' * W)

# ---------------------------------------------------------------------------
# §1  Elimination power
# ---------------------------------------------------------------------------
# A pattern is a shape plus the set of parameter types its declarations accept.
PATTERNS = {
    'q (_)':       {'number'},                  # not overloaded
    'show (_)':    {'number', 'text'},          # overloaded
    'render (_)':  {'number', 'text', 'list'},  # overloaded, admits everything
}

# A case: one token run, several structural readings.
# Each reading is (label, shape, argument type).
CASES = [
    ('q total of items', [
        ('q «total of items»',   'q (_)', 'list'),
        ('q (total of «items»)', 'q (_)', 'number'),
    ], 'callee NOT overloaded -- the baseline'),

    ('show total of items', [
        ('show «total of items»',   'show (_)', 'list'),
        ('show (total of «items»)', 'show (_)', 'number'),
    ], 'callee overloaded, and only ONE reading is admissible'),

    ('render total of items', [
        ('render «total of items»',   'render (_)', 'list'),
        ('render (total of «items»)', 'render (_)', 'number'),
    ], 'callee overloaded, BOTH readings admissible -- genuinely ambiguous'),
]


def survivors(readings, policy):
    out = []
    for label, shape, argty in readings:
        admits = PATTERNS[shape]
        if policy == 'A' and len(admits) > 1:
            # overload selection has not run yet, so the parameter type is not
            # known to the resolver. Nothing can be eliminated on it.
            out.append(label)
        elif argty in admits:
            out.append(label)
    return out


print('=' * W)
print('§1  What each policy can eliminate')
print('=' * W)
print(f'  {"run":24} {"A separate":>12} {"C candidate set":>18}   agree?')
line()
disagreements = 0
for run, readings, why in CASES:
    a = survivors(readings, 'A')
    c = survivors(readings, 'C')
    verdict = lambda s: 'UNIQUE' if len(s) == 1 else f'{len(s)} AMBIGUOUS' if s else 'no reading'
    same = (len(a) == len(c))
    if not same:
        disagreements += 1
    print(f'  {run:24} {verdict(a):>12} {verdict(c):>18}   {"yes" if same else "NO"}')
    print(f'  {"":24} {why}')
    if not same:
        print(f'  {"":24} A reports an ambiguity that does not exist:')
        for s in a:
            mark = 'ill-typed' if s not in c else 'the real reading'
            print(f'  {"":26} {s:32} {mark}')
    print()

print(f'''  policies disagree on {disagreements} of {len(CASES)} runs.

  The disagreement is the finding, and it is not about overload sites. It is
  about ORDINARY reading elimination at every call to an overloaded pattern:

      under A, «show (_)» has no single parameter type while readings are being
      eliminated, so the resolver cannot use it to eliminate ANYTHING -- and
      «show total of items» becomes an ambiguity error whose two readings are
      not both well-typed.

  So separating the passes does not merely add a second mechanism. It puts a
  hole in the first one, proportional to how much of the standard library is
  overloaded -- which, for the library that motivated the question, is a lot.
''')

# ---------------------------------------------------------------------------
# §2  What each policy costs
# ---------------------------------------------------------------------------
# A run with k call sites and s structural readings. Under B, each site's
# overloads multiply the derivation count. Under C they do not.
import itertools

def enumerate_B(structural, sites):
    """sites: list of shapes appearing in each structural reading."""
    out = []
    for s_ix, shapes in enumerate(structural):
        choices = [sorted(PATTERNS[sh]) for sh in shapes]
        for combo in itertools.product(*choices):
            out.append((s_ix, combo))
    return out

def enumerate_C(structural, sites):
    return [(s_ix, tuple(frozenset(PATTERNS[sh]) for sh in shapes))
            for s_ix, shapes in enumerate(structural)]

print('=' * W)
print('§2  Derivations produced, before any typing')
print('=' * W)
print(f'  {"call sites in the run":36} {"B split":>10} {"C set":>10}   ratio')
line()
for k in range(1, 6):
    structural = [['show (_)'] * k, ['render (_)'] * k]   # 2 structural readings
    b = len(enumerate_B(structural, None))
    c = len(enumerate_C(structural, None))
    desc = f'{k} site{"s" if k > 1 else ""}, 2 structural readings'
    print(f'  {desc:36} {b:>10} {c:>10}   {b/c:>5.0f}x')

print(f'''
  B multiplies the derivation count by the product of the overload arities at
  every call site in the run. C does not, because overloads are not structural:
  two declarations of one shape span the same tokens and build the same tree.
  The only thing that differs is which declaration the node ends up bound to,
  and that is a field, not a derivation.

  Both reach the same answer. B pays an exponential to get there, and pays it
  on every run containing an overloaded call, ambiguous or not.
''')

print('=' * W)
print('Conclusion')
print('=' * W)
print('''  A is wrong -- it weakens reading elimination wherever the library is
    overloaded, and manufactures ambiguity errors on runs that have exactly one
    well-typed reading.
  B is right about WHEN and wrong about GRANULARITY -- correct answers, paid
    for with a derivation explosion, on the many runs where nothing was
    ambiguous in the first place.
  C is A's cost with B's answers: one type-filter pass, run over derivations
    that carry a candidate SET rather than a single declaration.

  And C leaves Call.Alike comparing the SHAPE, which is what it already does.
  The Tuesday work is not a wrong fork to be backed out; it is missing a field.''')
