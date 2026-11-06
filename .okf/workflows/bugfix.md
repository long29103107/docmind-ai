# Bug Fix Workflow

1. Reproduce or trace the reported behavior and identify the expected API or product contract.
2. Write a bounded task note when the fix spans multiple files or has non-obvious acceptance criteria.
3. Fix the cause in the appropriate ASP.NET Core feature folder; keep validation and authorization server-side.
4. Add or update a focused regression check when a test harness exists and the behavior warrants it.
5. Run the relevant .NET build/tests and record exact results and remaining uncertainty.

## Done when

- The reported behavior is corrected without unrelated scope expansion.
- Relevant checks have run or their blocker is documented.
- User-facing contracts and README guidance are updated when they changed.
