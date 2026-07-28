#!/usr/bin/env python3
"""
Settling the two error questions.

Q: does the graph enforce propagation, or is it lift's job?
A: BOTH, and they do different jobs. Neither alone is sufficient.

Q: should a body that throws become an Error?
A: user-program failures should never throw in the first place. Interpreter
   faults should be caught for survivability but never masquerade as program
   errors.

And the doc does overstate. "Becomes an error without running its body" is not
achievable with opaque bodies -- you cannot abort a callable without
exceptions. The achievable guarantee is ADOPTION: whatever the body returns is
discarded if an error was read during its evaluation. Purity is what makes the
weaker guarantee equivalent to the stronger one, since running a pure body and
throwing the result away has no observable effect.
"""

from reactive_core import Graph, RoninError, NOTHING, PurityViolation

W = 74
def h(t): print('\n' + '=' * W + f'\n{t}\n' + '=' * W)
def ok(label, cond):
    print(f'  [{"PASS" if cond else "FAIL"}] {label}'); return cond

results = []


class Fault(RoninError):
    """An interpreter defect, not a program error. Recoverable so the live
    session survives, but never propagated as an ordinary value and never
    catchable by `otherwise` -- it is a bug, not a result."""
    def __repr__(self):
        return f'FAULT({self.message})'


class GuardedGraph(Graph):
    """Adds the graph-level guarantee that lift alone cannot give."""

    def __init__(self):
        super().__init__()
        self.seen_error = []          # per evaluation frame

    def read(self, name):
        value = super().read(name)
        if isinstance(value, RoninError) and self.seen_error:
            self.seen_error[-1] = value       # remember, for adoption
        return value

    def recompute(self, node):
        for dep in node.deps:
            self.nodes[dep].dependents.discard(node.name)
        node.deps.clear()

        node.evaluating = True
        self.reading_stack.append(node)
        self.seen_error.append(None)
        try:
            value = node.body(self)
        except PurityViolation as violation:
            value = RoninError(str(violation))
        except Exception as defect:                    # noqa: BLE001
            # an interpreter fault: caught so the session survives, tagged so
            # it can never be mistaken for a program error
            value = Fault(f'{type(defect).__name__}: {defect}')
        finally:
            adopted = self.seen_error.pop()
            self.reading_stack.pop()
            node.evaluating = False

        # ADOPTION: an error read during evaluation wins over whatever the
        # body chose to return
        if adopted is not None and not isinstance(value, Fault):
            value = adopted

        node.front = value
        node.dirty = False
        self.trace.append(node.name)


h('1. lift alone is not enough: a body can IGNORE an error')
g = Graph()
g.var('divisor', 0)
g.let('ratio', lambda e: RoninError('divide by zero') if e.read('divisor') == 0
                         else 100 / e.read('divisor'))
# a body that reads an error and returns something else entirely
g.let('sloppy', lambda e: (e.read('ratio'), 42)[1])
results.append(ok('unguarded graph lets the error be discarded',
                  g.read('sloppy') == 42))
print('      «sloppy» read an error and returned 42. lift never saw an operator.')

h('2. adoption fixes it, without needing to stop the body')
g = GuardedGraph()
g.var('divisor', 0)
g.let('ratio', lambda e: RoninError('divide by zero') if e.read('divisor') == 0
                         else 100 / e.read('divisor'))
g.let('sloppy', lambda e: (e.read('ratio'), 42)[1])
results.append(ok('guarded graph adopts the error regardless',
                  isinstance(g.read('sloppy'), RoninError)))
print(f'      {g.read("sloppy")}')
g.write('divisor', 4); g.step()
results.append(ok('and clears when the source is fixed', g.read('sloppy') == 42))

h('3. adoption is not enough either: lift stops the body EXPLODING')
g = GuardedGraph()
g.var('divisor', 0)
g.let('ratio', lambda e: RoninError('divide by zero') if e.read('divisor') == 0
                         else 100 / e.read('divisor'))
# a body doing raw arithmetic on what it read, with no lift
g.let('raw', lambda e: e.read('ratio') + 1)
value = g.read('raw')
results.append(ok('without lift the body raises; caught, but as a FAULT',
                  isinstance(value, Fault)))
print(f'      {value}')
print('''      That is the wrong diagnosis -- a program error reported as an
      interpreter bug. lift is what keeps errors inert INSIDE a body so the
      arithmetic never raises at all.''')

h('4. program errors and interpreter faults must not be one kind')
g = GuardedGraph()
g.var('x', 1)
g.let('buggy', lambda e: e.read('x').nonexistent_method())
value = g.read('buggy')
results.append(ok('an interpreter defect is caught (session survives)',
                  isinstance(value, Fault)))
results.append(ok('but is NOT an ordinary program error',
                  type(value) is Fault))
print(f'      {value}')
print('''
      Catching everything and calling it Error would make the interpreter
      undebuggable: every null-reference bug in the evaluator would surface as
      a user-facing spreadsheet error. Two kinds, one of which is a bug report.

      And `otherwise` must NOT catch a Fault -- a fallback for a program error
      is a fallback; a fallback for an interpreter bug is a hidden crash.''')

h('5. what the doc should say')
print('''  WRONG (INTERPRETER-DECISIONS.md today):
      "a node whose dependency is an error becomes an error WITHOUT RUNNING
       ITS BODY"

  RIGHT:
      "a node that reads an error ADOPTS it: whatever its body returns is
       discarded. The body may still execute -- an opaque callable cannot be
       aborted without exceptions -- but because let bodies are pure, running
       one and discarding the result has no observable effect."

  The correction matters because it names PURITY as the thing that makes the
  achievable guarantee equal to the promised one. Without purity, "the body
  may still execute" would be a hole.''')

print('\n' + '=' * W)
print(f'  {sum(results)}/{len(results)} scenarios pass')
print('=' * W)
