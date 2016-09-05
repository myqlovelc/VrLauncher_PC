using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace VrService
{
   public class HandleRequest
    {
        enum STATE
        {
            Idle,
            Running,
        }

        LaunchAppRequest _currentRequest;
        STATE _state = STATE.Idle;
        Process _lastProcess;
        public Process LastProcess
        {
            get { return _lastProcess; }
        }

        public CmdReceiver CmdChannel
        {
            private get;
            set;
        }

        bool ValidateSingleInstance(string processName_, out Process _process)
        {
            _process = null;
            if (processName_ == "" || processName_ == null) return true;
            var processes = Process.GetProcessesByName(processName_);
            if (processes.Length > 0)
            {
                _process = processes[0];
                return false;
            }
            else
            {
                _process = null;
                return true;
            }
        }

        public void HandleLaunch(LaunchAppRequest launch_)
        {
            ThreadStarter._instance.OnMessageSend(this, new MessageEventArgs("Launch App: " + launch_.EXEPath + "..."));
            var info = string.Format("LaunchRequestHandler_Handle: \n{0}\n", launch_);
            info += string.Format("_state: {0}\n", _state);
            //Debug.Log(info);
            LogUtil.WriteLog(info);
            if (_state != STATE.Idle)
            {
                CmdChannel.SendLaunchOK(
                    launch_, LaunchRequestResponse.ENUM_LAUNCH_STATUS.IsRunning,
                    _currentRequest.EXEPath + "(Not Idle)");
            }
            else
            {
                _currentRequest = launch_;
                var exePath = launch_.EXEPath;
                if (!File.Exists(exePath))
                {
                    CmdChannel.SendLaunchOK(launch_, LaunchRequestResponse.ENUM_LAUNCH_STATUS.NotFound, exePath);
                }
                else
                {
                    var processName = Path.GetFileNameWithoutExtension(exePath);
                    Process prevProcess;
                    if (ValidateSingleInstance(processName, out prevProcess))
                    {
                        try
                        {
                            _lastProcess = new Process();
                            var startInfo = _lastProcess.StartInfo;
                            startInfo.FileName = exePath;
                            startInfo.Arguments = launch_.Arguments;
                            startInfo.WorkingDirectory = Path.GetDirectoryName(exePath);

                            info = "Startup process\n";
                            info += string.Format("startInfo.FileName: {0}\n", startInfo.FileName);
                            info += string.Format("startInfo.Arguments: {0}\n", startInfo.Arguments);
                            info += string.Format("startInfo.WorkingDirectory: {0}\n", startInfo.WorkingDirectory);
                            //Debug.Log(info);
                            LogUtil.WriteLog(info);
                            //有bug，好像启动不起来
                            bool res = _lastProcess.Start();
                            LogUtil.WriteLog("Startup res:"+res);
                            //if (!AlertService.Interop.CreateProcess(exePath, Path.GetDirectoryName(exePath), launch_.Arguments))
                            //{
                            //    LogUtil.WriteLog(string.Format("Start Program Inner Error"));
                            //}

                            Process[] currentRunEditingProcess = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exePath));
                            int turn = 0;
                            while (currentRunEditingProcess == null || currentRunEditingProcess.Length == 0)
                            {
                                Thread.Sleep(1000);
                                turn++;
                                // 5秒之后视为程序启动失败
                                if (turn > 5)
                                {
                                    ThreadStarter._instance.OnMessageSend(this, new MessageEventArgs("Launch App Failed"));
                                    LogUtil.WriteLog(string.Format("Start Program Inner Error after 5 seconds"));
                                    return;
                                }
                                currentRunEditingProcess = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exePath));
                            }
                            _lastProcess = currentRunEditingProcess[0];

                            ThreadStarter._instance.OnMessageSend(this, new MessageEventArgs("Launch App Success"));

                            CmdChannel.SendLaunchOK(launch_, LaunchRequestResponse.ENUM_LAUNCH_STATUS.OK, exePath);
                            //向控制端发送alive反馈 todo
                            //StartCoroutine(RunLoop());
                            //Thread t = new Thread(KeepAliveLoop);
                            //t.Start();
                            LogUtil.WriteLog("Start KeepAliveLoop success");
                        }
                        catch (Exception ex)
                        {
                            //Debug.LogWarning(ex);
                            LogUtil.WriteLog(ex.Message);
                            CmdChannel.SendLaunchOK(launch_, LaunchRequestResponse.ENUM_LAUNCH_STATUS.Error, exePath);
                        }
                    }
                    else
                    {
                        ThreadStarter._instance.OnMessageSend(this, new MessageEventArgs("Process is already running"));
                        //Debug.Log("Process is already running");
                        LogUtil.WriteLog("Process is already running");
                        CmdChannel.SendLaunchOK(
                            launch_, LaunchRequestResponse.ENUM_LAUNCH_STATUS.IsRunning,
                            _currentRequest.EXEPath + "(Launched elsewhere)");
                        if (_lastProcess == null)
                        {
                            //Debug.Log("Use prevProcess as _lastProcess!");
                            LogUtil.WriteLog("Use prevProcess as _lastProcess!");
                            _lastProcess = prevProcess;
                            var startInfo = _lastProcess.StartInfo;
                            startInfo.FileName = exePath;
                            startInfo.Arguments = launch_.Arguments;
                            startInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
                            //向控制端发送alive反馈 todo
                            //StartCoroutine(RunLoop());
                            //Thread t = new Thread(KeepAliveLoop);
                            //t.Start();
                            LogUtil.WriteLog("Start KeepAliveLoop success");
                        }
                    }
                }
            }
        }

        public void HandleShutdown(MsgRequestShutdownPlayer shutdown_)
        {
            ThreadStarter._instance.OnMessageSend(this, new MessageEventArgs("Shutdown Last Launched App..."));
            var info = "HandleShutdown:\n";
            if (shutdown_ != null)
            {
                LogUtil.WriteLog("shutdown_!=null");
                if (_lastProcess != null)
                {
                    LogUtil.WriteLog("_lastProcess != null");
                    _lastProcess.Refresh();
                    if (_lastProcess.Responding)
                    {
                        LogUtil.WriteLog("_lastProcess.Responding true");
                        try
                        {
                            LogUtil.WriteLog("pos1");
                            if (_lastProcess.MainWindowHandle != IntPtr.Zero)
                            {
                                LogUtil.WriteLog("_lastProcess.MainWindowHandle != IntPtr.Zero");
                                info += string.Format("CloseMainWindow: {0}\n", _lastProcess.MainWindowTitle);
                                _lastProcess.CloseMainWindow();
                            }
                            LogUtil.WriteLog("pos2");
                            info += string.Format("Close _lastProcess: {0}\n", _lastProcess.ProcessName);
                            //info += string.Format("Closed {0} {1} {2}\n",
                                    //_lastProcess.ProcessName, _lastProcess.ExitCode, _lastProcess.ExitTime);
                            //_lastProcess.Close();
                            //新的关闭进程的方法
                            LogUtil.WriteLog("before close process");
                            _lastProcess.Kill();
                            _lastProcess.WaitForExit();
                            _lastProcess.Close();
                            LogUtil.WriteLog("after close process");

                            _lastProcess = null;
                            //Debug.Log(info);
                            LogUtil.WriteLog(info);
                            ThreadStarter._instance.OnMessageSend(this, new MessageEventArgs("Shutdown App Success"));
                            CmdChannel.SendShutdownOK(shutdown_, ShutdownRequestResponse.ENUM_SHUTDOWN_STATUS.OK);
                        }
                        catch (InvalidOperationException ex)
                        {
                            //Debug.Log(ex); // HasExited
                            LogUtil.WriteLog("InvalidOperationException1:" + ex.Message);
                            CmdChannel.SendShutdownOK(shutdown_, ShutdownRequestResponse.ENUM_SHUTDOWN_STATUS.OK);
                        }
                        catch (Exception ex)
                        {
                            //Debug.Log(ex);
                            LogUtil.WriteLog("Exception1:" + ex.Message);
                            CmdChannel.SendShutdownOK(shutdown_, ShutdownRequestResponse.ENUM_SHUTDOWN_STATUS.OK);
                        }
                    }
                    else
                    {
                        try
                        {
                            _lastProcess.Kill();
                            //_lastProcess.WaitForExit(5000);
                            _lastProcess.WaitForExit();
                            info += string.Format("Kill {0} {1} {2}\n", _lastProcess.ProcessName, _lastProcess.ExitCode, _lastProcess.ExitTime);
                            _lastProcess.Close();
                            _lastProcess = null;
                            //Debug.Log(info);
                            LogUtil.WriteLog(info);
                            ThreadStarter._instance.OnMessageSend(this, new MessageEventArgs("Shutdown App Success"));
                            CmdChannel.SendShutdownOK(shutdown_, ShutdownRequestResponse.ENUM_SHUTDOWN_STATUS.OK);
                        }
                        catch (InvalidOperationException ex)
                        {
                            //Debug.Log(ex);
                            LogUtil.WriteLog("InvalidOperationException2:" + ex.Message);
                            CmdChannel.SendShutdownOK(shutdown_, ShutdownRequestResponse.ENUM_SHUTDOWN_STATUS.OK);
                        }
                        catch (Exception ex)
                        {
                            //Debug.Log(ex);
                            LogUtil.WriteLog("Exception2:"+ex.Message);
                            CmdChannel.SendShutdownOK(shutdown_, ShutdownRequestResponse.ENUM_SHUTDOWN_STATUS.OK);
                        }
                    }
                }
                else
                {
                    //Debug.Log("There is not process running, _lastProcess is null");
                    LogUtil.WriteLog("There is not process running, _lastProcess is null");
                    //ThreadStarter._instance.OnMessageSend(this, new MessageEventArgs("Shutdown App Success"));
                    CmdChannel.SendShutdownOK(shutdown_, ShutdownRequestResponse.ENUM_SHUTDOWN_STATUS.OK);
                }
            }
        }

        public void KeepAliveLoop()
        {
            _state = STATE.Running;
            var keepAlive = _currentRequest.KeepAlive;
            LogUtil.WriteLog(this+" keepAliveTime: "+keepAlive);
            //var waitForSeconds = new WaitForSeconds(keepAlive);
            while (true)
            {
                if (_lastProcess == null)
                {
                    //Debug.Log("_lastProcess is null, may be closed.");
                    LogUtil.WriteLog("_lastProcess is null, may be closed.");
                    break;
                }

                var hasExited = false;
                try
                {
                    if (hasExited = _lastProcess.HasExited)
                    {
                        _lastProcess.WaitForExit(5000);
                        //_lastProcess.WaitForExit();
                        //Debug.Log(string.Format("_lastProcess exit: {0}, {1}, {2}",
                        //_lastProcess.ProcessName, _lastProcess.ExitCode, _lastProcess.ExitTime));
                        LogUtil.WriteLog(string.Format("_lastProcess exit: {0}, {1}, {2}",_lastProcess.ProcessName, _lastProcess.ExitCode, _lastProcess.ExitTime));
                    }
                }
                catch (InvalidOperationException ex)
                {
                    // HasExited throw process not started
                    //Debug.Log(ex);
                    LogUtil.WriteLog(ex.Message);
                }

                if (hasExited)
                {
                    _lastProcess.Close();
                    _lastProcess = null;
                    break;
                }

                try
                {
                    _lastProcess.Refresh();
                    //Debug.Log(string.Format(
                    //    "_lastProcess.ProcessName: {0}\n"
                    //    + "_lastProcess.Responding: {1}\n"
                    //    + "_lastProcess.MainWindowHandle: {2}\n"
                    //    + "_lastProcess.MainWindowTitle: {3}\n",
                    //    _lastProcess.ProcessName,
                    //    _lastProcess.Responding,
                    //    _lastProcess.MainWindowHandle,
                    //    _lastProcess.MainWindowTitle));
                    LogUtil.WriteLog(string.Format(
                          "_lastProcess.ProcessName: {0}\n"
                            + "_lastProcess.Responding: {1}\n"
                            + "_lastProcess.MainWindowHandle: {2}\n"
                            + "_lastProcess.MainWindowTitle: {3}\n",
                            _lastProcess.ProcessName,
                            _lastProcess.Responding,
                            _lastProcess.MainWindowHandle,
                            _lastProcess.MainWindowTitle));
                    CmdChannel.SendLaunchOK(
                        _currentRequest, LaunchRequestResponse.ENUM_LAUNCH_STATUS.IsRunning,
                        _lastProcess.ProcessName);
                }
                catch (InvalidOperationException ex)
                {
                    // ProcessHasExited is trigger unexpectedly!!!!
                    // ignore
                    //Debug.LogWarning(ex);
                    //Debug.Log("_lastProcess.HaxExited: " + _lastProcess.HasExited);
                    LogUtil.WriteLog(ex.Message);
                    LogUtil.WriteLog("_lastProcess.HaxExited: " + _lastProcess.HasExited);
                }
                //yield return waitForSeconds;
                Thread.Sleep(keepAlive);
            }

            _state = STATE.Idle;
        }
    }
}
