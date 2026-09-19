namespace ParkingApp.Application.Common.Cqrs;

public interface IRequestResult<TRequest, TResponse>
    where TRequest : class, IRequestResult<TRequest, TResponse>
{
}

public interface IRequestResultHandler<TRequest, TResponse>
    where TRequest : class, IRequestResult<TRequest, TResponse>
{
    Task<Result<TResponse>> Handle(TRequest request, CancellationToken cancellationToken = default);
}