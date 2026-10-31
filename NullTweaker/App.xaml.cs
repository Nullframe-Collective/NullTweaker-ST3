using System.IO;
using System.Windows;

namespace NullTweaker;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NullFrame", "NullTweaker");
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "errors.log"), DateTimeOffset.Now + Environment.NewLine + args.Exception + Environment.NewLine);
            MessageBox.Show("Unexpected editor error. Details were saved to " + directory, "NullTweaker", MessageBoxButton.OK, MessageBoxImage.Error);
        };
        base.OnStartup(e);
    }
}
