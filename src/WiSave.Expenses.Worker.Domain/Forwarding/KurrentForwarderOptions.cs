namespace WiSave.Expenses.Worker.Domain.Forwarding;

public sealed class KurrentForwarderOptions
{
    public string GroupName { get; set; } = "expenses-forwarder";
    public bool FromStartWhenCreated { get; set; } = true;
    public int MaxSubscriberCount { get; set; } = 1;
    public string ConsumerStrategyName { get; set; } = "DispatchToSingle";
    public string[] StreamPrefixes { get; set; } = ["account-", "expense-", "budget-"];
}
