# Decisions

Durable decisions that future agents should preserve.

## Format

### YYYY-MM-DD - Decision title

Decision

What was decided?

Reason

Why was it decided?

Consequences

What does this affect?

Do Not Break

What future agents must preserve.

## 2026-06-18 - Use a lightweight Codex harness

Decision

This repository uses a small Codex harness made of `AGENTS.md`, task briefs, validation docs, and stack-detecting scripts.

Reason

Future agent work needs a shared intake, verification, and handoff structure without adding a large documentation system.

Consequences

Agents should prefer the harness commands for repository readiness, checks, tests, and evaluation. The scripts may report that a tool or automated check is unavailable instead of pretending verification succeeded.

Do Not Break

Keep the harness lightweight, run it from the repository root, and record only durable project decisions here.
