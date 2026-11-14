using PipelinePatterns.Models;

namespace PipelinePatterns.Processors;

public interface IPaymentProcessor
{
    string Process(PaymentModel payment);
}
