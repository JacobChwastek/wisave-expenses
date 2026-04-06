namespace WiSave.Expenses.Core.Application.Tests;

public class CommandGuardTests
{
    [Fact]
    public void Ok_has_not_failed()
    {
        var guard = CommandGuard.Ok;

        Assert.False(guard.HasFailed(out _));
    }

    [Fact]
    public void Require_passing_condition_does_not_fail()
    {
        var guard = CommandGuard.Ok
            .Require(() => true, "should not appear");

        Assert.False(guard.HasFailed(out _));
    }

    [Fact]
    public void Require_failing_condition_returns_error()
    {
        var guard = CommandGuard.Ok
            .Require(() => false, "something went wrong");

        Assert.True(guard.HasFailed(out var error));
        Assert.Equal("something went wrong", error);
    }

    [Fact]
    public void Require_returns_first_failure()
    {
        var guard = CommandGuard.Ok
            .Require(() => true, "first")
            .Require(() => false, "second")
            .Require(() => false, "third");

        Assert.True(guard.HasFailed(out var error));
        Assert.Equal("second", error);
    }

    [Fact]
    public void Require_short_circuits_on_first_failure()
    {
        var thirdEvaluated = false;

        var guard = CommandGuard.Ok
            .Require(() => false, "first fails")
            .Require(() => { thirdEvaluated = true; return true; }, "should not evaluate");

        Assert.True(guard.HasFailed(out var error));
        Assert.Equal("first fails", error);
        Assert.False(thirdEvaluated);
    }

    [Fact]
    public void Require_short_circuits_prevents_null_reference()
    {
        string? value = null;

        var guard = CommandGuard.Ok
            .Require(() => value is not null, "value is null")
            .Require(() => value!.Length > 0, "value is empty");

        Assert.True(guard.HasFailed(out var error));
        Assert.Equal("value is null", error);
    }

    [Fact]
    public void Multiple_passing_conditions_do_not_fail()
    {
        var guard = CommandGuard.Ok
            .Require(() => true, "a")
            .Require(() => true, "b")
            .Require(() => true, "c");

        Assert.False(guard.HasFailed(out _));
    }

    [Fact]
    public async Task RequireAsync_passing_condition_does_not_fail()
    {
        var guard = await CommandGuard.Ok
            .RequireAsync(() => Task.FromResult(true), "should not appear");

        Assert.False(guard.HasFailed(out _));
    }

    [Fact]
    public async Task RequireAsync_failing_condition_returns_error()
    {
        var guard = await CommandGuard.Ok
            .RequireAsync(() => Task.FromResult(false), "async failure");

        Assert.True(guard.HasFailed(out var error));
        Assert.Equal("async failure", error);
    }

    [Fact]
    public async Task RequireAsync_short_circuits_when_already_failed()
    {
        var asyncEvaluated = false;

        var guard = await CommandGuard.Ok
            .Require(() => false, "sync failure")
            .RequireAsync(() => { asyncEvaluated = true; return Task.FromResult(true); }, "should not evaluate");

        Assert.True(guard.HasFailed(out var error));
        Assert.Equal("sync failure", error);
        Assert.False(asyncEvaluated);
    }

    [Fact]
    public async Task Mixed_sync_and_async_chain()
    {
        var guard = await CommandGuard.Ok
            .Require(() => true, "sync ok")
            .RequireAsync(() => Task.FromResult(true), "async ok")
            .Require(() => false, "sync fails here")
            .RequireAsync(() => Task.FromResult(true), "should not reach");

        Assert.True(guard.HasFailed(out var error));
        Assert.Equal("sync fails here", error);
    }

    [Fact]
    public async Task Chained_RequireAsync_calls_all_pass()
    {
        var guard = await CommandGuard.Ok
            .Require(() => true, "a")
            .RequireAsync(() => Task.FromResult(true), "b")
            .Require(() => true, "c")
            .RequireAsync(() => Task.FromResult(true), "d");

        Assert.False(guard.HasFailed(out _));
    }

    [Fact]
    public async Task RequireAsync_with_real_async_work()
    {
        async Task<bool> CheckExistsAsync()
        {
            await Task.Delay(1);
            return false;
        }

        var guard = await CommandGuard.Ok
            .Require(() => true, "sync ok")
            .RequireAsync(CheckExistsAsync, "entity not found");

        Assert.True(guard.HasFailed(out var error));
        Assert.Equal("entity not found", error);
    }
}
