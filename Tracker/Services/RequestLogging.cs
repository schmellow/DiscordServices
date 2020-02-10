using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Schmellow.DiscordServices.Tracker.Services
{
    public class RequestLogging
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;

        public RequestLogging(ILogger<RequestLogging> logger, RequestDelegate next)
        {
            _logger = logger;
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                _logger.LogInformation(
                    "[{ip}] {method} {url}",
                    context.Request?.HttpContext.Connection.RemoteIpAddress.MapToIPv4(),
                    context.Request?.Method,
                    context.Request?.Path.Value);
                await _next(context);
            }
            finally
            {
                sw.Stop();
                string username = context.User?.Identity?.Name;
                _logger.LogInformation(
                    "[{ip}{user}] {method} {url} => {statusCode} ({elapsed}ms)",
                    context.Request?.HttpContext.Connection.RemoteIpAddress.MapToIPv4(),
                    string.IsNullOrEmpty(username) ? "" : "/" + username,
                    context.Request?.Method,
                    context.Request?.Path.Value,
                    context.Response?.StatusCode,
                    sw.ElapsedMilliseconds);
            }
        }
    }
}
