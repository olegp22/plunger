// Form1_Designer.cs  —  auto-generated partial class (hand-edited for correctness)
namespace WinFormsApp1
{
    partial class Form1
    {
        private System.ComponentModel.IContainer? components = null;

        // Declared non-nullable + null! so CS8618 is satisfied at field level.
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
            gameTimer  = new System.Windows.Forms.Timer(components);
            SuspendLayout();

            // gameTimer — ~60 fps
            gameTimer.Interval = 16;
            // Use explicit EventHandler delegate → fixes CS8622
            gameTimer.Tick += new System.EventHandler(gameTimer_Tick);

            // Form1
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize          = new System.Drawing.Size(1315, 558);
            DoubleBuffered      = true;
            Name                = "Form1";
            Text                = "Plunger Dash";
            // Use explicit EventHandler delegate → fixes CS8622
            Load += new System.EventHandler(Form1_Load);

            ResumeLayout(false);
        }
    }
}
