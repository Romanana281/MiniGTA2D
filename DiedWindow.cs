using System;
using System.Drawing;
using System.Windows.Forms;

namespace Project5
{
    public partial class DiedWindow : Form
    {
        public event Action OnRestart;
        public event Action OnExit;

        public DiedWindow(Player player)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.Black;
            this.TopMost = true;

            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            Label labelTitle = new Label
            {
                Text = "ВЫ УМЕРЛИ",
                Font = new Font("Arial", 80, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 139, 0, 0),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            labelTitle.Location = new Point(
                (this.ClientSize.Width - labelTitle.Width) / 2,
                this.ClientSize.Height / 4
            );
            mainPanel.Controls.Add(labelTitle);


            Button buttonRestart = new Button
            {
                Text = "Начать заново",
                Font = new Font("Arial", 32, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.DarkGreen,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(600, 100)
            };
            buttonRestart.FlatAppearance.BorderSize = 8;
            buttonRestart.FlatAppearance.BorderColor = Color.LimeGreen;
            buttonRestart.Click += (s, e) =>
            {
                OnRestart?.Invoke();
                this.Close();
            };
            mainPanel.Controls.Add(buttonRestart);

            Button buttonExit = new Button
            {
                Text = "Выйти из игры",
                Font = new Font("Arial", 32, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.DarkRed,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(600, 100)
            };
            buttonExit.FlatAppearance.BorderSize = 8;
            buttonExit.FlatAppearance.BorderColor = Color.Red;
            buttonExit.Click += (s, e) =>
            {
                OnExit?.Invoke();
                this.Close();
                Application.Exit();
            };
            mainPanel.Controls.Add(buttonExit);

            void CenterControls()
            {
                labelTitle.Location = new Point(
                    (this.ClientSize.Width - labelTitle.Width) / 2,
                    this.ClientSize.Height / 3 - labelTitle.Height
                );

                buttonRestart.Location = new Point(
                    (this.ClientSize.Width - buttonRestart.Width) / 2,
                    this.ClientSize.Height / 2
                );

                buttonExit.Location = new Point(
                    (this.ClientSize.Width - buttonExit.Width) / 2,
                    this.ClientSize.Height / 2 + 120
                );
            }

            this.Load += (s, e) => CenterControls();
            this.SizeChanged += (s, e) => CenterControls();

            this.Controls.Add(mainPanel);
        }
    }
}