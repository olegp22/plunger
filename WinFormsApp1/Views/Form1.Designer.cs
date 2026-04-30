namespace WinFormsApp1
{
    // partial означает, что это только половина класса. 
    // Вторая половина находится в твоем файле Form1.cs.
    partial class Form1
    {
        /// <summary>
        /// Обязательная переменная конструктора (управляет временем жизни компонентов).
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        // Поле для таймера. Мы объявили его здесь, чтобы оно было доступно 
        // в основном коде Form1.cs для запуска и остановки[cite: 23].
        private System.Windows.Forms.Timer gameTimer;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">true, если управляемый ресурс должен быть удален; иначе false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            gameTimer = new System.Windows.Forms.Timer(components);
            SuspendLayout();
            // 
            // gameTimer
            // 
            gameTimer.Interval = 16;
            gameTimer.Tick += gameTimer_Tick;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            DoubleBuffered = true;
            Name = "Form1";
            Text = "Plunger Dash";
            Load += Form1_Load;
            ResumeLayout(false);
        }

        #endregion
    }
}