namespace OrderManagement.Domain.Tests;

public class OrderStateMachineTests
{
    private static readonly TimeProvider Clock = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private static Order Draft() => TestData.DraftOrder();

    [Fact]
    public void Submit_FromDraft_Succeeds_AndRecordsTimestamp()
    {
        var order = Draft();
        order.Submit(Clock).Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Submitted);
        order.SubmittedAt.Should().HaveValue();
    }

    [Fact]
    public void FullHappyPath_DraftToDelivered()
    {
        var order = Draft();
        order.Submit(Clock).Should().BeSuccess();
        order.Approve(Clock).Should().BeSuccess();
        order.Ship(Clock).Should().BeSuccess();
        order.ShippedAt.Should().HaveValue();
        order.Deliver(Clock).Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public void Approve_FromDraft_IsInvalidTransition()
    {
        var order = Draft();
        order.Approve(Clock).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Ship_FromSubmitted_IsInvalidTransition()
    {
        var order = Draft();
        order.Submit(Clock).Discard();
        order.Ship(Clock).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Cancel_FromDraft_Succeeds()
    {
        var order = Draft();
        order.Cancel(Clock).Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromApproved_Succeeds()
    {
        var order = Draft();
        order.Submit(Clock).Discard();
        order.Approve(Clock).Discard();
        order.ReservesStock.Should().BeTrue();
        order.Cancel(Clock).Should().BeSuccess();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromShipped_IsInvalidTransition()
    {
        var order = Draft();
        order.Submit(Clock).Discard();
        order.Approve(Clock).Discard();
        order.Ship(Clock).Discard();
        order.Cancel(Clock).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Cancel_FromDelivered_IsInvalidTransition()
    {
        var order = Draft();
        order.Submit(Clock).Discard();
        order.Approve(Clock).Discard();
        order.Ship(Clock).Discard();
        order.Deliver(Clock).Discard();
        order.Cancel(Clock).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void ReservesStock_IsTrue_OnlyForSubmittedOrApproved()
    {
        var order = Draft();
        order.ReservesStock.Should().BeFalse();
        order.Submit(Clock).Discard();
        order.ReservesStock.Should().BeTrue();
    }
}
