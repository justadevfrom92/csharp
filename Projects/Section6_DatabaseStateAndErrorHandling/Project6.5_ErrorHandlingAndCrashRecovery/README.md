# Project 6.5 — Site Error Handling & Crash Recovery

```bash
dotnet run --launch-profile Production
```

```bash
dotnet run --launch-profile Development
```

Open `http://localhost:5065` and click the links. Compare what the browser shows in each mode,
and what lands in the console and in `logs/errors-<date>.json`.

## What it covers

- `UseExceptionHandler("/Error/ServerError")` — catches any unhandled exception (Production).
- `UseDeveloperExceptionPage()` — full stack trace, Development only.
- `UseStatusCodePagesWithReExecute("/Error/Status/{0}")` — friendly 404 / other status pages.
- Custom views: `ServerError` (500), `NotFound` (404), `OtherStatus`.
- **Serilog** structured logging: console + JSON file (warnings and errors only), created before
  the app starts so startup crashes are logged too (`Log.Fatal` + `CloseAndFlush`).

## Exception sanitization — why the user never sees the real error

`/Home/Crash` throws an exception whose message contains a database user, password and server IP.
That's realistic: exception messages and stack traces often contain connection strings, SQL,
file paths, library versions, or other users' data.

If that text reached the browser, an attacker would learn exactly how to attack the system
(this is called **information disclosure**, OWASP "Security Misconfiguration").

So the app separates *who gets what*:

| Who | Gets | Where |
|---|---|---|
| User | Generic message + an **error id** | `ServerError.cshtml` |
| Developers / ops | Full exception, stack trace, path, same error id | Serilog log file |

The error id links the two: a user reports "error 00-4bf9...", support searches the logs for it.

**Why the Developer Exception Page is only for Development:** it shows the stack trace, source lines,
headers and cookies. Great for debugging on your machine, a disaster on a public server. The check is on
the environment name, so a production deployment can't turn it on by accident.

**Why status-code pages "re-execute" instead of redirect:** re-execute keeps the original URL and the
correct status code (404). A redirect would send the browser to `/Error` with status 200 (bad for search
engines and monitoring).

**Why the log file is JSON:** structured logs (`{ "ErrorId": "...", "Path": "/Home/Crash" }`) can be searched
and filtered by field in tools like Seq, Elastic or Azure, instead of grepping text.

**Why `UseExceptionHandler` first in the pipeline:** middleware only catches errors from things that run
after it, so it must wrap everything else.
