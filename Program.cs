using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernelPlayground.Plugins;
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001 
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .Build();

var modelName = configuration["ModelName"] ?? throw new ApplicationException("ModelName not found");
var endpoint = configuration["Endpoint"] ?? throw new ApplicationException("Endpoint not found");
var embedding = configuration["EmbeddingModel"] ?? throw new ApplicationException("ModelName not found");
var apiKey = configuration["ApiKey"] ?? throw new ApplicationException("ApiKey not found");

var builder = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(modelName, endpoint, apiKey)
    .AddAzureOpenAITextEmbeddingGeneration(embedding, endpoint, apiKey)
    .AddInMemoryVectorStore();

var kernel = builder.Build();
var vectorStore = kernel.GetRequiredService<IVectorStore>();
var textEmbeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
kernel.ImportPluginFromObject(new GitRepoPlugin(), "GitRepo");
kernel.ImportPluginFromObject(new GitLogPlugin(), "GitLog");
kernel.ImportPluginFromObject(new ReleaseNotesPlugin(kernel), "ReleaseNotes");
kernel.ImportPluginFromObject(new VersionManagerPlugin(), "VersionManager");
var codeDocPlugin = new CodeDocPlugin(vectorStore, textEmbeddingGenerator);
kernel.ImportPluginFromObject(codeDocPlugin, "CodeDoc");

GitRepoPlugin.OnRepoSelectedAsync = async (repoPath) =>
{
    Console.WriteLine($"[Auto-Ingest] Indexing codebase from: {repoPath}");
    var result = await codeDocPlugin.IngestCodebaseAsync(repoPath);
    Console.WriteLine($"[Auto-Ingest] Done: {result}");
};


var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

AzureOpenAIPromptExecutionSettings openAiPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

var history = new ChatHistory();
var systemPrompt = File.ReadAllText("system-prompt.txt");
history.AddSystemMessage(systemPrompt);

do
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("Me > ");
    Console.ResetColor();

    var userInput = Console.ReadLine();
    if (userInput == "exit")
    {
        break;
    }

    history.AddUserMessage(userInput!);

    var streamingResponse =
        chatCompletionService.GetStreamingChatMessageContentsAsync(
            history,
            openAiPromptExecutionSettings,
            kernel);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("Agent > ");
    Console.ResetColor();

    var fullResponse = "";
    await foreach (var chunk in streamingResponse)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(chunk.Content);
        Console.ResetColor();
        fullResponse += chunk.Content;
    }
    Console.WriteLine();

    history.AddMessage(AuthorRole.Assistant, fullResponse);


} while (true);
#pragma warning restore SKEXP0010
#pragma warning restore SKEXP0001