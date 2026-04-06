using MassTransit;
using Microsoft.EntityFrameworkCore;
using WiSave.Expenses.Contracts.Events.CreditCards;
using WiSave.Expenses.Projections.ReadModels;

namespace WiSave.Expenses.Projections.EventHandlers;

public sealed class CreditCardAccountEventHandler(ProjectionsDbContext db) :
    IConsumer<CreditCardAccountOpened>,
    IConsumer<CreditCardAccountUpdated>,
    IConsumer<CreditCardAccountClosed>,
    IConsumer<CreditCardStateSeeded>,
    IConsumer<CreditCardStatementIssued>,
    IConsumer<CreditCardStatementPaymentApplied>
{
    public Task Consume(ConsumeContext<CreditCardAccountOpened> context)
    {
        var message = context.Message;

        db.CreditCardAccounts.Add(new CreditCardAccountReadModel
        {
            Id = message.CreditCardAccountId,
            UserId = message.UserId,
            Name = message.Name,
            Currency = message.Currency.ToString(),
            SettlementAccountId = message.SettlementAccountId,
            BankProvider = message.BankProvider.ToString(),
            ProductCode = message.ProductCode,
            CreditLimit = message.CreditLimit,
            StatementClosingDay = message.StatementClosingDay,
            GracePeriodDays = message.GracePeriodDays,
            Color = message.Color,
            LastFourDigits = message.LastFourDigits,
            IsActive = true,
            CreatedAt = message.Timestamp,
        });

        return Task.CompletedTask;
    }

    public async Task Consume(ConsumeContext<CreditCardAccountUpdated> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var account = await db.CreditCardAccounts.FindAsync([message.CreditCardAccountId], ct);
        if (account is null) return;

        account.Name = message.Name;
        account.Currency = message.Currency.ToString();
        account.SettlementAccountId = message.SettlementAccountId;
        account.BankProvider = message.BankProvider.ToString();
        account.ProductCode = message.ProductCode;
        account.CreditLimit = message.CreditLimit;
        account.StatementClosingDay = message.StatementClosingDay;
        account.GracePeriodDays = message.GracePeriodDays;
        account.Color = message.Color;
        account.LastFourDigits = message.LastFourDigits;
        account.UpdatedAt = message.Timestamp;
    }

    public async Task Consume(ConsumeContext<CreditCardAccountClosed> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var account = await db.CreditCardAccounts.FindAsync([message.CreditCardAccountId], ct);
        if (account is null) return;

        account.IsActive = false;
        account.UpdatedAt = message.Timestamp;
    }

    public async Task Consume(ConsumeContext<CreditCardStateSeeded> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var account = await db.CreditCardAccounts.FindAsync([message.CreditCardAccountId], ct);
        if (account is null) return;

        account.UnbilledBalance = message.UnbilledBalance;
        account.UpdatedAt = message.Timestamp;

        if (message.ActiveStatementBalance == 0m)
        {
            account.ActiveStatementBalance = null;
            account.ActiveStatementOutstandingBalance = null;
            account.ActiveStatementMinimumPaymentDue = null;
            account.ActiveStatementPeriodCloseDate = null;
            account.ActiveStatementDueDate = null;
            return;
        }

        var periodCloseDate = message.ActiveStatementPeriodCloseDate
            ?? throw new InvalidOperationException("Seeded active statement requires period close date.");
        var dueDate = message.ActiveStatementDueDate
            ?? throw new InvalidOperationException("Seeded active statement requires due date.");
        var statementId = message.ActiveStatementId ?? "stmt-1";

        account.ActiveStatementBalance = message.ActiveStatementBalance;
        account.ActiveStatementOutstandingBalance = message.ActiveStatementBalance;
        account.ActiveStatementMinimumPaymentDue = message.ActiveStatementMinimumPaymentDue;
        account.ActiveStatementPeriodCloseDate = periodCloseDate;
        account.ActiveStatementDueDate = dueDate;

        await UpsertStatementAsync(
            message.CreditCardAccountId,
            statementId,
            periodCloseDate.AddMonths(-1).AddDays(1),
            periodCloseDate,
            periodCloseDate,
            dueDate,
            message.ActiveStatementBalance,
            message.ActiveStatementBalance,
            message.ActiveStatementMinimumPaymentDue,
            "SEEDED",
            "SEEDED",
            message.Timestamp,
            ct);
    }

    public async Task Consume(ConsumeContext<CreditCardStatementIssued> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var account = await db.CreditCardAccounts.FindAsync([message.CreditCardAccountId], ct);
        if (account is not null)
        {
            account.ActiveStatementBalance = message.StatementBalance;
            account.ActiveStatementOutstandingBalance = message.StatementBalance;
            account.ActiveStatementMinimumPaymentDue = message.MinimumPaymentDue;
            account.ActiveStatementPeriodCloseDate = message.PeriodTo;
            account.ActiveStatementDueDate = message.DueDate;
            account.UnbilledBalance = message.UnbilledBalanceAfterIssue;
            account.UpdatedAt = message.Timestamp;
        }

        await UpsertStatementAsync(
            message.CreditCardAccountId,
            message.StatementId,
            message.PeriodFrom,
            message.PeriodTo,
            message.StatementDate,
            message.DueDate,
            message.StatementBalance,
            message.StatementBalance,
            message.MinimumPaymentDue,
            message.PolicyCode,
            message.PolicyVersion,
            message.Timestamp,
            ct);
    }

    public async Task Consume(ConsumeContext<CreditCardStatementPaymentApplied> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var statement = await db.CreditCardStatements.FindAsync([message.CreditCardAccountId, message.StatementId], ct);
        if (statement is not null)
        {
            statement.OutstandingBalance = message.StatementOutstandingBalanceAfterApplication;
            statement.UpdatedAt = message.Timestamp;
        }

        var account = await db.CreditCardAccounts.FindAsync([message.CreditCardAccountId], ct);
        if (account is not null && statement is not null && account.ActiveStatementPeriodCloseDate == statement.PeriodTo)
        {
            account.ActiveStatementOutstandingBalance = message.StatementOutstandingBalanceAfterApplication;
            account.UpdatedAt = message.Timestamp;
        }
    }

    private async Task UpsertStatementAsync(
        string creditCardAccountId,
        string statementId,
        DateOnly periodFrom,
        DateOnly periodTo,
        DateOnly statementDate,
        DateOnly dueDate,
        decimal statementBalance,
        decimal outstandingBalance,
        decimal minimumPaymentDue,
        string policyCode,
        string policyVersion,
        DateTimeOffset timestamp,
        CancellationToken ct)
    {
        var statement = await db.CreditCardStatements.FindAsync([creditCardAccountId, statementId], ct);
        if (statement is null)
        {
            db.CreditCardStatements.Add(new CreditCardStatementReadModel
            {
                Id = statementId,
                CreditCardAccountId = creditCardAccountId,
                PeriodFrom = periodFrom,
                PeriodTo = periodTo,
                StatementDate = statementDate,
                DueDate = dueDate,
                StatementBalance = statementBalance,
                OutstandingBalance = outstandingBalance,
                MinimumPaymentDue = minimumPaymentDue,
                PolicyCode = policyCode,
                PolicyVersion = policyVersion,
                CreatedAt = timestamp,
            });
            return;
        }

        statement.PeriodFrom = periodFrom;
        statement.PeriodTo = periodTo;
        statement.StatementDate = statementDate;
        statement.DueDate = dueDate;
        statement.StatementBalance = statementBalance;
        statement.OutstandingBalance = outstandingBalance;
        statement.MinimumPaymentDue = minimumPaymentDue;
        statement.PolicyCode = policyCode;
        statement.PolicyVersion = policyVersion;
        statement.UpdatedAt = timestamp;
    }
}
