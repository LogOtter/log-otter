using FluentAssertions;
using Xunit;

namespace LogOtter.Obfuscate.Tests;

public class ObfuscationTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("bob.bobertson@bobertson.co.uk", "bo****n@bobertson.co.uk")]
    [InlineData("bob.bobertson+test1@bobertson.co.uk", "bo****1@bobertson.co.uk")]
    [InlineData("b@bobertson.co.uk", "b****@bobertson.co.uk")]
    [InlineData("bo@bobertson.co.uk", "b****@bobertson.co.uk")]
    [InlineData("bob@bobertson.co.uk", "b****@bobertson.co.uk")]
    [InlineData("bobb@bobertson.co.uk", "b****@bobertson.co.uk")]
    [InlineData("bob.b@bobertson.co.uk", "b****@bobertson.co.uk")]
    [InlineData("bob.bo@bobertson.co.uk", "bo****o@bobertson.co.uk")]
    [InlineData("bob.bo+bobertson.co.uk", "****")]
    [InlineData("bob.bobertson@bobertson", "****")]
    [InlineData("bob.bobertson@10.0.0.1", "****")]
    [InlineData("bob.bobertson@gmail.com", "bo****n@gmail.com")]
    [InlineData("hello world", "****")]
    public void ObfuscateEmail(string? email, string? expectedOutput)
    {
        var actualOutput = Obfuscate.Email(email);

        actualOutput.Should().Be(expectedOutput);
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData(null, "", "")]
    [InlineData("", null, "")]
    [InlineData("", "", "")]
    [InlineData("Bob", null, "B****")]
    [InlineData("Bob", "", "B****")]
    [InlineData("Bobby", null, "Bo****")]
    [InlineData("Bobby", "", "Bo****")]
    [InlineData(null, "Bobertson", "Bo****")]
    [InlineData("", "Bobertson", "Bo****")]
    [InlineData(null, "May", "M****")]
    [InlineData("", "May", "M****")]
    [InlineData("Bob", "Bobertson", "B**** Bo****")]
    [InlineData("Bobby", "Bobertson", "Bo**** Bo****")]
    [InlineData("Bob", "May", "B**** M****")]
    [InlineData("Bobby", "May", "Bo**** M****")]
    public void ObfuscateName(string? firstName, string? lastName, string? expectedOutput)
    {
        var actualOutput = Obfuscate.Name(firstName, lastName);

        actualOutput.Should().Be(expectedOutput);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData(" ", "")]
    [InlineData("  ", "")]
    [InlineData("\r\n", "")]
    [InlineData("07890123456", "07****56")]
    [InlineData("+447890123456", "+447****56")]
    [InlineData("+4478901", "****")]
    [InlineData("foo", "****")]
    [InlineData("01234", "****")]
    [InlineData("0123456789", "****")]
    public void ObfuscatePhone(string? phoneNumber, string? expectedOutput)
    {
        var actualOutput = Obfuscate.Phone(phoneNumber);

        actualOutput.Should().Be(expectedOutput);
    }
}
