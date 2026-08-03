#!/usr/bin/env python3
"""
instance_removal.py -- adjudicating the «removal leaves derived reads cached»
finding, by simulating the four policies rather than arguing them.

The finding is real. What is at stake is which of two fixes it gets, and they
are not equivalent:

    FIX A   removal additionally advances/dirties the grouped member nodes
    FIX B   removal stops bypassing the write path -- it IS a write, buffered
            to the round end like every other write, and dirtying falls out

Fix A leaves «Remove» a special case that has to remember to dirty. Fix B makes
forgetting impossible. The probe below shows that the difference is not
stylistic: under Fix A, whether removal takes effect immediately or at the round
end is still open, and taking effect immediately breaks the property the round
model exists for.

Model: struct-of-arrays. One node per member column plus one population node.
A "let" records the nodes it read. A round runs every «when» body against a
frozen view, buffers writes, applies them at the end, then settles the lets.
"""

W = 78
ERROR = '<Error: stale handle>'
NOTHING = 'nothing'


class Graph:
    def __init__(self, members, stale='error'):
        self.members = list(members)
        self.col = {m: {} for m in members}     # member -> row -> value
        self.alive, self.next_row = set(), 0
        self.clock = {('mem', m): 0 for m in members}
        self.clock['pop'] = 0
        self.lets = {}                          # name -> (fn, deps, value, at)
        self.stale = stale
        self.pending_clock = []

    # ---------------------------------------------------------- instances
    def create(self, **vals):
        """The HANDLE is immediate -- «var b = new box; b.cash = 5» must work
        in one step. The POPULATION advance is deferred to the round end like
        every other write."""
        r, self.next_row = self.next_row, self.next_row + 1
        self.alive.add(r)
        for m in self.members:
            self.col[m][r] = vals.get(m, 0)
        self.pending_clock.append('pop')
        return r

    def read_member(self, m, row, deps=None):
        if deps is not None:
            deps.add(('mem', m))
        if row not in self.alive:
            return ERROR if self.stale == 'error' else NOTHING
        return self.col[m][row]

    # -------------------------------------------------------------- lets
    def define_let(self, name, fn):
        self.lets[name] = [fn, set(), None, {}]
        self.recompute(name)

    def recompute(self, name):
        rec = self.lets[name]
        deps = set()
        val = rec[0](deps)
        rec[1] = deps
        rec[2] = val
        rec[3] = {d: self.clock[d] for d in deps}
        return val

    def value(self, name):
        return self.lets[name][2]

    def settle(self):
        """Recompute any let whose dependencies advanced."""
        for _ in range(10):
            moved = False
            for name, rec in self.lets.items():
                if any(self.clock[d] != rec[3].get(d) for d in rec[1]):
                    self.recompute(name)
                    moved = True
            if not moved:
                return

    # ------------------------------------------------------------ removal
    def remove_now(self, row, dirty):
        self.alive.discard(row)
        for m in self.members:
            self.col[m].pop(row, None)
        if dirty:
            self.clock['pop'] += 1
            for m in self.members:
                self.clock[('mem', m)] += 1


def run_round(g, bodies, buffered, dirty):
    """Each body gets (graph, emit). Under `buffered`, structural changes are
    queued and applied after every body has run."""
    pending, seen = [], []
    for label, body in bodies:
        seen.append((label, body(g, lambda r: (pending.append(r) if buffered
                                               else g.remove_now(r, dirty)))))
    for r in pending:
        g.remove_now(r, dirty)
    for node in g.pending_clock:
        g.clock[node] += 1
    g.pending_clock.clear()
    g.settle()
    return seen


# --------------------------------------------------------------------------
print('=' * W)
print('1. If removal takes effect immediately, a step is order-dependent')
print('=' * W)
print('''  Two «when» bodies in the SAME step: one removes the box, one reads it.
  The round model exists so that no body's write is visible to another body
  in the same step. Removal bypassing the buffer breaks exactly that.
''')
for order in (['remove', 'read'], ['read', 'remove']):
    g = Graph(['cash'])
    box = g.create(cash=0)
    bodies = []
    for kind in order:
        if kind == 'remove':
            bodies.append(('remove box', lambda gr, emit: (emit(box), 'removed')[1]))
        else:
            bodies.append(('read box.cash', lambda gr, emit: gr.read_member('cash', box)))
    out = run_round(g, bodies, buffered=False, dirty=True)
    rendered = ', '.join(f'{l}: {v}' for l, v in out)
    print(f'    arm order {str(order):26} -> {rendered}')
print('''
  Same program, two answers, decided by arm order. That is the defect the
  buffered-write rule was introduced to remove, so removal must be buffered
  regardless of what is decided about dirtying.
''')

g = Graph(['cash'])
box = g.create(cash=0)
print('  buffered, both orders:')
for order in (['remove', 'read'], ['read', 'remove']):
    g = Graph(['cash'])
    box = g.create(cash=0)
    bodies = []
    for kind in order:
        if kind == 'remove':
            bodies.append(('remove box', lambda gr, emit: (emit(box), 'removed')[1]))
        else:
            bodies.append(('read box.cash', lambda gr, emit: gr.read_member('cash', box)))
    out = run_round(g, bodies, buffered=True, dirty=True)
    print(f'    arm order {str(order):26} -> '
          + ', '.join(f'{l}: {v}' for l, v in out))
print()

# --------------------------------------------------------------------------
print('=' * W)
print('2. The finding itself: a derived cell across the round boundary')
print('=' * W)
for dirty in (False, True):
    g = Graph(['cash'])
    box = g.create(cash=0)
    g.define_let('observed', lambda deps: g.read_member('cash', box, deps))
    before = g.value('observed')
    run_round(g, [('remove box',
                   lambda gr, emit: (emit(box), 'removed')[1])],
              buffered=True, dirty=dirty)
    after_direct = g.read_member('cash', box)
    print(f'  removal advances member nodes: {str(dirty):5}')
    print(f'      let observed        before {before!r}   '
          f'after round {g.value("observed")!r}')
    print(f'      direct read                       '
          f'after round {after_direct!r}')
    print()
print('''  Without the advance the two paths disagree permanently, which is the
  finding, and the derived one is the confident wrong answer. With it they
  agree at the round boundary -- which is also the earliest moment they are
  ALLOWED to agree, because §1 forbids agreeing sooner.''')
print()

# --------------------------------------------------------------------------
print('=' * W)
print('3. What a stale read should yield')
print('=' * W)
for policy in ('freeze', 'nothing', 'error'):
    g = Graph(['cash'], stale='error' if policy == 'error' else 'nothing')
    box = g.create(cash=0)
    g.define_let('observed', lambda deps: g.read_member('cash', box, deps))
    run_round(g, [('remove box', lambda gr, emit: (emit(box), 'x')[1])],
              buffered=True, dirty=(policy != 'freeze'))
    v = g.value('observed')
    handled = ('0 (unchanged — «otherwise» never fires)' if policy == 'freeze'
               else 'caught by «otherwise»')
    print(f'  {policy:8} -> observed = {v!r:26} {handled}')
print('''
  «freeze» is what the code does today. It is the only one of the three in
  which the program cannot tell. «nothing» and «Error» are both catchable by
  the SAME existing operator, so the ergonomic difference between them is
  zero and the choice is purely about which signal is true:

      lookup miss     you probed for something that might not be there
      stale handle    you kept a reference to something you deleted

  The second is a bug, the first is a question. Error is the honest one, and
  «box.cash otherwise 0» already handles it with no new machinery.''')
print()

# --------------------------------------------------------------------------
print('=' * W)
print('4. Creation, which they are right to ask about now')
print('=' * W)
g = Graph(['cash'])
a = g.create(cash=1)
g.define_let('population', lambda deps: (deps.add('pop'), len(g.alive))[1])
print(f'  before: population = {g.value("population")}')


def make(gr, emit):
    r = gr.create(cash=7)
    gr.col['cash'][r] = 5                      # populate in the same step
    return (f'handle {r} usable immediately (cash={gr.col["cash"][r]}), '
            f'but population still reads {gr.value("population")}')


out = run_round(g, [('create box', make)], buffered=True, dirty=True)
print(f'  in-step: {out[0][1]}')
print(f'  after round: population = {g.value("population")}')
print('''
  Two observables with different timings, and they should be written down
  separately or the same omission recurs:

      the HANDLE          available immediately to its creator, because it is
                          a local value and «var b = new box; b.cash = 5»
                          must work in one step
      the POPULATION      enumeration, «count of», «for each» — advances at
                          the round boundary like every other write

  So creation advances the population node, and removal advances the
  population node AND every member column. Both are structural changes to the
  instance set; neither is a special case if removal goes through the write
  path.''')
