using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Common.Interfaces;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Application.Payments.Queries.GetPaymentGateways;

/// <summary>
/// Which payment methods the checkout screen should offer. Only registered
/// gateways come back — if no credentials are configured, the list is empty
/// and the app shows payments as unavailable.
/// </summary>
public sealed record GetPaymentGatewaysQuery
    : IRequestResult<GetPaymentGatewaysQuery, GetPaymentGatewaysResponse>;

public sealed class GetPaymentGatewaysQueryHandler(
    IEnumerable<IPaymentGateway> registered)
    : IRequestResultHandler<GetPaymentGatewaysQuery, GetPaymentGatewaysResponse>
{
    public Task<Result<GetPaymentGatewaysResponse>> Handle(
        GetPaymentGatewaysQuery request,
        CancellationToken cancellationToken = default)
    {
        var options = registered
            .Select(g => g.Gateway)
            .OrderBy(g => g)
            .Select(g => new PaymentGatewayOption(
                g,
                g.ToDescription(),
                $"Pay with {g.ToDescription()}."))
            .ToList();

        return Task.FromResult(
            Result<GetPaymentGatewaysResponse>.Success(
                new GetPaymentGatewaysResponse(options)));
    }
}
