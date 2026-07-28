#!/usr/bin/env python3
"""
Findings 5 and 7 are mine. Both came from reference implementations I wrote and
the programmer ported faithfully. Reproduced here against the audit's own
counterexamples, then fixed.

  5  Cascades.Cycles is a DFS back-edge collector, not an enumeration of every
     ring, so a non-feedback participant can sit in a ring nobody reports.
     That is a safety-rule bypass, not a missing diagnostic.

  7  Handling uses one graph-wide counter, so while `otherwise` protects one
     read, EVERY nested recompute has adoption disabled too.
"""

W = 74
def h(t): print('\n' + '=' * W + f'\n{t}\n' + '=' * W)
def ok(label, cond):
    print(f'  [{"PASS" if cond else "FAIL"}] {label}'); return cond

results = []

# =====================================================================
h('5. DFS misses a ring the safety rule depends on')

# the audit's construction
# the audit's construction, as EDGES, so the direction cannot be got wrong:
#   A -> B, A -> C, B -> A, C -> B    with only C lacking feedback
WHENS = {
    'A': dict(to={'B', 'C'}, feedback=True),
    'B': dict(to={'A'},      feedback=True),
    'C': dict(to={'B'},      feedback=False),
}

def edges(whens):
    return {a: set(w['to']) for a, w in whens.items()}

def dfs_cycles(whens):
    """What I gave them: back edges found during one DFS."""
    e, found, colour = edges(whens), [], {}
    def visit(n, path):
        colour[n] = 'grey'; path.append(n)
        for nxt in sorted(e[n]):
            if colour.get(nxt) == 'grey':
                found.append(path[path.index(nxt):] + [nxt])
            elif nxt not in colour:
                visit(nxt, path)
        path.pop(); colour[n] = 'black'
    for n in sorted(whens):
        if n not in colour: visit(n, [])
    return found

def sccs(whens):
    """Tarjan. Legality only needs the COMPONENTS, not the rings."""
    e, index, low, onstack, stack, out, counter = edges(whens), {}, {}, set(), [], [], [0]
    def strong(v):
        index[v] = low[v] = counter[0]; counter[0] += 1
        stack.append(v); onstack.add(v)
        for w in sorted(e[v]):
            if w not in index:
                strong(w); low[v] = min(low[v], low[w])
            elif w in onstack:
                low[v] = min(low[v], index[w])
        if low[v] == index[v]:
            comp = []
            while True:
                w = stack.pop(); onstack.discard(w); comp.append(w)
                if w == v: break
            out.append(comp)
    for v in sorted(whens):
        if v not in index: strong(v)
    return out

def illegal_by_dfs(whens):
    """A ring is a problem when any participant did not opt into feedback."""
    return [c for c in dfs_cycles(whens)
            if any(whens[n]['feedback'] is False for n in set(c))]

def illegal_by_scc(whens):
    e = edges(whens)
    bad = []
    for comp in sccs(whens):
        cyclic = len(comp) > 1 or any(n in e[n] for n in comp)
        if cyclic and any(whens[n]['feedback'] is False for n in comp):
            bad.append(sorted(comp))
    return bad

print(f'  when   precedes   feedback declared')
for n, w in WHENS.items():
    print(f'  {n:6} {sorted(w["to"])!s:10} {w["feedback"]}')

print(f'\n  rings the DFS finds:  {dfs_cycles(WHENS)}')
print(f'  DFS reports illegal:  {illegal_by_dfs(WHENS) or "NOTHING"}')
print(f'  SCC reports illegal:  {illegal_by_scc(WHENS) or "nothing"}')
results.append(ok('DFS lets «C» into a ring it never opted into',
                  illegal_by_dfs(WHENS) == []))
results.append(ok('SCC catches it', illegal_by_scc(WHENS) == [['A', 'B', 'C']]))
print('''
  The DFS finds A->B->A, filters it as allowed, marks B settled, and never
  revisits it — so A->C->B->A is never seen. C is in a feedback ring without
  declaring feedback, and nothing complains.

  SCC is also the RIGHT tool rather than merely a working one: legality is a
  property of the component, not of the individual rings, and enumerating
  elementary cycles is exponential in the worst case for an answer nobody
  needs.''')

# =====================================================================
h('7. one graph-wide suppression counter leaks into nested recomputes')

from reactive_core import Graph, RoninError, NOTHING, PurityViolation, otherwise


class Broken(Graph):
    """What I wrote: `suppressed` is one integer on the graph."""
    def __init__(self):
        super().__init__(); self.seen = []; self.suppressed = 0

    def handling(self, thunk):
        self.suppressed += 1
        try: return thunk()
        finally: self.suppressed -= 1

    def read(self, name):
        v = super().read(name)
        if isinstance(v, RoninError) and self.seen and not self.suppressed:
            self.seen[-1] = v
        return v

    def recompute(self, node):
        for d in node.deps: self.nodes[d].dependents.discard(node.name)
        node.deps.clear()
        node.evaluating = True; self.reading_stack.append(node); self.seen.append(None)
        try: value = node.body(self)
        except PurityViolation as v: value = RoninError(str(v))
        finally:
            adopted = self.seen.pop(); self.reading_stack.pop(); node.evaluating = False
        if adopted is not None: value = adopted
        node.front = value; node.dirty = False


class Fixed(Broken):
    """Suppression belongs to the FRAME, not the graph. A recompute opens a
    fresh frame, so a nested body never inherits its caller's handling."""
    def __init__(self):
        super().__init__(); self.suppression = []

    @property
    def suppressed(self): return bool(self.suppression and self.suppression[-1])

    @suppressed.setter
    def suppressed(self, _): pass          # base __init__ assigns 0; ignore

    def handling(self, thunk):
        if not self.suppression: self.suppression.append(0)
        self.suppression[-1] += 1
        try: return thunk()
        finally: self.suppression[-1] -= 1

    def recompute(self, node):
        self.suppression.append(0)         # a fresh frame, unsuppressed
        try: super().recompute(node)
        finally: self.suppression.pop()


def scenario(graph):
    graph.var('divisor', 0)
    graph.let('ratio', lambda e: RoninError('divide by zero'))
    # a nested let that reads the error, ignores it, and returns 42
    graph.let('sloppy', lambda e: (e.read('ratio'), 42)[1])
    # the outer body handles ITS read, which forces «sloppy» to recompute inside
    graph.let('outer', lambda e: otherwise(e.handling(lambda: e.read('sloppy')), 'fallback'))
    return graph.read('outer')

broken, fixed = scenario(Broken()), scenario(Fixed())
print(f'  graph-wide counter : outer = {broken!r}')
print(f'  frame-local        : outer = {fixed!r}')
results.append(ok('graph-wide counter lets the nested body discard the error',
                  broken == 42))
results.append(ok('frame-local restores adoption inside the nested body',
                  fixed == 'fallback'))
print('''
  «sloppy» recomputes while «outer» is handling. With one counter, sloppy's
  own adoption is disabled too, so it keeps its 42 and the handler sees a
  perfectly good value where an error passed through.

  Frame-local suppression means handling protects the expression it wraps and
  nothing deeper. That is what the rule always said; the reference just did
  not encode it.''')

print('\n' + '=' * W)
print(f'  {sum(results)}/{len(results)} checks pass')
print('=' * W)
