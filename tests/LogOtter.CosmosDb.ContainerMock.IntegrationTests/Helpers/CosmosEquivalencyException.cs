using System.Text.Json;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

public sealed class CosmosEquivalencyException(Exception? realException, Exception? testException, object? realValue, object? testValue)
    : Exception(FormatMessage(realException, testException, realValue, testValue))
{
    public Exception? RealException { get; } = realException;

    public Exception? TestException { get; } = testException;

    public object? RealValue { get; } = realValue;

    public object? TestValue { get; } = testValue;

    private static string FormatMessage(Exception? realException, Exception? testException, object? realValue, object? testValue)
    {
        var message = "An error occurred executing query on";

        if (realException != null && testException != null)
        {
            message += " both Test and Real CosmosDb\n";
        }
        else if (realException != null)
        {
            message += " only Real CosmosDb\n";
        }
        else if (testException != null)
        {
            message += " only Test CosmosDb\n";
        }

        if (realException != null)
        {
            message += "the real exception's message is: " + realException.Message + "\n";
        }

        if (testException != null)
        {
            message += "the test exception's message is: " + testException.Message + "\n";
        }

        if (realValue != null)
        {
            message += "the real value is: " + JsonSerializer.Serialize(realValue) + "\n";
        }

        if (testValue != null)
        {
            message += "the test value is: " + JsonSerializer.Serialize(testValue) + "\n";
        }

        return message;
    }
}
