#!/usr/bin/env python3
"""The scenario list. Each one pins a decision the interpreter has to make.
Port these as the interpreter's first test suite."""

from reactive_core import (Graph, Scope, Declaration, RoninError, NOTHING,
                           add, mul, gt, otherwise, PurityViolation)

W = 74
def h(t): print('\n' + '=' * W + f'\n{t}\n' + '=' * W)
def ok(label, condition):
    print(f'  [{"PASS" if condition else "FAIL"}] {label}')
    return condition

results = []

# ------------------------------------------------------------------------
h('1. var is a source; let is derived')
g = Graph()
g.var('base price', 100)
g.var('tax rate', 0.2)
g.let('total', lambda e: add(e.read('base price'),
                             mul(e.read('base price'), e.read('tax rate'))))
results.append(ok('let computes on first read', g.read('total') == 120))
g.write('base price', 200)
results.append(ok('write is invisible before the step', g.read('total') == 120))
g.step()
results.append(ok('write is visible after the step', g.read('total') == 240))

# ------------------------------------------------------------------------
h('2. a let does not recompute when nothing it reads changed')
g.trace = []
g.read('total')
results.append(ok('cached read does no work', g.trace == []))
g.write('tax rate', 0.2)          # same value
g.step()
results.append(ok('writing an equal value wakes nobody', g.trace == []))

# ------------------------------------------------------------------------
h('3. dependencies are DYNAMIC, not read off the AST')
g = Graph()
g.var('use metric', True)
g.var('metres', 100)
g.var('feet', 328)
g.let('distance', lambda e: e.read('metres') if e.read('use metric')
                            else e.read('feet'))
g.read('distance')
results.append(ok('depends on metres, not feet',
                  g.nodes['distance'].deps == {'use metric', 'metres'}))
g.write('use metric', False)
g.step()
g.read('distance')
results.append(ok('after switching, depends on feet, not metres',
                  g.nodes['distance'].deps == {'use metric', 'feet'}))
g.trace = []
g.write('metres', 999)
g.step()
results.append(ok('the now-unread branch no longer wakes it', g.trace == []))

# ------------------------------------------------------------------------
h('4. glitch freedom: a diamond evaluates its shared parent once')
g = Graph()
g.var('x', 1)
g.let('a', lambda e: add(e.read('x'), 1))
g.let('b', lambda e: mul(e.read('a'), 2))
g.let('c', lambda e: add(e.read('a'), 10))
g.let('d', lambda e: add(e.read('b'), e.read('c')))
results.append(ok('diamond computes correctly', g.read('d') == (2 * 2) + (2 + 10)))
g.write('x', 5)
g.step()
g.trace = []
value = g.read('d')
results.append(ok('recomputes to a consistent value', value == (6 * 2) + (6 + 10)))
results.append(ok('shared parent «a» recomputed exactly once',
                  g.trace.count('a') == 1))
print(f'      recompute order: {g.trace}')

# ------------------------------------------------------------------------
h('5. cycles are an error, detected by re-entry')
g = Graph()
g.let('p', lambda e: add(e.read('q'), 1))
g.let('q', lambda e: add(e.read('p'), 1))
results.append(ok('cycle yields an error value', isinstance(g.read('p'), RoninError)))
print(f'      {g.read("p")}')

# ------------------------------------------------------------------------
h('6. errors propagate like values, and clear on their own')
g = Graph()
g.var('divisor', 0)
g.let('ratio', lambda e: RoninError('divide by zero') if e.read('divisor') == 0
                         else 100 / e.read('divisor'))
g.let('report', lambda e: add(e.read('ratio'), 1))
results.append(ok('error reaches the dependent', isinstance(g.read('report'), RoninError)))
g.write('divisor', 4)
g.step()
results.append(ok('fixing the source clears it', g.read('report') == 26))

# ------------------------------------------------------------------------
h('7. `otherwise` is the only thing that catches')
g = Graph()
g.var('parsed', NOTHING)
g.let('count', lambda e: otherwise(e.read('parsed'), 0))
results.append(ok('catches nothing', g.read('count') == 0))
g.write('parsed', RoninError('bad input'))
g.step()
results.append(ok('catches error too', g.read('count') == 0))
g.write('parsed', 7)
g.step()
results.append(ok('passes a good value through', g.read('count') == 7))

# ------------------------------------------------------------------------
h('8. writes batch: many writes, one propagation, one consistent view')
g = Graph()
g.var('width', 2)
g.var('height', 3)
seen = []
def area(e):
    w, hh = e.read('width'), e.read('height')
    seen.append((w, hh))
    return mul(w, hh)
g.let('area', area)
g.read('area')
seen.clear()
g.write('width', 10)
g.write('height', 20)
g.step()
results.append(ok('both writes land together', g.read('area') == 200))
results.append(ok('never observed half-updated', seen == [(10, 20)]))
print(f'      observations: {seen}')

# ------------------------------------------------------------------------
h('9. purity is ENFORCED, not assumed')
g = Graph()
g.var('counter', 0)
def impure(e):
    e.write('counter', 1)         # a let may not assign a var
    return 1
g.let('bad', impure)
results.append(ok('assigning a var from a let body is an error',
                  isinstance(g.read('bad'), RoninError)))
print(f'      {g.read("bad")}')

# ------------------------------------------------------------------------
h('10. what a Call invokes')
scope = Scope()
scope.declare(Declaration(
    pattern=('compute', 'total', 'for', None),
    blocks=(('order',),),
    body=lambda e, order: mul(order, 2)))
scope.declare(Declaration(
    pattern=('draw', None, 'at', None),
    blocks=(('shape',), ('x', 'y')),          # second block binds TWO params
    body=lambda e, shape, x, y: f'{shape}@{x},{y}'))
scope.declare(Declaration(
    pattern=('save', None),
    blocks=(('data',),),
    body=lambda e, data: f'wrote {data}',
    pure=False))

g = Graph()
results.append(ok('single-parameter block binds directly',
                  scope.invoke(g, ('compute', 'total', 'for', None), [21], False) == 42))
results.append(ok('multi-parameter block destructures',
                  scope.invoke(g, ('draw', None, 'at', None),
                               ['circle', (3, 4)], False) == 'circle@3,4'))
results.append(ok('arity mismatch in a block is an error',
                  isinstance(scope.invoke(g, ('draw', None, 'at', None),
                                          ['circle', (3,)], False), RoninError)))
results.append(ok('an effectful call is rejected inside a let',
                  isinstance(scope.invoke(g, ('save', None), ['x'], True), RoninError)))
results.append(ok('the same call is fine outside a let',
                  scope.invoke(g, ('save', None), ['x'], False) == 'wrote x'))
results.append(ok('errors short-circuit before the body runs',
                  isinstance(scope.invoke(g, ('compute', 'total', 'for', None),
                                          [RoninError('upstream')], False), RoninError)))

# ------------------------------------------------------------------------
print('\n' + '=' * W)
print(f'  {sum(results)}/{len(results)} scenarios pass')
print('=' * W)
