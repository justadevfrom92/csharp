using Microsoft.AspNetCore.Mvc;

namespace ProductCatalog.Variations;

// The same "product id in the URL", with different route CONSTRAINTS. Try /variations/routes/....
// (A class deriving from Controller is found by MVC even though its name ends in "Variation";
// [Route] gives it its own URLs, so it doesn't clash with ProductsController.)
[Route("variations/routes")]
public class RouteConstraintVariation : Controller
{
    // ORIGINAL style ({id:int}): /int/abc doesn't MATCH the route at all -> 404.
    [HttpGet("int/{id:int}")]
    public IActionResult WithIntConstraint(int id)
    {
        return Content($"id={id}");
    }

    // No constraint: /plain/abc DOES match; binding "abc" to int fails, so id = 0 and ModelState is invalid.
    // Forgetting to check ModelState here means treating "abc" as product 0.
    [HttpGet("plain/{id}")]
    public IActionResult WithoutConstraint(int id)
    {
        return Content($"id={id}, ModelState.IsValid={ModelState.IsValid}");
    }

    // Several constraints: an int AND at least 1. /positive/0 -> 404 before the action runs.
    [HttpGet("positive/{id:int:min(1)}")]
    public IActionResult WithMinConstraint(int id)
    {
        return Content($"id={id}");
    }

    // The id in the QUERY STRING instead of the path: /variations/routes/query?id=5. Missing -> id = 0.
    [HttpGet("query")]
    public IActionResult FromQueryString([FromQuery] int id)
    {
        return Content($"id={id}");
    }
}
