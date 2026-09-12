# McBotConsole

Minecraft 批量登录客户端 v7.0.0

## 功能
- C# 图形化界面（WinForms）
- 批量登录假人，模拟多人同时在线
- 代理池轮换 / 自动重连 / 英文名字典
- 反机器人检测提醒
- Node.js + Python 插件系统
- 运行报告导出（CSV / HTML）

## 下载
见 [Releases](../../releases)

## 编译
### 主程序
用 VS 2022 打开 `JBSS261A.sln`，生成 → 重新生成解决方案

### 后端
```bash
cd backend
npm install
node index.js
