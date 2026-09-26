using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace JBSS261A
{
    public class NodeBackend : IDisposable
    {
        private Process _process;
        private readonly object _writeLock = new object();
        private bool _disposed;

        public event Action<GuiMessage> MessageReceived;
        public event Action<string> ErrorOccurred;
        public event Action<int> Exited;

        public bool IsRunning => _process != null && !_process.HasExited;

        public bool Start(string backendDir)
        {
            try
            {
                string exePath = Path.Combine(backendDir, "mc-bot-backend.exe");
                string nodeScript = Path.Combine(backendDir, "index.js");

                ProcessStartInfo psi;
                if (File.Exists(exePath))
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = "",
                        WorkingDirectory = backendDir,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };
                }
                else if (File.Exists(nodeScript))
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = "node",
                        Arguments = $"\"{nodeScript}\"",
                        WorkingDirectory = backendDir,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };
                }
                else
                {
                    ErrorOccurred?.Invoke($"找不到后端程序（{exePath} 或 {nodeScript}）");
                    return false;
                }

                // ★ 每个 C# 实例用 PID 作为实例 ID，避免多开时日志/成功文件互相覆盖
                int pid = Process.GetCurrentProcess().Id;
                try { psi.EnvironmentVariables["JBSS_INSTANCE_ID"] = pid.ToString(); } catch { }

                _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
                _process.OutputDataReceived += OnStdoutData;
                _process.ErrorDataReceived += OnStderrData;
                _process.Exited += OnProcessExit;

                if (!_process.Start())
                {
                    ErrorOccurred?.Invoke("无法启动后端进程");
                    return false;
                }

                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"启动失败: {ex.Message}");
                return false;
            }
        }

        private void OnStdoutData(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            try
            {
                var msg = JsonSerializer.Deserialize<GuiMessage>(e.Data);
                if (msg != null && !string.IsNullOrEmpty(msg.Type))
                {
                    MessageReceived?.Invoke(msg);
                }
            }
            catch { }
        }

        private void OnStderrData(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            ErrorOccurred?.Invoke(e.Data);
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            int code = 0;
            try { code = _process?.ExitCode ?? 0; } catch { }
            Exited?.Invoke(code);
        }

        public void Send(string json)
        {
            if (!IsRunning) return;
            lock (_writeLock)
            {
                try
                {
                    _process.StandardInput.WriteLine(json);
                    _process.StandardInput.Flush();
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke($"写入失败: {ex.Message}");
                }
            }
        }

        public void SendCmd(string text)
        {
            Send(JsonSerializer.Serialize(new { type = "cmd", text = text }));
        }

        public void SendStart(object parameters)
        {
            Send(JsonSerializer.Serialize(new { type = "start", @params = parameters }));
        }

        public void SendExit()
        {
            Send(JsonSerializer.Serialize(new { type = "exit" }));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    try { SendExit(); } catch { }
                    if (!_process.WaitForExit(2000))
                        _process.Kill();
                }
            }
            catch { }
            finally
            {
                _process?.Dispose();
                _process = null;
            }
        }
    }
}