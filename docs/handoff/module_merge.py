#!/usr/bin/env python3
"""
module_merge.py -- §1 of FAILUREMODES.md, measured.

Two claims to check, and they point in opposite directions.

CLAIM A (his):  compiled-scope resolution confines the conflict to "the
                importer's own NEW statements".
                -> tested by resolving importer statements written BEFORE the
                   import, with and without it. If any change reading, the
                   conflict is not confined to new statements.

CLAIM B (mine): R5/R6b are BLANKET declaration-time rules. They deliberately
                over-refuse, which is affordable inside one module (rename the
                variable) and unaffordable across a module boundary (you may
                rename nothing). So the question is how much of what they
                refuse is actually dangerous.
                -> tested by taking every declaration R5/R6b refuses and
                   asking whether ANY statement over the universe actually
                   changes reading.

The instrument is the same differential sweep as name_capture.py; only the
framing is new.
"""

import itertools
from dp_resolver import DPResolver, N, PA, HOLE

W = 78


def res(names, pats, src):
    v, c, s = DPResolver(names, pats).resolve(src)
    return v, s


def glue_of(pats):
    g = set()
    for p in pats:
        seen = False
        for s in p:
            if s is HOLE:
                seen = True
            elif seen:
                g.add(s)
    return g


def wordcontent(p):
    return tuple(s for s in p if s is not HOLE)


def anchoronly(pats):
    return [wordcontent(p) for p in pats
            if p[-1] is HOLE and all(s is not HOLE for s in p[:-1])]


print('=' * W)
print('1. Does compiled-scope resolution confine the conflict to NEW statements?')
print('=' * W)
print('''  Module A exports  send (_) to (_)  and  send (_)
  Module B exports  the name  «hello to alice»
  The importer already had names «hello» and «alice» and this line:

      send hello to alice
''')
PATS = PA('send _ to _', 'send _')
IMPORTER_BEFORE = N('hello', 'alice')
IMPORTER_AFTER = N('hello', 'alice', 'hello to alice')
bv, bs = res(IMPORTER_BEFORE, PATS, 'send hello to alice')
av, asw = res(IMPORTER_AFTER, PATS, 'send hello to alice')
print(f'    before «import B»:  {bv:8}  {bs}')
print(f'    after  «import B»:  {av:8}  {asw}')
print(f'''
  [{"NO" if (bv, bs) != (av, asw) else "yes"}] the statement was written before the
  import and its meaning changed anyway.

  Compiled-scope resolution protects MODULE A and MODULE B. It does not
  protect the IMPORTER's own existing code, because that code is inside the
  scope the new import joins. The hazard is not confined to new statements --
  it is confined to one module, which is a much weaker guarantee and still the
  right first step.''')
print()

print('=' * W)
print('2. How much does the blanket rule over-refuse?')
print('=' * W)


def sweep(universe, importer_names, patterns, maxsrc=4, maxname=3):
    pats = PA(*patterns)
    base = frozenset(tuple(x.split()) for x in importer_names)
    glue = glue_of(pats)
    runs = anchoronly(pats)
    srcs = [s for k in range(1, maxsrc + 1)
            for s in itertools.product(universe, repeat=k)]
    before = {s: res(base, pats, ' '.join(s)) for s in srcs}

    refused, dangerous, harmless = 0, 0, []
    for k in range(1, maxname + 1):
        for c in itertools.product(universe, repeat=k):
            if c in base:
                continue
            r5 = len(c) > 1 and any(w in glue for w in c)
            r6b = any(len(a) < len(c) and c[:len(a)] == a for a in runs)
            if not (r5 or r6b):
                continue
            refused += 1
            names = base | {c}
            hit = False
            for s in srcs:
                bv, bs = before[s]
                if bv != 'OK':
                    continue
                av, asw = res(names, pats, ' '.join(s))
                if av != 'OK' or bs != asw:
                    hit = True
                    break
            if hit:
                dangerous += 1
            else:
                harmless.append(c)
    return refused, dangerous, harmless, sorted(glue), runs


CONFIGS = [
    ('send (_) to (_) | send (_)', ['hello', 'alice', 'send', 'to'],
     ['hello', 'alice'], ['send _ to _', 'send _']),
    ('print (_)', ['print', 'a', 'job', 'queue'], ['a', 'job', 'queue'],
     ['print _']),
    ('sum of (_) | (_) otherwise (_)', ['sum', 'of', 'x', 'otherwise'],
     ['x'], ['sum of _', '_ otherwise _']),
]
tot_r, tot_d = 0, 0
for title, uni, imp, ps in CONFIGS:
    r, d, harm, g, runs = sweep(uni, imp, ps)
    tot_r += r
    tot_d += d
    pct = 100.0 * (r - d) / r if r else 0.0
    print(f'\n  {title}')
    print(f'      glue={g}  anchor-only={[" ".join(a) for a in runs]}')
    print(f'      refused by R5/R6b: {r:4}   actually dangerous: {d:4}   '
          f'over-refused: {r-d:4}  ({pct:.0f}%)')
    for c in harm[:4]:
        print(f'        refused, never captures anything: «{" ".join(c)}»')

pct = 100.0 * (tot_r - tot_d) / tot_r
print(f'''
  TOTAL   refused {tot_r}   dangerous {tot_d}   over-refused {tot_r-tot_d} ({pct:.0f}%)

  Inside one module that over-refusal is the right trade: predictability is
  worth more than precision, and the repair is a rename. Across an import it
  is a different trade entirely -- {pct:.0f}% of the library pairs it would reject
  could have coexisted, and the importer cannot rename either side.''')
print()

print('=' * W)
print('3. The differential check, as the alternative')
print('=' * W)
print('''  "An import may not change the reading of any statement already in the
  importing module."

  It is not a new instrument -- it is the sweep above, run on one module's
  actual statements instead of on a generated universe. By construction it
  flags every capture and nothing else, so it is exactly as strict as the
  danger and no stricter.

  Cost: resolve the importing module once per import, against the table
  without that import. n imports, n+1 resolutions, and only when the import
  list changes. In an always-running environment that is a background task.
''')
for label, extra in (('add «import B» (name «hello to alice»)',
                      {('hello', 'to', 'alice')}),
                     ('add «import C» (name «alice greeting»)',
                      {('alice', 'greeting')})):
    tbl = IMPORTER_BEFORE | extra
    changed = []
    for stmt in ['send hello to alice', 'send hello', 'send alice']:
        bv, bs = res(IMPORTER_BEFORE, PATS, stmt)
        av, asw = res(tbl, PATS, stmt)
        if bv == 'OK' and (av, asw) != (bv, bs):
            changed.append((stmt, bs, asw))
    print(f'  {label}')
    if changed:
        for stmt, b, a in changed:
            print(f'      REJECT: «{stmt}» would change meaning')
            print(f'              {b}  ->  {a}')
    else:
        print('      accept: no existing statement changes reading')
print()

print('=' * W)
print('4. The collision he did not list, which is more likely than R5')
print('=' * W)
print('''  Two modules exporting the SAME symbol is not an R5 problem and no
  spelling rule prevents it. Under no-shadowing and a flat merged table it is
  a duplicate declaration, and it fails for every importer of both:

      module A exports   print (_)
      module B exports   print (_)          -> duplicate on merge

  «print», «count of», «sort», «first of» are exactly the names two libraries
  independently pick. This needs the qualification escape hatch whether or not
  §1's R5 case is ever hit, and it is the case that will actually bite.''')
