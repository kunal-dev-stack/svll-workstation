using System;
using System.Windows;
using System.Windows.Threading;

namespace SVLL_IT_Workstation;

public class App : Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
        {
            MessageBox.Show($"Fatal System Exception:\n{e.ExceptionObject}", "SVLL Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        try
        {
            var app = new App();
            app.DispatcherUnhandledException += (object sender, DispatcherUnhandledExceptionEventArgs e) =>
            {
                MessageBox.Show($"UI Thread Exception:\n{e.Exception.Message}\n\nStack:\n{e.Exception.StackTrace}", "SVLL UI Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Handled = true;
            };

            app.Run(new MainWindow(args));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Application Startup Failed:\n{ex.Message}\n\n{ex.StackTrace}", "SVLL Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
