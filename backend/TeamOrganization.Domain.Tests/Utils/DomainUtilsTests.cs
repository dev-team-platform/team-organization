using AwesomeAssertions;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Domain.Enums.Users;
using TeamOrganization.Domain.Exceptions;
using TeamOrganization.Domain.Utils;
using Xunit;

namespace TeamOrganization.Domain.Tests.Utils;

public class DomainUtilsTests
{
    [Theory]
    [InlineData("male", UserGender.Male)]
    [InlineData("FEMALE", UserGender.Female)]
    [InlineData("Unknown", UserGender.Unknown)]
    public void EnumUtils_Parse_ShouldParseEnumName_WhenValueDiffersOnlyByCase(string value, UserGender expected)
    {
        EnumUtils.Parse<UserGender>(value).Should().Be(expected);
    }

    [Fact]
    public void EnumUtils_Parse_ShouldThrowWithValueDetails_WhenValueIsInvalid()
    {
        var exception = ((Action)(() => EnumUtils.Parse<UserGender>("other")))
            .Should().Throw<UnprocessableEntityException>().Which;

        exception.Message.Should().Be("This is not a valid value for the enum.");
        exception.Details!["enumType"].Should().Be(nameof(UserGender));
        exception.Details["value"].Should().Be("other");
    }

    [Fact]
    public void EnumUtils_Parse_ShouldThrowUnprocessableEntityException_WhenTypeIsNotAnEnum()
    {
        var exception = ((Action)(() => EnumUtils.Parse<int>("1")))
            .Should().Throw<UnprocessableEntityException>().Which;

        exception.Message.Should().Be("T must be an enumerated type");
    }

    [Theory]
    [InlineData(UserGender.Male, "Male")]
    [InlineData(UserGender.Female, "Female")]
    public void EnumUtils_ToString_ShouldReturnDeclaredName_WhenValueIsDefined(UserGender value, string expected)
    {
        EnumUtils.ToString(value).Should().Be(expected);
    }

    [Fact]
    public void EnumUtils_ToString_ShouldThrowUnprocessableEntityException_WhenValueIsUndefined()
    {
        var exception = ((Action)(() => EnumUtils.ToString((UserGender)99)))
            .Should().Throw<UnprocessableEntityException>().Which;

        exception.Message.Should().Be("This is not a valid value for the enum.");
        exception.Details!["enumValue"].Should().Be((UserGender)99);
    }

    [Fact]
    public void Sha256Utils_ComputeHexHash_ShouldReturnLowercaseSha256_WhenInputIsHello()
    {
        Sha256Utils.ComputeHexHash("hello").Should().Be(
            "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
    }

    [Theory]
    [InlineData("  Alice ", "  Smith ", "Alice Smith")]
    [InlineData(" Alice ", null, "Alice ")]
    [InlineData("Alice", "   ", "Alice ")]
    public void UserUtils_ConvertDisplayName_ShouldTrimNameParts_WhenLastNameIsPresentOrBlank(
        string firstName,
        string? lastName,
        string expected)
    {
        var user = new User { FirstName = firstName, LastName = lastName };

        UserUtils.ConvertDisplayName(user).Should().Be(expected);
    }
}
