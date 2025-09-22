using Serilog;
using System.Diagnostics;
using System.Text;

namespace UserAuthAPI.Api.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestTime = DateTime.UtcNow;

        // Log request
        await LogRequest(context);

        // Capture original response stream
        var originalResponseStream = context.Response.Body;
        using var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Log response
            await LogResponse(context, responseStream, requestTime, stopwatch.ElapsedMilliseconds);

            // Copy response back to original stream
            responseStream.Seek(0, SeekOrigin.Begin);
            await responseStream.CopyToAsync(originalResponseStream);
            context.Response.Body = originalResponseStream;
        }
    }

    private async Task LogRequest(HttpContext context)
    {
        var request = context.Request;
        var requestBody = string.Empty;

        // Read request body for POST/PUT requests
        if (request.Method == "POST" || request.Method == "PUT")
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            // Mask sensitive data
            if (requestBody.Contains("password", StringComparison.OrdinalIgnoreCase))
            {
                requestBody = "[REDACTED - Contains sensitive data]";
            }
        }

        _logger.LogInformation("HTTP Request: {Method} {Path} {QueryString} {RequestBody}",
            request.Method,
            request.Path,
            request.QueryString,
            requestBody);
    }

    private async Task LogResponse(HttpContext context, MemoryStream responseStream, DateTime requestTime, long elapsedMs)
    {
        var response = context.Response;

        responseStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(responseStream).ReadToEndAsync();
        responseStream.Seek(0, SeekOrigin.Begin);

        // Mask sensitive data in response
        if (responseBody.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            responseBody.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            responseBody = "[REDACTED - Contains sensitive data]";
        }

        var logLevel = response.StatusCode >= 500 ? LogLevel.Error :
                      response.StatusCode >= 400 ? LogLevel.Warning :
                      LogLevel.Information;

        _logger.Log(logLevel,
            "HTTP Response: {StatusCode} {ContentType} {ResponseBody} {ElapsedMs}ms {RequestTime}",
            response.StatusCode,
            response.ContentType,
            responseBody.Length > 1000 ? "[Large Response - Truncated]" : responseBody,
            elapsedMs,
            requestTime);
    }
}