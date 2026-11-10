using Microsoft.AspNetCore.Mvc;

namespace OutputCachingCatalog.Controllers;

// Serves /robots.txt - the file search-engine crawlers read to see what they may index.
// It's an action (not a static file in wwwroot) so the answer can depend on the environment:
// only Production may be indexed; Development/UAT/Staging tell every crawler to stay out.
// The route for it is in Program.cs and uses .ShortCircuit().
public class RobotsController : Controller
{
    private readonly IWebHostEnvironment environment;

    public RobotsController(IWebHostEnvironment environment)
    {
        this.environment = environment;
    }

    [HttpGet] // only GET; a POST to /robots.txt gets 405 Method Not Allowed
    public IActionResult Get()
    {
        string rules;
        if (environment.IsProduction())
        {
            rules = "User-agent: *\nAllow: /\nDisallow: /health\n";
        }
        else
        {
            // A test site showing up in Google search results is a classic accident. Block everything.
            rules = "User-agent: *\nDisallow: /\n";
        }
        return Content(rules, "text/plain");
    }
}
