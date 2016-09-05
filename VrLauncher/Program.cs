﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

using System.Configuration;

namespace VrService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string strIsFirstRun = "false";
            bool isFirstRun = false;
            strIsFirstRun = ConfigurationManager.AppSettings["isFirstRun"];
            Console.WriteLine(strIsFirstRun);

            if (string.IsNullOrEmpty(strIsFirstRun) || strIsFirstRun.ToLower() != "true")
            {
                isFirstRun = false;
            }
            else
            {
                isFirstRun = true;
            }
            strIsFirstRun = "false";

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings.Remove("IsFirstRun");
            config.AppSettings.Settings.Add("IsFirstRun", strIsFirstRun);
            config.Save();

            bool bCreatedNew;
            Mutex m = new Mutex(false, "Product_Index_Cntvs", out bCreatedNew);
            if (bCreatedNew)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                //Application.Run(new Form1(args));

                if (isFirstRun)
                {
                    /**
                    * 当前用户是管理员的时候，直接启动应用程序
                    * 如果不是管理员，则使用启动对象启动程序，以确保使用管理员身份运行
                    */
                    //获得当前登录的Windows用户标示
                    System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                    System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
                    //判断当前登录用户是否为管理员
                    if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
                    {
                        //如果是管理员，则直接运行
                        Application.Run(new Form1(args, isFirstRun));
                    }
                    else
                    {
                        //创建启动对象
                        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                        startInfo.UseShellExecute = true;
                        startInfo.WorkingDirectory = Environment.CurrentDirectory;
                        startInfo.FileName = Application.ExecutablePath;
                        startInfo.Arguments = "-s";
                        //设置启动动作,确保以管理员身份运行
                        startInfo.Verb = "runas";
                        try
                        {
                            System.Diagnostics.Process.Start(startInfo);
                        }
                        catch
                        {
                            return;
                        }
                        //退出
                        Application.Exit();
                    }
                }
                else
                {
                    Application.Run(new Form1(args, isFirstRun));
                }
            }
        }
    }
}
