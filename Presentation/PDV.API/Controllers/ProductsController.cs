using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDV.Core.Entities;
using PDV.Data.Cloud.Context;
using PDV.Shared.DTOs;

namespace PDV.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly CloudDbContext _context;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(CloudDbContext context, ILogger<ProductsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] bool? activeOnly = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.Products.AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(p =>
                p.Barcode.ToLower().Contains(searchLower) ||
                p.Description.ToLower().Contains(searchLower));
        }

        var products = await query
            .OrderBy(p => p.Description)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => MapToDto(p))
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound(new { Message = $"Product with ID {id} not found" });
        }

        return Ok(MapToDto(product));
    }

    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<ProductDto>> GetByBarcode(string barcode)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Barcode == barcode);

        if (product == null)
        {
            return NotFound(new { Message = $"Product with barcode {barcode} not found" });
        }

        return Ok(MapToDto(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest request)
    {
        // Check if barcode already exists
        var existingProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.Barcode == request.Barcode);

        if (existingProduct != null)
        {
            return Conflict(new { Message = $"Product with barcode {request.Barcode} already exists" });
        }

        try
        {
            var product = new Product(
                barcode: request.Barcode,
                description: request.Description,
                unitPrice: request.UnitPrice,
                shortDescription: request.ShortDescription,
                unitOfMeasure: request.UnitOfMeasure,
                stockQuantity: request.StockQuantity,
                taxCode: request.TaxCode,
                taxRate: request.TaxRate);

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product created: {ProductId} - {Description}",
                product.Id, product.Description);

            return CreatedAtAction(
                nameof(GetById),
                new { id = product.Id },
                MapToDto(product));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound(new { Message = $"Product with ID {id} not found" });
        }

        // Check if barcode is being changed to one that already exists
        if (product.Barcode != request.Barcode)
        {
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Barcode == request.Barcode && p.Id != id);

            if (existingProduct != null)
            {
                return Conflict(new { Message = $"Product with barcode {request.Barcode} already exists" });
            }
        }

        try
        {
            product.SetBarcode(request.Barcode);
            product.SetDescription(request.Description);
            product.SetUnitPrice(request.UnitPrice);

            // Update stock (set absolute value)
            var stockDiff = request.StockQuantity - product.StockQuantity;
            if (stockDiff != 0)
            {
                product.UpdateStock(stockDiff);
            }

            if (request.IsActive)
            {
                product.Activate();
            }
            else
            {
                product.Deactivate();
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Product updated: {ProductId} - {Description}",
                product.Id, product.Description);

            return Ok(MapToDto(product));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound(new { Message = $"Product with ID {id} not found" });
        }

        // Soft delete - just deactivate
        product.Deactivate();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product deactivated: {ProductId} - {Description}",
            product.Id, product.Description);

        return NoContent();
    }

    [HttpPost("{id:guid}/stock")]
    public async Task<ActionResult<ProductDto>> UpdateStock(Guid id, [FromBody] UpdateStockRequest request)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound(new { Message = $"Product with ID {id} not found" });
        }

        product.UpdateStock(request.Quantity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Stock updated for product {ProductId}: {Quantity} (new total: {Total})",
            product.Id, request.Quantity, product.StockQuantity);

        return Ok(MapToDto(product));
    }

    private static ProductDto MapToDto(Product product) => new()
    {
        Id = product.Id,
        Barcode = product.Barcode,
        Description = product.Description,
        ShortDescription = product.ShortDescription,
        UnitPrice = product.UnitPrice,
        UnitOfMeasure = product.UnitOfMeasure,
        StockQuantity = product.StockQuantity,
        TaxCode = product.TaxCode,
        TaxRate = product.TaxRate,
        IsActive = product.IsActive
    };
}

public record UpdateStockRequest
{
    public decimal Quantity { get; init; }
}
