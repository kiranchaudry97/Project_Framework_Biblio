using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Biblio_App
{
    public partial class App : Application
    {
        public App(IServiceProvider services)
        {
            InitializeComponent();

            var mainPage = services.GetRequiredService<MainPage>();
            MainPage = new NavigationPage(mainPage);
        }
    }
}
