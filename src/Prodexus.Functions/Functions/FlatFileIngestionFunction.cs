using System.Net;
using Prodexus.Functions.Mapping;
using Prodexus.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Prodexus.Functions.Functions;

public sealed class FlatFileIngestionFunction
{
    private readonly IFlatFileParser _parser;
    private readonly IRecordPersistor _persistor;
    private readonly RowMapper _rowMapper;

    public FlatFileIngestionFunction(IFlatFileParser parser, IRecordPersistor persistor, RowMapper rowMapper)
    {
        _parser = parser;
        _persistor = persistor;
        _rowMapper = rowMapper;
    }

    [Function("health")]
    public async Task<HttpResponseData> Health(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData request)
    {
        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            status = "ok",
            services = new
            {
                sql = true,
                blobStorage = true,
            },
        });

        return response;
    }

    [Function("ingest-file")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "ingest/file")] HttpRequestData request)
    {
        try
        {
            var rawBody = await new StreamReader(request.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, "The file content is empty.");
            }

            var rows = _parser.Parse(rawBody);
            if (rows.Count == 0)
            {
                return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, "No rows could be parsed from the uploaded content.");
            }

            var records = rows
                .Select((row, index) => _rowMapper.Map(row, index + 1))
                .ToList();

            var insertedCount = await _persistor.PersistAsync(records);

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                status = "ok",
                rowsProcessed = records.Count,
                inserted = insertedCount,
                errors = Array.Empty<string>(),
            });

            return response;
        }
        catch (Exception ex)
        {
            return await CreateErrorResponseAsync(request, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private static async Task<HttpResponseData> CreateErrorResponseAsync(HttpRequestData request, HttpStatusCode statusCode, string error)
    {
        var response = request.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new
        {
            status = "error",
            rowsProcessed = 0,
            inserted = 0,
            errors = new[] { error },
        });

        return response;
    }
}

