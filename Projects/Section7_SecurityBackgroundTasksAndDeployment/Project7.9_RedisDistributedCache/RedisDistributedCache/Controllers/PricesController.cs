using Microsoft.AspNetCore.Mvc;
using RedisDistributedCache.Databases;
using RedisDistributedCache.Models;
using RedisDistributedCache.Services;

namespace RedisDistributedCache.Controllers;

[ApiController]
public class PricesController : ControllerBase
{
    private readonly ProductPriceCacheService priceService;
    private readonly ProductPriceDatabase database;

    public PricesController(ProductPriceCacheService priceService, ProductPriceDatabase database)
    {
        this.priceService = priceService;
        this.database = database;
    }

    // GET /prices/1
    [HttpGet("/prices/{productId:int}")]
    public async Task<IActionResult> GetPrice(int productId)
    {
        if (productId <= 0)
        {
            return BadRequest("Product id must be more than 0.");
        }
        ProductPriceModel? product = await priceService.GetPriceAsync(productId);
        if (product == null)
        {
            return NotFound();
        }
        return Ok(product);
    }

    // PUT /prices/1?newPrice=30
    [HttpPut("/prices/{productId:int}")]
    public async Task<IActionResult> UpdatePrice(int productId, decimal newPrice)
    {
        if (productId <= 0 || newPrice <= 0)
        {
            return BadRequest("Product id and price must be more than 0.");
        }
        bool updated = await priceService.UpdatePriceAsync(productId, newPrice);
        if (!updated)
        {
            return NotFound();
        }
        return NoContent();
    }

    // GET /stats - how often the slow database was really used
    [HttpGet("/stats")]
    public IActionResult Stats()
    {
        return Ok(new { databaseReads = database.ReadCount });
    }
}
