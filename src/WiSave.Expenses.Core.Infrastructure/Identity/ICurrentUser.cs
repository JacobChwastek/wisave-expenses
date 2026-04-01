namespace WiSave.Expenses.Core.Infrastructure.Identity;

public interface ICurrentUser
{
    string UserId { get; }
    string Email { get; }
}
