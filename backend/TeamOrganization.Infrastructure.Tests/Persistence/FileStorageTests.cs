using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamOrganization.Application.Interfaces.Repositories;
using TeamOrganization.Domain.Entities;
using TeamOrganization.Domain.Enums.FileStorage;
using TeamOrganization.Infrastructure.Persistence;
using TeamOrganization.Infrastructure.Repositories;
using Xunit;

namespace TeamOrganization.Infrastructure.Tests.Persistence;

public class FileStorageTests
{
    [Fact]
    public void Configure_ShouldMapFileStorageAndFilterDeletedRows_WhenModelIsBuilt()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=test;Username=test;Password=test")
            .Options);

        var entity = context.Model.FindEntityType(typeof(FileStorage))!;
        entity.GetTableName().Should().Be("file_storage");
        entity.FindProperty(nameof(FileStorage.ReferenceId))!.GetMaxLength().Should().Be(255);
        entity.FindProperty(nameof(FileStorage.ReferenceId))!.IsNullable.Should().BeTrue();
        entity.FindProperty(nameof(FileStorage.ObjectKey))!.GetMaxLength().Should().Be(4096);
        entity.FindProperty(nameof(FileStorage.ObjectKey))!.IsNullable.Should().BeFalse();
        entity.FindProperty(nameof(FileStorage.FileName))!.GetMaxLength().Should().Be(2048);
        entity.FindProperty(nameof(FileStorage.ContentType))!.GetMaxLength().Should().Be(255);
        entity.FindProperty(nameof(FileStorage.ContentType))!.IsNullable.Should().BeFalse();
        entity.FindProperty(nameof(FileStorage.FileType))!.GetMaxLength().Should().Be(20);
        entity.GetIndexes().Select(index => index.GetDatabaseName()).Should().Contain([
            "idx_file_storage_reference_id",
            "idx_file_storage_file_type"
        ]);
        entity.FindProperty(nameof(FileStorage.FileType))!.GetTypeMapping().Converter!
            .ConvertToProvider(FileType.UserListView).Should().Be("UserListView");
        entity.FindProperty(nameof(FileStorage.ExpiredAt))!.IsNullable.Should().BeTrue();
        var sql = context.FileStorages.Where(file => file.ObjectKey == "storage-key").ToQueryString();
        sql.Should().Contain("object_key");
        sql.Should().Contain("is_deleted");
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterFileStorageRepository_WhenServicesAreConfigured()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
        }).Build();

        services.AddInfrastructure(configuration);

        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IFileStorageRepository)
            && descriptor.ImplementationType == typeof(FileStorageRepository)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
    }
}
