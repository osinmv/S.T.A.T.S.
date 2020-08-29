using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using Memory;
using System.Windows.Forms.DataVisualization.Charting;
using System.Security.Cryptography;
using System.Diagnostics;
using System.IO;

namespace S.T.A.T.S
{
    public partial class Form1 : Form
    {
        public Mem m = new Mem();
        public bool ProcOpen = false;
        public float x, y, z;
        public float prev_x, prev_y, prev_z;
        public string pos;
        public bool loading;
        public bool restart;
        public Point m_location;
        public bool down = false;
        public System.Windows.Forms.DataVisualization.Charting.Series series1;
        double speed;
        int cycle;
        double total;
        int over;
        string log_holder;
        int log_length;
        SHA1 crypt;
        bool hashed;
        string game_directory;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            clear_chart();
            cycle = 0;
            x = 0;
            y = 0;
            z = 0;
            total = 0.0;
            over = 0;
            log_length = 0;
            log_holder = "";
            crypt = SHA1.Create();
            hashed = false;
            game_directory = "";
        }
        public void clear_chart()
        {
            chart1.Series.Clear();
            series1 = new System.Windows.Forms.DataVisualization.Charting.Series
            {
                Name = "Speed",
                Color = System.Drawing.Color.Purple,
                BorderWidth = 4,
                IsVisibleInLegend = true,
                IsXValueIndexed = true,
                ChartType = SeriesChartType.FastLine
            };
            chart1.Series.Add(series1);
        }
        private string byte_to_string(Byte[] array)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i].ToString("x2"));
            }
            return sb.ToString()+"\n";
        }
        private string compute_hash_folder(string path, string file_ending, bool recursively)
        {

            // Recursively go through all directories and take hash of files that have a specific file type
            DirectoryInfo target = new DirectoryInfo(path);
            if (!target.Exists)
            {
                return "";
            }
            else
            {
                string logs = "";
                if (recursively)
                {
                    DirectoryInfo[] dirs = target.GetDirectories();
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        logs += compute_hash_folder(dirs[i].FullName, file_ending, true);
                    }
                }
                FileInfo[] files = target.GetFiles(file_ending);
                for (int i = 0; i < files.Length; i++)
                {
                    logs += byte_to_string(crypt.ComputeHash(File.Open(files[i].FullName,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)));
                }
                return logs;

            }
        }
        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void bg_worker(object sender, DoWorkEventArgs e)
        {

            ProcOpen = m.OpenProcess("XR_3DA");
            if (ProcOpen)
            {
                if (!hashed)
                {
                    hashed = true;
                    Process game = Process.GetProcessById(m.GetProcIdFromName("XR_3DA"));
                    game_directory = game.MainModule.FileName;
                    game.Dispose();
                    game_directory = game_directory.Replace("\\bin\\XR_3DA.exe", "");
                    log_holder += compute_hash_folder(game_directory + "/gamedata/scripts", "*.script", true);
                    log_holder += compute_hash_folder(game_directory + "/gamedata/configs", "*.ltx", true);
                    log_holder += compute_hash_folder(game_directory, "gamedata*", false);
                    // TODO: add code for filesystem watcher 
                }
                prev_x = x;
                prev_y = y;
                prev_z = z;
                x = m.ReadFloat("base+10BE94");
                y = m.ReadFloat("base+10BE98");
                z = m.ReadFloat("base+10BE9C");
                pos = m.ReadString("xrCore.dll+000BF368,4,0,40,8,10,48,4");
                cycle++;
                if (prev_x != 0.0 && prev_y != 0.0 && prev_z != 0.0 && x == 0.0 && y == 0.0 && z == 0.0)
                {
                    log_holder += DateTime.Now.Hour + "," + DateTime.Now.Minute + "," + DateTime.Now.Second + "," + speed.ToString() + "," + pos + "\n";
                    log_length++;
                    if (log_length > 100)
                    {
                        eventLog1.WriteEntry(log_holder);
                        log_length = 0;
                    }
                }

            }
            Thread.Sleep(1000);
            backgroundWorker1.ReportProgress(1);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (ProcOpen)
            {
                label1.Text = "Connected";
                speed = Math.Sqrt(Math.Pow(prev_x - x, 2) + Math.Pow(prev_y - y, 2) + Math.Pow(prev_z - z, 2));
                label3.Text = "x = " + x.ToString() + " y = " + y.ToString() + " z = " + z.ToString() + " place " + pos + " speed " + Math.Round(speed, 2).ToString();
                if (prev_x == 0.0 && prev_y == 0.0 && prev_z == 0.0 && x != 0.0 && y != 0.0 && z != 0.0)
                {
                    clear_chart();
                    cycle = 1;
                    total = 0;
                    over = 0;

                }
                if (speed > 0.0 && speed < 50.0)
                {
                    series1.Points.AddXY(cycle, speed);
                    total += speed;
                    over++;
                    label5.Text = "av speed:" + (total / over).ToString();
                }
            }
            else
            {
                label1.Text = "Waiting";
            }
        }

        private void label4_MouseDown(object sender, MouseEventArgs e)
        {
            m_location = e.Location;
            if (e.Button == MouseButtons.Left)
            {
                down = true;
            }
        }

        private void label4_MouseMove(object sender, MouseEventArgs e)
        {
            if (down)
            {
                this.Location = new Point(e.X + this.Location.X - m_location.X, e.Y + this.Location.Y - m_location.Y);
            }
        }


        private void smallViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            chart1.Visible = false;
            this.Height = 150;
        }
        private void fullToolStripMenuItem_Click(object sender, EventArgs e)
        {
            chart1.Visible = true;
            this.Height = 376;
        }
        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (log_holder.Length > 0)
            {


                eventLog1.WriteEntry(log_holder);
            }
            this.Close();
        }

        private void fileSystemWatcher1_Changed(object sender, System.IO.FileSystemEventArgs e)
        {

        }

        private void eventLog1_EntryWritten(object sender, System.Diagnostics.EntryWrittenEventArgs e)
        {

        }

        private void label4_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                down = false;
            }
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }
    }
}
