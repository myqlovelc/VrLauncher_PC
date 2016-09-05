using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;

namespace VrService
{
    public class ProcessUtil
    {
        public static bool StartProgram(string programPath, string programPara,ref string errorMsg)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.FileName = Path.GetFileName(programPath);
            processInfo.WorkingDirectory = Path.GetDirectoryName(programPath);
            try
            {
                if (!File.Exists(programPath))
                {
                    errorMsg = string.Format("Program:{0} not exist",programPath);
                    return false;
                }

                if (!AlertService.Interop.CreateProcess(programPath, Path.GetDirectoryName(programPath), programPara))
                {
                    errorMsg = string.Format("Start Program Inner Error");
                    return false;
                }
                bool started = true;
                int turn = 0;
                Process[] currentRunEditingProcess = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(programPath));
                while (currentRunEditingProcess == null || currentRunEditingProcess.Length == 0)
                {
                    Thread.Sleep(1000);
                    turn++;
                    // 5秒之后视为程序启动失败
                    if (turn > 5)
                    {
                        started = false;
                        return false;
                    }
                    currentRunEditingProcess = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(programPath));
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                return false;
            }
        }


        public static bool CloseProgram(string programPath,ref string errorMsg)
        {
            try
            {
                Process[] currentProgramProcess = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(programPath));
                if (currentProgramProcess != null || currentProgramProcess.Length > 0)
                {
                    foreach (Process p in currentProgramProcess)
                    {
                        p.Kill();
                        p.WaitForExit();
                        p.Close();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                return false;
            }
        }
    }
}
