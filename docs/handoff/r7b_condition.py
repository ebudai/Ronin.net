#!/usr/bin/env python3
"""
r7b_condition.py -- what exactly is R7b's condition on the remainder?

He has it "conditional on the remainder being a declared name, which is what
makes the second reading exist". That is nearly right and it is narrower than
the truth: what makes the second reading exist is the remainder being a
PARSEABLE ARGUMENT, and a name is only one way to be one.

Tested here with a remainder that is a pattern call rather than a name.
"""

from dp_resolver import DPResolver, N, PA

W = 78
PATS = PA('sum of _', 'sum of all _', 'count of _')


def res(names, src):
    v, c, s = DPResolver(names, PATS).resolve(src)
    return v, c, s


print('=' * W)
print('1. "Remainder is a declared name" -- the case it covers')
print('=' * W)
BASE = N('things', 'items')
for extra, label in ((set(), 'without «all things»'),
                     ({('all', 'things')}, 'with «all things»')):
    v, c, s = res(BASE | extra, 'sum of all things')
    print(f'  sum of all things   {label:22} {v:14} {c}  {s}')
print('''
  Correct: «things» is a declared name, so both readings exist and the tie
  appears. And with «things» NOT declared:''')
for extra, label in ((set(), 'without «all things»'),
                     ({('all', 'things')}, 'with «all things»')):
    v, c, s = res(N('items') | extra, 'sum of all things')
    print(f'  sum of all things   {label:22} {v:14} {c}  {s}')
print('''
  the rival reading cannot be built, so the name is harmless. His condition is
  right about this case.''')

print('=' * W)
print('2. The case it misses: a remainder that is a PATTERN CALL')
print('=' * W)
print('''  «count of items» is not a declared name. It is a pattern call, and it
  is just as good an argument for «sum of (_)».

  The name «all count of items» has no interior glue -- «of» precedes the hole
  in both «sum of (_)» and «count of (_)», so it is anchor, not glue -- and R5′
  admits it.
''')
NAME = ('all', 'count', 'of', 'items')
for extra, label in ((set(), 'without the name'),
                     ({NAME}, 'with «all count of items»')):
    v, c, s = res(BASE | extra, 'sum of all count of items')
    print(f'  sum of all count of items   {label:26} {v:14} {c}  {s}')
print('''
  Not even a tie -- the name is CHEAPER, so it wins silently. A condition
  phrased as "the remainder is a declared name" does not fire here, because
  «count of items» is not one.

  So the condition wants restating:

      refused when the REMAINDER RESOLVES AS AN EXPRESSION in the namespace
      the refined hole expects

  which is a resolve of the remainder span, not a symbol-table lookup. For
  «sum of all (_)» the hole is value-position, so the value language; for
  «(_) is a (_)» it is type-position, so the type table. One sentence covers
  both halves.''')

print('=' * W)
print('3. The re-check problem, which is the real cost of going conditional')
print('=' * W)
print('''  Conditional legality depends on the table, so a LATER declaration can
  invalidate an EARLIER name:

      var all things = ...      legal today, «things» is not declared
      var things = ...          now «sum of all things» has two readings

  SCOPING.md's convention refuses the declaration that arrives second -- which
  here means refusing «var things», a far more natural name than «all things»,
  with a message about a variable the author may not own. That is the worst
  shape of diagnostic in the language and it is the same one GLUE-AS-WHOLE-
  NAMES.md §2 flagged.

  Three ways out, and they are the same three as «time to live»:

      blanket        refuse any name beginning with an R7b word. Order-
                     independent, one-line rule, costs «all things»
      conditional    as above, plus a re-check on every later declaration and
                     the bad diagnostic when it fires
      differential   no declaration refused; the ambiguous STATEMENT errors,
                     repaired by a bracket

  The third is right and it is not built. The first is sound and conservative.
  Narrowing a refusal is backward-compatible, so blanket now costs nothing
  later.''')

print('=' * W)
print('4. And the question may be moot today')
print('=' * W)
print('''  R7b's pattern half is generated from pairs where one pattern is another
  with words inserted at the start of a hole:

      sum of (_)   ->   sum of all (_)

  «sum of all (_)» is my example, not a stdlib pattern. Before deciding the
  conditionality, GENERATE the set from the real pattern table. If no such pair
  exists today the set is EMPTY, the generator is still correct, and the
  decision defers itself at no cost.

  That is the cheapest possible answer and it is worth checking first.''')
