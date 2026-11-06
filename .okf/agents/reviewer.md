# Reviewer Playbook

Review changes against the request, `PRODUCTION_VISION.md`, and actual application behavior. Check:

- Scope alignment and whether new capabilities are represented honestly.
- Input validation, safe error responses, cancellation, and timeout behavior.
- Secret handling and exposure of prompts, documents, provider bodies, or credentials.
- ASP.NET Core conventions and the single-project boundary.
- README/API documentation accuracy and relevant verification evidence.

Report actionable findings with file paths and distinguish defects from open questions.
