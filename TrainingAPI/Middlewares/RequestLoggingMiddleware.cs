using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace TrainingAPI.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var method = context.Request.Method;
            var path = context.Request.Path;

            _logger.LogInformation("Nhận Request: Method = {Method}, Đường dẫn = {Path}", method, path);

            await _next(context);

            var statusCode = context.Response.StatusCode;

            _logger.LogInformation("Trả Response: Đường dẫn = {Path}, Mã trạng thái = {StatusCode}", path, statusCode);
        }
    }
}