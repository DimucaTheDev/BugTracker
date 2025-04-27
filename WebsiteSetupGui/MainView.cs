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
    }
}

