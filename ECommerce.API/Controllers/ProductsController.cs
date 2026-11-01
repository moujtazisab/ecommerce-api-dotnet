
using ECommerce.API.Data;
using ECommerce.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Products
    // Public catalog
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAllProducts()
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.IsAvailable)
            .Select(p => new
            {
                p.ProductId,
                p.Name,
                p.Description,
                p.Brand,
                p.Sku,
                p.Price,
                p.OriginalPrice,
                p.Quantity,
                p.MinStockThreshold,
                p.ImageUrl,
                p.SubCategoryId,
                p.SupplierId,
                p.DiscountId
            })
            .ToListAsync();

        return Ok(products);
    }

    // GET: api/Products/5
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProductById(int id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Where(p => p.ProductId == id && p.IsActive && p.IsAvailable)
            .Select(p => new
            {
                p.ProductId,
                p.Name,
                p.Description,
                p.Brand,
                p.Sku,
                p.Price,
                p.OriginalPrice,
                p.Quantity,
                p.MinStockThreshold,
                p.ImageUrl,
                p.SubCategoryId,
                p.SupplierId,
                p.DiscountId
            })
            .FirstOrDefaultAsync();

        if (product == null)
            return NotFound(new { message = "Product not found." });

        return Ok(product);
    }

    // GET: api/Products/admin/all
    // Admin: all products, including inactive products
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/all")]
    public async Task<IActionResult> GetAllProductsForAdmin()
    {
        var products = await _context.Products
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.ProductId,
                p.Name,
                p.Description,
                p.Brand,
                p.Sku,
                p.Price,
                p.OriginalPrice,
                p.Quantity,
                p.MinStockThreshold,
                p.ImageUrl,
                p.SubCategoryId,
                p.SupplierId,
                p.DiscountId,
                p.IsActive,
                p.IsAvailable,
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync();

        return Ok(products);
    }

    // POST: api/Products
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateProduct(
        [FromBody] ProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Product name is required." });

        if (string.IsNullOrWhiteSpace(request.Sku))
            return BadRequest(new { message = "SKU is required." });

        if (request.Name.Length > 200)
            return BadRequest(new { message = "Name cannot exceed 200 characters." });

        if (request.Sku.Length > 100)
            return BadRequest(new { message = "SKU cannot exceed 100 characters." });

        if (request.Brand?.Length > 100)
            return BadRequest(new { message = "Brand cannot exceed 100 characters." });

        if (request.ImageUrl?.Length > 500)
            return BadRequest(new { message = "Image URL cannot exceed 500 characters." });

        if (request.Price <= 0)
            return BadRequest(new { message = "Price must be greater than zero." });

        if (request.OriginalPrice.HasValue && request.OriginalPrice.Value < 0)
            return BadRequest(new { message = "Original price cannot be negative." });

        if (request.Quantity < 0 || request.MinStockThreshold < 0)
            return BadRequest(new { message = "Stock values cannot be negative." });

        var subCategoryExists = await _context.SubCategories
            .AnyAsync(s => s.SubCategoryId == request.SubCategoryId);

        if (!subCategoryExists)
            return BadRequest(new { message = "Subcategory not found." });

        if (request.SupplierId.HasValue)
        {
            var supplierExists = await _context.Suppliers
                .AnyAsync(s => s.SupplierId == request.SupplierId.Value);

            if (!supplierExists)
                return BadRequest(new { message = "Supplier not found." });
        }

        if (request.DiscountId.HasValue)
        {
            var discountExists = await _context.Discounts
                .AnyAsync(d => d.DiscountId == request.DiscountId.Value);

            if (!discountExists)
                return BadRequest(new { message = "Discount not found." });
        }

        var sku = request.Sku.Trim();

        var skuExists = await _context.Products
            .AnyAsync(p => p.Sku == sku);

        if (skuExists)
            return Conflict(new { message = "This SKU already exists." });

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Brand = request.Brand?.Trim(),
            Sku = sku,
            Price = request.Price,
            OriginalPrice = request.OriginalPrice,
            Quantity = request.Quantity,
            MinStockThreshold = request.MinStockThreshold,
            ImageUrl = request.ImageUrl?.Trim(),
            SubCategoryId = request.SubCategoryId,
            SupplierId = request.SupplierId,
            DiscountId = request.DiscountId,
            IsActive = true,
            IsAvailable = request.IsAvailable,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Unable to create product. Check SKU and related data." });
        }

        return CreatedAtAction(
            nameof(GetProductById),
            new { id = product.ProductId },
            new
            {
                product.ProductId,
                product.Name,
                product.Sku,
                product.Price,
                product.Quantity
            });
    }

    // PUT: api/Products/5
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProduct(
        int id,
        [FromBody] ProductRequest request)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
            return NotFound(new { message = "Product not found." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Product name is required." });

        if (string.IsNullOrWhiteSpace(request.Sku))
            return BadRequest(new { message = "SKU is required." });

        if (request.Name.Length > 200 ||
            request.Sku.Length > 100 ||
            request.Brand?.Length > 100 ||
            request.ImageUrl?.Length > 500)
        {
            return BadRequest(new { message = "One or more text fields exceed their maximum length." });
        }

        if (request.Price <= 0)
            return BadRequest(new { message = "Price must be greater than zero." });

        if (request.OriginalPrice.HasValue && request.OriginalPrice.Value < 0)
            return BadRequest(new { message = "Original price cannot be negative." });

        if (request.Quantity < 0 || request.MinStockThreshold < 0)
            return BadRequest(new { message = "Stock values cannot be negative." });

        var subCategoryExists = await _context.SubCategories
            .AnyAsync(s => s.SubCategoryId == request.SubCategoryId);

        if (!subCategoryExists)
            return BadRequest(new { message = "Subcategory not found." });

        if (request.SupplierId.HasValue)
        {
            var supplierExists = await _context.Suppliers
                .AnyAsync(s => s.SupplierId == request.SupplierId.Value);

            if (!supplierExists)
                return BadRequest(new { message = "Supplier not found." });
        }

        if (request.DiscountId.HasValue)
        {
            var discountExists = await _context.Discounts
                .AnyAsync(d => d.DiscountId == request.DiscountId.Value);

            if (!discountExists)
                return BadRequest(new { message = "Discount not found." });
        }

        var sku = request.Sku.Trim();

        var skuExists = await _context.Products
            .AnyAsync(p => p.Sku == sku && p.ProductId != id);

        if (skuExists)
            return Conflict(new { message = "This SKU already exists." });

        product.Name = request.Name.Trim();
        product.Description = request.Description;
        product.Brand = request.Brand?.Trim();
        product.Sku = sku;
        product.Price = request.Price;
        product.OriginalPrice = request.OriginalPrice;
        product.Quantity = request.Quantity;
        product.MinStockThreshold = request.MinStockThreshold;
        product.ImageUrl = request.ImageUrl?.Trim();
        product.SubCategoryId = request.SubCategoryId;
        product.SupplierId = request.SupplierId;
        product.DiscountId = request.DiscountId;
        product.IsAvailable = request.IsAvailable;
        product.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Unable to update product. Check SKU and related data." });
        }

        return Ok(new { message = "Product updated successfully." });
    }

    // Soft delete
    // DELETE: api/Products/5
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
            return NotFound(new { message = "Product not found." });

        // Vérifier si le produit a déjà été vendu
        var hasSales = await _context.OrderItems
            .AnyAsync(oi => oi.ProductId == id);

        if (hasSales)
        {
            return Conflict(new
            {
                message = "Impossible de supprimer ce produit : il existe déjà dans l'historique des commandes. Vous pouvez le désactiver."
            });
        }

        // Suppression définitive si aucune vente
        _context.Products.Remove(product);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Impossible de supprimer ce produit car il est encore utilisé dans d'autres données (panier, avis, etc.)."
            });
        }

        return Ok(new
        {
            message = "Product deleted successfully."
        });
    }
}

public class ProductRequest
{
    public int SubCategoryId { get; set; }
    public int? SupplierId { get; set; }
    public int? DiscountId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public string Sku { get; set; } = string.Empty;

    public decimal? OriginalPrice { get; set; }
    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public int Quantity { get; set; }
    public int MinStockThreshold { get; set; }

    public bool IsAvailable { get; set; } = true;
}
