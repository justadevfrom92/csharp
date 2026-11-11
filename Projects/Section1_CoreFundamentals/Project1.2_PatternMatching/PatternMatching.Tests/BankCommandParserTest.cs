using PatternMatching.Parsers;

namespace PatternMatching.Tests;

public class BankCommandParserTest
{
    [Theory]
    [InlineData("help", "Commands: balance | deposit <amount> | withdraw <amount>")]
    [InlineData("balance", "Showing balance")]
    [InlineData("deposit 25.50", "Depositing 25.50")]
    [InlineData("withdraw 10", "Withdrawing 10.00")]
    public void Parse_PositivePositive_ValidCommand_IsUnderstood(string line, string expected)
    {
        Assert.Equal(expected, BankCommandParser.Parse(line.Split(' ')));
    }

    [Theory]
    [InlineData("deposit -5")]   // right shape, but the amount breaks the rule
    [InlineData("withdraw 0")]
    [InlineData("deposit abc")]
    public void Parse_PositiveNegative_KnownCommandWithBadAmount_IsRefused(string line)
    {
        Assert.Throws<InvalidOperationException>(() => BankCommandParser.Parse(line.Split(' ')));
    }

    [Fact]
    public void Parse_NegativeNegative_NullEmptyOrUnknownCommand_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => BankCommandParser.Parse(null));
        Assert.Throws<ArgumentException>(() => BankCommandParser.Parse([]));
        Assert.Throws<ArgumentException>(() => BankCommandParser.Parse(["transfer", "10", "bob"]));
        Assert.Throws<ArgumentException>(() => BankCommandParser.Parse(["deposit"])); // missing amount
    }
}
