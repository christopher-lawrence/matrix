# Task 3 Report: Suggest `/help` For Unknown Commands

## Implementation

- Updated the unknown-command response in `Matrix.Server/Services/CommandHandler.cs` to include: `Use /help to see available commands.`
- Kept the change limited to unknown-command behavior; no README, command metadata, registration, or session behavior was modified.

## Verification

Command run:

```text
dotnet build Matrix.slnx
```

Result: passed. `Matrix.Core`, `Matrix.Client`, and `Matrix.Server` built successfully with 0 warnings and 0 errors.

Additional self-review:

- `git diff --check` passed.
- Confirmed the response preserves the unrecognized command and uses `IConnectionManager`.
- No automated tests were added because the repository has no test project and the task explicitly prohibits adding a test framework.

## Commit

`4376f08 feat: suggest help for unknown commands`

## Concerns

None.
