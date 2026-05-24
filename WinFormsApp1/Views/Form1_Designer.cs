namespace WinFormsApp1.Views
{
    partial class Form1
    {
        private System.ComponentModel.IContainer? components = null;
        private System.Windows.Forms.Timer gameTimer = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            gameTimer = new System.Windows.Forms.Timer(components);
            SuspendLayout();

            gameTimer.Interval = 33;
            gameTimer.Tick += new System.EventHandler(gameTimer_Tick);

            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            WindowState = System.Windows.Forms.FormWindowState.Maximized;

            DoubleBuffered = true;
            KeyPreview = true;
            Name = "Form1";
            Text = "Plunger Dash";

            Load += new System.EventHandler(Form1_Load);

            ResumeLayout(false);
        }
    }
}