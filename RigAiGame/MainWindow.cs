using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OllamaSharp;
using Terminal.Gui.Input;
using Terminal.Gui.Resources;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace RigAiGame;

internal sealed class MainWindow : Runnable
{
    private readonly DocumentIngestionService _docIngestionService;
    private readonly VectorStoreCollection<string, DocumentChunk> _docCollection;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly Chat _chat;

    private readonly TextView _userPromptField;
    private readonly Button _sendButton;
    private readonly Markdown _mdArea;
    private readonly Label _newChatWelcome;
    private readonly SpinnerView _loadingSpinner;
    private readonly Label _statusLabel;

    public MainWindow(DocumentIngestionService docIngestionService,
        VectorStoreCollection<string, DocumentChunk> docCollection, Chat chat,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _docIngestionService = docIngestionService;
        _docCollection = docCollection;
        _chat = chat;
        _embeddingGenerator = embeddingGenerator;

        var menuBar = new MenuBar([
            new MenuItem("_1 Sync Database", "", ResyncVectorStore),
            new MenuItem("_2 About...", "", () => App!.Run<AboutWindow>()),
            new MenuItem("_3 Quit", "", QuitApp),
        ]);
        _userPromptField = new TextView
        {
            X = 0,
            Y = Pos.AnchorEnd(),
            Height = Dim.Auto(minimumContentDim: 1, maximumContentDim: 4),
            WordWrap = true,
            ScrollBars = true,
        };
        _sendButton = new Button
        {
            X = Pos.AnchorEnd(),
            Y = Pos.AnchorEnd() + 1,
            Width = 6,
            Height = Dim.Height(_userPromptField) + 1,
            Text = "→",
            ShadowStyle = ShadowStyles.None,
        };
        // Jank cross-dependency so the framework bugs out layouts after resizes, so we do this hardcode PoS to fix it
        _userPromptField.Width = Dim.Fill() - _sendButton.Width - 2;
        var line = new Line { X = 0, Y = Pos.Y(_userPromptField) - 1 };

        _mdArea = new Markdown
        {
            X = 0,
            Y = Pos.Bottom(menuBar),
            Width = Dim.Fill(),
            Height = Dim.Fill(to: line),
        };

        _newChatWelcome = new Label
        {
            Text = "Greetings! How can I help?",
            X = Pos.Center(),
            Y = Pos.Center(),
        };
        _loadingSpinner = new SpinnerView {
            X = Pos.X(_userPromptField),
            Y = Pos.Y(_userPromptField),
            AutoSpin = true,
            Visible = false,
            Style = new SpinnerStyle.OrangeBluePulse()
        };
        _statusLabel = new Label {
            Text = "Thinking...",
            X = Pos.Right(_loadingSpinner),
            Y = Pos.Y(_loadingSpinner),
            Visible = false
        };

        _sendButton.Accepted += async (_, e) =>
        {
            e.Handled = true;
            await OnSendUserPrompt();
        };
        _userPromptField.KeyDown += async (_, key) =>
        {
            if (key == Key.Enter.WithCtrl)
            {
                _userPromptField.InsertText("\n");
                key.Handled = true;
            }
            else if (key == Key.Enter)
            {
                key.Handled = true;
                await OnSendUserPrompt();
            }
        };

        Add(_mdArea, menuBar, _userPromptField, line, _sendButton, _newChatWelcome, _loadingSpinner, _statusLabel);

        _userPromptField.SetFocus();
    }

    private void QuitApp()
    {
        int? btnIndex = MessageBox.Query(App!, "Confirm Exit", "Are you sure you want to exit?", Strings.cmdQuit, Strings.btnCancel);
        if (btnIndex == 0)
        {
            App!.RequestStop();
        }
    }

    private async void ResyncVectorStore()
    {
        string originalStatusText = _statusLabel.Text;
        try
        {
            int? btnIndex = MessageBox.Query(App!, "Confirm database resync",
                "Re-sync the document database? This may take a few moments...", Strings.btnYes, Strings.btnCancel);
            if (btnIndex != 0)
                return;
            SetWaitForResponse(true);
            // For now it's hardcoded, should add settings to the app...
            await _docIngestionService.ProcessDirectoryAsync("E:\\My Encyclopedia",
                info => { _statusLabel.Text = info; });
        } catch (Exception e)
        {
            MessageBox.ErrorQuery(App!, "Error", $"Could not re-sync the document database. Reason:\n{e.Message}", Strings.btnOk);
        } finally
        {
            SetWaitForResponse(false);
            _statusLabel.Text = originalStatusText;
        }
    }

    private async Task OnSendUserPrompt()
    {
        string question = _userPromptField.Text.Trim().Replace("\n", "<br>");
        if (question.Length > 0)
        {
            _mdArea.Text += $"> {question}\n\n";
            _userPromptField.Text = string.Empty;
            _newChatWelcome.Visible = false;
            
            //TODO: Move to automatic embedding... and improve chunks, it truncates the sources for some questions
            SetWaitForResponse(true);
            try
            {
                var uniqueFileNames = new HashSet<string>();
                var searchResults =
                    _docCollection.SearchAsync((await _embeddingGenerator.GenerateAsync(question)).Vector, top: 3);
                var contextSB = new StringBuilder();
                await foreach (var result in searchResults)
                {
                    contextSB.AppendLine($"Source: {result.Record.FileName}\n{result.Record.Text}");
                    contextSB.AppendLine();
                    uniqueFileNames.Add(result.Record.FileName);
                }
                //TODO: maybe rewrite the follow-up questions using the LLM, and send that to embedding model??
                var prompt = $"## Context:\n{contextSB}\n## Question:\n{question}";

                await foreach (string token in _chat.SendAsync(prompt))
                {
                    _mdArea.Text += token;
                }
                _mdArea.Text += $"\n\nSource(s): {string.Join(", ", uniqueFileNames)}";
                _mdArea.Text += "\n\n";
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery(App!, "Error", $"Failed to get response. Reason:\n{ex.Message}", Strings.btnOk);
            }
            finally
            {
                SetWaitForResponse(false);
            }
        }
    }

    private void SetWaitForResponse(bool isWait)
    {
        _userPromptField.Enabled = _sendButton.Enabled = !isWait;
        _loadingSpinner.Visible = _statusLabel.Visible = isWait;
    }
}
