using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Validators;
using FluentValidation.Results;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class EquipmentValidationTests
{
    [Theory]
    [InlineData(nameof(CreateEquipmentDto.Name), null)]
    [InlineData(nameof(CreateEquipmentDto.Name), "")]
    [InlineData(nameof(CreateEquipmentDto.Name), "   ")]
    [InlineData(nameof(CreateEquipmentDto.Type), null)]
    [InlineData(nameof(CreateEquipmentDto.Type), "")]
    [InlineData(nameof(CreateEquipmentDto.Type), "   ")]
    [InlineData(nameof(CreateEquipmentDto.SerialNumber), null)]
    [InlineData(nameof(CreateEquipmentDto.SerialNumber), "")]
    [InlineData(nameof(CreateEquipmentDto.SerialNumber), "   ")]
    [InlineData(nameof(CreateEquipmentDto.Location), null)]
    [InlineData(nameof(CreateEquipmentDto.Location), "")]
    [InlineData(nameof(CreateEquipmentDto.Location), "   ")]
    public async Task CreateValidator_Should_Reject_Invalid_Required_Fields(string propertyName, string? value)
    {
        var dto = CreateValidCreateDto();
        SetCreateField(dto, propertyName, value);

        var result = await ValidateCreateAsync(dto);

        AssertFieldHasMessage(result, propertyName);
    }

    [Fact]
    public async Task CreateValidator_Should_Report_All_Missing_Required_Fields()
    {
        var result = await ValidateCreateAsync(new CreateEquipmentDto());

        AssertFieldHasMessage(result, nameof(CreateEquipmentDto.Name));
        AssertFieldHasMessage(result, nameof(CreateEquipmentDto.Type));
        AssertFieldHasMessage(result, nameof(CreateEquipmentDto.SerialNumber));
        AssertFieldHasMessage(result, nameof(CreateEquipmentDto.Location));
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public async Task CreateValidator_Should_Enforce_Name_Length_Bounds(int length, bool isValid)
    {
        var dto = CreateValidCreateDto();
        dto.Name = new string('A', length);

        var result = await ValidateCreateAsync(dto);

        Assert.Equal(isValid, result.IsValid);
        if (!isValid)
        {
            AssertFieldHasMessage(result, nameof(CreateEquipmentDto.Name));
        }
    }

    [Theory]
    [InlineData("Датчик")]
    [InlineData("Контролер")]
    [InlineData("Сервер")]
    public async Task CreateValidator_Should_Accept_Allowed_Type_Examples(string type)
    {
        var dto = CreateValidCreateDto();
        dto.Type = type;

        var result = await ValidateCreateAsync(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateValidator_Should_Reject_Unknown_Type()
    {
        var dto = CreateValidCreateDto();
        dto.Type = "Невідомий тип";

        var result = await ValidateCreateAsync(dto);

        AssertFieldHasMessage(result, nameof(CreateEquipmentDto.Type));
    }

    [Theory]
    [InlineData(nameof(UpdateEquipmentDto.Name), null)]
    [InlineData(nameof(UpdateEquipmentDto.Name), "")]
    [InlineData(nameof(UpdateEquipmentDto.Name), "   ")]
    [InlineData(nameof(UpdateEquipmentDto.Type), null)]
    [InlineData(nameof(UpdateEquipmentDto.Type), "")]
    [InlineData(nameof(UpdateEquipmentDto.Type), "   ")]
    [InlineData(nameof(UpdateEquipmentDto.SerialNumber), null)]
    [InlineData(nameof(UpdateEquipmentDto.SerialNumber), "")]
    [InlineData(nameof(UpdateEquipmentDto.SerialNumber), "   ")]
    [InlineData(nameof(UpdateEquipmentDto.Location), null)]
    [InlineData(nameof(UpdateEquipmentDto.Location), "")]
    [InlineData(nameof(UpdateEquipmentDto.Location), "   ")]
    public async Task UpdateValidator_Should_Reject_Invalid_Required_Fields(string propertyName, string? value)
    {
        var dto = CreateValidUpdateDto();
        SetUpdateField(dto, propertyName, value);

        var result = await ValidateUpdateAsync(dto);

        AssertFieldHasMessage(result, propertyName);
    }

    [Fact]
    public async Task UpdateValidator_Should_Report_All_Missing_Required_Fields()
    {
        var dto = new UpdateEquipmentDto { Id = Guid.NewGuid() };

        var result = await ValidateUpdateAsync(dto);

        AssertFieldHasMessage(result, nameof(UpdateEquipmentDto.Name));
        AssertFieldHasMessage(result, nameof(UpdateEquipmentDto.Type));
        AssertFieldHasMessage(result, nameof(UpdateEquipmentDto.SerialNumber));
        AssertFieldHasMessage(result, nameof(UpdateEquipmentDto.Location));
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public async Task UpdateValidator_Should_Enforce_Name_Length_Bounds(int length, bool isValid)
    {
        var dto = CreateValidUpdateDto();
        dto.Name = new string('A', length);

        var result = await ValidateUpdateAsync(dto);

        Assert.Equal(isValid, result.IsValid);
        if (!isValid)
        {
            AssertFieldHasMessage(result, nameof(UpdateEquipmentDto.Name));
        }
    }

    [Theory]
    [InlineData("Датчик")]
    [InlineData("Контролер")]
    [InlineData("Сервер")]
    public async Task UpdateValidator_Should_Accept_Allowed_Type_Examples(string type)
    {
        var dto = CreateValidUpdateDto();
        dto.Type = type;

        var result = await ValidateUpdateAsync(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UpdateValidator_Should_Reject_Unknown_Type()
    {
        var dto = CreateValidUpdateDto();
        dto.Type = "Невідомий тип";

        var result = await ValidateUpdateAsync(dto);

        AssertFieldHasMessage(result, nameof(UpdateEquipmentDto.Type));
    }

    [Fact]
    public async Task CreateValidator_Should_Allow_New_SerialNumber()
    {
        var dto = CreateValidCreateDto();
        var result = await ValidateCreateAsync(dto, new FakeEquipmentSerialNumberChecker());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateValidator_Should_Reject_Already_Used_SerialNumber()
    {
        var checker = new FakeEquipmentSerialNumberChecker();
        checker.AddExisting("SN-001", Guid.NewGuid());

        var result = await ValidateCreateAsync(CreateValidCreateDto(), checker);

        AssertFieldHasMessage(result, nameof(CreateEquipmentDto.SerialNumber));
    }

    [Fact]
    public async Task UpdateValidator_Should_Allow_Equipment_To_Keep_Its_SerialNumber()
    {
        var equipmentId = Guid.NewGuid();
        var checker = new FakeEquipmentSerialNumberChecker();
        checker.AddExisting("SN-001", equipmentId);
        var dto = CreateValidUpdateDto();
        dto.Id = equipmentId;

        var result = await ValidateUpdateAsync(dto, checker);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UpdateValidator_Should_Reject_SerialNumber_Used_By_Other_Equipment()
    {
        var checker = new FakeEquipmentSerialNumberChecker();
        checker.AddExisting("SN-OTHER", Guid.NewGuid());
        var dto = CreateValidUpdateDto();
        dto.SerialNumber = "SN-OTHER";

        var result = await ValidateUpdateAsync(dto, checker);

        AssertFieldHasMessage(result, nameof(UpdateEquipmentDto.SerialNumber));
    }

    private static CreateEquipmentDto CreateValidCreateDto() => new()
    {
        Name = "Датчик",
        Type = "Датчик",
        SerialNumber = "SN-001",
        Location = "Лабораторія"
    };

    private static UpdateEquipmentDto CreateValidUpdateDto() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Датчик",
        Type = "Датчик",
        SerialNumber = "SN-001",
        Location = "Лабораторія"
    };

    private static void SetCreateField(CreateEquipmentDto dto, string propertyName, string? value)
    {
        switch (propertyName)
        {
            case nameof(CreateEquipmentDto.Name):
                dto.Name = value!;
                break;
            case nameof(CreateEquipmentDto.Type):
                dto.Type = value!;
                break;
            case nameof(CreateEquipmentDto.SerialNumber):
                dto.SerialNumber = value!;
                break;
            case nameof(CreateEquipmentDto.Location):
                dto.Location = value!;
                break;
        }
    }

    private static void SetUpdateField(UpdateEquipmentDto dto, string propertyName, string? value)
    {
        switch (propertyName)
        {
            case nameof(UpdateEquipmentDto.Name):
                dto.Name = value!;
                break;
            case nameof(UpdateEquipmentDto.Type):
                dto.Type = value!;
                break;
            case nameof(UpdateEquipmentDto.SerialNumber):
                dto.SerialNumber = value!;
                break;
            case nameof(UpdateEquipmentDto.Location):
                dto.Location = value!;
                break;
        }
    }

    private static async Task<ValidationResult> ValidateCreateAsync(
        CreateEquipmentDto dto,
        FakeEquipmentSerialNumberChecker? checker = null)
    {
        var validator = new CreateEquipmentDtoValidator(checker ?? new FakeEquipmentSerialNumberChecker());
        return await validator.ValidateAsync(dto, TestContext.Current.CancellationToken);
    }

    private static async Task<ValidationResult> ValidateUpdateAsync(
        UpdateEquipmentDto dto,
        FakeEquipmentSerialNumberChecker? checker = null)
    {
        var validator = new UpdateEquipmentDtoValidator(checker ?? new FakeEquipmentSerialNumberChecker());
        return await validator.ValidateAsync(dto, TestContext.Current.CancellationToken);
    }

    private static void AssertFieldHasMessage(ValidationResult result, string propertyName)
    {
        var fieldErrors = result.Errors.Where(error => error.PropertyName == propertyName).ToList();

        Assert.NotEmpty(fieldErrors);
        Assert.All(fieldErrors, error => Assert.False(string.IsNullOrWhiteSpace(error.ErrorMessage)));
    }

    private sealed class FakeEquipmentSerialNumberChecker : IEquipmentSerialNumberChecker
    {
        private readonly Dictionary<string, Guid> _existingSerialNumbers = new(StringComparer.Ordinal);

        public void AddExisting(string serialNumber, Guid equipmentId)
        {
            _existingSerialNumbers[serialNumber] = equipmentId;
        }

        public Task<bool> IsSerialNumberUniqueAsync(
            string serialNumber,
            Guid? excludedEquipmentId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var isUnique = !_existingSerialNumbers.TryGetValue(serialNumber, out var existingId)
                || existingId == excludedEquipmentId;

            return Task.FromResult(isUnique);
        }
    }
}
