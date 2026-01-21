using FluentAssertions;
using PDV.Core.Entities;
using PDV.Shared.Enums;
using Xunit;

namespace PDV.Tests.Entities;

public class SaleTests
{
    [Fact]
    public void CreateSale_ShouldHaveInProgressStatus()
    {
        // Arrange & Act
        var sale = new Sale(1);

        // Assert
        sale.SaleNumber.Should().Be(1);
        sale.Status.Should().Be(SaleStatus.InProgress);
        sale.Items.Should().BeEmpty();
        sale.Payments.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_ShouldAddToItemsAndUpdateTotal()
    {
        // Arrange
        var sale = new Sale(1);
        var product = new Product("123456", "Test Product", 10m);

        // Act
        var item = sale.AddItem(product, 2);

        // Assert
        sale.Items.Should().HaveCount(1);
        sale.Subtotal.Should().Be(20m);
        sale.Total.Should().Be(20m);
        item.Quantity.Should().Be(2);
    }

    [Fact]
    public void AddItem_SameProductTwice_ShouldIncreaseQuantity()
    {
        // Arrange
        var sale = new Sale(1);
        var product = new Product("123456", "Test Product", 10m);

        // Act
        sale.AddItem(product, 1);
        sale.AddItem(product, 2);

        // Assert
        sale.Items.Should().HaveCount(1);
        sale.Items.First().Quantity.Should().Be(3);
        sale.Total.Should().Be(30m);
    }

    [Fact]
    public void RemoveItem_ShouldRemoveFromItems()
    {
        // Arrange
        var sale = new Sale(1);
        var product = new Product("123456", "Test Product", 10m);
        var item = sale.AddItem(product, 1);

        // Act
        sale.RemoveItem(item.Id);

        // Assert
        sale.Items.Should().BeEmpty();
        sale.Total.Should().Be(0);
    }

    [Fact]
    public void AddPayment_ShouldAddToPayments()
    {
        // Arrange
        var sale = new Sale(1);
        var product = new Product("123456", "Test", 10m);
        sale.AddItem(product, 1);

        // Act
        var payment = sale.AddPayment(PaymentMethod.Cash, 10m);

        // Assert
        sale.Payments.Should().HaveCount(1);
        payment.Method.Should().Be(PaymentMethod.Cash);
        payment.Amount.Should().Be(10m);
    }

    [Fact]
    public void Complete_WithSufficientPayment_ShouldChangeStatus()
    {
        // Arrange
        var sale = new Sale(1);
        var product = new Product("123456", "Test", 10m);
        sale.AddItem(product, 1);
        sale.AddPayment(PaymentMethod.Cash, 10m);

        // Act
        sale.Complete();

        // Assert
        sale.Status.Should().Be(SaleStatus.Completed);
    }

    [Fact]
    public void Complete_WithInsufficientPayment_ShouldThrow()
    {
        // Arrange
        var sale = new Sale(1);
        var product = new Product("123456", "Test", 10m);
        sale.AddItem(product, 1);
        sale.AddPayment(PaymentMethod.Cash, 5m);

        // Act
        var act = () => sale.Complete();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*insufficient*");
    }

    [Fact]
    public void GetChange_ShouldReturnCorrectValue()
    {
        // Arrange
        var sale = new Sale(1);
        var product = new Product("123456", "Test", 10m);
        sale.AddItem(product, 1);
        sale.AddPayment(PaymentMethod.Cash, 20m);

        // Act
        var change = sale.GetChange();

        // Assert
        change.Should().Be(10m);
    }

    [Fact]
    public void Cancel_ShouldChangeStatus()
    {
        // Arrange
        var sale = new Sale(1);

        // Act
        sale.Cancel();

        // Assert
        sale.Status.Should().Be(SaleStatus.Cancelled);
    }
}
