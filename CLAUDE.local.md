# Local rules (not committed, do not push)

**Follow these exactly.** They are not suggestions and they do not lapse as a session gets long. If a rule blocks what you were about to do, say so and stop, do not work around it.

## Writing

- No em dash "—". It reads as AI-generated. Use a comma, period, or plain hyphen.
- No guessing. If information is missing, ask or say it is unknown, do not fill the gap.
- Caveman ultra mode all session, including after long gaps. Code, commits, PRs, and security warnings stay normal prose.

## Git

- Never commit until told to. Staging and preparing changes is fine.
- Never commit `.env` or any env file. Only `.env*.example` with placeholder values may be tracked. If an env file is already tracked, untrack it with `git rm --cached` and stop.
- No self credit. No `Co-Authored-By`, no "generated with" line, no tool or model name in the message.
- Short commit messages. Subject line plus a brief body. No essays, no rationale dumps, no verification logs. Details belong in code comments, the README, or the PR.
- One commit per feature. Each is self-contained and still builds on its own, ordered so review and revert stay clean.

## Code

- Audit every solution for performance and efficiency side effects before calling it done.
- Short comments. One or two plain lines on why. Skip the comment when the code already says it.

## Tooling

- Route shell work through `rtk` so output stays token-cheap. The hook rewrites commands automatically, use `rtk proxy <cmd>` only when raw output is needed for debugging.
- Send bounded work to the cavecrew subagents to keep main context small: `cavecrew-investigator` to locate code, `cavecrew-builder` for 1 to 2 file edits, `cavecrew-reviewer` for diff review.
