using System;
using System.Threading;
using System.Windows.Forms;

namespace Isabel_Visualizador_Proj
{
    internal static class Program
    {
        /// <summary>
        /// Ponto de entrada principal para o aplicativo.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Habilita estilos vsuais do Windows para o aplicativo
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Configura tratamento global de exceções não tratadas
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                // Executar o formulário principal do aplicativo
                Application.Run(new Paint());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao inciar o aplicativo: \n{ex.Message}", 
                    "Erro fatal", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show($"Erro não tratado: \n{e.Exception.Message}", 
                "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;            
                MessageBox.Show($"Erro crítico não tratado: \n{ex?.Message}", 
                    "Erro crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
