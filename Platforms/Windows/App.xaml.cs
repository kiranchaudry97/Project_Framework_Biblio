using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.ApplicationModel.DynamicDependency;

public partial class App : Application
{
    public App()
    {
        // Initialize Windows App SDK for unpackaged apps
        Bootstrap.TryInitialize();
        this.InitializeComponent();
    }
}