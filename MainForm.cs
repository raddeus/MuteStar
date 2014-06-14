using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using CoreAudio;

namespace WindowsFormsApplication1
{
    public partial class MainForm : Form
    {
        private static IntPtr windowHandle = IntPtr.Zero;
        private static int windowPid = 0;
        private static bool active = false;
        private static bool? windowActivePrev = null;
        private static MMDeviceEnumerator DevEnum;
        private static MMDevice device;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        public MainForm()
        {
            DevEnum = new MMDeviceEnumerator();
            device = DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
            InitializeComponent();
            Rectangle workingArea = Screen.GetWorkingArea(this);
            this.Location = new Point(workingArea.Right - Size.Width,
                                      workingArea.Bottom - Size.Height);
            lblStatusBar.Text = "Ready | Stopped";
        }

        private void start()
        {
            active = true;
            btnToggleActive.Text = "Stop";
            lblStatusBar.Text = "Starting...";
            btnToggleActive.BackColor = Color.FromArgb(43, 189, 121);
            btnToggleActive.FlatAppearance.MouseOverBackColor = Color.FromArgb(32, 201, 103);
            
            timer1.Start();
        }
        private void stop()
        {
            active = false;
            btnToggleActive.Text = "Start";
            btnToggleActive.BackColor = Color.FromArgb(214, 41, 49);
            btnToggleActive.FlatAppearance.MouseOverBackColor = Color.FromArgb(186, 17, 25);
            lblStatusBar.Text = "Ready | Stopped";
            timer1.Stop();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (windowHandle != IntPtr.Zero)
            {
                windowP.Refresh();
                if (windowP.HasExited)
                {
                    lblStatusBar.Text = "Game Window Exited | Searching for new window.";
                    doWindowSearch();
                }
                IntPtr currentForegroundWindow = GetForegroundWindow();
                if (currentForegroundWindow == windowHandle)
                {
                    lblStatusBar.Text = "Running | Game window active.";
                    if (windowActivePrev == null || windowActivePrev == false)
                    {
                        windowActivePrev = true;
                        lblStatusBar.Text = "Turning Volume On";
                        setMute(false, (uint)windowPid);
                        this.Icon = MuteStar.Properties.Resources.Speaker;
                    }
                }
                else
                {
                    lblStatusBar.Text = "Running | Game window inactive.";
                    if (windowActivePrev != null && windowActivePrev == true)
                    {
                        windowActivePrev = false;
                        lblStatusBar.Text = "Turning Volume Off";
                        setMute(true, (uint)windowPid);
                        this.Icon = MuteStar.Properties.Resources.Mute;
                    }
                }
            }
            else
            {
                lblStatusBar.Text = "Searching for wildstar window...";
                doWindowSearch();
            }
        }

        private void doWindowSearch()
        {
            
            IntPtr handle;
            int pid;
            if (findWindow(out handle, out pid))
            {
                windowPid = pid;
                windowHandle = handle;
                windowP = Process.GetProcessById(pid);
                // start();
            }
        }
        private bool findWindow(out IntPtr handle, out int pid)
        {
            Process[] pList = Process.GetProcesses();
            foreach (Process p in pList)
            {

                int buildNumber;
                bool parsed = int.TryParse(p.MainWindowTitle.Substring(Math.Max(0, p.MainWindowTitle.Length - 4)), out buildNumber);
                if (p.MainWindowTitle.Contains("WildStar ") && p.MainWindowTitle.Length == 13 && parsed && buildNumber > 1000 && buildNumber < 9999)
                {
                    handle = p.MainWindowHandle;
                    GetWindowThreadProcessId(handle, out pid);
                    return true;
                }
            }
            handle = IntPtr.Zero;
            pid = 0;
            return false;
        }

        private void setMute(bool mute, uint windowPid)
        {
            for (int i = 0; i < device.AudioSessionManager2.Sessions.Count; i++)
            {
                AudioSessionControl2 session = device.AudioSessionManager2.Sessions[i];
                uint pid = session.GetProcessID;
                AudioMeterInformation mi = session.AudioMeterInformation;
                SimpleAudioVolume vol = session.SimpleAudioVolume;
                if (pid == windowPid)
                {
                    vol.Mute = mute;
                }
            }
        }

        private void btnToggleActive_Click(object sender, EventArgs e)
        {
            if (active == true)
            {
                stop();
            }
            else
            {
                start();
            }
        }

        private void toolStripStatusLabel4_Click(object sender, EventArgs e)
        {

        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                notify.Visible = true;
                notify.ShowBalloonTip(500);
                this.Hide();
            }

            else if (FormWindowState.Normal == this.WindowState)
            {
                notify.Visible = false;
            }
        }
        private void notify_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (System.Windows.Forms.Application.MessageLoop)
            {
                // WinForms app
                System.Windows.Forms.Application.Exit();
            }
            else
            {
                // Console app
                System.Environment.Exit(1);
            }
        }

        public Process windowP { get; set; }
    }
}
