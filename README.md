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

> 待补充

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

**项目主页**：https://github.com/Lx934/McBotConsole
**问题反馈**：https://github.com/Lx934/McBotConsole/issues
**最新发布**：https://github.com/Lx934/McBotConsole/releases
