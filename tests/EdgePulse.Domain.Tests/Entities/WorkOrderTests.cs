using EdgePulse.Domain.Entities;
using FluentAssertions;

namespace EdgePulse.Domain.Tests.Entities;

public class WorkOrderTests
{
    private static WorkOrder Create() =>
        WorkOrder.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Fix pump", "tester", "HIGH");

    [Fact]
    public void Create_SetsOpenStatus_AndNumber()
    {
        var wo = Create();
        wo.Status.Should().Be(WorkOrder.StatusOpen);
        wo.Number.Should().StartWith("WO-").And.HaveLength(11);
        wo.Priority.Should().Be("HIGH");
    }

    [Fact]
    public void Lifecycle_OpenStartCompleteFlow_Works()
    {
        var wo = Create();
        wo.Start();
        wo.Status.Should().Be(WorkOrder.StatusInProgress);
        wo.Complete("tech", "replaced bearing", "SKF 6205 x1");
        wo.Status.Should().Be(WorkOrder.StatusCompleted);
        wo.CompletedBy.Should().Be("tech");
        wo.PartsUsed.Should().Be("SKF 6205 x1");
        wo.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_FromOpen_Throws()
    {
        var wo = Create();
        var act = () => wo.Complete("tech", null, null);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void HoldAndResume_Works()
    {
        var wo = Create();
        wo.Start();
        wo.Hold();
        wo.Status.Should().Be(WorkOrder.StatusOnHold);
        wo.Start();
        wo.Status.Should().Be(WorkOrder.StatusInProgress);
    }

    [Fact]
    public void Cancel_FromTerminal_Throws()
    {
        var wo = Create();
        wo.Start();
        wo.Complete("tech", null, null);
        var act = () => wo.Cancel();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Assign_OnCompleted_Throws()
    {
        var wo = Create();
        wo.Start();
        wo.Complete("tech", null, null);
        var act = () => wo.Assign("someone");
        act.Should().Throw<InvalidOperationException>();
    }
}
