#!/usr/bin/env python3
from reactive_events import EventGraph
from reactive_core import add, mul, gt, RoninError

W = 74
def h(t): print('\n' + '=' * W + f'\n{t}\n' + '=' * W)
def ok(label, cond):
    print(f'  [{"PASS" if cond else "FAIL"}] {label}'); return cond

results = []

h('1. `when x > 6` is EDGE triggered, not level triggered')
g = EventGraph()
g.var('x', 0)
g.var('alarms', 0)
g.when('x is high', lambda e: gt(e.read('x'), 6),
       lambda e: e.write('alarms', e.read('alarms') + 1))
g.prime()
for value in (7, 8, 9):
    g.write('x', value); g.step()
results.append(ok('fires once on the crossing, not per step above 6',
                  g.read('alarms') == 1))
g.write('x', 2); g.step()
g.write('x', 10); g.step()
results.append(ok('fires again after dropping and re-crossing', g.read('alarms') == 2))
print(f'      alarms = {g.read("alarms")}')

h('2. `when y changes` fires on every distinct value')
g = EventGraph()
g.var('y', 1)
g.var('log', 0)
g.when('y moved', lambda e: e.read('y'),
       lambda e: e.write('log', e.read('log') + 1), mode='changes')
g.prime()
for value in (2, 2, 3, 3, 4):
    g.write('y', value); g.step()
results.append(ok('three distinct values, three firings', g.read('log') == 3))

h('3. nothing fires just because the program started')
g = EventGraph()
g.var('hp', 0)
g.var('deaths', 0)
g.when('is dead', lambda e: e.read('hp') <= 0,
       lambda e: e.write('deaths', e.read('deaths') + 1))
g.prime()
results.append(ok('a condition already true at startup does not fire',
                  g.read('deaths') == 0))

h('4. `when` fires AFTER the graph settles, never mid-update')
g = EventGraph()
g.var('width', 1); g.var('height', 1)
g.let('area', lambda e: mul(e.read('width'), e.read('height')))
observed = []
g.when('area is big', lambda e: gt(e.read('area'), 50),
       lambda e: observed.append((e.read('width'), e.read('height'), e.read('area'))))
g.prime()
g.write('width', 10); g.write('height', 10)
g.step()
results.append(ok('sees a fully consistent graph', observed == [(10, 10, 100)]))
print(f'      observed: {observed}')

h('5. a fired body\'s writes land in the NEXT round, not this one')
g = EventGraph()
g.var('trigger', 0); g.var('a', 0); g.var('b', 0)
order = []
g.when('t1', lambda e: e.read('trigger'),
       lambda e: (order.append(('first sees b', e.read('b'))), e.write('a', 1)),
       mode='changes')
g.when('t2', lambda e: e.read('trigger'),
       lambda e: (order.append(('second sees a', e.read('a'))), e.write('b', 1)),
       mode='changes')
g.prime()
g.write('trigger', 1)
rounds = g.step()
results.append(ok('neither body saw the other\'s write this round',
                  order[0][1] == 0 and order[1][1] == 0))
results.append(ok('the writes land, in a later round',
                  g.read('a') == 1 and g.read('b') == 1))
print(f'      {order}, settled in {rounds} rounds')

h('6. self-retriggering is caught, with the culprit named')
g = EventGraph(cascade_limit=16)
g.var('temp', 0)
g.when('temp moved', lambda e: e.read('temp'),
       lambda e: e.write('temp', e.read('temp') + 1), mode='changes')
g.prime()
g.write('temp', 1)
try:
    g.step()
    results.append(ok('runaway cascade detected', False))
except RuntimeError as error:
    results.append(ok('runaway cascade detected', True))
    print(f'      {error}')

h('7. `now let` -- it works, and here is the bill')
g = EventGraph()
g.var('base', 10)
g.let('price', lambda e: mul(e.read('base'), 2))
first = g.read('price')

def rebind(graph, name, body):
    node = graph.nodes[name]
    for dep in node.deps:
        graph.nodes[dep].dependents.discard(name)
    node.deps.clear()
    node.body = body
    node.dirty = True
    graph.mark_dirty(node)

rebind(g, 'price', lambda e: add(e.read('base'), 100))
second = g.read('price')
results.append(ok('rebinding a let is mechanically trivial',
                  first == 20 and second == 110))
print(f'      price was {first}, is now {second} -- 12 lines, no new machinery')
print('''
  It costs nothing to IMPLEMENT, because dependencies are already dynamic --
  that price was paid when conditionals forced dynamic tracking. What it costs
  is elsewhere:

    - «let» stops being findable. Today, reading a value tells you where it
      came from: one definition, one place, searchable by name. With «now let»
      that question has only a runtime answer.
    - declaration order starts mattering again, and worse, EXECUTION order does.
    - record/replay breaks. Replaying sources works because bodies are fixed
      between edits; if bodies change at runtime, the body sequence becomes
      part of the state you must record.

  None of that shows up in a test. All of it shows up in a large program.''')

h('8. what «now let» is usually reaching for, at no cost')
g = EventGraph()
g.var('mode', 'light')
g.var('power', 10)
g.let('damage', lambda e: mul(e.read('power'), 5) if e.read('mode') == 'brutal'
                          else add(e.read('power'), 1))
results.append(ok('light mode', g.read('damage') == 11))
g.write('mode', 'brutal'); g.step()
results.append(ok('brutal mode, one definition, still findable', g.read('damage') == 50))
print('      Dependencies are dynamic, so only the taken branch is depended on.')

print('\n' + '=' * W)
print(f'  {sum(results)}/{len(results)} scenarios pass')
print('=' * W)
