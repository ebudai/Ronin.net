#!/usr/bin/env python3
"""
error_as_value.py -- what does "Error is a runtime value the static type ignores"
actually cost?

Budai's position: he has always thought of it that way and cannot construct a
case where it matters; the one he found is «if x is error» on something that can
never error, which unions detect trivially.

Two questions, because the answer turns on both:

  §1  how much does «T | Error» DISCRIMINATE? A union is only information if
      some expressions have it and some do not.

  §2  the easy way makes Error a value, so «is» has to compare Errors, so CUTOFF
      has to compare them -- and the reactive graph re-fires on inequality.
      Three policies, run over a node that keeps failing.
"""

W = 78

# ---------------------------------------------------------------------------
# §1  Which expression forms can produce an Error?
# ---------------------------------------------------------------------------
FORMS = [
    ('literal            3, "a", true',          False, ''),
    ('name reference     x',                     True,  'whatever it was bound to'),
    ('a + b   a - b',                            True,  'overflow; an Error operand'),
    ('a / b',                                    True,  'division by zero'),
    ('a is b',                                   True,  'an Error operand'),
    ('xs [ i ]',                                 True,  'out of range'),
    ('m [ k ]',                                  True,  'missing key -- E §8'),
    ('f (x)',                                    True,  'the body may fail'),
    ('[ e1, e2 ]',                               True,  'an element may be an Error'),
    ('[ k = v ]',                                True,  'an element or a key may be'),
    ('old x',                                    True,  'the previous value may have been'),
    ('x otherwise y',                            True,  'y may itself fail'),
    ('- a',                                      True,  'an Error operand'),
    ('a and b   a or b',                         True,  'an Error operand'),
]

fallible = sum(1 for _, f, _ in FORMS if f)
print('=' * W)
print('§1  What would carry «| Error» in its type?')
print('=' * W)
print(f'  {"form":36} {"can be Error":>13}   why')
print('  ' + '-' * 74)
for form, f, why in FORMS:
    print(f'  {form:36} {("YES" if f else "no"):>13}   {why}')

print(f'''
  {fallible} of {len(FORMS)} expression forms can produce an Error. The exception is a
  LITERAL.

  So under a union, essentially every type written in a real program is
  «T | Error», and the annotation partitions nothing. A distinction that holds
  of almost everything is not a distinction -- unless it comes with an
  OBLIGATION, which is the fork that actually matters:

    obligation ON    «a / b» has type «number | Error» and «a / b + 1» does not
                     type-check until it is handled or propagated. That is
                     Rust's «?» or Java's checked exceptions. Every arithmetic
                     expression in the language grows a marker.

    obligation OFF   «number | Error» is usable wherever «number» is, so the
                     union is decorative -- it records a possibility that is
                     true of everything and forces nothing.

  Obligation OFF is the easy way with extra syntax. Obligation ON is a different
  language from the one being built, and not the RAD one.

  Which is why Budai could not construct a case: there is no MIDDLE. The union
  either changes every expression or none.''')

# ---------------------------------------------------------------------------
# §2  Error equality, through cutoff
# ---------------------------------------------------------------------------
print()
print('=' * W)
print('§2  The obligation the easy way DOES create: Error equality, via cutoff')
print('=' * W)
print('''  Making Error a value means «is» compares Errors, and the reactive graph uses
  that comparison for cutoff: a node whose value did not change does not
  propagate. So a persistently failing node is decided by Error equality.
''')


class Err:
    def __init__(self, reason):
        self.reason = reason

    def __repr__(self):
        return f'Error«{self.reason}»'


ALWAYS  = lambda a, b: True                       # all Errors are one value
NEVER   = lambda a, b: False                      # every Error is fresh
REASON  = lambda a, b: a.reason == b.reason       # equal when they say the same


def run(policy, values, rounds=6):
    """A node produces `values[round]`. Report firings and what downstream sees."""
    old, fires, seen = None, 0, []
    for r in range(rounds):
        new = values[min(r, len(values) - 1)]
        if old is None:
            same = False
        elif isinstance(old, Err) and isinstance(new, Err):
            same = policy(old, new)
        else:
            same = (type(old) is type(new)) and old == new
        if not same:
            fires += 1
            old = new
        seen.append(old)
    return fires, seen


SCENARIOS = [
    ('the same failure every round',
     [Err('divide by zero')] * 6),
    ('a failure that CHANGES reason',
     [Err('divide by zero'), Err('divide by zero'), Err('missing key «b»'),
      Err('missing key «b»'), Err('missing key «b»'), Err('missing key «b»')]),
    ('a failure that recovers',
     [Err('divide by zero'), Err('divide by zero'), 42, 42, 42, 42]),
]

print(f'  {"scenario":34} {"policy":8} {"fires":>6}   downstream ends holding')
print('  ' + '-' * 74)
for name, vals in SCENARIOS:
    for pname, policy in (('always', ALWAYS), ('never', NEVER), ('reason', REASON)):
        fires, seen = run(policy, vals)
        print(f'  {name:34} {pname:8} {fires:>6}   {seen[-1]}')
    print()

print('''  Reading the three:

    ALWAYS   a failure that changes reason does not re-fire, so downstream keeps
             reporting «divide by zero» while the actual fault is a missing key.
             A wrong message that never corrects itself is worse than no message.

    NEVER    a node that keeps failing the same way fires EVERY round, forever.
             In a graph with rounds that is a livelock: one broken cell keeps the
             whole graph awake and every downstream recomputes on every tick.

    REASON   the same failure is quiet, a changed failure propagates, recovery
             propagates. The only one of the three that is both stable and
             truthful.

      >> Two Errors are equal when their reasons are equal.

  That obligation exists ONLY because Error is a value. It is the real price of
  the easy way, it is one sentence, and it is invisible until a reactive graph
  has a cell that keeps failing -- which is to say, until a user has one.''')
