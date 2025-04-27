using Terminal.Gui;

namespace WebsiteSetupGui;
internal class Program
{
    static void Main(string[] args)
    {
        if (!args.Any())
        {
            try
            {
                Console.WriteLine("Starting in TUI mode... Use --cli to use CLI instead.");
                Application.Init();
                Application.Run(new MainView());
            }
            finally
            {
                Application.Shutdown();
            }
            return;
        }

        Console.WriteLine("TODO");
    }
}
