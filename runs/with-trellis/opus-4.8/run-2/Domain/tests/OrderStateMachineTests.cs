namespace Domain.Tests;

using OrderManagement.Domain;

public class OrderStateMachineTests
{
    private static readonly FixedTimeProvider Clock = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

    private static Order Draft() => Build.DraftOrder();

    private static Order Submitted()
    {
        var o = Draft();
        o.Submit(Clock).Unwrap();
        return o;
    }

    private static Order Approved()
    {
        var o = Submitted();
        o.Approve(Clock).Unwrap();
        return o;
    }

    private static Order Shipped()
    {
        var o = Approved();
        o.Ship(Clock).Unwrap();
        return o;
    }

    private static Order Delivered()
    {
        var o = Shipped();
        o.Deliver(Clock).Unwrap();
        return o;
    }

    [Fact]
    public void Full_happy_path_transitions()
    {
        var o = Draft();

        o.Submit(Clock).Should().BeSuccess();
        o.Status.Should().Be(OrderStatus.Submitted);
        o.SubmittedAt.Should().HaveValue();

        o.Approve(Clock).Should().BeSuccess();
        o.Status.Should().Be(OrderStatus.Approved);

        o.Ship(Clock).Should().BeSuccess();
        o.Status.Should().Be(OrderStatus.Shipped);
        o.ShippedAt.Should().HaveValue();

        o.Deliver(Clock).Should().BeSuccess();
        o.Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public void Cancel_from_draft_succeeds_and_does_not_release_stock()
    {
        var o = Draft();
        o.ReleasesStockOnCancel.Should().BeFalse();

        o.Cancel(Clock).Should().BeSuccess();
        o.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_from_submitted_releases_stock()
    {
        var o = Submitted();
        o.ReleasesStockOnCancel.Should().BeTrue();

        o.Cancel(Clock).Should().BeSuccess();
        o.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_from_approved_releases_stock()
    {
        var o = Approved();
        o.ReleasesStockOnCancel.Should().BeTrue();

        o.Cancel(Clock).Should().BeSuccess();
        o.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_from_shipped_fails()
    {
        var o = Shipped();

        o.Cancel(Clock).Should().BeFailureOfType<Error.InvalidInput>();
        o.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void Cancel_from_delivered_fails()
    {
        var o = Delivered();

        o.Cancel(Clock).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Invalid_transition_draft_to_approve_fails()
    {
        var o = Draft();

        o.Approve(Clock).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Invalid_transition_draft_to_ship_fails()
    {
        Draft().Ship(Clock).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Invalid_transition_submitted_to_ship_fails()
    {
        Submitted().Ship(Clock).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Invalid_transition_approved_to_deliver_fails()
    {
        Approved().Deliver(Clock).Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Invalid_transition_shipped_to_approve_fails()
    {
        Shipped().Approve(Clock).Should().BeFailureOfType<Error.InvalidInput>();
    }
}
