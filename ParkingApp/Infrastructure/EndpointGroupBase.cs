using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParkingApp.Application.Common.Cqrs;

namespace ParkingApp.Api.Infrastructure;

public abstract class EndpointGroupBase
{
    public abstract void Map(IEndpointRouteBuilder app);

    protected static async Task<IResult> ExecuteCommand<TCommand, TResult>(
        ISender sender,
        TCommand command,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        where TCommand : class, IRequestResult<TCommand, TResult>
    {
        var validatorType = typeof(IValidator<TCommand>);
        var validator = (IValidator<TCommand>?)serviceProvider.GetService(validatorType);

        if (validator != null)
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
                    .ToArray();

                return Results.BadRequest(new { Errors = errors });
            }
        }

        var result = await sender.Send<TCommand, TResult>(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : BuildErrorResult(typeof(TCommand).Name, result.Error, serviceProvider);
    }

    protected static async Task<IResult> ExecuteQuery<TQuery, TResult>(
        ISender sender,
        TQuery query,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
        where TQuery : class, IRequestResult<TQuery, TResult>
    {
        var validatorType = typeof(IValidator<TQuery>);
        var validator = (IValidator<TQuery>?)serviceProvider.GetService(validatorType);

        if (validator != null)
        {
            var validationResult = await validator.ValidateAsync(query, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
                    .ToArray();

                return Results.BadRequest(new { Errors = errors });
            }
        }

        var result = await sender.Send<TQuery, TResult>(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : BuildErrorResult(typeof(TQuery).Name, result.Error, serviceProvider);
    }

    private static IResult BuildErrorResult(string requestType, string? message, IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<EndpointGroupBase>>();
        logger.LogWarning(
            "[ResultFailure] {RequestType} failed: {Message}",
            requestType,
            message);

        var errors = (message ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return Results.BadRequest(new { Errors = errors });
    }
}