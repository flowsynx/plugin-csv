using CsvHelper.Configuration;
using CsvHelper;
using FlowSynx.PluginCore;
using FlowSynx.Plugins.Csv.Models;
using System.Globalization;
using System.Text;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.Csv.Extensions;

namespace FlowSynx.Plugins.Csv.Services;

internal class CsvManager : ICsvManger
{
    private readonly IPluginLogger _logger;

    public CsvManager(IPluginLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public async Task<PluginContext> Read(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var readParameters = parameters.ToObject<ReadParameters>();
        return await ReadEntity(readParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task Write(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var writeParameters = parameters.ToObject<WriteParameters>();
        await WriteEntity(writeParameters, cancellationToken).ConfigureAwait(false);
    }

    #region internal methods
    private async Task<PluginContext> ReadEntity(ReadParameters readParameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = PathHelper.ToUnixPath(readParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new Exception(Resources.ThePathIsNotFile);

        var isExist = File.Exists(path);
        if (!isExist)
            throw new Exception(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

        var records = new List<Dictionary<string, string?>>();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = readParameters.Delimiter ?? ",",
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            DetectColumnCountChanges = true,
            HasHeaderRecord = readParameters.HasHader ?? true,
            BadDataFound = null
        };

        var structuredData = new List<Dictionary<string, object>>();
        using var reader = new StreamReader(path, Encoding.UTF8);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        if (readParameters.HasHader is true)
        {
            await csv.ReadAsync();
            csv.ReadHeader();
            var headers = csv.HeaderRecord;

            while (await csv.ReadAsync())
            {
                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var header in headers)
                {
                    var value = csv.GetField(header);
                    row[header] = value ?? string.Empty;
                }

                structuredData.Add(row);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        else
        {
            while (await csv.ReadAsync())
            {
                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; csv.TryGetField(i, out string? value); i++)
                {
                    row[$"Field{i}"] = value ?? string.Empty;
                }

                structuredData.Add(row);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        var context = new PluginContext(path, "File")
        {
            Format = "CSV",
            Content = File.ReadAllText(path),
            StructuredData = structuredData
        };

        _logger?.LogInfo($"Read '{records.Count}' records from '{path}'");
        return context;
    }

    private async Task WriteEntity(WriteParameters writeParameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = PathHelper.ToUnixPath(writeParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        var dataValue = writeParameters.Data;
        var pluginContextes = new List<PluginContext>();

        if (dataValue is PluginContext pluginContext)
        {
            if (!PathHelper.IsFile(path))
                throw new Exception(Resources.ThePathIsNotFile);

            pluginContextes.Add(pluginContext);
        }
        else if (dataValue is IEnumerable<PluginContext> pluginContextesList)
        {
            if (!PathHelper.IsDirectory(path))
                throw new Exception(Resources.ThePathIsNotDirectory);

            pluginContextes.AddRange(pluginContextesList);
        }
        else if (dataValue is string data)
        {
            if (!PathHelper.IsFile(path))
                throw new Exception(Resources.ThePathIsNotFile);

            var context = CreateContextFromStringData(path, data);
            pluginContextes.Add(context);
        }
        else if (dataValue is List<Dictionary<string, object>> dictionaryData)
        {
            if (!PathHelper.IsFile(path))
                throw new Exception(Resources.ThePathIsNotFile);

            var context = await CreateContextFromDictionaryData(path, writeParameters.Delimiter, dictionaryData, cancellationToken);
            pluginContextes.Add(context);
        }
        else
        {
            throw new NotSupportedException("The entered data format is not supported!");
        }

        foreach (var context in pluginContextes)
        {
            await WriteEntityFromContext(path, context, writeParameters.Overwrite, cancellationToken).ConfigureAwait(false);
        }
    }

    private PluginContext CreateContextFromStringData(string path, string data)
    {
        var root = Path.GetPathRoot(path) ?? string.Empty;
        var relativePath = Path.GetRelativePath(root, path);
        var dataBytesArray = data.IsBase64String() ? data.Base64ToByteArray() : data.ToByteArray();

        return new PluginContext(relativePath, "File")
        {
            RawData = dataBytesArray,
        };
    }

    private async Task<PluginContext> CreateContextFromDictionaryData(
        string path, 
        string? delimiter, 
        List<Dictionary<string, object>> data, 
        CancellationToken cancellationToken)
    {
        var root = Path.GetPathRoot(path) ?? string.Empty;
        var relativePath = Path.GetRelativePath(root, path);

        var headers = new List<string>(data[0].Keys);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter ?? ",",
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim
        };

        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, config);

        foreach (var header in headers)
            csv.WriteField(header);

        await csv.NextRecordAsync();

        foreach (var row in data)
        {
            foreach (var header in headers)
                csv.WriteField(row.TryGetValue(header, out var value) ? value : string.Empty);

            await csv.NextRecordAsync();

            cancellationToken.ThrowIfCancellationRequested();
        }

        return new PluginContext(relativePath, "File")
        {
            Content = writer.ToString(),
        };
    }

    private async Task WriteEntityFromContext(string path, PluginContext context, bool overwrite,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] dataToWrite;

        if (context.RawData is not null)
            dataToWrite = context.RawData;
        else if (context.Content is not null)
            dataToWrite = Encoding.UTF8.GetBytes(context.Content);
        else
            throw new InvalidDataException($"The entered data is invalid for '{context.Id}'");

        var rootPath = Path.GetPathRoot(context.Id);
        string relativePath = context.Id;

        if (!string.IsNullOrEmpty(rootPath))
            relativePath = Path.GetRelativePath(rootPath, context.Id);

        var fullPath = PathHelper.IsDirectory(path) ? PathHelper.Combine(path, relativePath) : path;

        if (!PathHelper.IsFile(fullPath))
            throw new Exception(Resources.ThePathIsNotFile);

        var isExist = File.Exists(fullPath);
        if (isExist && overwrite is false)
            throw new Exception(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, fullPath));

        await File.WriteAllBytesAsync(fullPath, dataToWrite);
    }
    #endregion
}