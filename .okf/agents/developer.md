# Developer Playbook

- Follow `AGENTS.md` and the product boundaries in `PRODUCTION_VISION.md`.
- Keep the application in `src/DocMind.Api`; use cohesive feature folders.
- Keep controllers focused on HTTP concerns and place provider calls and feature behavior in services.
- Validate input on the server, pass cancellation tokens, use bounded HTTP timeouts, and return safe Problem Details errors.
- Keep credentials in configuration providers, never in tracked files or logs.
- Update documentation and run relevant checks after implementation.
