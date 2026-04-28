using System;
using System.Windows.Forms;

namespace Project5
{
    public partial class LoadingForm : Form
    {
        public ProgressBar ProgressBar { get; private set; }
        public Label StatusLabel { get; private set; }
        public LoadingForm()
        {
            InitializeComponent();
            SetupForm();
        }
        private void SetupForm()
        {
            this.Text = "Загрузка...";
            this.Size = new System.Drawing.Size(400, 150);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ControlBox = false;

            ProgressBar = new ProgressBar
            {
                Location = new System.Drawing.Point(20, 50),
                Size = new System.Drawing.Size(350, 30),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };

            StatusLabel = new Label
            {
                Text = "Инициализация карты...",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(350, 20),
                Font = new System.Drawing.Font("Microsoft Sans Serif", 10, System.Drawing.FontStyle.Regular)
            };

            this.Controls.Add(StatusLabel);
            this.Controls.Add(ProgressBar);
        }
        public void UpdateProgress(string message, int value)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateProgress(message, value)));
                return;
            }

            StatusLabel.Text = message;

            if (ProgressBar.Style == ProgressBarStyle.Marquee && value > 0)
            {
                ProgressBar.Style = ProgressBarStyle.Continuous;
            }

            if (value >= 0 && value <= 100)
            {
                ProgressBar.Value = value;
            }
        }
        public void SetMarquee(bool isMarquee)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetMarquee(isMarquee)));
                return;
            }

            ProgressBar.Style = isMarquee ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
        }
    }
}