# Application Service Extraction & Documentation Alignment

**Date:** 2026-04-11
**Status:** Approved
**Driven by:** Graphify analysis — Community 0 low cohesion (0.05), Account cross-community bridge

## Context

Graphify knowledge graph analysis revealed that two command handlers mix cross-boundary orchestration with MassTransit transport concerns:

- `RecordExpenseHandler` — loads Account aggregate (read-only ownership/active check), queries `ICategoryRepository` (Postgres-backed application repository, not an aggregate), then writes Expense aggregate.
- `SetCategoryLimitHandler` — loads Budget aggregate, queries `ICategoryRepository` for existence check, then writes Budget aggregate.

Both follow the DDD rule (read many, write one) but couple domain orchestration to MassTransit's `ConsumeContext`. `ICategoryRepository` is an application-layer repository backed by Postgres reference data — not a domain aggregate — but the cross-concern pattern is the same.

Additionally, CLAUDE.md duplicates the graphify configuration already present in AGENTS.md, creating unnecessary maintenance burden across the multi-agent setup (Claude + Codex).

## Change 1: ExpenseApplicationService

### What

Extract cross-boundary domain orchestration from `RecordExpenseHandler` into a new `ExpenseApplicationService`. `SetCategoryLimitHandler` also uses `ICategoryRepository` but only writes to a single aggregate (Budget) — it stays as-is for now. If more budgeting handlers gain cross-boundary validation, a `BudgetApplicationService` can be extracted following the same pattern.

### New file

`src/WiSave.Expenses.Core.Application/Accounting/ExpenseApplicationService.cs`

```csharp
public sealed class ExpenseApplicationService(
    IAggregateRepository<Expense> expenseRepository,
    IAggregateRepository<Account> accountRepository,
    ICategoryRepository categoryRepository)
{
    /// <summary>
    /// Validates Account and Category, creates and saves the Expense aggregate.
    /// Returns the new ExpenseId on success, or an error message on validation failure.
    /// Throws DomainException for domain invariant violations.
    /// </summary>
    public async Task<(ExpenseId Id, string? Error)> RecordExpense(
        RecordExpense command, CancellationToken ct)
}
```

### Responsibilities

| Concern | Owner |
|---------|-------|
| Load Account, validate active + ownership | ExpenseApplicationService |
| Validate Category/Subcategory existence | ExpenseApplicationService |
| Create Expense aggregate and save | ExpenseApplicationService |
| MassTransit ConsumeContext handling | RecordExpenseHandler |
| CommandFailed publishing | RecordExpenseHandler |

### RecordExpenseHandler after refactor

Becomes a thin MassTransit adapter:

```csharp
public sealed class RecordExpenseHandler(
    ExpenseApplicationService service) : IConsumer<RecordExpense>
{
    public async Task Consume(ConsumeContext<RecordExpense> context)
    {
        var command = context.Message;
        try
        {
            var (_, error) = await service.RecordExpense(command, context.CancellationToken);
            if (error is not null)
            {
                await context.Publish(new CommandFailed(
                    command.CorrelationId, command.UserId,
                    nameof(RecordExpense), error, DateTimeOffset.UtcNow));
            }
        }
        catch (DomainException ex)
        {
            await context.Publish(new CommandFailed(
                command.CorrelationId, command.UserId,
                nameof(RecordExpense), ex.Message, DateTimeOffset.UtcNow));
        }
    }
}
```

### DI Registration

`ExpenseApplicationService` must be registered in the Worker.Domain composition root. Add registration in `src/WiSave.Expenses.Core.Infrastructure/Extensions.cs`:

```csharp
services.AddScoped<ExpenseApplicationService>();
```

This is added to `AddExpensesInfrastructure()` so both `Worker.Domain` and any future host that calls this method get the service. MassTransit's `AddConsumers` auto-resolves consumer constructor dependencies from the container, so `RecordExpenseHandler` will receive the service via DI with no additional wiring.

### Error handling

- `ExpenseApplicationService` returns `(default, errorMessage)` for validation failures (Account not found, access denied, category missing).
- `ExpenseApplicationService` throws `DomainException` for domain invariant violations — handler catches and publishes `CommandFailed`.
- No new Result type. Tuple keeps it simple. Extract a shared Result type only if more application services appear.

### What stays unchanged

- `SetCategoryLimitHandler` — uses `ICategoryRepository` but only writes to Budget aggregate. The cross-boundary concern is a single repository existence check, not multi-aggregate orchestration. Extract to `BudgetApplicationService` only if more budgeting handlers gain similar patterns.
- All other handlers (UpdateExpense, DeleteExpense, Account handlers, remaining Budget handlers) — single-aggregate, no extraction needed.
- Domain aggregates and contracts — no changes.
- CommandGuard — still used inside the service for validation flow.

### Pattern rule

Extract into an Application Service when a handler orchestrates across multiple aggregates (read or write). Handlers that only query an application repository (like `ICategoryRepository`) alongside a single aggregate may stay inline — use judgment based on complexity. The MassTransit handler becomes a thin transport adapter. Single-aggregate handlers stay as-is.

## Change 2: Documentation Alignment

### AGENTS.md

Add a new `## Application Services` section documenting the pattern:

```markdown
## Application Services

Extract into an Application Service when a handler orchestrates across multiple
aggregates (read or write). Handlers that only query an application repository
(like ICategoryRepository) alongside a single aggregate may stay inline.
The MassTransit handler becomes a thin transport adapter.
```

### CLAUDE.md

Remove the duplicated `## graphify` section. AGENTS.md is the canonical location for graphify configuration. Claude reads both files, so the duplication is unnecessary.

## Testing

**Baseline:** The application test project (`WiSave.Expenses.Core.Application.Tests`) currently contains only `CommandGuardTests`. There are no existing handler-level tests.

- New unit tests for `ExpenseApplicationService` in `tests/WiSave.Expenses.Core.Application.Tests/Accounting/ExpenseApplicationServiceTests.cs` — validate Account ownership, Account active check, Category existence, Subcategory existence, successful Expense creation, and DomainException propagation. Testable without MassTransit by mocking `IAggregateRepository<T>` and `ICategoryRepository`.
- No new handler-level tests for the thin `RecordExpenseHandler` adapter — it contains only delegation and CommandFailed publishing, which is the same pattern as all other handlers.
- `CommandGuardTests` unchanged.

## Out of Scope

- No new Result type or monadic abstraction.
- No changes to other handlers, domain aggregates, or contracts.
- No refactoring of Community 0 structure — the low cohesion is an artifact of shared `IConsumer<T>` interface, not a code problem.
- No changes to migration structure — the weakly-connected nodes are a graph artifact, not an architectural issue.
