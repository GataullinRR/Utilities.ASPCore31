using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Extensions;

namespace Utilities.Types
{
    /// <summary>
    /// See
    /// https://elanderson.net/2019/12/log-requests-and-responses-in-asp-net-core-3/
    /// </summary>
    public class RequestResponseLoggingMiddleware
    {
        readonly RequestDelegate _next;
        readonly ILogger _logger;
        readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public RequestResponseLoggingMiddleware(RequestDelegate next,
                                                ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory
                      .CreateLogger<RequestResponseLoggingMiddleware>();
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public async Task Invoke(HttpContext context)
        {
            await logRequest(context);

            await logResponse(context);
        }

        async Task logRequest(HttpContext context)
        {
            var requestStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(requestStream);
            _logger.LogInformation($"Request to: {context.Request.Method}:{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}/{context.Request.QueryString} {Environment.NewLine}Body:{ReadStreamInChunks(requestStream).Take(10000).Aggregate()}");
           
            requestStream.Position = 0;
            context.Request.Body = requestStream;

            string ReadStreamInChunks(Stream stream)
            {
                const int readChunkBufferLength = 4096;
                stream.Seek(0, SeekOrigin.Begin);
                using var textWriter = new StringWriter();
                var reader = new StreamReader(stream);
                var readChunk = new char[readChunkBufferLength];
                int readChunkLength;
                do
                {
                    readChunkLength = reader.ReadBlock(readChunk, 0, readChunkBufferLength);
                    textWriter.Write(readChunk, 0, readChunkLength);
                }
                while (readChunkLength > 0);

                return textWriter.ToString();
            }
        }

        async Task logResponse(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;
            await using var responseBody = _recyclableMemoryStreamManager.GetStream();
            context.Response.Body = responseBody;

            await _next(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            _logger.LogInformation($"Response from: {context.Request.Method}:{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}/{context.Request.QueryString}" +
                                   $"{Environment.NewLine}Status: {(System.Net.HttpStatusCode)context.Response.StatusCode} ({context.Response.StatusCode})" +
                                   $"{Environment.NewLine}Body: {text.Take(10000).Aggregate()}");

            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}
