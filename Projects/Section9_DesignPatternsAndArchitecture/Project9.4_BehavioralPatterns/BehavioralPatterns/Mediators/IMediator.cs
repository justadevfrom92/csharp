using BehavioralPatterns.Requests;

namespace BehavioralPatterns.Mediators;

// MEDIATOR: objects don't call each other directly; they send a message to the mediator,
// which finds the right handler. A controller then needs ONE dependency (IMediator) instead of
// ten services, and the handler can change without the caller knowing.
// (The popular library for this is MediatR; this is the same idea in ~20 lines.)
public interface IMediator
{
    TResult Send<TResult>(IRequest<TResult> request);
}
