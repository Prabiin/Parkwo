using Microsoft.Extensions.DependencyInjection;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Cqrs;

namespace ParkingApp.Infrastructure.Cqrs;

public sealed class Sender : ISender
{
    private readonly IServiceProvider _serviceProvider;

    public Sender(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<Result<TResponse>> Send<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : class, IRequestResult<TRequest, TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<IRequestResultHandler<TRequest, TResponse>>();
        return handler.Handle(request, cancellationToken);
    }
}