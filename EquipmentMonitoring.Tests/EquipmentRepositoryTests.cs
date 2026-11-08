using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class EquipmentRepositoryTests
{
    [Fact]
    public async Task AddAsync_Should_Save_Equipment_To_Database()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        var options = CreateOptions(connection);
        var equipment = CreateEquipment();

        await using (var context = new EquipmentDbContext(options))
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
            IEquipmentRepository repository = new EquipmentRepository(context);

            await repository.AddAsync(equipment, cancellationToken);
        }

        await using var verificationContext = new EquipmentDbContext(options);
        var storedEquipment = await verificationContext.Equipments
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == equipment.Id, cancellationToken);

        Assert.NotNull(storedEquipment);
        Assert.Equal(equipment.Id, storedEquipment.Id);
        Assert.Equal(equipment.Name, storedEquipment.Name);
        Assert.Equal(equipment.Type, storedEquipment.Type);
        Assert.Equal(equipment.SerialNumber, storedEquipment.SerialNumber);
        Assert.Equal(equipment.Location, storedEquipment.Location);
        Assert.Equal(equipment.Status, storedEquipment.Status);
        Assert.Equal(equipment.CreatedAt, storedEquipment.CreatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Equipment_With_All_Properties()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        var options = CreateOptions(connection);
        var equipment = CreateEquipment();

        await using var context = new EquipmentDbContext(options);
        await context.Database.EnsureCreatedAsync(cancellationToken);
        IEquipmentRepository repository = new EquipmentRepository(context);
        await repository.AddAsync(equipment, cancellationToken);

        var result = await repository.GetByIdAsync(equipment.Id, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(equipment.Id, result.Id);
        Assert.Equal(equipment.Name, result.Name);
        Assert.Equal(equipment.Type, result.Type);
        Assert.Equal(equipment.SerialNumber, result.SerialNumber);
        Assert.Equal(equipment.Location, result.Location);
        Assert.Equal(equipment.Status, result.Status);
        Assert.Equal(equipment.CreatedAt, result.CreatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Equipment_Does_Not_Exist()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        var options = CreateOptions(connection);

        await using var context = new EquipmentDbContext(options);
        await context.Database.EnsureCreatedAsync(cancellationToken);
        IEquipmentRepository repository = new EquipmentRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), cancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Load_Equipment_Using_A_New_DbContext()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        var options = CreateOptions(connection);
        var equipment = CreateEquipment();

        await using (var writeContext = new EquipmentDbContext(options))
        {
            await writeContext.Database.EnsureCreatedAsync(cancellationToken);
            IEquipmentRepository writeRepository = new EquipmentRepository(writeContext);
            await writeRepository.AddAsync(equipment, cancellationToken);
        }

        await using var readContext = new EquipmentDbContext(options);
        IEquipmentRepository readRepository = new EquipmentRepository(readContext);

        var result = await readRepository.GetByIdAsync(equipment.Id, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(equipment.Id, result.Id);
        Assert.Equal(equipment.Name, result.Name);
        Assert.Equal(equipment.CreatedAt, result.CreatedAt);
    }

    private static async Task<SqliteConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static DbContextOptions<EquipmentDbContext> CreateOptions(SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<EquipmentDbContext>()
            .UseSqlite(connection)
            .Options;
    }

    private static Equipment CreateEquipment() =>
        new("Test Equipment", "Sensor", "SN-REPOSITORY-001", "Lab 1");
}
