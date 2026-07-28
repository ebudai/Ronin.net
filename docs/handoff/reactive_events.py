#!/usr/bin/env python3
"""
reactive_events.py -- `when` as a third node kind, and what `now let` costs.

Kept SEPARATE from reactive_core.py deliberately: that file has already been
ported, and changing it underneath a port in progress is how test suites start
lying. This subclasses instead.

THE SHAPE THAT MAKES `when` FIT

    var    source        holds state
    let    derived       PURE, pulled on demand, produces a value
    when   sink          EFFECTFUL, pushed after settle, produces no value

A `when` is a let that nobody reads and that is allowed to have effects. Since
nobody reads it, it cannot be pulled -- it has to be pushed. So a step becomes
three phases instead of one:

    1. PROPAGATE   make pending writes visible, mark dependents dirty
    2. SETTLE      recompute what the triggers need (ordinary pull)
    3. FIRE        run triggered bodies

Firing after settle is what keeps a `when` from observing a half-updated graph.
And effects from a fired body go into the NEXT step's pending set, never the
current one -- otherwise a body's write would be visible to a body that fires
after it in the same step, and the consistent-generation guarantee is gone.
"""

from reactive_core import Graph, Node, RoninError, PurityViolation, NOTHING

MISSING = object()


class EventGraph(Graph):
    def __init__(self, cascade_limit=64):
        super().__init__()
        self.whens = {}
        self.cascade_limit = cascade_limit
        self.fired = []            # trace, for the scenarios

    # --------------------------------------------------------- declaration
    def when(self, name, trigger, body, mode='becomes true'):
        """mode 'becomes true' -- edge triggered on false -> true
           mode 'changes'      -- fires whenever the value differs

        Edge triggering, not level triggering, is the important choice. Level
        triggering fires every step while the condition holds, which is almost
        never wanted and is very hard to notice you have."""
        node = Node(name, 'let', body=trigger, dirty=True)
        self.nodes[name] = node
        self.whens[name] = {'trigger': name, 'body': body,
                            'mode': mode, 'previous': MISSING}
        return node

    # ------------------------------------------------------------ stepping
    def step(self):
        """One turn. Returns the number of cascade rounds it took."""
        rounds = 0
        self.fired = []
        while self.pending and rounds < self.cascade_limit:
            rounds += 1

            # 1. propagate
            super().step()

            # 2. settle: evaluate every trigger. Ordinary pull, so a trigger
            #    reading derived values gets consistent ones.
            triggered = []
            for name, w in self.whens.items():
                value = self.read(name)
                if isinstance(value, RoninError):
                    w['previous'] = value
                    continue
                previous = w['previous']
                w['previous'] = value
                if previous is MISSING:
                    continue          # first observation establishes a baseline
                if w['mode'] == 'changes':
                    if value != previous:
                        triggered.append(name)
                else:
                    if value and not previous:     # false -> true edge only
                        triggered.append(name)

            # 3. fire. Writes land in self.pending, which is the NEXT round's
            #    input -- never this round's.
            for name in triggered:
                self.fired.append(name)
                self.whens[name]['body'](self)

        if self.pending and rounds >= self.cascade_limit:
            names = ', '.join(f'«{n}»' for n in self.fired[-3:])
            raise RuntimeError(
                f'cascade did not settle after {rounds} rounds; last fired: '
                f'{names}. A when body is feeding its own trigger.')
        return rounds

    def prime(self):
        """Establish baselines without firing anything. A `when` should not
        fire just because the program started."""
        for name, w in self.whens.items():
            w['previous'] = self.read(name)
