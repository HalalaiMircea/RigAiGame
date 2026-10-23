using System.Text;
using CommunityToolkit.VectorData.SqliteVec;
using Microsoft.Extensions.AI;
using OllamaSharp;
using RigAiGame;
using Spectre.Console;

using IEmbeddingGenerator<string, Embedding<float>> ollamaEmbeddingGenerator =
    new OllamaApiClient(new Uri("http://localhost:11434/"), "qwen3-embedding:8b");
var vectorStore = new SqliteVectorStore(
    "Data Source=./embeddings.db",
    new SqliteVectorStoreOptions
    {
        EmbeddingGenerator = ollamaEmbeddingGenerator
    }
);
var docIngestionService = new DocumentIngestionService(ollamaEmbeddingGenerator, vectorStore);

Style userPromptStyle = Style.Plain.Background(Color.Purple);
AnsiConsole.Write(
    new FigletText("RigAiGame (RAG)")
        .Color(Color.Green)
);

if (AnsiConsole.Confirm("Refresh vector database embeddings?", false))
{
    await docIngestionService.ProcessDirectoryAsync("E:\\My Encyclopedia");
}
var collection = vectorStore.GetCollection<string, DocumentChunk>("documents");

using var ollama = new OllamaApiClient(new Uri("http://localhost:11434/"), "qwen3.5:4b");
var chat = new Chat(
    ollama,
    """
    Answer using the supplied documentation. If the answer is not present, say so.
    Do not invent fixes or settings. Cite source files at the end with format "Source: ", comma separated if multiple.
    """
);
var cliPrompt = "[green]Troubleshooting your gaming rig? Ask me[/] >>";
while (true)
{
    string question = AnsiConsole.Prompt(new TextPrompt<string>(cliPrompt).PromptStyle(userPromptStyle));

    //TODO: Move to automatic embedding...
    var searchResults = collection.SearchAsync((await ollamaEmbeddingGenerator.GenerateAsync(question)).Vector, top: 3);
    var contextSB = new StringBuilder();
    await foreach (var result in searchResults)
    {
        contextSB.AppendLine($"Source: {result.Record.FileName}\n{result.Record.Text}");
        contextSB.AppendLine();
    }
    //TODO: Remove the previous context to not fill it with context irrelevant to the current question? But then how can it do more advanced conversations
    // This is a design question... multiple tutorials use this system prompt. SO WHAT IS A RAG?? Just a citation machine?
    // RAG - is augmented ffs
    var prompt = $"## Context:\n{contextSB}\n\n## Question:\n{question}";

    //TODO: Add a progress spinner, due to it taking time until first stream in the foreach.
    await foreach (var token in chat.SendAsync(prompt))
    {
        AnsiConsole.Write(token);
    }

    AnsiConsole.WriteLine();
    cliPrompt = ">>";
}

//TODO: Setup the SQLite vector store + embedding generation and do the doc ingestion. Need to do chunking???
//TODO: When searching for retrieval, use the same embeddingGenerator automatically
