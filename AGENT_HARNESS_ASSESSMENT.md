# Agent Harness Assessment

**Assessment date:** 2026-11-06  
**Repositories compared:** `D:\2.BE\meeting-room-booking` (source setup) and this DocMind AI repository  
**Scope:** repository instructions, agent skills and workflows, verification, automation, project memory, security boundaries, and developer onboarding.

## Is there an official scoring standard?

There is no single official Agent Harness score or required repository layout. OpenAI's Codex guidance describes `AGENTS.md`, repo-local skills, optional scripts/references, and CI integration as useful parts of a repeatable setup; it treats the file layout as adaptable to the project. OpenAI's skill-evaluation guidance suggests measuring outcome, process, style, and efficiency, including whether a skill triggers when intended and avoids triggering on nearby requests.

This document therefore uses a **local, evidence-based rubric**, informed by those public recommendations. The percentages are comparative readiness estimates, not a certification or an official OpenAI score.

References:

- [Using skills to accelerate OSS maintenance](https://developers.openai.com/blog/skills-agents-sdk)
- [Testing Agent Skills Systematically with Evals](https://developers.openai.com/blog/eval-skills)
- [Iterating Development Workflows with Codex](https://developers.openai.com/cookbook/examples/codex/iterating-development-workflows-with-codex)

## Scoring method

Each category has a weight that totals 100 points. The score reflects observable evidence in the repository, not how polished the prose looks.

| Level | Meaning |
| ---: | --- |
| 0 | Absent |
| 1 | Mentioned or partially covered |
| 2 | Documented and usable by a person |
| 3 | Repeatable locally with clear commands or artifacts |
| 4 | Enforced or automated in normal development/CI |
| 5 | Evaluated with outcome evidence and maintained against regressions |

Category points are scaled to the category weight. Source and DocMind were assessed using the same rubric.

## Weighted comparison

| Criterion | Weight | Meeting-room-booking | DocMind AI | Evidence and assessment |
| --- | ---: | ---: | ---: | --- |
| Repository instructions and product context | 15 | 14 | 13 | Both define mission, stack, scope boundaries, and relevant source-of-truth documents. Source `AGENTS.md` and vision are more mature and contain more operational context; DocMind has clear single-project and RAG boundaries. |
| Workflows and task templates | 15 | 14 | 13 | Both have role playbooks, bug-fix/feature/refactor workflows, and spec/task templates. Source also has established phase/spec/decision history. DocMind now has adapted workflows and templates but little usage history. |
| Skill catalog and routing | 15 | 11 | 11 | Both have five repo-local skills with names and descriptions. DocMind descriptions and guidance were adapted to .NET and DocMind. Neither repo has evidence that skill triggering has been evaluated with representative positive and negative prompts. |
| Verification and test harness | 15 | 12 | 4 | Source has backend unit/integration/security tests and documented Maven/frontend build commands; Testcontainers checks require Docker. DocMind has a successful .NET build and an HTTP request sample, but no test project, provider fake, or automated regression suite. |
| CI quality gates | 10 | 4 | 0 | Source GitHub Actions builds container images on pull requests, but does not run its test suite as a dedicated gate. DocMind has no CI workflow. |
| Skill/workflow evaluations | 10 | 0 | 0 | Neither repo has a prompt/eval set measuring expected skill activation, task outcome, style, or unnecessary activation. |
| Project memory and traceability | 10 | 9 | 2 | Source has populated specs, phase records, standards, and a decision record. DocMind has templates and product vision, but no active spec, decision log, or phase/task evidence yet. |
| Safety and scope boundaries | 5 | 4 | 4 | Both instruct the agent to preserve user work and protect secrets. DocMind explicitly warns that current chat is not RAG and discourages public exposure without authentication and abuse controls. |
| Developer onboarding and repeatability | 5 | 4 | 4 | Both document setup and useful commands. Source additionally documents Docker-backed stack behavior; DocMind documents .NET 10, User Secrets, and the chat request. DocMind does not pin the SDK with `global.json`. |
| **Total** | **100** | **72%** | **51%** | **Source setup is more mature overall, mainly due to tests and accumulated project memory.** |

## What the source setup does well

- Clear `AGENTS.md` hierarchy: product vision, repository boundaries, required reading, coding rules, and verification expectations.
- A useful `.okf` system with five role playbooks, three workflows, four templates, implementation standards, specs, decisions, and phase records.
- Five repo-local skills cover status auditing, frontend work, spec writing, verification triage, and OKF compaction.
- Existing backend tests exercise service, API integration, error handling, and security behavior; README explains relevant commands and Docker-dependent checks.
- A pull-request GitHub workflow builds the backend and frontend container images, providing a useful packaging check.

## What should improve in the source setup

1. **Make CI run tests and frontend checks.** Image builds on pull requests do not replace `mvn test` and `npm run build` gates.
2. **Add targeted skill evaluations.** For each skill, include prompts that should trigger it, nearby prompts that should not, and observable success criteria. A small initial set can reveal routing problems early.
3. **Review skills for stale project-specific rules.** Keep booking constraints in booking-specific guidance, and keep generic skills limited to behavior that applies across tasks.
4. **Keep evidence current.** Update phase/task status only after running the stated checks; preserve environment blockers such as Docker-dependent tests.

## What DocMind already does well

- The repo has a concise `AGENTS.md`, a product vision, setup README, and explicit one-project architecture boundary.
- The copied skill set and workflows are adapted to ASP.NET Core/.NET rather than carrying over Java, Maven, or meeting-room booking rules.
- The instructions distinguish general LLM chat from future document-grounded RAG and require safe secret handling.
- Feature, bug-fix, and refactor workflows plus spec/task/decision templates give future work a consistent starting point.

## DocMind improvements, in priority order

1. **Add a .NET test project and provider fake.** Cover request validation, successful chat response parsing, missing API key, provider errors, timeout, and cancellation without making live LLM calls.
2. **Add CI gates.** Run restore, build, and tests on pull requests. Add frontend checks when a frontend is introduced.
3. **Evaluate skill routing and results.** Create a small prompt set with positive and negative trigger cases, then record whether the expected skill ran and whether its outcome met acceptance criteria.
4. **Add durable project records only when there is real work to track.** Record accepted architecture decisions and create specs for multi-requirement features; do not create empty phase machinery for routine fixes.
5. **Pin the .NET SDK or document supported SDK versions.** This reduces differences between local and CI builds.

## Interpretation

The 72% source score means its repository harness is established and backed by tests and project history, but CI and skill evaluation remain weak. The 51% DocMind score means its instructions and workflow foundations are in place, while automated verification, CI, and skill-evaluation evidence are still missing. Neither score measures product feature completeness or production readiness.
