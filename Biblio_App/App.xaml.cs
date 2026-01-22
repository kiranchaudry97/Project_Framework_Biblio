using System;
using System.Threading;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Biblio_App.Infrastructure;

namespace Biblio_App
{
    /// <summary>
    /// App
    /// 
    /// Dit is de hoofdklasse van de MAUI applicatie.
    /// 
    /// Verantwoordelijkheden:
    /// - Initialiseren van de applicatie
    /// - Instellen van de hoofdpagina (AppShell)
    /// - Configureren van globale exception handling
    /// - Beheren van window-instellingen (platform-specifiek)
    /// - Initiële navigatie (login vs hoofdscherm)
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// SecurityViewModel (singleton via DI)
        /// 
        /// Houdt bij of de gebruiker is ingelogd
        /// en bepaalt de startnavigatie.
        /// </summary>
        private readonly ViewModels.SecurityViewModel _security;

        /// <summary>
        /// Constructor van de MAUI Application.
        /// 
        /// Wordt één keer aangeroepen bij het starten van de app.
        /// </summary>
        public App(ViewModels.SecurityViewModel security)
        {
            InitializeComponent();

            // Bewaar referentie naar het SecurityViewModel
            _security = security;

            // Stel AppShell in als hoofd-navigatiecontainer
            // (bevat menu, routes en tabs)
            MainPage = new AppShell();

            // Huidige SynchronizationContext ophalen
            var current = SynchronizationContext.Current;

            /*
             * Alleen wrappen als er effectief een context bestaat.
             * 
             * Het vervangen van een NULL context door een threadpool-context
             * kan leiden tot:
             * - deadlocks
             * - ANR (Application Not Responding) op Android
             */
            if (current != null)
            {
                // SafeSynchronizationContext beschermt tegen:
                // - verkeerde state dispatching
                // - crashes bij async callbacks
                SynchronizationContext.SetSynchronizationContext(
                    new SafeSynchronizationContext(current)
                );
            }

            // -------------------------------------------------
            // Globale exception handling (laat de app niet stil crashen)
            // -------------------------------------------------

            // Onbehandelde exceptions (synchronous)
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    var ex = e.ExceptionObject as Exception;

                    System.Diagnostics.Debug.WriteLine(
                        $"[UNHANDLED EXCEPTION] {ex?.Message}"
                    );
                    System.Diagnostics.Debug.WriteLine(
                        $"StackTrace: {ex?.StackTrace}"
                    );

                    // Log fout naar bestand (lokale storage)
                    ErrorLogger.Log(ex);
                }
                catch
                {
                    // Nooit laten crashen in een exception handler
                }
            };

            // DEVELOPMENT HELPER
            // ------------------
            // Forceert éénmalig het opnieuw aanmaken van de lokale database.
            // Handig bij:
            // - schemawijzigingen
            // - seeding testen
            // 
            // ⚠️ Na eerste succesvolle run UITCOMMENTARIËREN of verwijderen
            try
            {
                Microsoft.Maui.Storage.Preferences.Default
                    .Set("biblio-recreate-db", true);
            }
            catch { }

            // Ongeobserveerde async task exceptions
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[UNOBSERVED TASK EXCEPTION] {e.Exception?.Message}"
                    );
                    System.Diagnostics.Debug.WriteLine(
                        $"StackTrace: {e.Exception?.StackTrace}"
                    );

                    ErrorLogger.Log(e.Exception);
                }
                catch { }

                // Markeer als afgehandeld om app-crash te voorkomen
                e.SetObserved();
            };
        }

        /// <summary>
        /// CreateWindow
        /// 
        /// Wordt aangeroepen wanneer het applicatievenster wordt aangemaakt.
        /// 
        /// Hier:
        /// - stellen we titel en venstergrootte in
        /// - doen we platform-specifieke configuratie
        /// - starten we initiële navigatie (login of home)
        /// </summary>
        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Basis window aanmaken
            var window = base.CreateWindow(activationState);

            // Titel van het venster
            window.Title = "Biblio App";

#if WINDOWS
            // ----------------------------
            // Windows-specifieke instellingen
            // ----------------------------
            window.MinimumWidth = 1000;
            window.MinimumHeight = 700;
            window.Width = 1280;
            window.Height = 800;

            // Centreer het venster bij opstart
            window.Created += (s, e) =>
            {
                var displayInfo = DeviceDisplay.Current.MainDisplayInfo;

                window.X =
                    (displayInfo.Width / displayInfo.Density - window.Width) / 2;

                window.Y =
                    (displayInfo.Height / displayInfo.Density - window.Height) / 2;
            };
#elif MACCATALYST
            // ----------------------------
            // macOS (Mac Catalyst) instellingen
            // ----------------------------
            window.MinimumWidth = 1000;
            window.MinimumHeight = 700;
            window.Width = 1280;
            window.Height = 800;
#endif

            /*
             * Initiële navigatie
             * ------------------
             * 
             * We gebruiken Dispatcher.Dispatch zodat:
             * - Shell volledig geïnitialiseerd is
             * - routes correct geregistreerd zijn
             * 
             * window.Created is hier NIET betrouwbaar genoeg.
             */
            Dispatcher.Dispatch(async () =>
            {
                // Kleine delay om race conditions te vermijden
                await Task.Delay(250);

                try
                {
                    if (_security.IsAuthenticated)
                    {
                        // Gebruiker is ingelogd
                        // → ga naar hoofdscherm (boeken)
                        System.Diagnostics.Debug.WriteLine(
                            "User is authenticated, navigating to BoekenShell"
                        );

                        await Shell.Current.GoToAsync("//BoekenShell");
                    }
                    else
                    {
                        // Gebruiker is niet ingelogd
                        // → toon loginpagina
                        System.Diagnostics.Debug.WriteLine(
                            "User is not authenticated, navigating to LoginPage"
                        );

                        await Shell.Current.GoToAsync("//LoginPage");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Navigation error: {ex.Message}"
                    );

                    // Fallback: probeer minstens het hoofdscherm te tonen
                    try
                    {
                        await Shell.Current.GoToAsync("//BoekenShell");
                    }
                    catch (Exception ex2)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Fallback navigation also failed: {ex2.Message}"
                        );
                    }
                }
            });

            return window;
        }
    }
}
