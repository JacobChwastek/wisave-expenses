namespace WiSave.Expenses.Core.Infrastructure.EventStore.Forwarding.Configuration;

public sealed class KurrentForwarderOptions
{
    public string GroupName { get; set; } = "expenses-forwarder";
    public bool FromStartWhenCreated { get; set; } = true;
    public int MaxSubscriberCount { get; set; } = 1;
    public string ConsumerStrategyName { get; set; } = "DispatchToSingle";
    public string[] StreamPrefixes { get; set; } = ["funding-account-", "credit-card-account-", "budget-"];
    public int ReconnectDelaySeconds { get; set; } = 5;
    public int MaxReconnectDelaySeconds { get; set; } = 30;
}
