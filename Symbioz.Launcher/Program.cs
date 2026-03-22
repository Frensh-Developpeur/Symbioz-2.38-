using System;
using System.Windows.Forms;

namespace Symbioz.Launcher
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new LauncherForm());
        }
    }
}
