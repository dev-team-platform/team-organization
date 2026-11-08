using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using TeamOrganization.Api.Controllers.V1;
using TeamOrganization.Api.Dtos.V1.Users;
using TeamOrganization.Application.Interfaces.Services.IdentityProviders;
using TeamOrganization.Application.Interfaces.Services.Users;
using TeamOrganization.Application.Models.IdentityProviders;
using TeamOrganization.Application.Models.Users;
using TeamOrganization.Domain.Constants.Uam;
using TeamOrganization.Domain.Enums.Users;
using TeamOrganization.Domain.Exceptions;
using TeamOrganization.Infrastructure.Options;
using Xunit;

namespace TeamOrganization.Api.Tests.Controllers;

public class UserControllerTests
{
    private readonly Mock<Serilog.ILogger> _logger = new();
    private readonly Mock<IUserQueryService> _queryService = new();
    private readonly Mock<IUserCommandService> _commandService = new();
    private readonly Mock<IIdentityProviderService> _identityProvider = new();
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _controller = new UserController(
            _logger.Object,
            _queryService.Object,
            _commandService.Object,
            _identityProvider.Object,
            Microsoft.Extensions.Options.Options.Create(new UserManagementOptions { DefaultPassword = "test-password" }));
    }

    [Fact]
    public async Task GetMeAsync_ShouldReturnMappedUser_WhenQuerySucceeds()
    {
        var response = new GetCurrentUserResponseModel
        {
            Id = Guid.NewGuid(),
            IdentitySubject = "sub",
            Username = "alice",
            Email = "a@example.test",
            EmployeeCode = "E1",
            FirstName = "Alice",
            DisplayName = "Alice",
            Gender = UserGender.Female,
            RoleName = "Employee",
            PermissionCodes = ["users.read"]
        };
        _queryService.Setup(service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = (await _controller.GetMeAsync()).Should().BeOfType<OkObjectResult>().Which;
        var dto = result.Value.Should().BeOfType<GetCurrentUserResponse>().Which;

        dto.Id.Should().Be(response.Id);
        dto.Gender.Should().Be("Female");
        dto.Permissions.Should().Equal(response.PermissionCodes);
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldMapRequestAndReturnResponse_WhenQuerySucceeds()
    {
        GetAllUsersRequestModel? capturedModel = null;
        var list = new GetAllUsersResponseModel { CurrentPage = 1, ItemsPerPage = 10, TotalItems = 0, TotalPages = 0 };
        _queryService.Setup(service => service.GetAllUsersAsync(It.IsAny<GetAllUsersRequestModel>(), It.IsAny<CancellationToken>()))
            .Callback<GetAllUsersRequestModel, CancellationToken>((model, _) => capturedModel = model)
            .ReturnsAsync(list);

        var result = (await _controller.GetAllUsersAsync(new GetAllUsersRequest
        {
            CurrentPage = 2,
            ItemsPerPage = 10,
            SearchGlobalText = "alice"
        })).Should().BeOfType<OkObjectResult>().Which;

        result.Value.Should().BeOfType<GetAllUsersResponse>();
        capturedModel.Should().NotBeNull();
        capturedModel!.CurrentPage.Should().Be(2);
        capturedModel.SearchGlobalText.Should().Be("alice");
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUseRouteIdAndReturnMappedResult_WhenUpdateSucceeds()
    {
        var userId = Guid.NewGuid();
        UpdateUserRequestModel? capturedModel = null;
        var updated = new UpdateUserResponseModel
        {
            Id = userId,
            IdentitySubject = "sub",
            Username = "alice",
            Email = "a@example.test",
            FirstName = "Alice",
            DisplayName = "Alice",
            EmployeeCode = "E1",
            Gender = UserGender.Female
        };
        _commandService.Setup(service => service.UpdateUserAsync(It.IsAny<UpdateUserRequestModel>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateUserRequestModel, CancellationToken>((model, _) => capturedModel = model)
            .ReturnsAsync(updated);

        var result = (await _controller.UpdateUserAsync(userId, new UpdateUserRequest
        {
            FirstName = "Alice",
            DisplayName = "Alice",
            EmployeeCode = "E1",
            Gender = "Female"
        })).Should().BeOfType<OkObjectResult>().Which;

        capturedModel!.UserId.Should().Be(userId);
        result.Value.Should().BeOfType<UpdateUserResponse>().Which.Gender.Should().Be("Female");
    }

    [Fact]
    public async Task CreateNewUserAsync_ShouldCreateIdentityAndApplicationUser_WhenEmployeeRoleIsRequested()
    {
        IdentityProviderNewUserRequestModel? identityModel = null;
        CreateUserRequestModel? appModel = null;
        var created = NewCreateResponse();
        _identityProvider.Setup(service => service.GetAccessTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync("token");
        _identityProvider.Setup(service => service.CreateNewUserAsync(
                "token", It.IsAny<IdentityProviderNewUserRequestModel>(), "test-password", It.IsAny<CancellationToken>()))
            .Callback<string, IdentityProviderNewUserRequestModel, string, CancellationToken>((_, model, _, _) => identityModel = model)
            .ReturnsAsync("identity-123");
        _commandService.Setup(service => service.CreateNewUserAsync(It.IsAny<CreateUserRequestModel>(), It.IsAny<CancellationToken>()))
            .Callback<CreateUserRequestModel, CancellationToken>((model, _) => appModel = model)
            .ReturnsAsync(created);

        var result = (await _controller.CreateNewUserAsync(NewCreateRequest())).Should().BeOfType<ObjectResult>().Which;

        result.StatusCode.Should().Be(201);
        identityModel!.Username.Should().Be("alice");
        appModel!.IdentitySubject.Should().Be("identity-123");
        appModel.RoleCode.Should().Be(RC.Employee);
        result.Value.Should().BeOfType<CreateNewUserResponse>().Which.Id.Should().Be(created.Id);
        _identityProvider.Verify(service => service.DeleteUserAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewUserAsync_ShouldThrowUnprocessableEntityException_WhenRoleIsNotEmployee()
    {
        var request = NewCreateRequest();
        request.RoleCode = RC.Admin;

        Func<Task> act = () => _controller.CreateNewUserAsync(request);
        await act.Should().ThrowAsync<UnprocessableEntityException>();

        _identityProvider.Verify(service => service.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewAdminUserAsync_ShouldCreateAdminUser_WhenAdminRoleIsRequested()
    {
        var request = NewCreateRequest();
        request.RoleCode = RC.Admin;
        _identityProvider.Setup(service => service.GetAccessTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync("token");
        _identityProvider.Setup(service => service.CreateNewUserAsync(
                It.IsAny<string>(), It.IsAny<IdentityProviderNewUserRequestModel>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("identity-admin");
        _commandService.Setup(service => service.CreateNewUserAsync(It.IsAny<CreateUserRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewCreateResponse());

        var result = (await _controller.CreateNewAdminUserAsync(request)).Should().BeOfType<ObjectResult>().Which;

        result.StatusCode.Should().Be(201);
        _commandService.Verify(service => service.CreateNewUserAsync(
            It.Is<CreateUserRequestModel>(model => model.RoleCode == RC.Admin),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateNewUserAsync_ShouldDeleteProvisionedIdentity_WhenApplicationCreateFails()
    {
        _identityProvider.Setup(service => service.GetAccessTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync("token");
        _identityProvider.Setup(service => service.CreateNewUserAsync(
                It.IsAny<string>(), It.IsAny<IdentityProviderNewUserRequestModel>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("identity-123");
        _commandService.Setup(service => service.CreateNewUserAsync(It.IsAny<CreateUserRequestModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        Func<Task> act = () => _controller.CreateNewUserAsync(NewCreateRequest());
        await act.Should().ThrowAsync<InvalidOperationException>();

        _identityProvider.Verify(service => service.DeleteUserAsync(
            "token", "identity-123", It.Is<CancellationToken>(token => token.CanBeCanceled)), Times.Once);
    }

    private static CreateUserRequest NewCreateRequest() => new()
    {
        Username = "alice",
        Email = "alice@example.test",
        FirstName = "Alice",
        LastName = "Smith",
        DisplayName = "Alice Smith",
        EmployeeCode = "E1",
        RoleCode = RC.Employee,
        Gender = "Female"
    };

    private static CreateUserResponseModel NewCreateResponse() => new()
    {
        Id = Guid.NewGuid(),
        IdentitySubject = "identity-123",
        Username = "alice",
        Email = "alice@example.test",
        FirstName = "Alice",
        LastName = "Smith",
        DisplayName = "Alice Smith",
        EmployeeCode = "E1",
        Gender = UserGender.Female
    };
}
