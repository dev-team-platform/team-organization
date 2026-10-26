using AwesomeAssertions;
using TeamOrganization.Api.Utils;
using TeamOrganization.Domain.Enums.Common;
using TeamOrganization.Domain.Exceptions;
using Xunit;

namespace TeamOrganization.Api.Tests.Utils;

public class FilterGroupConverterTests
{
    [Fact]
    public void Build_ShouldReturnNull_WhenCriterionCountIsZero()
    {
        FilterGroupConverter.Build("{0}", 0).Should().BeNull();
    }

    [Fact]
    public void Build_ShouldGroupEveryCriterionWithAnd_WhenPatternIsMissing()
    {
        var group = FilterGroupConverter.Build(null, 3)!;

        group.Logic.Should().Be(FilterGroupLogic.And);
        group.Children.Select(child => child.CriterionIndex).Should().Equal(0, 1, 2);
    }

    [Fact]
    public void Build_ShouldRespectOperatorPrecedenceAndAppendUnreferencedCriteria_WhenPatternOmitsCriterion()
    {
        var group = FilterGroupConverter.Build("{0} OR {1} AND {2}", 4)!;

        group.Logic.Should().Be(FilterGroupLogic.And);
        group.Children.Should().HaveCount(2);
        var expression = group.Children[0].Group!;
        expression.Logic.Should().Be(FilterGroupLogic.Or);
        expression.Children[0].CriterionIndex.Should().Be(0);
        var andGroup = expression.Children[1].Group!;
        andGroup.Logic.Should().Be(FilterGroupLogic.And);
        andGroup.Children.Select(child => child.CriterionIndex).Should().Equal(1, 2);
        group.Children[1].CriterionIndex.Should().Be(3);
    }

    [Fact]
    public void Build_ShouldSupportNotAndCaseInsensitiveKeywords_WhenNotIsParenthesized()
    {
        var group = FilterGroupConverter.Build("({0} and (not {1}))", 2)!;

        group.Logic.Should().Be(FilterGroupLogic.And);
        group.Children[0].CriterionIndex.Should().Be(0);
        var notGroup = group.Children[1].Group!;
        notGroup.Logic.Should().Be(FilterGroupLogic.Not);
        notGroup.Children[0].CriterionIndex.Should().Be(1);
    }

    [Theory]
    [InlineData("{2}", 2)]
    [InlineData("{0} AND", 1)]
    [InlineData("{0} {1}", 2)]
    [InlineData("({0}", 1)]
    [InlineData("{0} OR OR {1}", 2)]
    [InlineData("NOT {0}", 1)]
    public void Build_ShouldThrowUnprocessableEntityException_WhenPatternIsInvalid(string pattern, int criterionCount)
    {
        Action act = () => FilterGroupConverter.Build(pattern, criterionCount);
        act.Should().Throw<UnprocessableEntityException>();
    }
}
