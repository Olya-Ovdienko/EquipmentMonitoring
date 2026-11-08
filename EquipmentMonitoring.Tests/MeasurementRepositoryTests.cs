using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class MeasurementRepositoryTests
{
    private static readonly DateTime StartDate = new(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime MiddleDate = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndDate = new(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AddAsync_Should_Persist_All_Measurement_Fields_And_Read_Them_From_New_Context()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var equipment = CreateEquipment();
        var measurement = new Measurement(
            equipment.Id,
            MeasurementType.Voltage,
            230.75,
            MiddleDate);

        await using (var writeContext = database.CreateContext())
        {
            writeContext.Equipments.Add(equipment);
            await writeContext.SaveChangesAsync(cancellationToken);
            var repository = new MeasurementRepository(writeContext);

            // Act
            await repository.AddAsync(measurement, cancellationToken);
        }

        // Assert
        await using var readContext = database.CreateContext();
        var stored = await readContext.Measurements
            .AsNoTracking()
            .SingleAsync(item => item.Id == measurement.Id, cancellationToken);

        Assert.Equal(measurement.Id, stored.Id);
        Assert.Equal(equipment.Id, stored.EquipmentId);
        Assert.Equal(MeasurementType.Voltage, stored.Type);
        Assert.Equal(230.75, stored.Value);
        Assert.Equal("V", stored.Unit);
        Assert.Equal(MiddleDate, stored.Timestamp);
        Assert.Equal(DateTimeKind.Utc, stored.Timestamp.Kind);
    }

    [Fact]
    public async Task GetByEquipmentIdAsync_Should_Return_Only_Matching_Equipment_And_Not_Track_Results()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var firstEquipment = CreateEquipment();
        var secondEquipment = CreateEquipment();
        await SeedAsync(database, cancellationToken, firstEquipment, secondEquipment);

        await using var context = database.CreateContext();
        var repository = new MeasurementRepository(context);
        await repository.AddAsync(
            new Measurement(firstEquipment.Id, MeasurementType.Temperature, 18.5, MiddleDate),
            cancellationToken);
        await repository.AddAsync(
            new Measurement(secondEquipment.Id, MeasurementType.Temperature, 19.5, MiddleDate),
            cancellationToken);
        context.ChangeTracker.Clear();

        // Act
        var result = await repository.GetByEquipmentIdAsync(
            firstEquipment.Id,
            null,
            null,
            null,
            cancellationToken);

        // Assert
        var measurement = Assert.Single(result);
        Assert.Equal(firstEquipment.Id, measurement.EquipmentId);
        Assert.Empty(context.ChangeTracker.Entries<Measurement>());
    }

    [Fact]
    public async Task GetByEquipmentIdAsync_Should_Return_Empty_List_When_No_Measurements_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var equipment = CreateEquipment();
        await SeedAsync(database, cancellationToken, equipment);
        await using var context = database.CreateContext();
        var repository = new MeasurementRepository(context);

        // Act
        var result = await repository.GetByEquipmentIdAsync(
            equipment.Id,
            null,
            null,
            null,
            cancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByEquipmentIdAsync_Should_Filter_By_Measurement_Type()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var equipment = CreateEquipment();
        await SeedAsync(database, cancellationToken, equipment);
        await SeedMeasurementsAsync(
            database,
            cancellationToken,
            new Measurement(equipment.Id, MeasurementType.Temperature, 20, MiddleDate),
            new Measurement(equipment.Id, MeasurementType.Voltage, 220, MiddleDate),
            new Measurement(equipment.Id, MeasurementType.Load, 45, MiddleDate));
        await using var context = database.CreateContext();
        var repository = new MeasurementRepository(context);

        // Act
        var result = await repository.GetByEquipmentIdAsync(
            equipment.Id,
            MeasurementType.Voltage,
            null,
            null,
            cancellationToken);

        // Assert
        var measurement = Assert.Single(result);
        Assert.Equal(MeasurementType.Voltage, measurement.Type);
    }

    [Fact]
    public async Task GetByEquipmentIdAsync_Should_Filter_By_StartDate_Including_Boundary()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var equipment = CreateEquipment();
        await SeedAsync(database, cancellationToken, equipment);
        await SeedMeasurementsAsync(
            database,
            cancellationToken,
            new Measurement(equipment.Id, MeasurementType.Temperature, 10, StartDate.AddTicks(-1)),
            new Measurement(equipment.Id, MeasurementType.Temperature, 20, StartDate),
            new Measurement(equipment.Id, MeasurementType.Temperature, 30, MiddleDate));
        await using var context = database.CreateContext();
        var repository = new MeasurementRepository(context);

        // Act
        var result = await repository.GetByEquipmentIdAsync(
            equipment.Id,
            null,
            StartDate,
            null,
            cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Value == 20 && item.Timestamp == StartDate);
        Assert.Contains(result, item => item.Value == 30);
    }

    [Fact]
    public async Task GetByEquipmentIdAsync_Should_Filter_By_EndDate_Including_Boundary()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var equipment = CreateEquipment();
        await SeedAsync(database, cancellationToken, equipment);
        await SeedMeasurementsAsync(
            database,
            cancellationToken,
            new Measurement(equipment.Id, MeasurementType.Temperature, 10, MiddleDate),
            new Measurement(equipment.Id, MeasurementType.Temperature, 20, EndDate),
            new Measurement(equipment.Id, MeasurementType.Temperature, 30, EndDate.AddTicks(1)));
        await using var context = database.CreateContext();
        var repository = new MeasurementRepository(context);

        // Act
        var result = await repository.GetByEquipmentIdAsync(
            equipment.Id,
            null,
            null,
            EndDate,
            cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Value == 20 && item.Timestamp == EndDate);
        Assert.Contains(result, item => item.Value == 10);
    }

    [Fact]
    public async Task GetByEquipmentIdAsync_Should_Apply_Equipment_Type_And_Date_Range_Together()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var equipment = CreateEquipment();
        var otherEquipment = CreateEquipment();
        await SeedAsync(database, cancellationToken, equipment, otherEquipment);
        await SeedMeasurementsAsync(
            database,
            cancellationToken,
            new Measurement(equipment.Id, MeasurementType.Voltage, 210, StartDate.AddTicks(-1)),
            new Measurement(equipment.Id, MeasurementType.Voltage, 220, StartDate),
            new Measurement(equipment.Id, MeasurementType.Temperature, 21, MiddleDate),
            new Measurement(equipment.Id, MeasurementType.Voltage, 230, EndDate),
            new Measurement(equipment.Id, MeasurementType.Voltage, 240, EndDate.AddTicks(1)),
            new Measurement(otherEquipment.Id, MeasurementType.Voltage, 250, MiddleDate));
        await using var context = database.CreateContext();
        var repository = new MeasurementRepository(context);

        // Act
        var result = await repository.GetByEquipmentIdAsync(
            equipment.Id,
            MeasurementType.Voltage,
            StartDate,
            EndDate,
            cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal(equipment.Id, item.EquipmentId));
        Assert.All(result, item => Assert.Equal(MeasurementType.Voltage, item.Type));
        Assert.Contains(result, item => item.Value == 220 && item.Timestamp == StartDate);
        Assert.Contains(result, item => item.Value == 230 && item.Timestamp == EndDate);
    }

    [Fact]
    public async Task GetByEquipmentIdAsync_Should_Restore_Utc_Kind_After_Read()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var equipment = CreateEquipment();
        await SeedAsync(database, cancellationToken, equipment);
        var expectedTimestamp = new DateTime(2026, 3, 12, 8, 30, 45, DateTimeKind.Utc).AddTicks(1234);
        await SeedMeasurementsAsync(
            database,
            cancellationToken,
            new Measurement(equipment.Id, MeasurementType.Temperature, 17.25, expectedTimestamp));
        await using var context = database.CreateContext();
        var repository = new MeasurementRepository(context);

        // Act
        var result = await repository.GetByEquipmentIdAsync(
            equipment.Id,
            null,
            null,
            null,
            cancellationToken);

        // Assert
        var measurement = Assert.Single(result);
        Assert.Equal(expectedTimestamp, measurement.Timestamp);
        Assert.Equal(DateTimeKind.Utc, measurement.Timestamp.Kind);
    }

    [Fact]
    public async Task Measurement_Table_Should_Have_Composite_EquipmentId_Timestamp_Index_In_Sqlite()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        await using var command = database.Connection.CreateCommand();
        command.CommandText = "PRAGMA index_list('Measurements');";

        // Act
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var indexNames = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            indexNames.Add(reader.GetString(reader.GetOrdinal("name")));
        }

        // Assert
        Assert.Contains("IX_Measurements_EquipmentId_Timestamp", indexNames);
        var indexColumns = await GetIndexColumnsAsync(
            database.Connection,
            "IX_Measurements_EquipmentId_Timestamp",
            cancellationToken);
        Assert.Equal(["EquipmentId", "Timestamp"], indexColumns);
    }

    [Fact]
    public async Task Repository_Async_Operations_Should_Observe_CancellationToken()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var equipment = CreateEquipment();
        await SeedAsync(database, cancellationToken, equipment);
        await using var context = database.CreateContext();
        var repository = new MeasurementRepository(context);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancelledToken = cancellationSource.Token;
        var measurement = new Measurement(equipment.Id, MeasurementType.Temperature, 15, MiddleDate);

        // Act / Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.AddAsync(measurement, cancelledToken));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByEquipmentIdAsync(
                equipment.Id,
                null,
                null,
                null,
                cancelledToken));
    }

    private static Equipment CreateEquipment() => new(
        "Sensor",
        "Датчик",
        $"SN-{Guid.NewGuid():N}",
        "Repository test lab");

    private static async Task SeedAsync(
        SqliteTestDatabase database,
        CancellationToken cancellationToken,
        params Equipment[] equipment)
    {
        await using var context = database.CreateContext();
        context.Equipments.AddRange(equipment);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedMeasurementsAsync(
        SqliteTestDatabase database,
        CancellationToken cancellationToken,
        params Measurement[] measurements)
    {
        await using var context = database.CreateContext();
        context.Measurements.AddRange(measurements);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<List<string>> GetIndexColumnsAsync(
        SqliteConnection connection,
        string indexName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA index_info('{indexName}');";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var columns = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(reader.GetOrdinal("name")));
        }

        return columns;
    }

    private sealed class SqliteTestDatabase : IAsyncDisposable
    {
        private SqliteTestDatabase(SqliteConnection connection)
        {
            Connection = connection;
        }

        public SqliteConnection Connection { get; }

        public EquipmentDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<EquipmentDbContext>()
                .UseSqlite(Connection)
                .Options;
            return new EquipmentDbContext(options);
        }

        public static async Task<SqliteTestDatabase> CreateAsync(CancellationToken cancellationToken)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(cancellationToken);
            var database = new SqliteTestDatabase(connection);
            await using var context = database.CreateContext();
            await context.Database.EnsureCreatedAsync(cancellationToken);
            return database;
        }

        public async ValueTask DisposeAsync()
        {
            await Connection.DisposeAsync();
        }
    }
}
