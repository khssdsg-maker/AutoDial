using System.Windows;

namespace AutoDial
{
    public class App : Application
    {
        [System.STAThread]
        static void Main()
        {
            var app = new App();
            app.Run(new MainWindow());
        }
    }
}