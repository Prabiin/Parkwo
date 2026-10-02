using ParkingApp.Api.Infrastructure;
using ParkingApp.Application.Common.Cqrs;
using ParkingApp.Application.Payments.Commands.Create;
using ParkingApp.Application.Payments.Commands.ProcessCallback;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Api.Apis;

public class PaymentApi : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        app.MapGroup("payments")
            .MapPost(CreatePayment, "", "")
            .RequireAuthorization();

        // Khalti redirects the payer's browser here with query params. It cannot
        // carry our Bearer token, so it is anonymous and unauthenticated BY
        // DESIGN — authority comes from the server-to-server lookup the handler
        // performs using the stored pidx, never from the query params.
        app.MapGet("/payments/khalti/return", KhaltiReturn)
            .AllowAnonymous();
    }

    private static async Task<IResult> CreatePayment(ISender sender, IServiceProvider serviceProvider,
        CreatePaymentCommand request, CancellationToken cancellationToken)
        => await ExecuteCommand<CreatePaymentCommand, CreatePaymentResponse>(sender,
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
}