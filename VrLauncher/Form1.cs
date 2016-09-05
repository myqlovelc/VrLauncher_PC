using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

using Microsoft.Win32;

namespace VrService
{
    public partial class Form1 : Form
    {
        public delegate void MessageHandler(MessageEventArgs e);

        string arg = "";

        ThreadStarter start = null;

        public Form1(string[] args, bool isFirstRun)
        {
            if (args.Length > 0)
            {
                //获取启动时的命令行参数
                arg = args[0];
            }

            if (isFirstRun)
            {
                SetAutoRun();
            }
            //string path = this.GetType().Assembly.Location;
            //Console.WriteLine(path);
            InitializeComponent();
        }

        private void SetAutoRun()
        {
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                for (int i = 0; i < key.GetValueNames().Length; i++ )
                {
                    Console.WriteLine(key.GetValueNames()[i]);
                }
                if (key == null)
                {
                    key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
                    key.SetValue("VrLauncher", this.GetType().Assembly.Location + " -s");
                }
                else
                {
                    key.SetValue("VrLauncher", this.GetType().Assembly.Location + " -s");
                }
                key.Close();
            }
            catch (Exception e) {
                Console.WriteLine(e.StackTrace);
            }
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
                //this.notifyIcon1.Visible = false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Console.WriteLine("Form1_FormClosing");
            //MessageBox.Show("程序将最小化到系统托盘区");
            e.Cancel = true; // 取消关闭窗体
            this.Hide();
            this.ShowInTaskbar = false;//取消窗体在任务栏的显示
            //this.notifyIcon1.Visible = true;//显示托盘图标
        }
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                this.ShowInTaskbar = false;
                //this.notifyIcon1.Visible = true;
            }
        }

        private void menuShow_Click(object sender, EventArgs e)
        {
            this.Show();
            this.ShowInTaskbar = true;
            //this.notifyIcon1.Visible = false;
        }
        private void menuExit_Click(object sender, EventArgs e)
        {
            ThreadStarter.ClearLog();
            this.ShowInTaskbar = false;
            this.notifyIcon1.Visible = false;
            this.Close();
            this.Dispose(true);
            Application.ExitThread();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (arg != null)
            {
                //arg不为空,说明有启动参数,是从注册表启动的,则直接最小化到托盘
                this.Visible = false;
                this.ShowInTaskbar = false;
            }

            start = new ThreadStarter();
            start.MessageSend += new ThreadStarter.MessageEventHandler(this.EventHandler);
            start.StartThread();
        }

        private void EventHandler(object sender, MessageEventArgs e)
        {
            //实例化代理
            MessageHandler handler = new MessageHandler(AddMessage);
            //调用Invoke
            this.Invoke(handler, new object[] { e });
        }

        public void AddMessage(MessageEventArgs e)
        {
            try
            {
                richTextBox1.Text += e.Message + System.Environment.NewLine;
            }
            catch (Exception ee)
            {
                ListViewItem Item = new ListViewItem();
                Item.SubItems[0].Text = ee.Message.ToString();
                //this.listView1.Items.Add(Item);
            }   
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.SelectionStart = richTextBox1.Text.Length; //Set the current caret position at the end
            richTextBox1.ScrollToCaret(); //Now scroll it automatically
        }

        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = string.Empty;
        }

    }
}
