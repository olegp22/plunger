// Form1_Designer.cs
namespace WinFormsApp1
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

            // ~60 fps
            gameTimer.Interval = 16;
            // object? sender — совпадает с сигнатурой gameTimer_Tick в Form1.cs
            gameTimer.Tick += new System.EventHandler(gameTimer_Tick);

            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;

            // Полноэкранный режим без рамки
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            WindowState = System.Windows.Forms.FormWindowState.Maximized;

            DoubleBuffered = true;
            KeyPreview = true;
            Name = "Form1";
            Text = "Plunger Dash";

            // object? sender — совпадает с сигнатурой Form1_Load в Form1.cs
            Load += new System.EventHandler(Form1_Load);

            ResumeLayout(false);
        }
    }
}