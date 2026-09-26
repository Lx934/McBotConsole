using System;
using System.Collections.Generic;

namespace JBSS261A.Patching
{
    /// <summary>补丁元数据（patch.json）</summary>
    public class PatchInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Risk { get; set; }
        public string ReleaseNotes { get; set; }
        public string TargetVersion { get; set; }
        public string TargetVersionRange { get; set; }
        public long CreatedAt { get; set; }
        public string Nonce { get; set; }
        public bool ReplaceSelf { get; set; }
        public bool RequiresRestart { get; set; }
    }

    /// <summary>文件清单（manifest.json）</summary>
    public class PatchManifest
    {
        public List<PatchFileEntry> Files { get; set; } = new List<PatchFileEntry>();
    }

    public class PatchFileEntry
    {
        public string Path { get; set; }
        public string Sha256 { get; set; }
        public long Size { get; set; }
    }

    /// <summary>补丁状态</summary>
    public enum PatchStatus
    {
        Unknown,           // 未知
        SignatureInvalid,  // 签名错误
        ManifestInvalid,   // 清单校验失败
        VersionMismatch,   // 版本不匹配
        AlreadyApplied,    // 已应用
        Unofficial,        // 非官方
        NetworkError,      // 网络错误（无法验证官方来源）
        Official,          // 官方认证
        ApplyFailed,       // 应用失败
        Applied            // 已应用成功
    }

    /// <summary>单个补丁的加载结果</summary>
    public class PatchLoadResult
    {
        public PatchStatus Status { get; set; }
        public string Message { get; set; }
        public string FilePath { get; set; }
        public string TempExtractDir { get; set; }
        public PatchInfo Info { get; set; }
        public PatchManifest Manifest { get; set; }
        public string OfficialTag { get; set; }
        public string OfficialCommitSha { get; set; }
        public bool CanProceedWithSignatureOnly { get; set; }
    }

    /// <summary>已应用补丁记录（applied.jsonl 每行一条）</summary>
    public class AppliedPatchEntry
    {
        public string PatchId { get; set; }
        public string Nonce { get; set; }
        public long AppliedAt { get; set; }
        public List<string> Files { get; set; } = new List<string>();
        public string BackupDir { get; set; }
    }
}