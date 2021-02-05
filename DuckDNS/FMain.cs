﻿using DuckDNS.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace DuckDNS
{
    public partial class FMain : Form
    {
        private DDns ddns = new DDns();
        private int intervalMS;
        private bool allowshowdisplay = false;
        private bool canClose = false;
        private Icon icoTray = Resources.tray;
        private Icon icoTrayC = Resources.tray_checking;
        private Icon icoTrayE = Resources.tray_error;
        private bool saveOrDie;

        public FMain()
        {
            InitializeComponent();
            notifyIcon.Icon = Icon;
            ddns.Load();
            tbDomain.Text = ddns.Domain;
            tbToken.Text = ddns.Token;
            cbInterval.Text = ddns.Interval;
            ParseInterval();
            RefreshTimer();
            notifyIcon.Icon = icoTray;
            allowshowdisplay = tbDomain.Text.Length == 0 || tbToken.Text.Length == 0;
            if (!allowshowdisplay)
                UpdateDNS();
            saveOrDie = false;
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!allowshowdisplay)
            {
                allowshowdisplay = true;
                if (!IsHandleCreated && value)
                    CreateHandle();
            }
            else
                base.SetVisibleCore(value);
        }

        private void UpdateDNS()
        {
            try
            {
                notifyIcon.Icon = icoTrayC;
                bool update = ddns.Update();
                lblInfo.Text = "Last update on "+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " (" + (update ? "OK" : "FAILED") + ")";
                if (!update)
                {
                    notifyIcon.Icon = icoTrayE;
                    notifyIcon.ShowBalloonTip(5000, "Error updating", "Update of DuckDNS failed, check token and domain.", ToolTipIcon.Error);
                }
                else
                    notifyIcon.Icon = icoTray;
            }
            catch (Exception e)
            {
                notifyIcon.Icon = icoTrayE;
                notifyIcon.ShowBalloonTip(5000, "Error updating", "Error updating domain: " + e.Message, ToolTipIcon.Error);
            }
        }

        private void ParseInterval()
        {
            string istr = cbInterval.Text.ToLower();
            int iint;
            bool error = false;

            if (istr.Length == 0 || !int.TryParse(istr.Substring(0, istr.Length - 1), out iint))
                error = true;
            else
            {
                switch (istr[istr.Length - 1])
                {
                    case 's':
                        intervalMS = iint * 1000;
                        break;
                    case 'm':
                        intervalMS = iint * 60000;
                        break;
                    case 'h':
                        intervalMS = iint * 3600000;
                        break;
                    case 'd':
                        intervalMS = iint * 86400000;
                        break;
                    default:
                        error = true;
                        break;
                }
            }
            cbInterval.BackColor = error ? Color.LightPink : SystemColors.Window;
        }

        private void RefreshTimer()
        {
            timer.Enabled = false;
            timer.Interval = intervalMS;
            timer.Enabled = true;
        }

        private void SaveConf()
        {
            ddns.Domain = tbDomain.Text;
            ddns.Token = tbToken.Text;
            ddns.Interval = cbInterval.Text;
            if (!ddns.Save())
                notifyIcon.ShowBalloonTip(3000, "Error saving", "The configuration can not be saved, check if the filesystem have write rights for the user '" + System.Security.Principal.WindowsIdentity.GetCurrent().Name + "'.", ToolTipIcon.Error);
        }

        private void btOk_Click(object sender, EventArgs e)
        {
            SaveConf();
            Hide();
            UpdateDNS();
            RefreshTimer();
        }

        private void cbInterval_TextChanged(object sender, EventArgs e)
        {
            ParseInterval();
            saveOrDie = true;
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !canClose)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            UpdateDNS();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            canClose = true;
            Close();
        }

        private void updateNowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateDNS();
        }

        private void installStartupShortcutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string linkPath = Windows.GetStartupPath() + Path.DirectorySeparatorChar + "DuckDNS.lnk";
            WShellLink.CreateLink(linkPath, "Duck DNS Updater", Assembly.GetExecutingAssembly().Location);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            icoTray.Dispose();
            icoTrayC.Dispose();
        }

        private void pTitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                Windows.DragWindow(Handle);
        }

        private void FMain_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawRectangle(Windows.framePen, 0, 0, Size.Width - 1, Size.Height - 1);
        }

        private void btAbout_Click(object sender, EventArgs e)
        {
            Visible = false;
            FAbout.Execute();
            Visible = true;
        }

        private void btIconify_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btClose_Click(object sender, EventArgs e)
        {
            canClose = true;
            Close();
        }

        private void valueChanged(object sender, EventArgs e)
        {
            saveOrDie = true;
        }

        private void FMain_Deactivate(object sender, EventArgs e)
        {
            if (saveOrDie)
                SaveConf();
        }
    }
}
