using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Resources;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace RigAiGame;

public class AboutWindow : Window
{
    private const string schemeName = "Dialog";

    public AboutWindow()
    {
        Title = "About";
        X = Pos.Center();
        Y = Pos.Center();
        Width = Dim.Auto();
        Height = Dim.Auto();
        Padding.Thickness = new Thickness(2, 1, 2, 0);
        SchemeName = schemeName;

        var logoLabel = new Label
        {
            Text = """
                   ██████╗ ██╗ ██████╗      █████╗ ██╗     ██████╗  █████╗ ███╗   ███╗███████╗
                   ██╔══██╗██║██╔════╝     ██╔══██╗██║    ██╔════╝ ██╔══██╗████╗ ████║██╔════╝
                   ██████╔╝██║██║  ███╗    ███████║██║    ██║  ███╗███████║██╔████╔██║█████╗  
                   ██╔══██╗██║██║   ██║    ██╔══██║██║    ██║   ██║██╔══██║██║╚██╔╝██║██╔══╝  
                   ██║  ██║██║╚██████╔╝    ██║  ██║██║    ╚██████╔╝██║  ██║██║ ╚═╝ ██║███████╗
                   ╚═╝  ╚═╝╚═╝ ╚═════╝     ╚═╝  ╚═╝╚═╝     ╚═════╝ ╚═╝  ╚═╝╚═╝     ╚═╝╚══════╝
                   """,
            X = Pos.Center(),
            Y = 0,
        };
        var infoLabel = new Label
        {
            Text = "Your personal AI powered search over your own documents." +
                   "\nYou own it, nothing goes to the cloud. You just need a GPU 😉" +
                   "\nPowered by Terminal.Gui and Ollama" +
                   "\nv1.0.0",
            X = Pos.Center(),
            Y = Pos.Bottom(logoLabel),
            TextAlignment = Alignment.Center
        };

        // Needed for same background color as parent, as this TUI doesn't support transparency :)
        Scheme parentScheme = SchemeManager.GetScheme(schemeName);
        logoLabel.SetScheme(new Scheme(parentScheme)
        {
            Normal = new Attribute(Color.Parse("#FF7B00"), parentScheme.Normal.Background)
        });

        var okBtn = new Button
        {
            Text = Strings.btnOk,
            X = Pos.Center(),
            Y = Pos.Bottom(infoLabel) + 1,
        };
        okBtn.Accepted += (s, e) =>
        {
            e.Handled = true;
            RequestStop();
        };

        Add(logoLabel, infoLabel, okBtn);
    }
}
