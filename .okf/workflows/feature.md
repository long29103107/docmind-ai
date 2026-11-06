# Feature Workflow

1. **Clarify:** identify the user outcome and confirm it fits `PRODUCTION_VISION.md`.
2. **Specify:** use `.okf/templates/spec.md` when the feature has multiple requirements, API/data effects, or unresolved decisions. State observable acceptance criteria.
3. **Plan:** identify API, persistence, security, compatibility, and optional client impacts. Keep the plan proportional to the work.
4. **Implement:** build the smallest complete feature within the single `DocMind.Api` project unless a concrete requirement calls for another project.
5. **Review:** check scope, server-side validation, error semantics, secret handling, and coverage against the acceptance criteria.
6. **Verify and record:** run relevant .NET checks; update the spec, README, vision, or decision record when behavior or durable choices change.

## Done when

Acceptance criteria are implemented, relevant checks are reported accurately, and durable behavior is documented.
