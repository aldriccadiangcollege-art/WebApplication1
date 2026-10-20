using Microsoft.AspNetCore.Mvc;
using WebApplication1.Exceptions;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private static readonly List<Product> _products =
    [
        new() { Id = 1, Name = "Keyboard", Sku = "KB-001", Price = 49.99m },
        new() { Id = 2, Name = "Mouse",    Sku = "MS-001", Price = 19.99m },
        new() { Id = 3, Name = "Monitor",  Sku = "MN-001", Price = 199.99m },
    ];
    private static int _nextId = 4;

    private static Product FindOrThrow(int id) =>
        _products.FirstOrDefault(p => p.Id == id)
        ?? throw new NotFoundException($"Product {id} was not found.");

    private static void EnsureSkuIsUnique(string sku, int? excludeId = null)
    {
        if (_products.Any(p => p.Id != excludeId &&
                               p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase)))
            throw new ConflictException($"A product with SKU '{sku}' already exists.");
    }

    // 200 OK, paged envelope
    [HttpGet]
    public ActionResult<PagedResponse<Product>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = _products.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var data = _products.OrderBy(p => p.Id)
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

        return Ok(new PagedResponse<Product>(
            data, new PageMeta(page, pageSize, totalCount, totalPages)));
    }

    // 200 OK, single resource returned directly
    [HttpGet("{id:int}")]
    public ActionResult<Product> GetById(int id) => Ok(FindOrThrow(id));

    // 201 Created + Location header
    [HttpPost]
    public ActionResult<Product> Create(CreateProductRequest request)
    {
        EnsureSkuIsUnique(request.Sku);

        var product = new Product
        {
            Id = _nextId++,
            Name = request.Name,
            Sku = request.Sku,
            Price = request.Price,
            SalePrice = request.SalePrice
        };
        _products.Add(product);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // 200 OK
    [HttpPut("{id:int}")]
    public ActionResult<Product> Update(int id, UpdateProductRequest request)
    {
        var existing = FindOrThrow(id);
        EnsureSkuIsUnique(request.Sku, excludeId: id);

        existing.Name = request.Name;
        existing.Sku = request.Sku;
        existing.Price = request.Price;
        existing.SalePrice = request.SalePrice;
        return Ok(existing);
    }

    // 204 No Content
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        _products.Remove(FindOrThrow(id));
        return NoContent();
    }
}