#!/usr/bin/env python3
"""
Where standard PL tooling stops working for Ronin, demonstrated rather than
asserted.

Part 1: the declaration/block skeleton IS context-free. A stock parser
        generator eats it.
Part 2: the expression layer is NOT, and no EBNF can describe it. Proof by
        counterexample: the SAME token sequence must parse two different ways
        depending only on what is in scope. A context-free grammar has no
        access to what is in scope, so no CFG exists.
"""

from lark import Lark

W = 76
def h(t): print('\n' + '=' * W + f'\n{t}\n' + '=' * W)

SOURCE = '''
part of world simulation;

import standard math calculus;
import matrix math = standard math algebra;

type Dog (speed => Number, how much fun)
{
    var name => Text;
    hidden var running speed = speed;
    let is fun = how much fun >= 2;
    var owners => [Owner];

    function fetch (the ball)
    {
        location = the park;
        return the ball;
    }
}

type Transformer = Robot and Vehicle;

function save the (species)
{
    species is saved = true;
    return species;
}

var my car => Car = 9001;
let expensive calculation result = calculate things and stuff;
'''

h('PART 1 — the skeleton is context-free, and a stock generator handles it')
parser = Lark(open('ronin_skeleton.lark').read(), parser='earley', ambiguity='resolve')
tree = parser.parse(SOURCE)

counts = {}
for node in tree.iter_subtrees():
    counts[node.data] = counts.get(node.data, 0) + 1
for kind in ('module_decl', 'import_decl', 'type_decl', 'function_decl',
             'data_decl', 'block', 'parameters', 'identifier'):
    print(f'  {kind:16} {counts.get(kind, 0)}')
print('\n  A generated parser gets the whole declaration structure right,')
print('  including identifiers that interleave name words and parameter')
print('  blocks. This layer needs no custom code at all.')

h('PART 2 — why no EBNF can describe the expression layer')
print('''  Take one fixed token sequence:

      compute total for order

  Scope A          patterns: «compute total for (_)»      names: «order»
      => one call, one argument                            2 lookups

  Scope B          patterns: «compute (_)»                 names: «total for order»
      => one call, whose argument is a single 3-word name   2 lookups

  Same tokens. Different parse trees. The only thing that differs is the
  symbol table.

  A context-free grammar is a fixed set of productions over a fixed alphabet.
  It has no access to a symbol table by construction, so no CFG can assign
  both parses to that string based on scope. This is not an inconvenience to
  be engineered around -- it is a category difference.

  And it gets stronger: every user-declared pattern is effectively a NEW
  PRODUCTION. «function save the (species)» adds «save the (_)» to the
  grammar. A generator builds one parse table from one grammar, ahead of
  time. Ronin's grammar grows as the program declares things.''')

h('DEMONSTRATION')
from dp_resolver import DPResolver, N, PA

scope_a = DPResolver(N('order'), PA('compute total for _'))
scope_b = DPResolver(N('total for order'), PA('compute _'))
for label, resolver in (('Scope A', scope_a), ('Scope B', scope_b)):
    verdict, cost, reading = resolver.resolve('compute total for order')
    print(f'  {label}: {verdict:6} {cost} lookups   {reading}')

both = DPResolver(N('order', 'total for order'), PA('compute total for _', 'compute _'))
verdict, cost, reading = both.resolve('compute total for order')
print(f'\n  Both in scope at once: {verdict}')
print('  -- which is exactly the tie the minimum-lookup rule is there to catch.')

h('WHAT THIS MEANS FOR TOOLING')
print('''  layer                       standard tools?
  --------------------------  ------------------------------------------
  lexing                      YES. Fully regular. Already hand-written
                              and well tested -- no reason to change it.
  declaration / block         YES, as shown above. But you already have
    structure                 this working with 257 tests behind it.
  expression + name           NO. Symbol-table dependent, and productions
    resolution                are introduced by user declarations.
  type checking, semantics    NO generator exists for this in any
                              language. Everyone hand-writes it.
  codegen                     YES. LLVM, Cranelift, IL emitters -- all
                              standard, once you get there.

  So it is one layer, and it is the layer you now have a design for.''')
