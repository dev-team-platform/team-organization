using AwesomeAssertions;
using TeamOrganization.Api.Dtos.V1.Users;
using TeamOrganization.Domain.Enums.Common;
using TeamOrganization.Domain.Enums.Users;
using TeamOrganization.Domain.Exceptions;
using Xunit;

namespace TeamOrganization.Api.Tests.Dtos;

public class UserRequestMappingTests
{
    [Fact]
    public void CreateUserRequest_ToModel_ShouldMapFieldsAndParseGender_WhenRequestIsValid()
    {
        var request = new CreateUserRequest
        {
            Username = "alice", Email = "alice@example.test", FirstName = "Alice", LastName = "Smith",
            DisplayName = "Alice Smith", EmployeeCode = "E-12", RoleCode = "EMPLOYEE", Gender = "female",
            BirthDate = new DateOnly(1990, 4, 3)
        };

        var model = CreateUserRequest.ToModel(request);

        model.Username.Should().Be(request.Username);
        model.Email.Should().Be(request.Email);
        model.EmployeeCode.Should().Be(request.EmployeeCode);
        model.Gender.Should().Be(UserGender.Female);
        model.BirthDate.Should().Be(request.BirthDate);
    }

    [Fact]
    public void UpdateUserRequest_ToModel_ShouldMapFieldsAndParseGender_WhenRequestIsValid()
    {
        var request = new UpdateUserRequest
        {
            FirstName = "Alice", LastName = "Smith", DisplayName = "Alice Smith",
            EmployeeCode = "E-12", Gender = "Unknown", BirthDate = new DateOnly(1990, 4, 3)
        };

        var model = UpdateUserRequest.ToModel(request);

        model.FirstName.Should().Be("Alice");
        model.LastName.Should().Be("Smith");
        model.Gender.Should().Be(UserGender.Unknown);
        model.BirthDate.Should().Be(request.BirthDate);
    }

    [Fact]
    public void GetAllUsersRequest_ToModel_ShouldMapFiltersSortsAndPaging_WhenRequestIsValid()
    {
        var request = new GetAllUsersRequest
        {
            CurrentPage = 3,
            ItemsPerPage = 25,
            SearchGlobalText = "alice",
            GroupPattern = "{0} OR {1}",
            FilterCriteria =
            [
                new() { FieldName = "email", Operator = "Contains", Values = ["example.test"] },
                new() { FieldName = "status", Operator = "Equals", Values = ["Active"] }
            ],
            SortFields = [new() { FieldName = "displayName", IsAscending = true }]
        };

        var model = GetAllUsersRequest.ToModel(request);

        model.CurrentPage.Should().Be(3);
        model.ItemsPerPage.Should().Be(25);
        model.SearchGlobalText.Should().Be("alice");
        model.FilterGroup!.Logic.Should().Be(FilterGroupLogic.Or);
        model.FilterCriteria[0].FieldName.Should().Be("email");
        model.FilterCriteria[0].Operator.Should().Be(FilterOperator.Contains);
        model.SortFields[0].FieldName.Should().Be("displayName");
        model.SortFields[0].IsAscending.Should().BeTrue();
    }

    [Fact]
    public void GetAllUsersRequest_ToModel_ShouldThrowUnprocessableEntityException_WhenFilterFieldIsUnknown()
    {
        var request = new GetAllUsersRequest
        {
            FilterCriteria = [new() { FieldName = "password", Operator = "Equals", Values = ["x"] }]
        };

        var exception = ((Action)(() => GetAllUsersRequest.ToModel(request)))
            .Should().Throw<UnprocessableEntityException>().Which;

        exception.Message.Should().Be("Unknown filter field.");
        exception.Details!["fieldName"].Should().Be("password");
    }

    [Fact]
    public void GetAllUsersRequest_ToModel_ShouldThrowUnprocessableEntityException_WhenSortFieldIsUnknown()
    {
        var request = new GetAllUsersRequest
        {
            SortFields = [new() { FieldName = "password", IsAscending = false }]
        };

        Action act = () => GetAllUsersRequest.ToModel(request);
        act.Should().Throw<UnprocessableEntityException>();
    }
}
