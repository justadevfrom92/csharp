# Builds ../questions.json from the question bank and checks every question first.
import json, os, sys, collections
sys.path.insert(0, os.path.dirname(__file__))
from q_extra import Q
problems = []
for n, q in enumerate(Q):
    where = f"#{n} {q['p']} {q['q'][:40]}"
    if q["t"] == "blank":
        if not isinstance(q["a"], list) or not q["a"]: problems.append(where + ": blank needs answers")
        if "____" not in (q["code"] or ""): problems.append(where + ": blank needs ____ in code")
    else:
        if not (0 <= q["a"] < len(q["choices"])): problems.append(where + ": answer index out of range")
        if len(set(q["choices"])) != len(q["choices"]): problems.append(where + ": duplicate choices")
    if not q["why"]: problems.append(where + ": no explanation")
    if not str(q["p"]).startswith(str(q["s"]) + "."): problems.append(where + ": project not in section")
if problems:
    print("\n".join(problems)); sys.exit(1)
counter = collections.defaultdict(int)
for q in Q:
    counter[q["s"]] += 1
    q["id"] = f"s{q['s']}-{counter[q['s']]:03d}"
    if q.get("code") is None: q.pop("code", None)
# Every question shows code: the snippets for questions without their own code live in code_*.py.
from code_5_10 import CODE
unused = set(CODE) - {q["id"] for q in Q}
if unused:
    print("snippets for unknown ids:", sorted(unused)); sys.exit(1)
for q in Q:
    if not q.get("code"):
        if q["id"] not in CODE:
            problems.append(q["id"] + ": no code")
        else:
            q["code"] = CODE[q["id"]]
if problems:
    print("\n".join(problems)); sys.exit(1)
out = os.path.join(os.path.dirname(__file__), "..", "questions.json")
with open(out, "w") as f:
    f.write("[\n" + ",\n".join(json.dumps(q, ensure_ascii=False) for q in Q) + "\n]\n")
types = collections.Counter(q["t"] for q in Q)
print("total", len(Q), dict(sorted(counter.items())), dict(types), os.path.getsize(out), "bytes")
