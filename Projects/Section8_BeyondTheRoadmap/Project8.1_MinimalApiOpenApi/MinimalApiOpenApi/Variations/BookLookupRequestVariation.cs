using Microsoft.AspNetCore.Mvc;
using MinimalApiOpenApi.Stores;

namespace MinimalApiOpenApi.Variations;

// [AsParameters]: gather an endpoint's parameters (route, query, services) into ONE object instead of a long list.
// GOTCHA (found by the test): in minimal APIs a non-nullable parameter WITHOUT a default is REQUIRED -
// "bool Uppercase" made every request without ?uppercase=... fail with 400. "= false" makes it optional.
public record BookLookupRequestVariation([FromRoute] int Id, BookStore Store, [FromQuery] bool Uppercase = false);
