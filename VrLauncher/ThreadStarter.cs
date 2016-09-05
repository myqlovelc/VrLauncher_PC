using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.IO;
using System.Configuration;

namespace VrService
{
    public class MessageEventArgs : EventArgs
    {
        public String Message; //传递辽符串信息
        public MessageEventArgs(string message)
        {
            this.Message = message;
        }
    }

    public class ThreadStarter
    {
        public static ThreadStarter _instance = null;

        public delegate void MessageEventHandler(object sender, MessageEventArgs e);
        public event MessageEventHandler MessageSend;

        public static LogUtil log;

        private CmdReceiver _cmdReceiver;

        public ThreadStarter()
        {
            if (_instance != null)
            {
                return;
            }
            _instance = this;
        }

        /*
         * 说明:定义事件处理函数,当然这里也可以不用直接在引发事件时调用this.MessageSend(sender, e);
         * 这里的参数要和事件代理的参数一样
         * */
        public void OnMessageSend(object sender, MessageEventArgs e)
        {
            if (MessageSend != null)
                this.MessageSend(sender, e);
        }

        public void StartThread()
        {
            string path = this.GetType().Assembly.Location;
            DirectoryInfo dr = new DirectoryInfo(path);
            path = dr.Parent.FullName;
            Console.WriteLine("log path: " + path);
            log = new LogUtil(path + "\\Log.txt");
            //创建UDP监听线程，监听控制端的开启，关闭命令

            _cmdReceiver = new CmdReceiver();
            //_cmdReceiver.InPort = 8010;
            _cmdReceiver.InPort = int.Parse(ConfigurationManager.AppSettings["InPort"]);
            _cmdReceiver.Init();

            LogUtil.WriteLog("Server Started");
            OnMessageSend(this, new MessageEventArgs("Listening Thread Started"));

            System.Timers.Timer t = new System.Timers.Timer();
            t.Interval = 20;
            t.Elapsed += new ElapsedEventHandler(t_Elapsed);
            t.AutoReset = true;
            t.Enabled = true;
        }


        void t_Elapsed(object sender, ElapsedEventArgs e)
        {
            _cmdReceiver.Update();
        }

        public static void ClearLog() {
            log.Clear();
        }
    }
}
