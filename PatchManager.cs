using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JBSS261A.Patching
{
    public class PatchManager
    {
        // ⚠️ 用 keygen.ps1 生成的公钥替换这里 ⚠️
        private const string PUBLIC_KEY_XML =
            "<RSAKeyValue><Modulus>z+ZDKEUHD9Bnr7Us3KOrhzTbnYYKJEgDJ2hQoPSXp9iBrAGYuyVP9nM2GTwjaX3RsRp/oL3ddsT8xKV0xpvboZ43P25TyPAJiyTRLYGSeDYYvVzTZxYfI2CNg0efvxZtTLCVu/cZvATZGESzzr9nVakVmFGUAW7JS3p311YIEdjvvxsVoNQR5kTyHdk8Ix9GqQnzJg/kOEkcZR/F7R5yAiragd90yE67TaYwe7XuXBfCQK3c+IPL5VBqbpMGfTl506GV8NqW/kDWYS8g6D3I5gT3hyiw5yipIKzOmgQFK6kROs3pecPJkfbGsp+yZOPdezZdnZWPJqSMaO9Sh1tRbQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        private readonly string _programDir;
        private readonly string _patchesDir;
        private readonly string _backupRootDir;
        private readonly string _appliedJsonPath;
        private readonly Action<string> _log;

        public PatchManager(string programDir, Action<string> log)
        {
            _programDir = programDir;
            _patchesDir = Path.Combine(programDir, "patches");
            _backupRootDir = Path.Combine(_patchesDir, "backup");
            _appliedJsonPath = Path.Combine(_patchesDir, "applied.jsonl");
            _log = log ?? (s => { });

            Directory.CreateDirectory(_patchesDir);
            Directory.CreateDirectory(_backupRootDir);
        }

        public string PatchesDir => _patchesDir;

        // ==================== 扫描 ====================
        public List<string> ScanPatchFiles()
        {
            var result = new List<string>();
            if (!Directory.Exists(_patchesDir)) return result;

            foreach (var f in Directory.GetFiles(_patchesDir, "*.mcpatch"))
                result.Add(f);

            return result;
        }

        // ==================== 加载并验证 ====================
        public async Task<PatchLoadResult> LoadAndVerifyAsync(string patchFilePath)
        {
            _log("DevMode 状态 = " + DevModeFlag.IsActive()
                 + "，CurrentValue = " + DevModeFlag.CurrentValue());
            var result = new PatchLoadResult { FilePath = patchFilePath };

            if (!File.Exists(patchFilePath))
            {
                result.Status = PatchStatus.Unknown;
                result.Message = "补丁文件不存在";
                return result;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "mcpatch-" + Guid.NewGuid().ToString("N"));

            try
            {
                // 1. 解压
                Directory.CreateDirectory(tempDir);
                ZipFile.ExtractToDirectory(patchFilePath, tempDir);
                result.TempExtractDir = tempDir;

                // 2. 定位关键文件
                string patchJsonPath = Path.Combine(tempDir, "patch.json");
                string manifestJsonPath = Path.Combine(tempDir, "manifest.json");
                string sigPath = Path.Combine(tempDir, "signature.bin");
                string payloadDir = Path.Combine(tempDir, "payload");

                if (!File.Exists(patchJsonPath) || !File.Exists(manifestJsonPath) || !File.Exists(sigPath))
                {
                    result.Status = PatchStatus.ManifestInvalid;
                    result.Message = "补丁包缺少必需文件（patch.json / manifest.json / signature.bin）";
                    return result;
                }

                byte[] patchBytes = File.ReadAllBytes(patchJsonPath);
                byte[] manifestBytes = File.ReadAllBytes(manifestJsonPath);
                byte[] signature = File.ReadAllBytes(sigPath);

                var info = JsonSerializer.Deserialize<PatchInfo>(patchBytes,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var manifest = JsonSerializer.Deserialize<PatchManifest>(manifestBytes,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (info == null || string.IsNullOrEmpty(info.Id))
                {
                    result.Status = PatchStatus.ManifestInvalid;
                    result.Message = "patch.json 解析失败";
                    return result;
                }

                result.Info = info;
                result.Manifest = manifest;

                // 3. RSA 签名验证
                bool sigOk = VerifySignature(patchBytes, manifestBytes, info.Nonce, signature);
                if (!sigOk)
                {
                    result.Status = PatchStatus.SignatureInvalid;
                    result.Message = "签名验证失败，补丁可能被篡改或非官方发布";
                    return result;
                }

                // 4. 检查 nonce 是否已应用
                var applied = GetAppliedPatches();
                if (applied.Any(a => a.Nonce == info.Nonce))
                {
                    result.Status = PatchStatus.AlreadyApplied;
                    result.Message = $"该补丁已应用过（nonce 前 8 位：{SafeShort(info.Nonce, 8)}）";
                    return result;
                }

                // 5. 校验 payload 文件 sha256
                if (manifest != null && manifest.Files != null)
                {
                    foreach (var entry in manifest.Files)
                    {
                        string filePath = Path.Combine(payloadDir, entry.Path);
                        if (!File.Exists(filePath))
                        {
                            result.Status = PatchStatus.ManifestInvalid;
                            result.Message = $"payload 缺少文件: {entry.Path}";
                            return result;
                        }

                        string actualSha = ComputeSha256(filePath);
                        if (!string.Equals(actualSha, entry.Sha256, StringComparison.OrdinalIgnoreCase))
                        {
                            result.Status = PatchStatus.ManifestInvalid;
                            result.Message = $"文件校验失败: {entry.Path}";
                            return result;
                        }
                    }
                }

                // 6. 官方来源验证
                // ★ 开发者模式：跳过 GitHub API 验证
                _log("ZZZ_6_1 到达官方验证检查点，DevMode=" + DevModeFlag.IsActive());
                if (DevModeFlag.IsActive())
                {
                    _log("ZZZ_6_2 DevMode 命中，直接返回 Official");
                    result.Status = PatchStatus.Official;
                    result.OfficialTag = "(dev-mode)";
                    result.OfficialCommitSha = "(dev-mode)";
                    result.Message = "✅ 开发者模式，已跳过 GitHub 官方验证";
                    return result;
                }
                _log("ZZZ_6_3 DevMode 未命中，将调用 GitHub API");

                var officialResult = await OfficialVerifier.VerifyAsync(info.Id, 10000);

                switch (officialResult.Status)
                {
                    case OfficialVerifier.VerifyStatus.Official:
                        result.Status = PatchStatus.Official;
                        result.OfficialTag = officialResult.OfficialTagName;
                        result.OfficialCommitSha = officialResult.OfficialCommitSha;
                        result.Message = "✅ 官方认证通过";
                        break;

                    case OfficialVerifier.VerifyStatus.NotFound:
                        result.Status = PatchStatus.Unofficial;
                        result.Message = "❌ 该补丁不在官方仓库标签列表中，已拒绝加载。";
                        return result;

                    case OfficialVerifier.VerifyStatus.NetworkError:
                        result.Status = PatchStatus.NetworkError;
                        result.Message =
                            "⚠️ 无法连接 GitHub 验证官方来源。\n" +
                            "你可以选择：\n" +
                            "  · 稍后重试\n" +
                            "  · 跳过官方验证（仅使用签名验证）";
                        result.CanProceedWithSignatureOnly = true;
                        break;

                    default:
                        result.Status = PatchStatus.NetworkError;
                        result.Message = "⚠️ 官方验证响应异常：" + officialResult.Message;
                        result.CanProceedWithSignatureOnly = true;
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Status = PatchStatus.Unknown;
                result.Message = "补丁处理异常：" + ex.Message;
                try { Directory.Delete(tempDir, true); } catch { }
                return result;
            }
        }

        // ==================== RSA 签名验证 ====================
        private bool VerifySignature(byte[] patchBytes, byte[] manifestBytes, string nonce, byte[] signature)
        {
            try
            {
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.PersistKeyInCsp = false;
                    rsa.FromXmlString(PUBLIC_KEY_XML);

                    using (var ms = new MemoryStream())
                    {
                        ms.Write(patchBytes, 0, patchBytes.Length);
                        ms.Write(manifestBytes, 0, manifestBytes.Length);
                        if (!string.IsNullOrEmpty(nonce))
                        {
                            var nonceBytes = Encoding.UTF8.GetBytes(nonce);
                            ms.Write(nonceBytes, 0, nonceBytes.Length);
                        }

                        byte[] data = ms.ToArray();
                        using (var sha = SHA256.Create())
                        {
                            return rsa.VerifyData(data, sha, signature);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log("签名验证异常：" + ex.Message);
                return false;
            }
        }

        // ==================== DevMode 判定 ====================
        private static bool IsDevModeEnabled()
        {
            // 方式 1：环境变量 MCBOT_DEV=1
            if (Environment.GetEnvironmentVariable("MCBOT_DEV") == "1") return true;

            // 方式 2：命令行参数 --dev
            var args = Environment.GetCommandLineArgs();
            if (args.Any(a => a.Equals("--dev", StringComparison.OrdinalIgnoreCase))) return true;

            return false;
        }
        // ==================== SHA256 ====================
        public static string ComputeSha256(string filePath)
        {
            using (var sha = SHA256.Create())
            using (var fs = File.OpenRead(filePath))
            {
                byte[] hash = sha.ComputeHash(fs);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        // ==================== 已应用补丁记录 ====================
        public List<AppliedPatchEntry> GetAppliedPatches()
        {
            var list = new List<AppliedPatchEntry>();
            if (!File.Exists(_appliedJsonPath)) return list;

            try
            {
                foreach (var line in File.ReadAllLines(_appliedJsonPath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var entry = JsonSerializer.Deserialize<AppliedPatchEntry>(line,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (entry != null) list.Add(entry);
                    }
                    catch { }
                }
            }
            catch { }

            return list;
        }

        // ==================== 应用补丁 ====================
        public bool ApplyPatch(PatchLoadResult result)
        {
            if (result == null || result.Info == null || result.Manifest == null)
            {
                _log("补丁信息不完整，无法应用");
                return false;
            }

            if (result.Status != PatchStatus.Official &&
                !(result.Status == PatchStatus.NetworkError && result.CanProceedWithSignatureOnly))
            {
                _log("补丁未通过验证，禁止应用");
                return false;
            }

            // 找 updater.exe
            string updaterSrc = Path.Combine(result.TempExtractDir, "payload", "updater.exe");
            if (!File.Exists(updaterSrc))
                updaterSrc = Path.Combine(_programDir, "updater.exe");

            if (!File.Exists(updaterSrc))
            {
                _log("找不到 updater.exe（补丁里没带，程序目录也没有）");
                return false;
            }

            // 释放 updater 到临时目录，避免占用当前目录
            string updaterTmp = Path.Combine(result.TempExtractDir, "updater_run.exe");
            try
            {
                File.Copy(updaterSrc, updaterTmp, true);
            }
            catch (Exception ex)
            {
                _log("释放 updater 失败：" + ex.Message);
                return false;
            }

            // 组装 job.json
            var job = new
            {
                programDir = _programDir,
                payloadDir = Path.Combine(result.TempExtractDir, "payload"),
                backupDir = Path.Combine(_backupRootDir, result.Info.Id),
                appliedJsonPath = _appliedJsonPath,
                mainExePath = Application.ExecutablePath,
                parentPid = System.Diagnostics.Process.GetCurrentProcess().Id,
                patchId = result.Info.Id,
                nonce = result.Info.Nonce,
                files = result.Manifest.Files.Select(f => f.Path).ToList()
            };

            string jobJsonPath = Path.Combine(result.TempExtractDir, "job.json");
            File.WriteAllText(jobJsonPath,
                JsonSerializer.Serialize(job, new JsonSerializerOptions { WriteIndented = true }),
                new UTF8Encoding(false));

            // 启动 updater
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = updaterTmp,
                    Arguments = "\"" + jobJsonPath + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = result.TempExtractDir
                };

                System.Diagnostics.Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                _log("启动 updater 失败：" + ex.Message);
                return false;
            }
        }

        // ==================== 回滚补丁 ====================
        public bool RollbackPatch(string patchId)
        {
            var entries = GetAppliedPatches();
            var target = entries.FirstOrDefault(e => e.PatchId == patchId);
            if (target == null)
            {
                _log("未找到补丁记录：" + patchId);
                return false;
            }

            string backupDir = target.BackupDir ?? Path.Combine(_backupRootDir, patchId);
            if (!Directory.Exists(backupDir))
            {
                _log("备份目录不存在：" + backupDir);
                return false;
            }

            foreach (var rel in target.Files)
            {
                string src = Path.Combine(backupDir, rel);
                string dst = Path.Combine(_programDir, rel);

                if (!File.Exists(src))
                {
                    _log("备份缺失：" + rel);
                    continue;
                }

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dst));
                    File.Copy(src, dst, true);
                    _log("已恢复：" + rel);
                }
                catch (Exception ex)
                {
                    _log("恢复失败：" + rel + " - " + ex.Message);
                    return false;
                }
            }

            try
            {
                var remaining = entries.Where(e => e.PatchId != patchId).ToList();
                var lines = remaining.Select(e => JsonSerializer.Serialize(e));
                File.WriteAllLines(_appliedJsonPath, lines);
            }
            catch (Exception ex)
            {
                _log("更新记录失败：" + ex.Message);
            }

            return true;
        }

        private static string SafeShort(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }
}