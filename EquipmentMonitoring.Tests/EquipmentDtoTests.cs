using System.ComponentModel.DataAnnotations;
using System.Reflection;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Mappings;
using EquipmentMonitoring.Domain;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class EquipmentDtoTests
{
    [Fact]
    public void CreateEquipmentDto_Should_Expose_Expected_Properties_And_Types()
    {
        var dtoType = typeof(CreateEquipmentDto);

        Assert.Equal(typeof(string), dtoType.GetProperty(nameof(CreateEquipmentDto.Name))?.PropertyType);
        Assert.Equal(typeof(string), dtoType.GetProperty(nameof(CreateEquipmentDto.Type))?.PropertyType);
        Assert.Equal(typeof(string), dtoType.GetProperty(nameof(CreateEquipmentDto.SerialNumber))?.PropertyType);
        Assert.Equal(typeof(string), dtoType.GetProperty(nameof(CreateEquipmentDto.Location))?.PropertyType);
    }

    [Fact]
    public void CreateEquipmentDto_Name_Should_Have_Required_And_MaxLength_100_Attributes()
    {
        var nameProperty = typeof(CreateEquipmentDto).GetProperty(nameof(CreateEquipmentDto.Name));

        Assert.NotNull(nameProperty);
        Assert.NotNull(nameProperty.GetCustomAttribute<RequiredAttribute>());
        Assert.Equal(100, nameProperty.GetCustomAttribute<MaxLengthAttribute>()?.Length);
    }

    [Fact]
    public void CreateEquipmentDto_SerialNumber_Should_Have_Required_Attribute()
    {
        var serialNumberProperty = typeof(CreateEquipmentDto)
            .GetProperty(nameof(CreateEquipmentDto.SerialNumber));

        Assert.NotNull(serialNumberProperty);
        Assert.NotNull(serialNumberProperty.GetCustomAttribute<RequiredAttribute>());
    }

    [Theory]
    [InlineData("", "SN-001")]
    [InlineData("Laptop", "")]
    public void CreateEquipmentDto_Should_Reject_Empty_Required_Values(string name, string serialNumber)
    {
        var dto = new CreateEquipmentDto
        {
            Name = name,
            Type = "Computer",
            SerialNumber = serialNumber,
            Location = "Office 1"
        };

        Assert.NotEmpty(Validate(dto));
    }

    [Fact]
    public void CreateEquipmentDto_Should_Reject_Name_Of_101_Characters()
    {
        var dto = new CreateEquipmentDto
        {
            Name = new string('A', 101),
            Type = "Computer",
            SerialNumber = "SN-001",
            Location = "Office 1"
        };

        Assert.NotEmpty(Validate(dto));
    }

    [Fact]
    public void CreateEquipmentDto_Should_Accept_Valid_Values()
    {
        var dto = new CreateEquipmentDto
        {
            Name = "Laptop",
            Type = "Computer",
            SerialNumber = "SN-001",
            Location = "Office 1"
        };

        Assert.Empty(Validate(dto));
    }

    [Fact]
    public void EquipmentResponseDto_Should_Expose_All_Properties_And_Types()
    {
        var dtoType = typeof(EquipmentResponseDto);

        Assert.Equal(typeof(Guid), dtoType.GetProperty(nameof(EquipmentResponseDto.Id))?.PropertyType);
        Assert.Equal(typeof(string), dtoType.GetProperty(nameof(EquipmentResponseDto.Name))?.PropertyType);
        Assert.Equal(typeof(string), dtoType.GetProperty(nameof(EquipmentResponseDto.Type))?.PropertyType);
        Assert.Equal(typeof(string), dtoType.GetProperty(nameof(EquipmentResponseDto.SerialNumber))?.PropertyType);
        Assert.Equal(typeof(string), dtoType.GetProperty(nameof(EquipmentResponseDto.Location))?.PropertyType);
        Assert.Equal(typeof(string), dtoType.GetProperty(nameof(EquipmentResponseDto.Status))?.PropertyType);
        Assert.Equal(typeof(DateTime), dtoType.GetProperty(nameof(EquipmentResponseDto.CreatedAt))?.PropertyType);
    }

    [Fact]
    public void EquipmentMapper_ToResponse_Should_Map_All_Equipment_Properties()
    {
        var equipment = new Equipment("Laptop", "Computer", "SN-001", "Office 1");

        var dto = EquipmentMapper.ToResponse(equipment);

        Assert.Equal(equipment.Id, dto.Id);
        Assert.Equal(equipment.Name, dto.Name);
        Assert.Equal(equipment.Type, dto.Type);
        Assert.Equal(equipment.SerialNumber, dto.SerialNumber);
        Assert.Equal(equipment.Location, dto.Location);
        Assert.Equal(equipment.Status.ToString(), dto.Status);
        Assert.Equal(equipment.CreatedAt, dto.CreatedAt);
    }

    [Fact]
    public void EquipmentMapper_ToDomain_Should_Map_CreateEquipmentDto_Properties()
    {
        var dto = new CreateEquipmentDto
        {
            Name = "Laptop",
            Type = "Computer",
            SerialNumber = "SN-001",
            Location = "Office 1"
        };

        var equipment = EquipmentMapper.ToDomain(dto);

        Assert.Equal(dto.Name, equipment.Name);
        Assert.Equal(dto.Type, equipment.Type);
        Assert.Equal(dto.SerialNumber, equipment.SerialNumber);
        Assert.Equal(dto.Location, equipment.Location);
    }

    private static List<ValidationResult> Validate(object instance)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(
            instance,
            new ValidationContext(instance),
            validationResults,
            validateAllProperties: true);

        return validationResults;
    }
}
