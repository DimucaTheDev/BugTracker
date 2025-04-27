using Terminal.Gui;

namespace WebsiteSetupGui;
public partial class MainView
{
    public MainView()
    {
        InitializeComponent();
        quit.Clicked += () =>
        {
            Application.RequestStop();
        };
        edit.Enabled = IsInstalled();
        reset.Enabled = IsInstalled();
    }
    bool IsInstalled() => (File.Exists("database.db") || File.Exists("debug_database.db")) && File.Exists("Website.dll");
}

