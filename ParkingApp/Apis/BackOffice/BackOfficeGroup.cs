using ParkingApp.Api.Infrastructure;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Api.Apis.BackOffice;

/// <summary>
/// Shared base for all BackOffice endpoint groups: the BackOfficeOnly
/// policy plus the init-code query-param parsers.
/// </summary>
public abstract class BackOfficeGroup : EndpointGroupBase
{
    public const string BackOfficePolicy = "BackOfficeOnly";

    protected static bool TryParseVehicleType(string? value, out VehicleTypeEnum? vehicleType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            vehicleType = null;
            return true;
        }

        if (int.TryParse(value, out var number) && Enum.IsDefined(typeof(VehicleTypeEnum), number))
        {
            vehicleType = (VehicleTypeEnum)number;
            return true;
        }

        vehicleType = null;
        return false;
    }

    protected static bool TryParseApprovalStatus(string? value, out ApprovalStatusEnum? approvalStatus)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            approvalStatus = null;
            return true;
        }

        if (int.TryParse(value, out var number) && Enum.IsDefined(typeof(ApprovalStatusEnum), number))
        {
            approvalStatus = (ApprovalStatusEnum)number;
            return true;
        }

        approvalStatus = null;
        return false;
    }
}
