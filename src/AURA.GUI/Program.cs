using System;
using System.Windows.Forms;
using AURA.Core.Bootstrap;
using AURA.Core.Logging;

namespace AURA.GUI
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string logPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "logs", "aura.log");

            ILogger logger = new FileLogger(logPath);
            var bootstrap = new AuraBootstrap(logger);

            try
            {
                bootstrap.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Falha ao inicializar o AURA:\n" + ex.Message,
                    "AURA Genesis Core",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Application.Run(new MainForm(bootstrap));
        }
    }
}
