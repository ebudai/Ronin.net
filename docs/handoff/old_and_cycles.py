#!/usr/bin/env python3
"""
old_and_cycles.py -- two settled decisions, with the evidence that settled them.

DECISION 1: `old x` is an INJECTED NAME, not an operator and not a pattern.
    Declaring a cell injects a second symbol into the same scope whose name is
    the cell's name prefixed with `old`, typed `optional T`. Nothing else in
    the language changes -- no new atom kind, no binding power, no special case
    in the resolver, no exemption in cycle detection.

DECISION 2: `when` cascades are STATICALLY detectable; only termination is not.
    So the runtime limit is the third tier of a defence, not the whole of it.

Run this. Both halves are demonstrations, not assertions.
"""

from dp_resolver import DPResolver, N, PA, HOLE

W = 74
def h(t): print('\n' + '=' * W + f'\n{t}\n' + '=' * W)

# =====================================================================
h('1.  `old x` is an injected name')

names = N('smoothed', 'old smoothed', 'reading', 'old reading')
r = DPResolver(names, PA())
print('  scope after declaring «let smoothed» and «let reading»:')
print('    smoothed, reading            -- declared')
print('    old smoothed, old reading    -- injected, typed optional\n')
for src in ['old smoothed * 0.9 + reading * 0.1',
            'smoothed - old smoothed',
            'old reading + reading']:
    verdict, cost, reading = r.resolve(src)
    print(f'  {src:36} {verdict:6} {cost}  {reading}')

print('''
  A name is an atom and an atom is an operand at every binding level, so this
  needs nothing new. Two alternatives were tried and rejected:''')

h('1a. REJECTED: `old (_)` as an ordinary word pattern')
r2 = DPResolver(N('smoothed', 'reading'), PA('old _'))
src = 'old smoothed * 0.9 + reading * 0.1'
r2.resolve(src)
from dp_resolver import lex
toks = lex(src); n = len(toks)
seen = {}
for m in range(31):
    cell = r2.E[0][n][m]
    if cell.cost != float('inf'):
        for show in cell.derivs:
            seen.setdefault(show, cell.cost)
print('  every reading of the moving average, at any cost and any level:')
for show, cost in sorted(seen.items()):
    print(f'    {cost}  {show}')
absent = not any(s.startswith('(old') for s in seen)
print(f'''
  «old» swallows the whole expression. The wanted reading «(old smoothed) *
  0.9 + ...» is not merely outranked -- it is absent from the forest entirely
  (absent: {absent}).

  R8 makes an open pattern call unavailable as an operand above pattern_bp,
  and cannot tell «old» from any other prefix pattern. A type constraint would
  not rescue it either, since a filter can only remove readings, never add
  one. So this spelling turns a correct program into one that will not parse.''')

h('1b. REJECTED: `old` as an atom-level prefix operator')
print('''  This parses correctly, but it is strictly more machinery than injection
  for strictly less: a new atom kind in the resolver, a binding-power
  decision, and an exemption in cycle detection so that reading «old x» does
  not create an edge on «x». Injection gets all three for free, because
  «old x» IS A DIFFERENT CELL -- the edge lands on the shadow, so

      let smoothed = old smoothed * 0.9 + reading * 0.1;

  is not a self-cycle by construction rather than by exemption.''')

h('1c. Rules injection needs')
print('''  RESERVED WORD. «old» joins the keyword list -- no pattern may use it as a
  segment. Demonstrated below: without the reservation, one hostile pattern
  makes R5 reject every injected name in scope.''')

def glue(patterns):
    out = set()
    for p in patterns:
        run = 0
        while run < len(p) and p[run] is not HOLE:
            run += 1
        out |= {s for s in p[run:] if s is not HOLE}
    return out

g = glue(PA('recall _ old _'))
bad = sorted(' '.join(x) for x in names if len(x) > 1 and any(w in g for w in x))
print(f'\n    «recall (_) old (_)» puts «old» in the glue set: {sorted(g)}')
print(f'    R5 then rejects: {bad}')

print('''
  COLLISION IS A DECLARATION ERROR. «let smoothed» injects «old smoothed»; a
  user-declared «var old smoothed» must be rejected, naming the injector.

  NO «old old x». Injection applies to declared cells only, never to injected
  ones. One generation. Two means declaring a let to capture it.

  SEED IS `nothing`, NOT AN ERROR. An error seed LATCHES: the cell errors, so
  next step its shadow is still an error, forever. `nothing` plus the existing
  `otherwise` forces the seed to be stated:

      let smoothed = (old smoothed otherwise reading) * 0.9 + reading * 0.1;

  and since «old x» is typed optional T, forgetting the seed is a compile
  error rather than a runtime surprise. No new checking required.

  INJECT ALWAYS, ALLOCATE LAZILY. Whether «old x» is read is unknown until
  after resolution, but the name must be in scope DURING resolution. So
  declaration injects the symbol unconditionally and a post-resolution pass
  allocates a shadow only where a reference was found. Zero cost when unused.

  SHADOW UPDATES AT THE STEP, before pending writes apply, so «old x» is the
  previous step's value consistently for the whole step.''')

h('1d. One surprise worth documenting')
print('''  A let that reads its own «old» advances only when something observes it:

      let tick = (old tick otherwise 0) + 1;

  Evaluation is demand-driven, so «tick» recomputes on read; its shadow copies
  «front», and «front» only moves when it recomputed. Unobserved, it stands
  still. That is correct for a cache or a smoothing filter and WRONG for a
  clock. A real clock is a var written by the frame loop, not a self-reading
  let. Worth saying in the guide before someone files it as a bug.''')

# =====================================================================
h('2.  when-cascades: the cycle is static, only termination is not')

WHENS = {
    'temp moved':    dict(reads={'temp'},          writes={'temp'}),
    'on damage':     dict(reads={'health'},        writes={'is alive', 'log'}),
    'on death':      dict(reads={'is alive'},      writes={'respawn timer'}),
    'ping':          dict(reads={'pong count'},    writes={'ping count'}),
    'pong':          dict(reads={'ping count'},    writes={'pong count'}),
    'on respawn':    dict(reads={'respawn timer'}, writes={'health'}),
    'layout settle': dict(reads={'box sizes'},     writes={'box sizes'}),
}

def cycles(whens):
    """W1 precedes W2 when W1 writes something W2 reads. A cycle in THAT graph
    is a plain graph property -- no analysis of any body required."""
    edges = {a: {b for b, wb in whens.items()
                 if a != b and whens[a]['writes'] & wb['reads']}
             for a in whens}
    for name, w in whens.items():
        if w['writes'] & w['reads']:
            edges[name].add(name)

    found, colour = [], {}
    def visit(node, path):
        colour[node] = 'grey'
        path.append(node)
        for nxt in sorted(edges[node]):
            if colour.get(nxt) == 'grey':
                found.append(path[path.index(nxt):] + [nxt])
            elif nxt not in colour:
                visit(nxt, path)
        path.pop()
        colour[node] = 'black'
    for node in sorted(whens):
        if node not in colour:
            visit(node, [])
    return found

print('  when          reads                 writes')
print('  ' + '-' * (W - 4))
for name, w in WHENS.items():
    print(f'  {name:14}{", ".join(sorted(w["reads"])):22}{", ".join(sorted(w["writes"]))}')

found = cycles(WHENS)
print('\n  statically detected cycles:')
for cycle in found:
    print(f'    {" -> ".join(cycle)}')

print(f'''
  All {len(found)} found before anything runs, including the ping-pong between two
  separate whens and the three-hop damage -> death -> respawn ring that nobody
  spots by reading.

  What is NOT decidable is whether a cycle CONVERGES. «layout settle» is a
  real self-cycle that terminates -- constraint relaxation writes the sizes it
  reads until they stop moving. Banning cycles would forbid layout solving,
  physics settling, and state machines that transition on their own state.

  THREE TIERS:
    1. STATIC    a when-cycle is reported at declaration, naming the whole
                 ring. Catches accidents at the mistake, not at 3am.
    2. DECLARED  a when needing feedback says so, and says what bounds it.
                 Deliberate, visible, greppable.
    3. RUNTIME   the cascade limit, as a backstop for a declared cycle that
                 fails to converge on particular data.

  Tier 1 cannot tell «layout settle» from «temp moved», and tier 2 is a
  promise a programmer can get wrong, so tier 3 stays.''')
