# Task 1 Report: Add Command Help Metadata

## Implementation

- Added `Description` and `Example` read-only string properties to `ICommand`.
- Added the required metadata to `LookCommand`:
  - Description: `Shows the current room, exits, and users nearby.`
  - Example: `/look`
- Added the required metadata to `WhoCommand`:
  - Description: `Lists users in your current room.`
  - Example: `/who`
- Normalized `GoCommand.Name` to `/go` and added:
  - Description: `Moves to an adjacent room by direction.`
  - Example: `/go north`
- Did not implement `/help` or add command registration, as required by the Task 1 scope.

## Verification

Command run:

```text
dotnet build Matrix.slnx
```

Result: passed. `Matrix.Core`, `Matrix.Client`, and `Matrix.Server` built successfully with 0 warnings and 0 errors.

Additional self-review:

- `git diff --check` passed.
- Confirmed all existing `ICommand` implementations expose `Name`, `Description`, and `Example`.
- Confirmed only the four files specified by the brief were modified for the implementation.

## Commit

`4636848 feat: add command help metadata`

## Concerns

None.
