using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace Project5
{
    public partial class MainForm : Form
    {
        private Timer gameTimer;
        private LoadingForm loadingForm;
        private BackgroundWorker loadingWorker;
        public Map Maps;

        public MainForm()
        {
            InitializeComponent();

            this.Opacity = 0;
            this.ShowInTaskbar = false;

            ShowLoadingForm();
        }

        private void ShowLoadingForm()
        {
            loadingForm = new LoadingForm();
            loadingForm.Show();
            loadingForm.UpdateProgress("Подготовка...", 0);

            loadingWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = false
            };

            loadingWorker.DoWork += LoadingWorker_DoWork;
            loadingWorker.ProgressChanged += LoadingWorker_ProgressChanged;
            loadingWorker.RunWorkerCompleted += LoadingWorker_RunWorkerCompleted;

            loadingWorker.RunWorkerAsync();
        }

        private void LoadingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;

            worker.ReportProgress(10, "Инициализация карты...");

            Map map = null;

            this.Invoke(new Action(() =>
            {
                map = new Map(this, this.Player);
                map.BackColor = System.Drawing.Color.Transparent;
                map.Controls.Add(this.Player);
                map.Location = new System.Drawing.Point(0, 0);
                map.Name = "Maps";
                map.Size = new System.Drawing.Size(4096, 5120);
                map.TabIndex = 0;
            }));

            worker.ReportProgress(30, "Карта создана");

            worker.ReportProgress(50, "Создание маски дорог...");

            WayMask wayMask = new WayMask(map);

            wayMask.ProgressChanged += (message, progress) =>
            {
                worker.ReportProgress(50 + progress / 2, message);
            };

            wayMask.CreateMask();

            worker.ReportProgress(85, "Создание машин...");

            this.Invoke(new Action(() =>
            {
                map.wayMask = wayMask;
                map.CreateCar();
                map.carUpdate.Interval = 33;
                map.carUpdate.Tick += map.CarUpdate_Tick;
                map.carUpdate.Start();
            }));

            worker.ReportProgress(90, "Создание игроков...");

            this.Invoke(new Action(() =>
            {
                map.CreateHumon();
                map.humonUpdate.Interval = 33;
                map.humonUpdate.Tick += map.HumonUpdate_Tick;
                map.humonUpdate.Start();
            }));

            Maps = map;
            worker.ReportProgress(100, "Завершение инициализации...");
            System.Threading.Thread.Sleep(300);
        }

        private void LoadingWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (loadingForm != null && !loadingForm.IsDisposed)
            {
                loadingForm.UpdateProgress(e.UserState.ToString(), e.ProgressPercentage);
            }
        }

        private void LoadingWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;

            if (loadingForm != null && !loadingForm.IsDisposed)
            {
                loadingForm.Close();
                loadingForm.Dispose();
                loadingForm = null;
            }

            if (e.Error == null)
            {
                this.ShowInTaskbar = true;
                this.Opacity = 1;

                this.KeyDown += MainForm_KeyDown;
                this.KeyUp += MainForm_KeyUp;
                this.KeyPreview = true;

                this.Controls.Add(Maps);
                Maps.BringToFront();

                InitializeTimer();
            }
            else
            {
                MessageBox.Show($"Ошибка загрузки: {e.Error.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void InitializeTimer()
        {
            gameTimer = new Timer();
            gameTimer.Interval = 17;
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            if (Maps.tekCar == null)
            {
                Player.Visible = true;
                this.Player.UpdatePosition(Maps, this);
                Maps.UpdateCamera(this.Player.WorldPosition);
            }
            else
            {
                this.Player.Visible = false;
                Maps.UpdateCamera(Maps.tekCar.WorldPosition);
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W) this.Player.KeysUp = true;

            if (e.KeyCode == Keys.A) this.Player.KeysLeft = true;

            if (e.KeyCode == Keys.S) this.Player.KeysDown = true;

            if (e.KeyCode == Keys.D) this.Player.KeysRight = true;

            if (e.KeyCode == Keys.D1)
                this.Player.tekWeapon = null;
            if (e.KeyCode == Keys.D2 && this.Player.weapons.Count > 0)
                this.Player.tekWeapon = this.Player.weapons.Find(x => x.type == 0);
            if (e.KeyCode == Keys.D3 && this.Player.weapons.Count > 0)
                this.Player.tekWeapon = this.Player.weapons.Find(x => x.type == 1);

            if (e.KeyCode == Keys.P) Player.searchTimer = 30;

            if (e.KeyCode == Keys.I) Maps.pressedKeys.Add(e.KeyCode);

            if (e.KeyCode == Keys.L) Console.WriteLine(Maps.tekCar.WorldPosition);
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W) this.Player.KeysUp = false;

            if (e.KeyCode == Keys.A) this.Player.KeysLeft = false;

            if (e.KeyCode == Keys.S) this.Player.KeysDown = false;

            if (e.KeyCode == Keys.D) this.Player.KeysRight = false;

            if (e.KeyCode == Keys.I) Maps.pressedKeys.Remove(e.KeyCode);

        }
    }
}
