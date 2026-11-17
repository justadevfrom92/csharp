using PipelinePatterns.Decorators;
using PipelinePatterns.Filters;
using PipelinePatterns.Handlers;
using PipelinePatterns.Models;
using PipelinePatterns.Pipelines;
using PipelinePatterns.Processors;
using PipelinePatterns.Variations;

namespace PipelinePatterns.Tests;

// Each pipeline pattern's original vs a lighter variation (PipelinePatterns/Variations/).
public class VariationsTest
{
    private const string Line = "  paid by  ann@example.com card 4111-1111-1111-1234 ";

    [Fact]
    public void Filters_PositivePositive_AggregateOverFunctionsOrObjects_SameAsTextPipeline()
    {
        string original = new TextPipeline().Add(new TrimWhitespaceFilter()).Add(new MaskEmailFilter()).Add(new MaskCardNumberFilter()).Run(Line);

        string functions = TextPipelineVariation.WithAggregate(Line, [new TrimWhitespaceFilter().Apply, new MaskEmailFilter().Apply, new MaskCardNumberFilter().Apply]);
        string objects = TextPipelineVariation.WithFilterObjects(Line, [new TrimWhitespaceFilter(), new MaskEmailFilter(), new MaskCardNumberFilter()]);

        Assert.Equal(original, functions);
        Assert.Equal(original, objects);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(1_000)]
    [InlineData(1_000.01)]
    [InlineData(100_000)]
    [InlineData(100_000.01)]
    public void Chain_PositivePositive_TableGivesTheSameAnswersAsLinkedHandlers(decimal amount)
    {
        var teamLead = new TeamLeadApprovalHandler();
        teamLead.SetNext(new ManagerApprovalHandler()).SetNext(new DirectorApprovalHandler());
        var expense = new ExpenseModel("Ann", amount, "x");

        Assert.Equal(teamLead.Approve(expense), ApprovalTableVariation.Approve(expense));
    }

    [Fact]
    public void Decorators_PositivePositive_FunctionPipeline_LogsAndValidatesLikeTheClasses()
    {
        var classLog = new List<string>();
        var functionLog = new List<string>();
        IPaymentProcessor classes = new LoggingPaymentProcessorDecorator(new ValidationPaymentProcessorDecorator(new FlakyPaymentProcessor(0)), classLog);
        Func<PaymentModel, string> functions = PaymentMiddlewareVariation.Build(
            [PaymentMiddlewareVariation.Logging(functionLog), PaymentMiddlewareVariation.Validation()],
            payment => new FlakyPaymentProcessor(0).Process(payment));

        Assert.Equal(classes.Process(new PaymentModel("A-1", 10m)), functions(new PaymentModel("A-1", 10m)));
        Assert.Equal(classLog, functionLog);
    }

    [Fact]
    public void Decorators_PositiveNegative_OrderOfSteps_ChangesWhatIsLogged()
    {
        var logFirst = new List<string>();
        var validateFirst = new List<string>();
        Func<PaymentModel, string> loggingOutside = PaymentMiddlewareVariation.Build([PaymentMiddlewareVariation.Logging(logFirst), PaymentMiddlewareVariation.Validation()], payment => "paid");
        Func<PaymentModel, string> validationOutside = PaymentMiddlewareVariation.Build([PaymentMiddlewareVariation.Validation(), PaymentMiddlewareVariation.Logging(validateFirst)], payment => "paid");

        Assert.Throws<ArgumentOutOfRangeException>(() => loggingOutside(new PaymentModel("A-2", -5m)));
        Assert.Throws<ArgumentOutOfRangeException>(() => validationOutside(new PaymentModel("A-2", -5m)));

        Assert.Equal(["start A-2"], logFirst);   // logging ran before the rejection
        Assert.Empty(validateFirst);             // rejected before logging ever saw it
    }

    [Fact]
    public void Variations_NegativeNegative_NullOrZero_Rejected()
    {
        Assert.Throws<ArgumentNullException>(() => TextPipelineVariation.WithAggregate(null!, []));
        Assert.Throws<ArgumentNullException>(() => ApprovalTableVariation.Approve(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => ApprovalTableVariation.Approve(new ExpenseModel("Ann", 0m, "x")));
    }
}
