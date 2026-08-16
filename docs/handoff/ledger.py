#!/usr/bin/env python3
"""
ledger.py -- the live-verdict index, generated rather than remembered.

Every design memo and ruling in this folder opens with a ledger header
(see LEDGERSCHEMA.md): a marker `[V]`/`[R]`, a one-line summary, an optional
answer edge, and the two supersession fields. This walks those headers and emits
the one page a successor actually wants -- what currently binds -- and reports any
design document whose header is missing, malformed, or holds a dangling edge, so a
broken header is a visible defect in a generated artefact rather than an invisible
one in a file nobody opened (LEDGERRULING §7, ANSWEREDEDGE §3).

    python3 ledger.py             emit the index
    python3 ledger.py --check F   compare against a checked-in index; nonzero on drift

The generator's first job is edge reciprocity: every `answered by: X` must be
matched by an `answers:` on X, and the reverse. Audit reports (REAUDIT*, AUDIT*,
FRESHAUDIT*, CODEREVIEW) keep their native format and carry disposition, not
supersession, so they are not expected to have a header (LEDGERRULING §4). Scripts
are skipped by the .md glob. Everything else is a design document and must have one.
"""

import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
GENERATED = "LEDGER.md"                       # this script's own output; never a source

AUDIT = re.compile(r"^((RE)?AUDIT|FRESHAUDIT|CODEREVIEW)\d*$")
LEDGER_LINE = re.compile(r"^> \*\*Ledger\*\* — ")
MARKER = re.compile(r"`\[(V|R)\]`")
FIELDS = {
    "answers": re.compile(r"^> answers: (.+?)\s*$"),
    "answered by": re.compile(r"^> answered by: (.+?)\s*$"),
    "supersedes": re.compile(r"^> supersedes: (.+?)\s*$"),
    "superseded by": re.compile(r"^> superseded by: (.+?)\s*$"),
    "measured at": re.compile(r"^> measured at: (.+?)\s*$"),
}
UNWALKED = "not yet checked"
LEGAL_ABSENT = {"none", UNWALKED}


class Doc:
    def __init__(self, name):
        self.name = name
        self.marker = None                    # 'V' | 'R' | None
        self.summary = ""
        self.fields = {}                      # field name -> value string, when the line is present
        self.defects = []

    @property
    def is_audit(self):
        return bool(AUDIT.match(self.name))

    @property
    def is_measurement(self):
        # a measurement is a claim about the tree at a commit, not a design position;
        # it goes stale when the code moves, not when a document overturns it
        return "measured at" in self.fields

    @property
    def superseded_by(self):
        return self.fields.get("superseded by")

    @property
    def unwalked(self):
        return self.fields.get("supersedes") == UNWALKED or self.superseded_by == UNWALKED


def header_block(lines):
    """The consecutive '>' lines of the leading ledger blockquote, or None."""
    start = next((i for i, ln in enumerate(lines) if LEDGER_LINE.match(ln)), None)
    if start is None:
        return None
    end = start
    while end < len(lines) and lines[end].startswith(">"):
        end += 1
    return lines[start:end]


def parse(path):
    doc = Doc(path.stem)
    lines = path.read_text(encoding="utf-8").splitlines()
    block = header_block(lines)

    if block is None:
        if not doc.is_audit:
            doc.defects.append("no ledger header")
        return doc

    marker = MARKER.search(block[0])
    doc.marker = marker.group(1) if marker else None
    if doc.marker is None:
        doc.defects.append("header has no `[V]`/`[R]` marker")

    summary_lines, field_lines = [], set()
    for i, ln in enumerate(block):
        matched = False
        for name, pattern in FIELDS.items():
            m = pattern.match(ln)
            if m:
                doc.fields[name] = m.group(1)
                field_lines.add(i)
                matched = True
        if not matched:
            summary_lines.append(ln.lstrip("> ").rstrip())
    doc.summary = MARKER.sub("", " ".join(summary_lines), count=1).replace("**Ledger** — ", "").strip()

    if doc.is_measurement:
        for field in ("supersedes", "superseded by", "answers", "answered by"):
            if field in doc.fields:
                doc.defects.append(f"measurement carries `{field}`; a measurement takes `measured at` and nothing else")
    else:
        for field in ("supersedes", "superseded by"):
            value = doc.fields.get(field)
            if value is None:
                doc.defects.append(f"missing `{field}` field")
            elif not value or value == "nothing":
                doc.defects.append(f"`{field}` has an illegal value ({value!r}; use `none` or `not yet checked`)")
    return doc


def collect():
    return [parse(p) for p in sorted(HERE.glob("*.md")) if p.name != GENERATED]


def refs(value, stems):
    """Document names cited in a field value, matched whole against known stems."""
    if not value:
        return []
    return [s for s in stems if re.search(r"(?<![\w-])" + re.escape(s) + r"(?![\w-])", value)]


def check_reciprocity(docs):
    """Every `answered by: X` must be matched by an `answers:` on X, and the reverse."""
    stems = {d.name for d in docs}
    by_name = {d.name: d for d in docs}
    for d in docs:
        for near, far in (("answered by", "answers"), ("answers", "answered by")):
            for target in refs(d.fields.get(near), stems):
                other = by_name[target]
                if d.name not in refs(other.fields.get(far), stems):
                    d.defects.append(f"`{near}: {target}` is one-sided — {target} has no matching `{far}`")


def superseded_fully(value):
    return value not in LEGAL_ABSENT and "(§" not in (value or "")


def bullet(doc, tail=""):
    summary = (doc.summary[:96] + "…") if len(doc.summary) > 97 else doc.summary
    edge = ""
    if doc.fields.get("answered by"):
        edge = f"  _(answered by {doc.fields['answered by']})_"
    elif doc.fields.get("answers"):
        edge = f"  _(answers {doc.fields['answers']})_"
    return f"- **{doc.name}** — {summary}{edge}{tail}"


def render(docs):
    design = [d for d in docs if not d.is_audit]
    measurements = [d for d in design if d.is_measurement]
    ruled = [d for d in design if not d.is_measurement]
    clean = [d for d in ruled if not d.defects]
    headered = [d for d in design if "no ledger header" not in d.defects]

    verdicts_gone = [d for d in clean if d.marker == "V" and superseded_fully(d.superseded_by)]
    verdicts_open = [d for d in clean if d.marker == "V" and d.superseded_by == UNWALKED]
    verdicts_live = [d for d in clean if d.marker == "V" and d not in verdicts_gone and d not in verdicts_open]
    recommendations = [d for d in clean if d.marker == "R"]
    worklist2 = [d for d in clean if d.unwalked]
    worklist1 = [d for d in design if "no ledger header" in d.defects]
    defective = [d for d in design if d.defects and "no ledger header" not in d.defects]

    out = []
    out.append("# Ledger — what currently binds")
    out.append("")
    out.append("Generated by `ledger.py` from the headers in `docs/handoff`. Do not edit by")
    out.append("hand — run `python3 ledger.py` to regenerate.")
    out.append("")
    out.append(
        f"{len(design)} design documents · {len(headered)} headed · {len(measurements)} measurements · "
        f"{len(worklist1)} awaiting a header · {len([d for d in docs if d.is_audit])} audit reports (excluded)."
    )

    out.append("")
    out.append("## Verdicts in force")
    out.append("")
    out += [bullet(d) for d in verdicts_live] or ["_none_"]

    if verdicts_open:
        out.append("")
        out.append("## Verdicts — supersession not yet checked")
        out.append("")
        out += [bullet(d) for d in verdicts_open]

    if verdicts_gone:
        out.append("")
        out.append("## Superseded verdicts")
        out.append("")
        out += [bullet(d, f"  (superseded by: {d.superseded_by})") for d in verdicts_gone]

    out.append("")
    out.append("## Recommendations")
    out.append("")
    out += [bullet(d) for d in recommendations] or ["_none_"]

    out.append("")
    out.append(f"## Measurements — staleness-gated, not superseded ({len(measurements)})")
    out.append("")
    out += [f"- **{d.name}** — measured at `{d.fields['measured at']}`" for d in measurements] or ["_none_"]

    out.append("")
    out.append(f"## Pass 2 worklist — supersession not yet checked ({len(worklist2)})")
    out.append("")
    out += [f"- **{d.name}**" for d in worklist2] or ["_none_"]

    out.append("")
    out.append(f"## Pass 1 worklist — design documents with no ledger header ({len(worklist1)})")
    out.append("")
    out += [f"- {d.name}" for d in worklist1] or ["_none_"]

    if defective:
        out.append("")
        out.append("## Defects")
        out.append("")
        for d in defective:
            out += [f"- **{d.name}** — {problem}" for problem in d.defects]

    out.append("")
    return "\n".join(out)


def main(argv):
    docs = collect()
    check_reciprocity(docs)
    index = render(docs)

    if len(argv) >= 2 and argv[0] == "--check":
        current = Path(argv[1]).read_text(encoding="utf-8")
        if current != index:
            sys.stderr.write(f"{argv[1]} is out of date; run `python3 ledger.py` and commit.\n")
            return 1
        return 0

    sys.stdout.write(index)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
