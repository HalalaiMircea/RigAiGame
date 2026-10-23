using System.Text;
using CommunityToolkit.VectorData.SqliteVec;
using Microsoft.Extensions.AI;
using OllamaSharp;
using RigAiGame;
using Spectre.Console;

// sets height and width in columns... the native Console API doesnt work with modern win 11 terminal
Console.Write($"\e[8;{70};{100}t");

using IEmbeddingGenerator<string, Embedding<float>> ollamaEmbeddingGenerator =
    new OllamaApiClient("http://localhost:11434/", "qwen3-embedding:8b");
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

using var ollama = new OllamaApiClient("http://localhost:11434/", "qwen3.5:4b");
var chat = new Chat(
    ollama,
    """
    You are an expert gaming rig troubleshooting assistant. 
    Use the provided documentation as your primary source of truth to answer questions. 
    If the documentation doesn't fully cover the issue, blend your own advanced technical knowledge and training data to provide a helpful solution.
    Always prioritize the documentation if there is a conflict. 
    If you used the documentation, cite source files at the end with format "Source(s): ", comma separated if multiple.
    """
);
// chat.Options?.NumThread = 4;
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
    //TODO: maybe rewrite the follow-up questions using the LLM, and send that to embedding model??
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
