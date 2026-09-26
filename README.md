# McBotConsole

Minecraft Java 版服务器批量登录客户端，用于测试**自己拥有或已获书面授权**的服务器的承载能力。

图形化界面（C# WinForms）+ Node.js 后端（mineflayer），支持批量登录、真并发、代理池、玩家池、插件系统、热补丁。

**当前版本**：26H2 ｜ **最新发布**：[Releases](https://github.com/Lx934/McBotConsole/releases)

---

## ⚠️ 免责声明

**本项目仅供测试自己拥有或已获得书面授权的服务器。**

- ❌ 禁止用于攻击、压测、骚扰他人服务器
- ❌ 禁止用于绕过服务器反机器人机制
- ❌ 禁止用于任何违反当地法律法规的用途

因使用本工具产生的任何后果，由使用者自行承担。作者不对任何滥用行为负责。

---

## ✨ 功能特性

### 核心能力

- **批量登录**：基于 [mineflayer](https://github.com/PrismarineJS/mineflayer) 实现 MC 协议
- **真并发**：每个假人独立后台运行，不再串行阻塞
- **代理池**：支持 SOCKS4 / SOCKS5 / HTTP / HTTPS，自动健康追踪与轮换
- **玩家池**：从文件按顺序取名字，每个用一次，可配置耗尽后停止或循环
- **英文名字典**：内置常见英文名，可自定义 `names.txt`
- **自动重连**：掉线后自动重连（可配置最大次数）
- **多开支持**：用进程 PID 隔离日志和成功记录

### 服务器诊断

- **DNS 纯解析**：不依赖 ICMP，服务器禁 ping 也能用
- **服务器解析器**：DNS / Ping / TCP / SLP / Query / 插件探测
- **插件探测**：通过 Tab 补全扫描服务器插件列表
- **调试命令行**：`help` / `status` / `bots` / `kick` / `proxies` / `memory` 等

### 插件系统

- **双语言支持**：Node.js 插件 + Python 插件
- **权限分级**：0 只读 / 1 可聊天 / 2 可控制 / 3 系统级
- **热重载**：无需重启程序即可加载 / 卸载 / 重载插件
- **生命周期钩子**：`beforeConnect` / `afterConnect` / `onLogin` / `onKick` / `onError` / `onDisconnect` / `connectionResult`

### 补丁系统

- **热补丁**：`.mcpatch` 格式，不用重新下载完整包
- **RSA-2048 签名**：只有官方私钥签名的补丁才能应用
- **防篡改**：manifest SHA256 校验
- **防重放**：nonce 一次性
- **官方验证**：补丁 ID 必须在官方 GitHub tag 列表中
- **自动备份**：替换前备份，可一键回滚
- **路径穿越防护**：补丁无法写入程序目录之外的路径
- **失败自动回滚**：替换中途出错，自动恢复

### 更新检查

- 启动时静默检查最新版本
- 菜单【帮助 → 检查更新】可手动检查
- 显示更新说明、发布时间、下载链接
- 检测到已应用补丁时提示"旧补丁可能失效"
- 10 分钟内存缓存，避免刷爆 GitHub API

---

## 🖼 截图

<img width="1440" height="860" alt="5" src="https://github.com/user-attachments/assets/f0f06b24-b839-4969-96ae-631bdcd49af0" />

<img width="1440" height="860" alt="4" src="https://github.com/user-attachments/assets/babdf0c5-6926-46f6-95bc-bcef82edfcf0" />

---

## 🖥 系统要求

| 项 | 要求 |

|---|---|
| 操作系统 | Windows 10 / 11（64 位） |
| .NET Framework | 4.8 或更高（Win10 1903+ 自带） |
| 磁盘空间 | 至少 500 MB 可用 |
| 网络 | 检查更新需访问 `api.github.com` |

**不需要单独安装 Node.js**——后端已用 pkg 打包成独立 exe。

---

## 🚀 快速开始

### 1. 下载

从 [Releases](https://github.com/Lx934/McBotConsole/releases) 页面下载最新版本的 `McBotConsole-26H2.zip`。

**校验 SHA256**（PowerShell）：

```powershell
Get-FileHash .\McBotConsole-26H2.zip -Algorithm SHA256
```

与 Release 页面公布的哈希对比。不一致请勿使用。

### 2. 解压

解压到任意目录。**不要放在中文路径或含有空格的路径**。

### 3. 启动

双击 `JBSS261A.exe`。

### 4. 配置

在顶部输入框填入：
- 服务器地址（IP 或域名）
- 端口（默认 25565）
- 游戏版本（需与服务器一致）
- 尝试次数 / 并发数

点【⚙ 设置】可配置连接参数、代理、玩家名模式等。

### 5. 运行

点【▶ 开始】启动任务。

---

## 📅 版本规则

采用 **半年版本 + 补丁** 的编号方式：

| 类型 | 格式 | 示例 |
|---|---|---|
| 主版本 | `YYHn` | `26H1`（2026 上半年）、`26H2`（2026 下半年） |
| 补丁 | `YYHnuN` | `26H1u1`、`26H2u3` |

**补丁兼容规则**：

- 26H2 可加载 26H1 / 26H2 的补丁（同 H 号向下兼容）
- 跨年补丁不兼容（如 25H2 的补丁不能在 26H2 上用）
- 如果新版本修改了补丁要改的文件，补丁会提示"目标已修改"，需要确认后应用

---

## 🧩 补丁系统

### 应用补丁

1. 从官方渠道下载 `.mcpatch` 文件
2. 放入程序目录的 `patches/` 文件夹
3. 打开程序 → 点【🧩 补丁】
4. 选中补丁 → 点【应用】
5. 程序会自动重启（`requiresRestart: true` 时）

### 回滚补丁

1. 打开程序 → 点【🧩 补丁】
2. 找到已应用的补丁 → 点【回滚】

**注意**：回滚只会恢复被覆盖的文件内容，**不会删除补丁新增的文件**。

### 安全机制

- 补丁必须由官方私钥签名
- 补丁 ID 必须存在于官方 GitHub tag 列表
- 补丁内容受 SHA256 保护
- 每个补丁只能应用一次（nonce 防重放）

---

## 🔄 更新检查

程序启动后 3 秒会**静默检查**最新版本：

- 有新版本时，状态栏右侧出现 `🔔 发现新版本 26H2，点击查看`
- 点击可在对话框中查看更新说明、打开下载页

也可以随时通过菜单【帮助 → 检查更新】手动触发。

---

## 🔌 插件开发

### Node.js 插件

目录结构：

```
backend/plugins/<插件名>/
├── <插件名>.js      # 主文件
├── config/
│   └── <插件名>.json
└── log/
```

`<插件名>.js` 示例：

```javascript
module.exports = {
    name: 'my-plugin',
    version: '1.0.0',
    author: 'YourName',
    description: '插件说明',
    requiredPermission: 0,

    init(config, hooks, logger, api) {
        hooks.register('onLogin', (bot, ctx) => {
            logger.info(`Bot ${ctx.botId} 登录成功`);
        });
    },

    unload(reason, logger) {
        logger.info(`插件卸载：${reason}`);
    }
};
```

### Python 插件

目录结构：

```
backend/plugins_py/<插件名>/
├── plugin.json
├── main.py
└── config.json
```

`plugin.json`：

```json
{
    "name": "my-plugin",
    "version": "1.0.0",
    "author": "YourName",
    "description": "插件说明",
    "usage": "使用说明",
    "main": "main.py"
}
```

详细开发指南见 [PLUGIN_DEVELOPMENT.md](PLUGIN_DEVELOPMENT.md)。

---

## 📁 目录结构（发布包）

```
McBotConsole-26H2/
├── JBSS261A.exe                          ← 主程序
├── JBSS261A.exe.config
├── System.Text.Json.dll
├── System.Threading.Tasks.Extensions.dll
├── System.Memory.dll
├── System.Buffers.dll
├── System.Numerics.Vectors.dll
├── System.IO.Pipelines.dll
├── System.Runtime.CompilerServices.Unsafe.dll
├── System.Text.Encodings.Web.dll
├── Microsoft.Bcl.AsyncInterfaces.dll
├── updater.exe                           ← 补丁更新器
├── README.txt
└── backend/
    ├── mc-bot-backend.exe                ← Node 后端（pkg 打包）
    ├── names.txt                         ← 英文名字典
    └── config.json                       ← 默认配置
```

运行时自动生成（不用手动创建）：

- `backend/player_pool.txt`（首次启用玩家池时生成）
- `backend/logs/`（日志目录）
- `patches/`（补丁目录）
- `patches/applied.jsonl`（补丁应用记录）

---

## 🛠 从源码构建

### 环境要求

- Visual Studio 2022（含 .NET Framework 4.8 SDK）
- Node.js 18 或 20（用于打包后端）
- MinGW-w64（用于编译 updater）
- pkg 或 @yao-pkg/pkg（Node 打包工具）

### 编译 C# 主程序

```
打开 JBSS261A.sln
→ 选择 Release 配置
→ 生成解决方案
输出：bin\Release\JBSS261A.exe
```

### 打包 Node 后端

```cmd
cd backend
npm install
pkg index.js --target node18-win-x64 --output mc-bot-backend.exe
```

### 编译 updater

```cmd
cd updater
g++ updater.cpp -o updater.exe -std=c++11 -static -mwindows -lshell32
```

### 组装发布包

参照"目录结构"章节。

---

## ❓ 常见问题

**Q：程序启动后日志一直显示 "后台还未就绪"**

A：检查 `backend/mc-bot-backend.exe` 是否存在。如果缺失，说明发布包不完整。

**Q：登录全部失败，日志显示 `Connection throttled`**

A：服务器启用了连接限流。解决方式：
1. 调大【设置 → 连接参数 → 连接间隔】
2. 使用代理池
3. 检查服务器 `spigot.yml` 的 `connection-throttle` 配置

**Q：连接数明明设置了 6，实际并发远低于 6**

A：26H2 已修复此问题。如果仍出现，检查是否有大量重试占用了并发槽。

**Q：补丁应用后程序无法启动**

A：从 `patches/backup/<补丁ID>/` 手动恢复文件，或通过程序的【🧩 补丁 → 回滚】功能恢复。

**Q：更新检查失败**

A：可能是 GitHub API 速率限制（未认证用户每小时 60 次）。稍后重试，或配置代理。

**Q：能不能破解服务器的反机器人机制**

A：**不能，也不应该。** 程序只会记录限流信息，不会绕过。请自行调整连接节奏。

---

## 📜 License

[MIT License](LICENSE)

---

## 🙏 致谢

- [mineflayer](https://github.com/PrismarineJS/mineflayer) —— MC 协议实现
- [PrismarineJS](https://github.com/PrismarineJS) —— 相关工具链
- 所有提交 Issues 和 PR 的用户

---

# McBotConsole

A batch login client for Minecraft Java Edition servers, used to test the capacity of **servers you own or have written authorization to test**.

GUI (C# WinForms) + Node.js backend (mineflayer). Supports batch login, true concurrency, proxy pools, player pools, plugin system, and hot patching.

**Current version**: 26H2 | **Latest release**: [Releases](https://github.com/Lx934/McBotConsole/releases)

---

## ⚠️ Disclaimer

**This project is intended only for testing servers you own or have written authorization to test.**

- ❌ Do not use for attacking, stress-testing, or harassing servers you don't own
- ❌ Do not use to bypass server anti-bot mechanisms
- ❌ Do not use for any purpose that violates local laws or regulations

Any consequences arising from the use of this tool are the sole responsibility of the user. The author is not liable for any misuse.

---

## ✨ Features

### Core Capabilities

- **Batch login**: Minecraft protocol implementation based on [mineflayer](https://github.com/PrismarineJS/mineflayer)
- **True concurrency**: Each bot runs independently in the background, no longer serialized
- **Proxy pool**: Supports SOCKS4 / SOCKS5 / HTTP / HTTPS with automatic health tracking and rotation
- **Player pool**: Reads names from a file in order, each used once; configurable stop-or-loop on exhaustion
- **English name dictionary**: Built-in common English names, customizable via `names.txt`
- **Auto-reconnect**: Reconnects after disconnect (max retries configurable)
- **Multi-instance support**: Logs and success records isolated by process PID

### Server Diagnostics

- **Pure DNS resolution**: No ICMP dependency; works even when the server blocks ping
- **Server analyzer**: DNS / Ping / TCP / SLP / Query / plugin probing
- **Plugin detection**: Scans server plugin list via Tab completion
- **Debug console**: `help` / `status` / `bots` / `kick` / `proxies` / `memory`, etc.

### Plugin System

- **Dual-language support**: Node.js plugins + Python plugins
- **Permission levels**: 0 read-only / 1 chat / 2 control / 3 system-level
- **Hot reload**: Load / unload / reload plugins without restarting
- **Lifecycle hooks**: `beforeConnect` / `afterConnect` / `onLogin` / `onKick` / `onError` / `onDisconnect` / `connectionResult`

### Patch System

- **Hot patches**: `.mcpatch` format, no need to redownload the full package
- **RSA-2048 signature**: Only patches signed with the official private key can be applied
- **Tamper-proof**: manifest SHA256 verification
- **Replay protection**: One-time nonce
- **Official verification**: Patch ID must exist in the official GitHub tag list
- **Auto backup**: Backup before replacement, one-click rollback
- **Path traversal protection**: Patches cannot write outside the program directory
- **Auto rollback on failure**: Automatically restores if replacement fails midway

### Update Check

- Silent check for the latest version on startup
- Manual check via menu: Help → Check for Updates
- Shows release notes, publish time, and download link
- Warns "old patches may become invalid" when applied patches are detected
- 10-minute in-memory cache to avoid hitting the GitHub API rate limit

---

## 🖼 Screenshots

<img width="1440" height="860" alt="5" src="https://github.com/user-attachments/assets/876ba165-a210-4f8f-b243-3cc9405e3d3a" />

<img width="1440" height="860" alt="4" src="https://github.com/user-attachments/assets/c9e647f2-e333-4bac-b4c8-1f5d8f3cd121" />


---

## 🖥 System Requirements

| Item | Requirement |
|---|---|
| OS | Windows 10 / 11 (64-bit) |
| .NET Framework | 4.8 or higher (bundled with Win10 1903+) |
| Disk space | At least 500 MB free |
| Network | `api.github.com` accessible for update checks |

**Node.js is not required** — the backend is packaged into a standalone exe via pkg.

---

## 🚀 Quick Start

### 1. Download

Download the latest `McBotConsole-26H2.zip` from the [Releases](https://github.com/Lx934/McBotConsole/releases) page.

**Verify SHA256** (PowerShell):

```powershell
Get-FileHash .\McBotConsole-26H2.zip -Algorithm SHA256
```

Compare with the hash published on the release page. Do not use if they don't match.

### 2. Extract

Extract to any directory. **Do not use a path containing Chinese characters or spaces.**

### 3. Launch

Double-click `JBSS261A.exe`.

### 4. Configure

Fill in the top bar:
- Server address (IP or domain)
- Port (default 25565)
- Game version (must match the server)
- Attempt count / concurrency

Click **⚙ Settings** to configure connection parameters, proxy, player name mode, etc.

### 5. Run

Click **▶ Start** to begin the task.

---

## 📅 Versioning

The project uses a **half-year version + patch** numbering scheme:

| Type | Format | Example |
|---|---|---|
| Major version | `YYHn` | `26H1` (2026 H1), `26H2` (2026 H2) |
| Patch | `YYHnuN` | `26H1u1`, `26H2u3` |

**Patch compatibility rules**:

- 26H2 can load patches for 26H1 / 26H2 (same H number, backwards compatible)
- Cross-year patches are incompatible (e.g., a 25H2 patch cannot be used on 26H2)
- If a new version modified a file targeted by the patch, the patch will warn "target modified" and require confirmation

---

## 🧩 Patch System

### Applying a Patch

1. Download the `.mcpatch` file from an official source
2. Place it in the `patches/` folder in the program directory
3. Open the program → click **🧩 Patches**
4. Select the patch → click **Apply**
5. The program will restart automatically (when `requiresRestart: true`)

### Rolling Back a Patch

1. Open the program → click **🧩 Patches**
2. Find the applied patch → click **Rollback**

**Note**: Rollback only restores the contents of overwritten files; it does **not** delete files added by the patch.

### Security Mechanisms

- Patches must be signed with the official private key
- Patch ID must exist in the official GitHub tag list
- Patch contents are protected by SHA256
- Each patch can only be applied once (nonce replay protection)

---

## 🔄 Update Check

The program **silently checks** for new versions 3 seconds after startup:

- When a new version is available, `🔔 New version 26H2 available, click to view` appears in the status bar
- Clicking opens a dialog with release notes and a download link

You can also trigger a manual check anytime via **Help → Check for Updates**.

---

## 🔌 Plugin Development

### Node.js Plugins

Directory structure:

```
backend/plugins/<plugin-name>/
├── <plugin-name>.js      # Main file
├── config/
│   └── <plugin-name>.json
└── log/
```

`<plugin-name>.js` example:

```javascript
module.exports = {
    name: 'my-plugin',
    version: '1.0.0',
    author: 'YourName',
    description: 'Plugin description',
    requiredPermission: 0,

    init(config, hooks, logger, api) {
        hooks.register('onLogin', (bot, ctx) => {
            logger.info(`Bot ${ctx.botId} logged in`);
        });
    },

    unload(reason, logger) {
        logger.info(`Plugin unloaded: ${reason}`);
    }
};
```

### Python Plugins

Directory structure:

```
backend/plugins_py/<plugin-name>/
├── plugin.json
├── main.py
└── config.json
```

`plugin.json`:

```json
{
    "name": "my-plugin",
    "version": "1.0.0",
    "author": "YourName",
    "description": "Plugin description",
    "usage": "Usage instructions",
    "main": "main.py"
}
```

For detailed development guide, see [PLUGIN_DEVELOPMENT.md](PLUGIN_DEVELOPMENT.md).

---

## 📁 Directory Structure (Release Package)

```
McBotConsole-26H2/
├── JBSS261A.exe                          ← Main program
├── JBSS261A.exe.config
├── System.Text.Json.dll
├── System.Threading.Tasks.Extensions.dll
├── System.Memory.dll
├── System.Buffers.dll
├── System.Numerics.Vectors.dll
├── System.IO.Pipelines.dll
├── System.Runtime.CompilerServices.Unsafe.dll
├── System.Text.Encodings.Web.dll
├── Microsoft.Bcl.AsyncInterfaces.dll
├── updater.exe                           ← Patch updater
├── README.txt
└── backend/
    ├── mc-bot-backend.exe                ← Node backend (pkg-packaged)
    ├── names.txt                         ← English name dictionary
    └── config.json                       ← Default config
```

Auto-generated at runtime (no need to create manually):

- `backend/player_pool.txt` (created when player pool is first enabled)
- `backend/logs/` (log directory)
- `patches/` (patch directory)
- `patches/applied.jsonl` (patch application record)

---

## 🛠 Building from Source

### Prerequisites

- Visual Studio 2022 (with .NET Framework 4.8 SDK)
- Node.js 18 or 20 (for packaging the backend)
- MinGW-w64 (for compiling the updater)
- pkg or @yao-pkg/pkg (Node packaging tool)

### Building the C# Main Program

```
Open JBSS261A.sln
→ Select Release configuration
→ Build Solution
Output: bin\Release\JBSS261A.exe
```

### Packaging the Node Backend

```cmd
cd backend
npm install
pkg index.js --target node18-win-x64 --output mc-bot-backend.exe
```

### Compiling the Updater

```cmd
cd updater
g++ updater.cpp -o updater.exe -std=c++11 -static -mwindows -lshell32
```

### Assembling the Release Package

Refer to the "Directory Structure" section.

---

## ❓ FAQ

**Q: The log keeps showing "Backend not ready" after startup**

A: Check whether `backend/mc-bot-backend.exe` exists. If missing, the release package is incomplete.

**Q: All logins fail, the log shows `Connection throttled`**

A: The server has connection throttling enabled. Solutions:
1. Increase **Settings → Connection → Connection Interval**
2. Use a proxy pool
3. Check the server's `spigot.yml` `connection-throttle` setting

**Q: Concurrency is set to 6, but actual concurrency is far below 6**

A: Fixed in 26H2. If it still occurs, check whether many retries are occupying concurrency slots.

**Q: The program won't start after applying a patch**

A: Manually restore files from `patches/backup/<patch-id>/`, or use the program's **🧩 Patches → Rollback** feature.

**Q: Update check fails**

A: Possibly hit the GitHub API rate limit (60 requests/hour for unauthenticated users). Retry later, or configure a proxy.

**Q: Can it bypass a server's anti-bot mechanism?**

A: **No, and it shouldn't.** The program only logs throttling information; it does not bypass anything. Adjust your connection pace yourself.

---

## 🙏 Acknowledgments

- [mineflayer](https://github.com/PrismarineJS/mineflayer) — Minecraft protocol implementation
- [PrismarineJS](https://github.com/PrismarineJS) — Related toolchain
- All users who submitted issues and PRs

---

**Project Home**: https://github.com/Lx934/McBotConsole
**Issues**: https://github.com/Lx934/McBotConsole/issues
**Latest Release**: https://github.com/Lx934/McBotConsole/releases

**项目主页**：https://github.com/Lx934/McBotConsole
**问题反馈**：https://github.com/Lx934/McBotConsole/issues
**最新发布**：https://github.com/Lx934/McBotConsole/releases
