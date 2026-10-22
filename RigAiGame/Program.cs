using OllamaSharp;
using Spectre.Console;

using var ollama = new OllamaApiClient(new Uri("http://localhost:11434/"), "qwen3.5:4b");
var chat = new Chat(ollama);

Style userPromptStyle = Style.Plain.Background(Color.Purple);
AnsiConsole.Write(
    new FigletText("RigAiGame (RAG)")
        .Color(Color.Green)
);

var cliPrompt = "[green]Troubleshooting your gaming rig? Ask me[/] >>";
while (true)
{
    var input = AnsiConsole.Prompt(new TextPrompt<string>(cliPrompt).PromptStyle(userPromptStyle));
    await foreach (var token in chat.SendAsync(input))
    {
        AnsiConsole.Write(token);
    }

    AnsiConsole.WriteLine();
    cliPrompt = ">>";
}
