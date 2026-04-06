using WiSave.Expenses.Core.Domain.SharedKernel;

namespace WiSave.Expenses.Core.Domain.CreditCards.Exceptions;

/// <summary>
/// Base type for credit-card domain invariant violations.
/// </summary>
/// <remarks>
/// Application handlers can continue catching <see cref="DomainException" />
/// while tests and future workflows can catch specific credit-card failures.
/// </remarks>
public abstract class CreditCardDomainException(string message) : DomainException(message);

/// <summary>Thrown when a statement payment application amount is zero or negative.</summary>
public sealed class PaymentApplicationAmountMustBeGreaterThanZeroException()
    : CreditCardDomainException("Payment application amount must be greater than zero.");

/// <summary>Thrown when a statement is created with a negative statement balance.</summary>
public sealed class StatementBalanceCannotBeNegativeException()
    : CreditCardDomainException("Statement balance cannot be negative.");

/// <summary>Thrown when the minimum payment due is negative.</summary>
public sealed class MinimumPaymentDueCannotBeNegativeException()
    : CreditCardDomainException("Minimum payment due cannot be negative.");

/// <summary>Thrown when the minimum payment due is greater than the statement balance.</summary>
public sealed class MinimumPaymentDueCannotExceedStatementBalanceException()
    : CreditCardDomainException("Minimum payment due cannot exceed statement balance.");

/// <summary>Thrown when an unbilled balance value is negative.</summary>
public sealed class UnbilledBalanceCannotBeNegativeException()
    : CreditCardDomainException("Unbilled balance cannot be negative.");

/// <summary>Thrown when statement outstanding balance is negative.</summary>
public sealed class StatementOutstandingBalanceCannotBeNegativeException()
    : CreditCardDomainException("Statement outstanding balance cannot be negative.");

/// <summary>Thrown when statement outstanding balance exceeds the original statement balance.</summary>
public sealed class StatementOutstandingBalanceCannotExceedStatementBalanceException()
    : CreditCardDomainException("Statement outstanding balance cannot exceed statement balance.");

/// <summary>Thrown when a payment application is larger than the target statement outstanding balance.</summary>
public sealed class PaymentApplicationAmountCannotExceedStatementOutstandingBalanceException()
    : CreditCardDomainException("Payment application amount cannot exceed statement outstanding balance.");

/// <summary>Thrown when no statement policy supports the configured card provider and product.</summary>
public sealed class UnsupportedCreditCardStatementPolicyException()
    : CreditCardDomainException("Unsupported credit card statement policy.");

/// <summary>Thrown when an open active statement snapshot is created with a non-positive balance.</summary>
public sealed class ActiveStatementBalanceMustBeGreaterThanZeroException()
    : CreditCardDomainException("Active statement balance must be greater than zero.");

/// <summary>Thrown when an active statement outstanding balance is negative.</summary>
public sealed class ActiveStatementOutstandingBalanceCannotBeNegativeException()
    : CreditCardDomainException("Active statement outstanding balance cannot be negative.");

/// <summary>Thrown when active statement outstanding balance exceeds active statement balance.</summary>
public sealed class ActiveStatementOutstandingBalanceCannotExceedActiveStatementBalanceException()
    : CreditCardDomainException("Active statement outstanding balance cannot exceed active statement balance.");

/// <summary>Thrown when active statement minimum payment due is negative.</summary>
public sealed class ActiveStatementMinimumPaymentDueCannotBeNegativeException()
    : CreditCardDomainException("Active statement minimum payment due cannot be negative.");

/// <summary>Thrown when active statement minimum payment due exceeds active statement balance.</summary>
public sealed class ActiveStatementMinimumPaymentDueCannotExceedActiveStatementBalanceException()
    : CreditCardDomainException("Active statement minimum payment due cannot exceed active statement balance.");

/// <summary>Thrown when a seeded active statement balance is negative.</summary>
public sealed class ActiveStatementBalanceCannotBeNegativeException()
    : CreditCardDomainException("Active statement balance cannot be negative.");

/// <summary>Thrown when a zero active-statement seed also contains minimum-payment or date values.</summary>
public sealed class ZeroActiveStatementSeedCannotIncludeMinimumPaymentOrDatesException()
    : CreditCardDomainException("Zero active statement seed cannot include minimum payment or dates.");

/// <summary>Thrown when a non-zero active-statement seed omits period close date or due date.</summary>
public sealed class ActiveStatementSeedRequiresPeriodCloseDateAndDueDateException()
    : CreditCardDomainException("Active statement seed requires period close date and due date.");

/// <summary>Thrown when a settlement payment amount is zero or negative.</summary>
public sealed class PaymentAmountMustBeGreaterThanZeroException()
    : CreditCardDomainException("Payment amount must be greater than zero.");

/// <summary>Thrown when a statement policy receives a negative current unbilled balance.</summary>
public sealed class CurrentUnbilledBalanceCannotBeNegativeException()
    : CreditCardDomainException("Current unbilled balance cannot be negative.");

/// <summary>Thrown when statement computation is attempted on a date other than the configured closing day.</summary>
public sealed class StatementCanOnlyBeComputedOnConfiguredClosingDayException()
    : CreditCardDomainException("Statement can only be computed on the configured closing day.");

/// <summary>Thrown when seeding is attempted after statement history already exists.</summary>
public sealed class CannotSeedCreditCardStateAfterStatementHistoryExistsException()
    : CreditCardDomainException("Cannot seed credit card state after statement history exists.");

/// <summary>Thrown when the same statement period was already issued with different values.</summary>
public sealed class StatementAlreadyIssuedWithDifferentValuesException()
    : CreditCardDomainException("Statement already issued with different values.");

/// <summary>Thrown when a settlement transfer amount is zero or negative.</summary>
public sealed class SettlementAmountMustBeGreaterThanZeroException()
    : CreditCardDomainException("Settlement amount must be greater than zero.");

/// <summary>Thrown when replaying or retrying a transfer with allocations different from the original application.</summary>
public sealed class SettlementTransferAlreadyAppliedWithDifferentAllocationsException()
    : CreditCardDomainException("Settlement transfer already applied with different allocations.");

/// <summary>Thrown when a command or event references a statement that does not exist in the aggregate.</summary>
public sealed class StatementNotFoundException()
    : CreditCardDomainException("Statement not found.");

/// <summary>Thrown when allocation decisions exceed the settlement transfer amount.</summary>
public sealed class PaymentApplicationDecisionsCannotExceedSettlementAmountException()
    : CreditCardDomainException("Payment application decisions cannot exceed settlement amount.");

/// <summary>Thrown when a mutation is attempted against a closed credit-card account.</summary>
public sealed class CannotModifyClosedCreditCardAccountException()
    : CreditCardDomainException("Cannot modify a closed credit card account.");

/// <summary>Thrown when one settlement transfer contains more than one allocation for the same statement.</summary>
public sealed class PaymentApplicationDecisionsMustBeUniquePerStatementException()
    : CreditCardDomainException("Payment application decisions must be unique per statement.");

/// <summary>Thrown when a credit-card account name is missing or whitespace.</summary>
public sealed class CreditCardAccountNameRequiredException()
    : CreditCardDomainException("Credit card account name is required.");

/// <summary>Thrown when a credit-card configuration does not include a settlement funding account.</summary>
public sealed class SettlementAccountRequiredException()
    : CreditCardDomainException("Settlement account is required.");

/// <summary>Thrown when a credit-card limit is zero or negative.</summary>
public sealed class CreditLimitMustBeGreaterThanZeroException()
    : CreditCardDomainException("Credit limit must be greater than zero.");

/// <summary>Thrown when a seeded active statement snapshot does not include a statement identifier.</summary>
public sealed class ActiveStatementSeedRequiresStatementIdException()
    : CreditCardDomainException("Active statement seed requires statement id.");
