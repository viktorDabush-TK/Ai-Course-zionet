#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;
using Microsoft.Extensions.VectorData;
using SemanticKernelPlayground.Plugins.Models;
using System.Text;

namespace SemanticKernelPlayground.Plugins;

public class CodeDocPlugin(IVectorStore vectorStore, ITextEmbeddingGenerationService embeddingService)
{
    private const string IndexName = "code-docs";
    private readonly GitRepoPlugin _gitRepoPlugin = new();

    [KernelFunction]
    public async Task<string> IngestCodebaseAsync(string? path = null)
    {
        var collection = vectorStore.GetCollection<string, TextChunk>(IndexName);
        await collection.CreateCollectionIfNotExistsAsync();

        // Use current Git repo if path not explicitly passed
        if (string.IsNullOrWhiteSpace(path))
        {
            path = _gitRepoPlugin.GetRepoPathInternal();
            if (string.IsNullOrEmpty(path))
                return "No path provided and no Git repo selected.";
        }

        if (!Directory.Exists(path))
            return $"Invalid path: {path}";

        var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
        int totalChunks = 0;

        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file, Encoding.UTF8);
            var chunks = TextChunker.SplitPlainTextLines(content, maxTokensPerLine: 400);

            foreach (var (chunkText, i) in chunks.Select((text, i) => (text, i)))
            {
                var embedding = await embeddingService.GenerateEmbeddingAsync(chunkText);

                var record = new TextChunk
                {
                    Key = $"{file}#{i}",
                    Text = chunkText,
                    TextEmbedding = embedding
                };

                Console.WriteLine($"Upserting chunk: {record.Key}");
                await collection.UpsertAsync(record);
                totalChunks++;
            }
        }

        return $"Ingested {files.Length} files, total {totalChunks} chunks into '{IndexName}'.";
    }

    [KernelFunction]
    public async Task<string> SearchCodeDocsAsync(string query)
    {
        var collection = vectorStore.GetCollection<string, TextChunk>(IndexName);
        await collection.CreateCollectionIfNotExistsAsync();

        var embedding = await embeddingService.GenerateEmbeddingAsync(query);

        try
        {
            var results = collection.SearchEmbeddingAsync(embedding, 5);
            var sb = new StringBuilder();

            await foreach (var result in results)
            {
                var chunk = result.Record;
                sb.AppendLine($"- {chunk.Text}");
            }

            if (sb.Length == 0)
            {
                await IngestCodebaseAsync();
                return "No results found. The repo has now been indexed — try again.";
            }

            return sb.ToString();
        }
        catch (VectorStoreOperationException)
        {
            var ingestMessage = await IngestCodebaseAsync();
            return $"Collection was missing. Auto-ingested:\n{ingestMessage}";
        }
    }

}
