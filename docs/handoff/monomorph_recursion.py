#!/usr/bin/env python3
"""
monomorph_recursion.py -- does monomorphisation really dissolve the polymorphic
recursion residue?

The programmer's claim: Ronin monomorphises (forced by the instance decision),
so within an instantiation every recursive call is at fixed types and the fixed
point is trivial. A body calling itself at a different instantiation is an
infinite instantiation chain, which the monomorphiser must detect anyway.

I think he is right. Testing it rather than agreeing, because the claim has a
seam: "calls itself at a different type" and "generates infinitely many
instantiations" are not the same set, and only the second one is caught by the
monomorphiser.

Modelled: a worklist monomorphiser. Each function has recursive call sites, each
described by a transform on its argument type. Instantiate until closure.
"""

W = 78
LIMIT = 16          # a monomorphiser MUST have one of these

def show(t):
    return t if isinstance(t, str) else f'list of {show(t[1])}'


def instantiate(entry, calls, limit=LIMIT):
    """calls: fn -> [(callee, transform)]. Returns (set of instantiations, why)."""
    seen, work, depth = set(), [entry], 0
    while work:
        depth += 1
        if depth > limit:
            return seen, f'STOPPED at the depth limit -- {len(seen)} and growing'
        nxt = []
        for fn, ty in work:
            if (fn, ty) in seen:
                continue
            seen.add((fn, ty))
            for callee, tf in calls[fn]:
                nxt.append((callee, tf(ty)))
        work = nxt
    return seen, 'closed'


SAME = lambda t: t
TO_NUMBER = lambda t: 'number'
WRAP = lambda t: ('list', t)

CASES = [
    ('monomorphic recursion',
     {'f': [('f', SAME)]}, ('f', 'number'),
     'factorial -- calls itself at its own type'),

    ('polymorphic, but finite',
     {'f': [('f', TO_NUMBER)]}, ('f', 'text'),
     'f (T) calls f (number) -- a DIFFERENT type, and it bottoms out'),

    ('polymorphic, nested datatype',
     {'f': [('f', WRAP)]}, ('f', 'number'),
     'f (T) calls f (list of T) -- the classic undecidable one'),

    ('mutual, nested',
     {'f': [('g', WRAP)], 'g': [('f', SAME)]}, ('f', 'number'),
     'the same thing spread over two functions'),
]

print('=' * W)
print('What the monomorphiser does with each')
print('=' * W)
for name, calls, entry, why in CASES:
    seen, verdict = instantiate(entry, calls)
    ex = ', '.join(f'{f}@{show(t)}' for f, t in sorted(seen, key=lambda x: len(str(x))))
    print(f'  {name:28} {len(seen):>3} instantiation(s)   {verdict}')
    print(f'  {"":28} {why}')
    print(f'  {"":28} {ex[:44]}{"..." if len(ex) > 44 else ""}')
    print()

print(f'''  Two of the four are the point.

  «polymorphic, but finite» calls itself at a DIFFERENT type and terminates in
  two instantiations. So "polymorphic recursion" is not the residue -- it is
  perfectly buildable, and the monomorphiser does not even notice. My
  RETURN-AND-LITERALS.md §4 named a set that is larger than the problem.

  «polymorphic, nested datatype» is the real one, and what happens to it is not
  a type error -- it is an instantiation chain that never closes. The
  monomorphiser stops it with the depth limit it has to have anyway, and the
  message is "this instantiates forever", which tells the author what they did.

  So the residue for the TYPE rule is empty, and the programmer is right.
  Within any one instantiation every call is at fixed types, so inference is
  ordinary monomorphic inference and base-case-first solves it. The only rule
  left is his: a function needs a written answer type when NO return site is
  independent of the recursive group.''')

print()
print('=' * W)
print('The two operational consequences')
print('=' * W)
print(f'''  1. THE DEPTH LIMIT IS LOAD-BEARING, and it is needed before the first
     generic recursive function is written rather than after. Without it the
     nested case is not an error, it is a HANG -- and in a language whose
     premise is that the IDE is always running and debug == development, a hang
     is a far worse failure than in a batch compiler. Rust hits exactly this and
     answers it the same way, with a recursion limit and a message naming it.

     limit used here: {LIMIT}. The real number matters less than its existing.

  2. INFERENCE NOW RUNS PER INSTANTIATION, not per function -- that is what
     "within an instantiation the types are fixed" buys, and it is also what it
     costs. For a heavily generic standard library, re-inferring on every
     keystroke is the thing that gets slow first. The mitigation is cheap and
     worth building in from the start: cache the inference result keyed by
     (function, instantiation), which is the same key the monomorphiser already
     maintains to avoid emitting duplicates.''')
