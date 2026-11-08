using AwesomeAssertions;
using TeamOrganization.Api.Dtos.V1.Users;
using TeamOrganization.Application.Models.Common;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Enums.Common;
using TeamOrganization.Domain.Enums.Users;
using TeamOrganization.Domain.Exceptions;
using Xunit;

namespace TeamOrganization.Api.Tests.Dtos;

public class UserRequestMappingTests
{
    [Fact]
    public void GetAllUsersResponse_FromModel_ShouldMapPendingActionFlags_WhenUsersArePending()
    {
        var response = GetAllUsersResponse.FromModel(new GetAllUsersResponseModel
        {
            Items =
            [
                new GetAllUsersResponseModelItem
                {
                    Id = Guid.NewGuid(),
                    EmployeeCode = "E1",
                    Username = "alice",
                    Email = "alice@example.test",
                    FirstName = "Alice",
                    DisplayName = "Alice",
                    Status = UserStatus.Active,
                    Gender = UserGender.Female,
                    IsPendingDisabled = true,
                    IsPendingChangedRole = false
                }
            ],
            CurrentPage = 1,
            ItemsPerPage = 10,
            TotalItems = 1,
            TotalPages = 1
        });

        response.Items.Single().IsPendingDisabled.Should().BeTrue();
        response.Items.Single().IsPendingChangedRole.Should().BeFalse();
    }

    [Fact]
    public void GetAllUsersResponse_FromModel_ShouldMapAuditMetadata_WhenPresent()
    {
        var createdDate = DateTimeOffset.UtcNow.AddDays(-2);
        var updatedDate = DateTimeOffset.UtcNow.AddHours(-1);
        var createdById = Guid.NewGuid();
        var updatedById = Guid.NewGuid();

        var response = GetAllUsersResponse.FromModel(new GetAllUsersResponseModel
        {
            Items =
            [
                new GetAllUsersResponseModelItem
                {
                    Id = Guid.NewGuid(),
                    EmployeeCode = "E1",
                    Username = "alice",
                    Email = "alice@example.test",
                    FirstName = "Alice",
                    DisplayName = "Alice",
                    Status = UserStatus.Active,
                    Gender = UserGender.Female,
                    CreatedAt = createdDate,
                    CreatedBy = new NavigationResponseModel { Id = createdById, Name = "Creator" },
                    UpdatedAt = updatedDate,
                    UpdatedBy = new NavigationResponseModel { Id = updatedById, Name = "Updater" }
                }
            ]
        });

        var item = response.Items.Single();
        item.CreatedAt.Should().Be(createdDate);
        item.CreatedBy!.Id.Should().Be(createdById);
        item.CreatedBy.Name.Should().Be("Creator");
        item.UpdatedAt.Should().Be(updatedDate);
        item.UpdatedBy!.Id.Should().Be(updatedById);
        item.UpdatedBy.Name.Should().Be("Updater");
    }

    [Fact]
    public void CreateUserRequest_ToModel_ShouldMapFieldsAndParseGender_WhenRequestIsValid()
    {
        var request = new CreateUserRequest
        {
            Username = "alice",
            Email = "alice@example.test",
            FirstName = "Alice",
            LastName = "Smith",
            DisplayName = "Alice Smith",
            EmployeeCode = "E-12",
            RoleCode = "EMPLOYEE",
            Gender = "female",
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
            FirstName = "Alice",
            LastName = "Smith",
            DisplayName = "Alice Smith",
            EmployeeCode = "E-12",
            Gender = "Unknown",
            BirthDate = new DateOnly(1990, 4, 3)
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
    public void GetAllUsersRequest_ToModel_ShouldAcceptCreatedByAndUpdatedByIdFilters_WhenRequestIsValid()
    {
        var createdById = Guid.NewGuid();
        var updatedById = Guid.NewGuid();
        var request = new GetAllUsersRequest
        {
            GroupPattern = "{0} AND {1}",
            FilterCriteria =
            [
                new() { FieldName = "createdBy", Operator = "Equals", Values = [createdById.ToString()] },
                new() { FieldName = "updatedBy", Operator = "Equals", Values = [updatedById.ToString()] }
            ]
        };

        var model = GetAllUsersRequest.ToModel(request);
        var applicationFilterMap = model.GetFilterQueryMap();
        var apiFilterMap = request.GetFilterRequestMap();

        model.FilterCriteria.Select(criterion => criterion.FieldName)
            .Should().BeEquivalentTo("createdBy", "updatedBy");
        applicationFilterMap.TryGetField("createdBy", out var createdByField).Should().BeTrue();
        applicationFilterMap.TryGetField("updatedBy", out var updatedByField).Should().BeTrue();
        createdByField.FieldType.Should().Be(typeof(Guid?));
        updatedByField.FieldType.Should().Be(typeof(Guid?));
        apiFilterMap.TryGetField("createdBy", out var createdByRequestField).Should().BeTrue();
        apiFilterMap.TryGetField("updatedBy", out var updatedByRequestField).Should().BeTrue();
        createdByRequestField.FieldType.Should().Be(typeof(Guid?));
        updatedByRequestField.FieldType.Should().Be(typeof(Guid?));
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
