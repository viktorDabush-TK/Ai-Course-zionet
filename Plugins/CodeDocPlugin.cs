#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;
using Microsoft.Extensions.VectorData;
using SemanticKernelPlayground.Plugins.Models;
using System.Text;
using System.ComponentModel;

namespace SemanticKernelPlayground.Plugins;

/// <summary>
/// This plugin enables documentation-style search and introspection over a C# codebase
/// using semantic chunking and vector embeddings.
/// </summary>
public class CodeDocPlugin(IVectorStore vectorStore, ITextEmbeddingGenerationService embeddingService)
{
    private const string IndexName = "code-docs";
    private readonly GitRepoPlugin _gitRepoPlugin = new();

    /// <summary>
    /// Reads your C# codebase, splits it into semantic chunks, generates embeddings,
    /// and stores them in a vector store for later retrieval.
    /// </summary>
    /// <param name="path">Optional path to the root of your C# project. If omitted, the active Git repository path is used.</param>
    [KernelFunction, Description("Indexes your C# project by generating embeddings for all .cs files and storing them in a vector store.")]
    public async Task<string> IngestCodebaseAsync(
        [Description("Optional path to your codebase root folder. Uses Git repo path if empty.")] string? path = null)
    {
        var collection = vectorStore.GetCollection<string, TextChunk>(IndexName);
        await collection.CreateCollectionIfNotExistsAsync();

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

    /// <summary>
    /// Performs a semantic search against the previously indexed codebase.
    /// </summary>
    /// <param name="query">A natural language query such as "what plugins do I have" or "list all functions".</param>
    /// <returns>Top relevant code chunks matching your query.</returns>
    [KernelFunction, Description("Searches the indexed codebase using a natural language query. Returns relevant code chunks.")]
    public async Task<string> SearchCodeDocsAsync(
        [Description("The natural language query to search the codebase with.")] string query)
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
                return "No results found. I’ve now indexed the repo — try your question again.";
            }

            return sb.ToString();
        }
        catch (VectorStoreOperationException)
        {
            var ingestMessage = await IngestCodebaseAsync();
            return $"Vector collection was missing. I’ve now indexed your repo:\n{ingestMessage}";
        }
    }
}
