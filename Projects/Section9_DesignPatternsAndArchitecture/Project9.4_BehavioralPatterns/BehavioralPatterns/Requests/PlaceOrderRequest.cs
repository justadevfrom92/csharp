using BehavioralPatterns.Results;

namespace BehavioralPatterns.Requests;

public record PlaceOrderRequest(string Customer, decimal Total, string ShippingOption, decimal WeightKilograms) : IRequest<PlaceOrderResult>;
