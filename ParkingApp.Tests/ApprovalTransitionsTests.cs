using ParkingApp.Application.Common.Helpers;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Tests;

public class ApprovalTransitionsTests
{
    [Theory]
    [InlineData(ApprovalStatusEnum.Pending, ApprovalStatusEnum.Verified)]
    [InlineData(ApprovalStatusEnum.Pending, ApprovalStatusEnum.UnderReview)]
    [InlineData(ApprovalStatusEnum.Pending, ApprovalStatusEnum.Rejected)]
    [InlineData(ApprovalStatusEnum.UnderReview, ApprovalStatusEnum.Verified)]
    [InlineData(ApprovalStatusEnum.UnderReview, ApprovalStatusEnum.Rejected)]
    [InlineData(ApprovalStatusEnum.Rejected, ApprovalStatusEnum.UnderReview)]
    [InlineData(ApprovalStatusEnum.Verified, ApprovalStatusEnum.UnderReview)]
    public void Allows_Legal_Moves(ApprovalStatusEnum from, ApprovalStatusEnum to)
    {
        Assert.True(ApprovalTransitions.IsAllowed(from, to));
    }

    [Theory]
    [InlineData(ApprovalStatusEnum.Pending, ApprovalStatusEnum.Pending)]
    [InlineData(ApprovalStatusEnum.Verified, ApprovalStatusEnum.Verified)]
    [InlineData(ApprovalStatusEnum.Verified, ApprovalStatusEnum.Rejected)]
    [InlineData(ApprovalStatusEnum.Verified, ApprovalStatusEnum.Pending)]
    [InlineData(ApprovalStatusEnum.Rejected, ApprovalStatusEnum.Verified)]
    [InlineData(ApprovalStatusEnum.Rejected, ApprovalStatusEnum.Pending)]
    [InlineData(ApprovalStatusEnum.Rejected, ApprovalStatusEnum.Rejected)]
    [InlineData(ApprovalStatusEnum.UnderReview, ApprovalStatusEnum.Pending)]
    [InlineData(ApprovalStatusEnum.UnderReview, ApprovalStatusEnum.UnderReview)]
    public void Blocks_Same_And_Illegal_Moves(ApprovalStatusEnum from, ApprovalStatusEnum to)
    {
        Assert.False(ApprovalTransitions.IsAllowed(from, to));
    }
}
