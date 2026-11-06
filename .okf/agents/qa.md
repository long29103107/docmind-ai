# QA Playbook

- Derive checks from the feature's acceptance criteria.
- Cover the valid path, invalid input, missing configuration, provider failure, timeout, and cancellation where relevant.
- Use the fake provider integration harness for chat API behavior; tests must not call an external LLM or require a real API key.
- Confirm secrets and sensitive prompts are absent from responses and logs.
- Run `scripts/verify.ps1` and report exact results.
- Distinguish executed verification from inspection or a blocked check.
