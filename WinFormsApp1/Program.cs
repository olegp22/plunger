namespace WinFormsApp1
{
    internal static class Program
    {
        [System.STAThread]
        static void Main()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            // Instantiate the view from Views namespace
            System.Windows.Forms.Application.Run(new Views.Form1());
        }
    }
}
