using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Payments.Commands.Create;
using ParkingApp.Application.Payments.Commands.CreateOverstay;
using ParkingApp.Application.Payments.Commands.ProcessCallback;
using ParkingApp.Application.Payments.Commands.ProcessOverstayCallback;
using ParkingApp.Application.Payments.Queries.GetPaymentGateways;
using ParkingApp.Domain.Common.Enums;
using ParkingApp.Infrastructure.Auth;

namespace ParkingApp.Api.Apis;

public class PaymentApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        app.MapGroup("payments")
            .MapPost(CreatePayment, "", "")
            // A separate route rather than a flag on the same one: prepaid and
            // overstay payments live in different tables, have different guards
            // and settle without touching each other's booking state.
            .MapPost(CreateOverstayPayment, "overstay", "")
            .RequireAuthorization(ProfileCompleteRequirement.PolicyName);

        // Its own group so it is only signed in, not profile-complete: this is
        // what the checkout screen asks for before any payment can be started,
        // and the answer (which gateways this deployment has credentials for)
        // is not a thing a rider's profile gates.
        app.MapGroup("payments")
            .MapGet(GetPaymentGateways, "gateways", "")
            .RequireAuthorization();

        // Khalti redirects the payer's browser here with query params. It cannot
        // carry our Bearer token, so it is anonymous and unauthenticated BY
        // DESIGN — authority comes from the server-to-server lookup the handler
        // performs using the stored pidx, never from the query params.
        app.MapGet("/payments/khalti/return", KhaltiReturn)
            .AllowAnonymous();

        // Same reasoning, different table: the prepaid return handler settles by
        // moving the booking, which must never happen for an overstay.
        app.MapGet("/payments/khalti/overstay/return", KhaltiOverstayReturn)
            .AllowAnonymous();
    }

    private static async Task<IResult> CreatePayment(ISender sender, IServiceProvider serviceProvider,
        CreatePaymentCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CreatePaymentCommand, CreatePaymentResponse>(sender,
            request, serviceProvider, cancellationToken);

    private static Task<IResult> GetPaymentGateways(ISender sender, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => ExecuteQuery<GetPaymentGatewaysQuery, GetPaymentGatewaysResponse>(sender,
            new GetPaymentGatewaysQuery(), serviceProvider, cancellationToken);

    private static async Task<IResult> CreateOverstayPayment(ISender sender, IServiceProvider serviceProvider,
        CreateOverstayPaymentCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CreateOverstayPaymentCommand, CreateOverstayPaymentResponse>(sender,
            request, serviceProvider, cancellationToken);

    private static async Task<IResult> KhaltiReturn(
        ISender sender,
        IServiceProvider serviceProvider,
        string? pidx,
        string? status,
        string? amount,
        CancellationToken cancellationToken)
    {
        // Khalti reports the amount in the redirect, but only as a hint: the
        // handler re-derives the truth from the gateway lookup.
        long? amountPaisa = null;
        if (!string.IsNullOrWhiteSpace(amount)
            && long.TryParse(amount, out var parsed))
            amountPaisa = parsed;

        return await ExecuteQuery<ProcessPaymentCallbackCommand, ProcessPaymentCallbackResponse>(
            sender,
            new ProcessPaymentCallbackCommand(
                PaymentGatewayEnum.Khalti,
                pidx,
                status,
                amountPaisa),
            serviceProvider,
            cancellationToken);
    }

    private static async Task<IResult> KhaltiOverstayReturn(
        ISender sender,
        IServiceProvider serviceProvider,
        string? pidx,
        string? status,
        string? amount,
        CancellationToken cancellationToken)
    {
        long? amountPaisa = null;
        if (!string.IsNullOrWhiteSpace(amount)
            && long.TryParse(amount, out var parsed))
            amountPaisa = parsed;

        return await ExecuteQuery<ProcessOverstayPaymentCallbackCommand, ProcessOverstayPaymentCallbackResponse>(
            sender,
            new ProcessOverstayPaymentCallbackCommand(
                PaymentGatewayEnum.Khalti,
                pidx,
                status,
                amountPaisa),
            serviceProvider,
            cancellationToken);
    }
}
