# PF1 open questions

- Status: Shared index
- Active questions: see [`02_branch-access-control/OPEN_QUESTIONS.md`](02_branch-access-control/OPEN_QUESTIONS.md)

## Rule

Keep only unresolved questions here or in the active branch folder. When a
question is resolved:

```text
Question
	-> inspect code, tests and roadmap
		-> compare a small number of options
			-> choose a rule
				-> update the brief and test plan
					-> record an ADR only when the decision is reusable
```

## PF1 boundary questions

| Question | Where to resolve it |
|---|---|
| What is the exact duplicate-membership behavior? | Branch 02 brief and questions |
| Is membership removal a delete or a deactivation? | Branch 02 brief and questions |
| Which authorization convention protects administration? | Existing API policies and tests |
| Which rules need database constraints? | Branch-specific file and test plans |

Do not add speculative decisions just to make this file longer. An explicit
`UNKNOWN` is better than an invented answer.
