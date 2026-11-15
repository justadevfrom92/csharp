using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;

namespace FormSecurityXssCsrf.Variations;

// The SAME user input made safe for different places in a page. Each place needs its OWN encoder:
// HTML-encoded text is still dangerous inside a <script> block or a URL. Returns plain text, so this page
// itself shows the results without running anything. Try /variations/encoding?input=...
[Route("variations/encoding")]
public class EncodingVariation : Controller
{
    [HttpGet("")]
    public IActionResult Show(string input = "<script>alert('hi')</script> & \"quotes\"")
    {
        string html = HtmlEncoder.Default.Encode(input);            // what Razor's @value does (original)
        string script = JavaScriptEncoder.Default.Encode(input);    // for a value inside a <script> string: var name = "...";
        string url = UrlEncoder.Default.Encode(input);              // for a value inside a URL: /search?q=...
        string raw = input;                                         // what @Html.Raw(value) writes: NOTHING encoded

        string report = $"input:              {input}\n" +
                        $"HTML  (Razor @):    {html}\n" +
                        $"JavaScript string:  {script}\n" +
                        $"URL query value:    {url}\n" +
                        $"Html.Raw (unsafe):  {raw}\n";
        return Content(report, "text/plain");
    }
}
