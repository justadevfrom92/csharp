# Roadmap Test Runner (quiz)

**Play: https://claude.ai/artifact/CBbFEtL3HsKVKTPehWnamV**

An interactive quiz, by section, over everything this repo teaches: **327 questions** across all 10 sections.

| Type | Count | Example |
|---|---|---|
| Multiple choice | 192 | "Which type should hold money amounts?" |
| What does it print? | 29 | `Console.WriteLine(Math.Round(2.5m));` → `2` (banker's rounding) |
| Fill in the blank | 75 | `var p2 = p1 ____ { Name = "Filip Ekberg" };` → `with` |
| True or false | 31 | "`ExecuteSql("UPDATE ...")` is filtered by the tenant query filter" → false |

How it plays: pick a section (or all, mixed), 10 / 20 / all questions, Easy / Medium / Hard. Every run goes to the
last question (no lives); 70% or more passes. **Every question shows code.**
Each answer reports **Passed** / **Failed** with the explanation; the end screen lists failed questions and can retry just those.
Best score per section is kept in your browser. Keys: 1–4 to answer, Enter to check and continue.
Fill-in-the-blank answers are case-sensitive, like C#. The newest features (extension blocks, the `field` keyword) are labelled ".NET 10".

## Files

| File | What |
|---|---|
| `index.html` | the game (one page, no libraries) |
| `questions.json` | the questions, generated - don't edit by hand |
| `build/q1_3.py`, `q4_6.py`, `q7_10.py`, `q_extra.py` | the question bank, written per section |
| `build/code_1_4.py`, `code_5_10.py` | the code shown with questions that don't carry their own, keyed by question id |
| `build/build_questions.py` | checks every question (answer in range, no duplicate choices, `____` in fill-in code, project inside its section, **code present**) and writes `questions.json` |

Add or change questions in `build/`, then:

```bash
cd build
python3 build_questions.py
```

Every "what does it print?" answer that doesn't depend on timing or a database was checked by running the code
(22 snippets, all matched). The rest come from what each project's tests showed.
