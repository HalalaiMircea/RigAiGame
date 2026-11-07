using CommunityToolkit.VectorData.SqliteVec;
using Microsoft.Extensions.AI;
using OllamaSharp;
using RigAiGame;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// sets height and width in columns... the native Console API doesnt work with modern win 11 terminal
// Console.Write($"\e[8;{70};{100}t");

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

// if (AnsiConsole.Confirm("Refresh vector database embeddings?", false))
// {
//     await docIngestionService.ProcessDirectoryAsync("E:\\My Encyclopedia");
// }
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
/*
var cliPrompt = "[green]Troubleshooting your gaming rig? Ask me[/] >>";
while (true)
{
    string question = AnsiConsole.Prompt(new TextPrompt<string>(cliPrompt).PromptStyle(userPromptStyle));

    //TODO: Move to automatic embedding... and improve chunks, it truncates the sources for some questions
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
*/
using IApplication app = Application.Create();
app.Init();
using var window = new Window {BorderStyle = LineStyle.None};

var menuBar = new MenuBar
{
    Menus =
    [
        new MenuBarItem("_File", [
            new MenuItem("_Quit", "Exit app", app.RequestStop)
        ]),
        new MenuBarItem("_Help", [
            new MenuItem("_About...", "Learn about this application", ShowAboutDialog)
        ])
    ],
};

var userPromptField = new TextView
{
    X = 0,
    Y = Pos.AnchorEnd(),
    Height = Dim.Auto(minimumContentDim: 1, maximumContentDim: 4),
    WordWrap = true,
    ScrollBars = true,
};
var sendButton = new Button
{
    X = Pos.AnchorEnd(),
    Y = Pos.Top(userPromptField),
    Width = Dim.Auto(),
    Height = Dim.Fill() + 1,
    Text = "Send",
    ShadowStyle = ShadowStyles.None,
    NoPadding = true,
};
userPromptField.Width = Dim.Fill(to: sendButton, margin: 1);

// TODO: find a way to make Markdig show hard breaks (<br>, \\n or space space \n)
var md = new Markdown
{
    Text = "# Welcome\nThis a demo of the md **renderer**!<br>New line. Does it render on next line?",
    /*"""
       # Welcome
       This a demo of the md **renderer**!
       New line. Does it render on next line?
       fsdfsdfsdfsdfsdf
       """,*/
    // Text=Markdown.DefaultMarkdownSample,
    X = 0,
    Y = 1,
    Width = Dim.Fill(),
    Height = Dim.Fill(to: userPromptField),
};
sendButton.Accepted += (sender, eventArgs) => OnSendUserPrompt();
userPromptField.KeyDown += (_, key) =>
{
    if (key == Key.Enter.WithCtrl)
    {
        userPromptField.InsertText("\n");
        key.Handled = true;
    }
    else if (key == Key.Enter)
    {
        OnSendUserPrompt();
        key.Handled = true;
    }
};

window.Add(menuBar, md, userPromptField, sendButton);
app.Run(window);
return;


void ShowAboutDialog()
{
    MessageBox.Query(app, "About",
        /*"""
        ░       ░░░        ░░░      ░░░░░░░░░░      ░░░        ░░░░░░░░░      ░░░░      ░░░  ░░░░  ░░        ░
        ▒  ▒▒▒▒  ▒▒▒▒▒  ▒▒▒▒▒  ▒▒▒▒▒▒▒▒▒▒▒▒▒▒  ▒▒▒▒  ▒▒▒▒▒  ▒▒▒▒▒▒▒▒▒▒▒  ▒▒▒▒▒▒▒▒  ▒▒▒▒  ▒▒   ▒▒   ▒▒  ▒▒▒▒▒▒▒
        ▓       ▓▓▓▓▓▓  ▓▓▓▓▓  ▓▓▓   ▓▓▓▓▓▓▓▓  ▓▓▓▓  ▓▓▓▓▓  ▓▓▓▓▓▓▓▓▓▓▓  ▓▓▓   ▓▓  ▓▓▓▓  ▓▓        ▓▓      ▓▓▓
        █  ███  ██████  █████  ████  ████████        █████  ███████████  ████  ██        ██  █  █  ██  ███████
        █  ████  ██        ███      █████████  ████  ██        █████████      ███  ████  ██  ████  ██        █
        """*/
        """
        ██████╗ ██╗ ██████╗      █████╗ ██╗     ██████╗  █████╗ ███╗   ███╗███████╗
        ██╔══██╗██║██╔════╝     ██╔══██╗██║    ██╔════╝ ██╔══██╗████╗ ████║██╔════╝
        ██████╔╝██║██║  ███╗    ███████║██║    ██║  ███╗███████║██╔████╔██║█████╗  
        ██╔══██╗██║██║   ██║    ██╔══██║██║    ██║   ██║██╔══██║██║╚██╔╝██║██╔══╝  
        ██║  ██║██║╚██████╔╝    ██║  ██║██║    ╚██████╔╝██║  ██║██║ ╚═╝ ██║███████╗
        ╚═╝  ╚═╝╚═╝ ╚═════╝     ╚═╝  ╚═╝╚═╝     ╚═════╝ ╚═╝  ╚═╝╚═╝     ╚═╝╚══════╝
        """ +
        "\nYour personal AI powered search over your own documents." +
        "\nYou own it, nothing goes to the cloud. You just need a GPU :)" +
        "\nCreated using Terminal.Gui (tui-cs) v2!" +
        "\nVersion 1.0.0",
        "OK");
}

void OnSendUserPrompt()
{
    string userPromptFieldValue = userPromptField.Text.Trim().Replace("\n", "<br>");
    if (userPromptFieldValue.Length > 0)
    {
        md.Text += $"\n\n{userPromptFieldValue}";
        userPromptField.Text = string.Empty;
    }
}

//TODO: When searching for retrieval, use the same embeddingGenerator automatically
