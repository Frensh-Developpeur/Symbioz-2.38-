using System;
using System.Windows.Forms;

namespace Symbioz.Admin
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new AdminForm());
        }
    }
}
