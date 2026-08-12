#!/usr/bin/env python3
"""
glue_cost.py -- how expensive is reserving a given word, measured on a corpus.

The programmer's §3 is right that the operator case needs R5's bill: a word
that can appear ANYWHERE inside a multi-word name, not merely as a prefix. He
calls that "the more expensive rule, which is presumably why «otherwise» was
left open."

That is the right instinct in general and I think it is wrong for this
particular word, so it should be measured rather than argued. R5's bill is
expensive because glue words are usually short prepositions -- «to», «of»,
«in», «by», «with», «on». «otherwise» is a long connective that almost never
appears inside an identifier.

Corpus: every .py on this machine (stdlib + installed packages). Identifiers
are split on underscores and camelCase and lowercased; only MULTI-WORD
identifiers count, because R5 only examines those. The measure is:

    bill(w) = distinct multi-word identifiers containing w, as a share of all
              distinct multi-word identifiers

Not Ronin code, and identifier style differs by language -- but the question
is about the English vocabulary programmers put in names, which travels.
"""

import collections
import os
import re
import sys

IDENT = re.compile(r'[A-Za-z_][A-Za-z0-9_]*')
CAMEL = re.compile(r'[A-Z]?[a-z0-9]+|[A-Z]+(?![a-z])')

CANDIDATES = ['is', 'a', 'an', 'not', 'same', 'equals', 'equal', 'matches',
              'like', 'kind', 'otherwise', 'to', 'of', 'in', 'as', 'by']


def words(ident):
    out = []
    for part in ident.split('_'):
        if part:
            out.extend(m.group().lower() for m in CAMEL.finditer(part))
    return out


def main(roots):
    seen = set()
    files = 0
    for root in roots:
        for dirpath, _, names in os.walk(root):
            for n in names:
                if not n.endswith('.py'):
                    continue
                p = os.path.join(dirpath, n)
                try:
                    src = open(p, 'r', encoding='utf-8', errors='ignore').read()
                except OSError:
                    continue
                files += 1
                for m in IDENT.finditer(src):
                    seen.add(m.group())

    multi = {}
    for ident in seen:
        w = words(ident)
        if len(w) > 1:
            multi[ident] = w

    total = len(multi)
    counts = collections.Counter()
    for w in multi.values():
        for x in set(w):
            counts[x] += 1

    W = 70
    print('=' * W)
    print('R5 bill per word -- share of multi-word identifiers killed')
    print('=' * W)
    print(f'  {files} files, {len(seen)} distinct identifiers, '
          f'{total} of them multi-word\n')
    print(f'  {"word":12} {"identifiers hit":>16} {"share":>9}')
    print('  ' + '-' * 40)
    for w in sorted(CANDIDATES, key=lambda x: -counts[x]):
        c = counts[w]
        print(f'  {w:12} {c:>16} {100.0*c/total:>8.3f}%')

    print()
    print('  examples of what each kills:')
    for w in ('is', 'a', 'not', 'an'):
        ex = [i for i, ws in multi.items() if w in ws][:8]
        print(f'    {w:10} {", ".join(sorted(ex, key=len)[:6])}')
    print()

    # rank «otherwise» against the whole vocabulary
    rank = sorted(counts.items(), key=lambda kv: -kv[1])
    pos = next((i for i, (w, _) in enumerate(rank) if w == 'otherwise'), None)
    print(f'  vocabulary size (distinct words in multi-word identifiers): '
          f'{len(counts)}')
    if pos is not None:
        print(f'  «otherwise» ranks {pos + 1} of {len(counts)} by frequency')
    for w in ('is', 'a', 'an', 'not', 'to'):
        r = next((i for i, (x, _) in enumerate(rank) if x == w), None)
        print(f'  «{w}» ranks {r + 1 if r is not None else "-"}')
    print()


if __name__ == '__main__':
    main(sys.argv[1:] or ['/usr/lib/python3.10', '/usr/lib/python3',
                          '/root/.cache/uv', '/usr/local/lib'])
