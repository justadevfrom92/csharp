using BehavioralPatterns.Requests;

namespace BehavioralPatterns.Handlers;

// Handles ONE kind of request. There's exactly one handler per request type.
public interface IRequestHandler<TRequest, TResult> where TRequest : IRequest<TResult>
{
    TResult Handle(TRequest request);
}
