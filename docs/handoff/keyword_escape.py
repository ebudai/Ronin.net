#!/usr/bin/env python3
"""
keyword_escape.py -- the law behind three of the five rulings.

Claim: a KEYWORD is a word that participates in parsing but is not in the table
the name rules run over. So the self-ambiguity rule cannot see it, and a name
that captures it is accepted at declaration and then silently changes what the
keyword-using program means.

Tested against the real resolver, with «return» in both costumes:

  as a BUILTIN PATTERN   «return (_)» is in the pattern table, so the
                         self-ambiguity check has something to compare against
                         and refuses the capturing name at DECLARATION.

  as a KEYWORD           the word is handled by the lexer. The name table
                         contains no «return» anything, so the capturing name
                         is accepted -- and the capture is only discovered at
                         the use site, if at all.
"""

from dp_resolver import DPResolver, N, PA

W = 78
print('=' * 78)
print('The capture, with «return» as a PATTERN')
print('=' * 78)

PATS = PA('return _', 'sum of _')
NAMES = N('value', 'x')

for src in ['return value', 'return sum of x']:
    v, c, s = DPResolver(NAMES, PATS).resolve(src)
    print(f'  {src:22} -> {v}   cost={c}')

print()
print('  now a user declares the name «return value»:')
NAMES2 = N('value', 'x', 'return value')
for src in ['return value', 'return sum of x']:
    v, c, s = DPResolver(NAMES2, PATS).resolve(src)
    print(f'  {src:22} -> {v}   cost={c}')

# the self-ambiguity check: may «return value» be declared at all?
def self_ambiguous(name, names, pats):
    """Does this name's own token span have another reading?"""
    others = frozenset(n for n in names if n != name)
    src = ' '.join(name)
    v, c, s = DPResolver(others, pats).resolve(src)
    return (v == 'OK'), c, v

nm = tuple('return value'.split())
v, c, raw = self_ambiguous(nm, NAMES2, PATS)
print(f'''
  self-ambiguity check on «return value», pattern table = {{return (_), sum of (_)}}
      other reading exists : {v}   (resolver says: {raw})
      VERDICT              : {"REFUSED at declaration" if v else "ACCEPTED -- nothing refuses it"}''')

print()
print('=' * 78)
print('The same capture, with «return» as a KEYWORD')
print('=' * 78)
print('''  A keyword is not in the pattern table -- that is what makes it a keyword.
  So the check runs against an empty rival set:''')

PATS_KW = PA('sum of _')          # «return» handled by the lexer, not here
v2, c2, raw2 = self_ambiguous(nm, NAMES2, PATS_KW)
print(f'''
  self-ambiguity check on «return value», pattern table = {{sum of (_)}}
      other reading exists : {v2}   (resolver says: {raw2})
      VERDICT              : {"REFUSED at declaration" if v2 else "ACCEPTED -- nothing refuses it"}''')

print(f'''
  Same program, same hazard, opposite verdicts. The rule did not change; the
  TABLE did. And the failure is silent in exactly the way GENERICS-II §8b
  predicted for the type registry:

      "the registry generator has to cover the type namespace or the check
       silently does not run there."

  That is not a property of type registries. It is a property of ANY word that
  parses but is not in the table:

      >> a word that participates in parsing must live in the table the name
      >> rules run over. A keyword is a name the rules cannot see.

  Symbols are exempt, and for a reason rather than by fiat: no name and no
  operator may span a symbol, so a symbol cannot be captured in the first
  place. Words can. That is why «=>» costs nothing and «return» costs 0.058%.''')
