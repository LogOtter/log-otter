using FluentAssertions;
using Xunit;

namespace LogOtter.ShortGuid.Tests;

public class GuidExtensionsTests
{
    [Fact]
    public void KnownShortGuid_CorrectlyConverts()
    {
        var guid = Guid.Parse("0a3d2017-7581-4831-ba3c-a46556c87304");
        var shortGuid = guid.ToShortString();

        shortGuid.Should().Be("sZCkPjP1rt2B8QVN4GhvctDB");
    }

    [Theory]
    [InlineData("0a3d2017-7581-4831-ba3c-a46556c87304")]
    [InlineData("04030201-0605-0007-0000-000c0d0e0f10")]
    [InlineData("00000035-0000-0000-0000-00000d0e0f10")]
    [InlineData("CA084F5E-AD86-4A71-B722-C857A50F4B47")]
    [InlineData("F15E25C2-AD26-4CE9-83CE-E41B2B9557E4")]
    [InlineData("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")]
    public void TheAlgorithmShouldBeReversibleWithNoLossOfInformation(string guidString)
    {
        var guidValue = Guid.Parse(guidString);

        var shortString = guidValue.ToShortString();
        var reversed = ToGuidFromShortString(shortString);

        reversed.Should().Be(guidValue);
    }

    [Fact]
    public void RandomShortGuid_ShouldHaveCorrectLength()
    {
        var shortGuid = Guid.NewGuid().ToShortString();

        shortGuid.Should().NotBeNullOrEmpty();
        shortGuid.Should().HaveLength(24);
    }

    [Fact]
    public void DoesNotDropCharactersWhenSecondHalfLeadsInWithZero()
    {
        var guid = Guid.Parse("04030201-0605-0007-0000-000c0d0e0f10");
        var shortGuid = guid.ToShortString();

        shortGuid.Should().NotBeNullOrEmpty();
        shortGuid.Should().HaveLength(24);
    }

    [Fact]
    public void DoesNotDropCharactersWhenThereIsARemainderThatIsLessThanDoubleTheAlphabetSize()
    {
        var guid = Guid.Parse("00000035-0000-0000-0000-00000d0e0f10");
        var shortGuid = guid.ToShortString();

        shortGuid.Should().NotBeNullOrEmpty();
        shortGuid.Should().HaveLength(24);
    }

    [Fact]
    public void GuidsWithSwappedParts_ShouldGenerateDifferentShortGuids()
    {
        var firstSection = BitConverter.GetBytes(0UL);
        var secondSection = BitConverter.GetBytes(2UL);

        var guid1 = new Guid(firstSection.Concat(secondSection).ToArray());
        var guid2 = new Guid(secondSection.Concat(firstSection).ToArray());

        guid1.Should().NotBe(guid2);

        var shortGuid1 = guid1.ToShortString();
        var shortGuid2 = guid2.ToShortString();

        shortGuid1.Should().NotBe(shortGuid2);
    }

    [Theory]
    [InlineData("sZCkPjP1rt2B8QVN4GhvctDB", true, "is valid")]
    [InlineData("sZCkPjP1rt2B8QVN4GhvctDBB", false, "too long")]
    [InlineData("", false, "too short")]
    [InlineData("AZCkPjP1rt2B8QVN4GhvctDB", false, "invalid character")]
    public void ShortStringsCanBeValidated(string shortString, bool isValid, string reason)
    {
        var valid = shortString.IsValidShortGuid();

        valid.Should().Be(isValid, reason);
    }

    /// <summary>
    ///     In order to be satisfied that no entropy is lost, if we can safely reverse the string then this is true.
    ///     This should not be put in the main code base as we don't want to encourage 2 way conversions from the short string
    /// </summary>
    private static Guid ToGuidFromShortString(string input)
    {
        Assert.Equal(24, input.Length);

        var part1 = new string(input[..12].Reverse().ToArray());
        var part2 = new string(input[12..].Reverse().ToArray());

        static byte[] ParsePart(string part)
        {
            ulong accumulator = 0;

            while (part.Length > 0)
            {
                if (accumulator > 0)
                {
                    accumulator *= (ulong)GuidExtensions.Alphabet.Length;
                }

                var value = GuidExtensions.Alphabet.IndexOf(part[0]);
                Assert.True(value >= 0);
                accumulator += (ulong)value;
                part = part.Remove(0, 1);
            }

            return BitConverter.GetBytes(accumulator);
        }

        var part1Bytes = ParsePart(part1);
        var part2Bytes = ParsePart(part2);

        var bytes = part1Bytes.Concat(part2Bytes);

        return new Guid(bytes.ToArray());
    }
}
