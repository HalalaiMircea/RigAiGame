using CommunityToolkit.VectorData.SqliteVec;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;
using RigAiGame;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;

// sets height and width in columns... the native Console API doesnt work with modern win 11 terminal
// Console.Write($"\e[8;{70};{100}t");

const string dataSourceEmbeddingsDb = "Data Source=./embeddings.db";

var services = new ServiceCollection();
services.AddEmbeddingGenerator(_ => new OllamaApiClient("http://localhost:11434/", "qwen3-embedding:8b"));
services.AddSqliteVectorStore(p => dataSourceEmbeddingsDb, p => new SqliteVectorStoreOptions
{
    EmbeddingGenerator = p.GetRequiredService<IEmbeddingGenerator>(),
});
services.AddSqliteCollection<string, DocumentChunk>("documents", p => dataSourceEmbeddingsDb);
services.AddSingleton<DocumentIngestionService>();
services.AddSingleton(p =>
{
    var ollama = new OllamaApiClient("http://localhost:11434/", "qwen3.5:4b");
    return new Chat(ollama,
        """
        You are an expert gaming rig troubleshooting assistant. 
        Use the provided documentation as your primary source of truth to answer questions. 
        If the documentation doesn't fully cover the issue, blend your own advanced technical knowledge and training data to provide a helpful solution.
        Always prioritize the documentation if there is a conflict. 
        If you used the documentation, cite source files at the end with format "Source(s): ", comma separated if multiple.
        """);
});
services.AddSingleton<MainWindow>();
using IApplication app = Application.Create();
TuiConfigurationBuilder builder = new ("MyApp");

// Define a structural theme block containing specific UI Color Schemes
builder.RuntimeConfig = """
                        {
                          "Themes": {
                            "MyCustomEmeraldTheme": {
                              "ColorSchemes": {
                                "Base": {
                                  "Normal": { "Foreground": "Green", "Background": "Black" },
                                  "Focus": { "Foreground": "Black", "Background": "Green" },
                                  "HotNormal": { "Foreground": "BrightGreen", "Background": "Black" },
                                  "HotFocus": { "Foreground": "Black", "Background": "BrightGreen" }
                                },
                                "Dialog": {
                                  "Normal": { "Foreground": "White", "Background": "DarkGray" },
                                  "Focus": { "Foreground": "Black", "Background": "White" }
                                }
                              }
                            }
                          }
                        }
                        """;

builder.ApplyToStaticFacades();
// Globally active your newly registered theme configuration block
builder.ThemeManager.SwitchTheme("MyCustomEmeraldTheme");
using var serviceProvider = services.BuildServiceProvider();
app.Init();
app.Run(serviceProvider.GetRequiredService<MainWindow>());

//TODO: When searching for retrieval, use the same embeddingGenerator automatically
