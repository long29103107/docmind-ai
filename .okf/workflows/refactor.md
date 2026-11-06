# Refactor Workflow

1. State the maintenance outcome and behavior that must remain unchanged.
2. Bound the touched feature folders and identify public API/configuration contracts.
3. Refactor in small steps without mixing unrelated feature work.
4. Run relevant existing build/tests. Add a focused behavior test only when needed to establish that a contract is preserved.
5. Record meaningful architecture changes in `PRODUCTION_VISION.md` or a decision note.
