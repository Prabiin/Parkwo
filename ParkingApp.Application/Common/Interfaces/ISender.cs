using ParkingApp.Application.Common;

namespace ParkingApp.Application.Common.Cqrs;

public interface ISender
{
    Task<Result<TResponse>> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : class, IRequestResult<TRequest, TResponse>;
}