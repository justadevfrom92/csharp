using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SharedCacheAndSettings.Settings;

namespace SharedCacheAndSettings.Variations;

// The same setting (Shop:ShopName) read five ways. Open /variations/options, edit "ShopName" in appsettings.json
// while the app runs, and open it again: they don't all change.
[Route("variations/options")]
public class OptionsVariation : ControllerBase
{
    private readonly IOptions<ShopSettings> options;
    private readonly IOptionsSnapshot<ShopSettings> snapshot;
    private readonly IOptionsMonitor<ShopSettings> monitor;
    private readonly IConfiguration configuration;

    public OptionsVariation(IOptions<ShopSettings> options, IOptionsSnapshot<ShopSettings> snapshot, IOptionsMonitor<ShopSettings> monitor, IConfiguration configuration)
    {
        this.options = options;
        this.snapshot = snapshot;
        this.monitor = monitor;
        this.configuration = configuration;
    }

    [HttpGet("")]
    public IActionResult Read()
    {
        return Ok(new
        {
            // ORIGINAL: IOptions<T> - read ONCE (first use) and never again. A singleton. Edits need a restart.
            iOptions = options.Value.ShopName,
            // IOptionsSnapshot<T> - read again for EACH REQUEST. Scoped: can't be injected into a singleton.
            iOptionsSnapshot = snapshot.Value.ShopName,
            // IOptionsMonitor<T> - always the LATEST value; works in singletons; OnChange(...) tells you when it changes (7.5 uses it).
            iOptionsMonitor = monitor.CurrentValue.ShopName,
            // IConfiguration by key - live too, but a plain string: no class, no validation, typos return null.
            configurationKey = configuration["Shop:ShopName"],
            configurationTypo = configuration["Shop:ShopNmae"],
            // Bind a NEW object by hand each time - live, but skips ValidateOnStart/ValidateDataAnnotations.
            boundByHand = configuration.GetSection("Shop").Get<ShopSettings>()?.ShopName,
        });
    }
}
