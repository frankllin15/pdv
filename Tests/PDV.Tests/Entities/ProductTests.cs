using FluentAssertions;
using PDV.Core.Entities;
using Xunit;

namespace PDV.Tests.Entities;

public class ProductTests
{
    [Fact]
    public void CreateProduct_WithValidData_ShouldSucceed()
    {
        // Arrange & Act
        var product = new Product(
            barcode: "7891000100103",
            description: "Test Product",
            unitPrice: 10.99m);

        // Assert
        product.Barcode.Should().Be("7891000100103");
        product.Description.Should().Be("Test Product");
        product.UnitPrice.Should().Be(10.99m);
        product.IsActive.Should().BeTrue();
        product.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateProduct_WithEmptyBarcode_ShouldThrowException()
    {
        // Arrange & Act
        var act = () => new Product(
            barcode: "",
            description: "Test Product",
            unitPrice: 10.99m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Barcode*");
    }

    [Fact]
    public void CreateProduct_WithNegativePrice_ShouldThrowException()
    {
        // Arrange & Act
        var act = () => new Product(
            barcode: "123456",
            description: "Test Product",
            unitPrice: -10m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*price*");
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var product = new Product("123456", "Test", 10m);

        // Act
        product.Deactivate();

        // Assert
        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdateStock_ShouldAddToQuantity()
    {
        // Arrange
        var product = new Product("123456", "Test", 10m, stockQuantity: 100);

        // Act
        product.UpdateStock(50);

        // Assert
        product.StockQuantity.Should().Be(150);
    }
}
