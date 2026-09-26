/**
 * Minecraft 批量登录客户端 - Node 后端 v7.0.0
 * 前端：C# WinForms
 * 通信：stdin/stdout JSON
 * 支持：代理池轮换 / 自动重连 / 多开隔离 / 配置容错 / 插件探测 / 字典模式(txt) / 玩家池 / 配置持久化 / 调试命令行 / 真并发
 *
 * pkg 打包后：运行时文件（config.json / names.txt / plugins / logs / patches）
 *            全部读写 exe 所在目录，不再打进去。
 */

const mineflayer = require('mineflayer');
const fs = require('fs');
const path = require('path');
const zlib = require('zlib');
const archiver = require('archiver');
const readline = require('readline');
const { performance } = require('perf_hooks');

// ==================== 实例 ID ====================
const INSTANCE_ID = process.env.JBSS_INSTANCE_ID || String(process.pid);

// ==================== 运行时目录 ====================
const RUNTIME_DIR = process.pkg
    ? path.dirname(process.execPath)
    : __dirname;

function runtimePath(name) {
    if (!name) return RUNTIME_DIR;
    return path.isAbsolute(name) ? name : path.join(RUNTIME_DIR, name);
}

// ==================== 英文名字典 ====================
const NAMES_TXT_PATH = runtimePath('names.txt');

const FALLBACK_NAMES = [
    'Peter', 'Emma', 'Liam', 'Olivia', 'Noah', 'Ava', 'James', 'Sophia',
    'William', 'Isabella', 'Oliver', 'Mia', 'Benjamin', 'Charlotte', 'Elijah',
    'Amelia', 'Lucas', 'Harper', 'Mason', 'Evelyn', 'Logan', 'Abigail',
    'Alexander', 'Emily', 'Ethan', 'Elizabeth', 'Jacob', 'Mila', 'Michael',
    'Ella', 'Daniel', 'Avery', 'Henry', 'Sofia', 'Jackson', 'Camila',
    'Sebastian', 'Aria', 'Aiden', 'Scarlett', 'Matthew', 'Victoria',
    'Samuel', 'Madison', 'David', 'Luna', 'Joseph', 'Grace', 'Carter',
    'Chloe', 'Owen', 'Penelope', 'Wyatt', 'Layla', 'John', 'Riley',
    'Jack', 'Zoey', 'Luke', 'Nora', 'Jayden', 'Lily', 'Dylan', 'Eleanor',
    'Grayson', 'Hannah', 'Levi', 'Lillian', 'Isaac', 'Addison', 'Gabriel',
    'Aubrey', 'Julian', 'Ellie', 'Mateo', 'Stella', 'Anthony', 'Natalie',
    'Jaxon', 'Zoe', 'Lincoln', 'Leah', 'Joshua', 'Hazel', 'Christopher',
    'Violet', 'Andrew', 'Aurora', 'Theodore', 'Savannah', 'Caleb', 'Audrey',
    'Ryan', 'Brooklyn', 'Asher', 'Bella', 'Nathan', 'Claire', 'Thomas',
    'Skylar', 'Leo', 'Lucy', 'Isaiah', 'Paisley', 'Charles', 'Everly',
    'Josiah', 'Anna', 'Hudson', 'Caroline', 'Christian', 'Nova', 'Hunter',
    'Genesis', 'Connor', 'Emilia', 'Eli', 'Kennedy', 'Ezra', 'Samantha',
    'Aaron', 'Maya', 'Landon', 'Willow', 'Adrian', 'Kinsley', 'Jonathan',
    'Naomi', 'Nolan', 'Elena', 'Jeremiah', 'Sarah', 'Easton', 'Ariana',
    'Elias', 'Allison', 'Colton', 'Gabriella', 'Cameron', 'Alice', 'Carson',
    'Madelyn', 'Robert', 'Cora', 'Angel', 'Ruby', 'Maverick', 'Eva',
    'Nicholas', 'Serenity', 'Dominic', 'Autumn', 'Jaxson', 'Adeline',
    'Greyson', 'Hailey', 'Adam', 'Gianna', 'Ian', 'Valentina', 'Austin',
    'Isla', 'Santiago', 'Eliana', 'Jordan', 'Quinn', 'Cooper', 'Nevaeh',
    'Brayden', 'Ivy', 'Roman', 'Sadie', 'Evan', 'Piper', 'Ezekiel', 'Lydia',
    'Steven', 'Alexa', 'Kenneth', 'Josephine', 'Brian', 'Emery', 'Kevin',
    'Julia', 'Jason', 'Delilah', 'Eric', 'Arianna', 'Frank', 'Vivian',
    'Raymond', 'Kaylee', 'Patrick', 'Sophie', 'Dennis', 'Brielle', 'Jerry',
    'Madeline', 'Walter', 'Harold', 'Douglas', 'Bruce', 'Roger', 'Keith',
    'Terry', 'Lawrence', 'Sean', 'Carl', 'Arthur', 'Albert', 'Eugene',
    'Louis', 'Ralph', 'Roy', 'Bobby', 'Johnny', 'Billy'
];

let _nameDict = null;

function loadNameDict(forceReload = false) {
    if (_nameDict && !forceReload) return _nameDict;

    try {
        if (fs.existsSync(NAMES_TXT_PATH)) {
            const content = fs.readFileSync(NAMES_TXT_PATH, 'utf-8');
            const names = content
                .split(/\r?\n/)
                .map(l => l.trim())
                .filter(l => l && !l.startsWith('#'))
                .map(l => l.replace(/[^a-zA-Z0-9_]/g, ''))
                .filter(l => l.length >= 2 && l.length <= 14);

            const uniq = Array.from(new Set(names));
            if (uniq.length > 0) {
                _nameDict = uniq;
                return _nameDict;
            }
            cliWrite(`names.txt 为空或格式不正确，已恢复为默认字典`, 'yellow');
        }
    } catch (e) {
        cliWrite(`读取 names.txt 失败: ${e.message}，使用内置字典`, 'yellow');
    }

    _nameDict = FALLBACK_NAMES.slice();
    try {
        if (!fs.existsSync(NAMES_TXT_PATH)) {
            fs.writeFileSync(NAMES_TXT_PATH, FALLBACK_NAMES.join('\n'), 'utf-8');
            cliWrite(`已生成默认名字字典: ${NAMES_TXT_PATH}`, 'yellow');
        }
    } catch (e) { }

    return _nameDict;
}

// ==================== 代理库 ====================
let SocksProxyAgent = null;
let HttpsProxyAgent = null;
try { SocksProxyAgent = require('socks-proxy-agent').SocksProxyAgent; } catch (e) { }
try { HttpsProxyAgent = require('https-proxy-agent').HttpsProxyAgent; } catch (e) { }

function normalizeProxyUrl(str) {
    let s = String(str || '').trim();
    if (!s) return null;
    if (!/^[a-z0-9]+:\/\//i.test(s)) s = 'socks5://' + s;
    return s;
}

function createProxyAgent(proxyUrl) {
    if (!proxyUrl) return null;
    try {
        const lower = proxyUrl.toLowerCase();
        if (lower.startsWith('socks://') || lower.startsWith('socks4://') || lower.startsWith('socks5://')) {
            if (!SocksProxyAgent) return null;
            return new SocksProxyAgent(proxyUrl);
        }
        if (lower.startsWith('http://') || lower.startsWith('https://')) {
            if (!HttpsProxyAgent) return null;
            return new HttpsProxyAgent(proxyUrl);
        }
    } catch (e) { }
    return null;
}

// ==================== 代理池 ====================
class ProxyPool {
    constructor(list, mode) {
        this.mode = mode || 'none';
        this.entries = [];
        if (Array.isArray(list)) {
            for (const raw of list) {
                const url = normalizeProxyUrl(raw);
                if (url) this.entries.push(url);
            }
        }
        this.health = new Map();
        this.cursor = 0;
    }

    isEnabled() {
        return this.mode !== 'none' && this.entries.length > 0;
    }

    next() {
        if (!this.isEnabled()) return null;
        if (this.mode === 'single') return this.entries[0];
        const total = this.entries.length * 5;
        for (let i = 0; i < total; i++) {
            const url = this.entries[this.cursor % this.entries.length];
            this.cursor++;
            if (!this._isBad(url)) return url;
        }
        this.health.clear();
        return this.entries[0];
    }

    markGood(url) {
        if (!url) return;
        const h = this.health.get(url) || { success: 0, fail: 0, bannedUntil: 0 };
        h.success++;
        h.fail = 0;
        h.bannedUntil = 0;
        this.health.set(url, h);
    }

    markBad(url) {
        if (!url) return;
        const h = this.health.get(url) || { success: 0, fail: 0, bannedUntil: 0 };
        h.fail++;
        if (h.fail >= 3) h.bannedUntil = Date.now() + 60 * 1000;
        this.health.set(url, h);
    }

    _isBad(url) {
        const h = this.health.get(url);
        if (!h) return false;
        return h.bannedUntil > Date.now();
    }

    get size() { return this.entries.length; }
}

// ==================== 插件探测（登录 + Tab 补全）====================
async function probePluginsViaTabComplete(host, port, version, timeoutMs = 90000) {
    return new Promise((resolve) => {
        const plugins = new Set();
        let totalCmds = 0;
        let resolved = false;
        let bot = null;

        const finish = (result) => {
            if (resolved) return;
            resolved = true;
            clearTimeout(timeoutHandle);
            try { if (bot) bot.end(); } catch (e) { }
            resolve(result);
        };

        const timeoutHandle = setTimeout(() => {
            finish({
                success: plugins.size > 0,
                plugins: Array.from(plugins).sort(),
                cmdCount: totalCmds,
                error: plugins.size > 0 ? '' : '超时（服务器未响应或屏蔽了补全）'
            });
        }, timeoutMs);

        try {
            bot = mineflayer.createBot({
                host, port,
                username: 'Probe_' + Math.random().toString(36).slice(2, 8),
                version: version || false,
                auth: 'offline',
                connectTimeout: 8000
            });
        } catch (e) {
            finish({ success: false, plugins: [], error: e.message });
            return;
        }

        bot.once('error', (err) => {
            finish({ success: false, plugins: [], error: err.message });
        });

        bot.once('end', (reason) => {
            if (!resolved) {
                finish({
                    success: plugins.size > 0,
                    plugins: Array.from(plugins).sort(),
                    cmdCount: totalCmds,
                    error: plugins.size > 0 ? '' : '断开连接: ' + reason
                });
            }
        });

        bot.once('login', async () => {
            try {
                await new Promise(r => setTimeout(r, 1500));

                const prefixes = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
                    'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
                    'u', 'v', 'w', 'x', 'y', 'z',
                    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];

                for (const prefix of prefixes) {
                    if (resolved) break;

                    try {
                        const results = await Promise.race([
                            bot.tabComplete('/' + prefix, true, false),
                            new Promise((_, rej) => setTimeout(() => rej(new Error('单步超时')), 3000))
                        ]);

                        if (!Array.isArray(results)) continue;

                        for (const item of results) {
                            const text = (typeof item === 'string') ? item : (item && item.match);
                            if (!text) continue;
                            totalCmds++;
                            const cleaned = text.startsWith('/') ? text.slice(1) : text;
                            const colon = cleaned.indexOf(':');
                            if (colon > 0) {
                                const ns = cleaned.substring(0, colon).toLowerCase();
                                if (ns && ns !== 'minecraft' && ns !== 'brigadier' &&
                                    ns !== 'bukkit' && ns !== 'spigot' && ns !== 'paper') {
                                    plugins.add(ns);
                                }
                            }
                        }
                    } catch (e) {
                        // 单步失败继续
                    }
                }

                finish({
                    success: true,
                    plugins: Array.from(plugins).sort(),
                    cmdCount: totalCmds
                });
            } catch (e) {
                finish({ success: false, plugins: [], error: e.message });
            }
        });
    });
}

// ==================== 与 C# 通信 ====================
function nowStr() {
    return new Date().toISOString().replace('T', ' ').slice(0, -1);
}

function safeStringify(v) {
    if (typeof v === 'string') return v;
    if (v === null || v === undefined) return String(v);
    try { return JSON.stringify(v); }
    catch (e) { return String(v); }
}

function send(obj) {
    try { process.stdout.write(JSON.stringify(obj) + '\n'); } catch (e) { }
}

function emitLog(level, message) {
    send({
        type: 'log',
        level: level || 'INFO',
        time: nowStr(),
        message: String(message)
    });
}

function cliWrite(msg, color = 'white') {
    let level = 'INFO';
    if (color === 'red') level = 'ERROR';
    else if (color === 'yellow') level = 'WARN';
    else if (color === 'gray') level = 'DEBUG';
    else if (color === 'green') level = 'INFO';
    emitLog(level, msg);
}

console.log = (...args) => {
    emitLog('INFO', args.map(safeStringify).join(' '));
};
console.warn = (...args) => {
    emitLog('WARN', args.map(safeStringify).join(' '));
};
console.error = (...args) => {
    emitLog('ERROR', args.map(safeStringify).join(' '));
};

// ==================== 默认配置 ====================
const DEFAULT_CONFIG = {
    server: { ip: '127.0.0.1', port: 25565 },
    infinite: false,
    count: 100,
    concurrency: 1,
    versions: ['1.19.2'],
    fixed_version: "",
    player_name_prefix: "",
    player_name_min_length: 3,
    player_name_max_length: 16,
    use_name_dict: false,
    use_player_pool: false,
    player_pool_path: 'player_pool.txt',
    player_pool_exhausted: 'stop',
    send_brand: true,
    send_hello: false,
    stay_connected: false,
    auto_disconnect_after: 10,
    connection_timeout: 5,
    max_timeouts: 20,
    retry_delay: 5000,
    max_retries: 2,
    interval_between_connections: 3000,
    auto_reconnect: false,
    max_reconnects: 3,
    reconnect_delay: 5000,
    proxy_mode: 'none',
    proxy_list: [],
    debug: false,
    log_file: 'logs/bot.log',
    json_log: 'success_log.json',
    stats_file: 'throttle_stats.json',
    log_max_size_mb: 1,
    log_archive_dir: 'logs',
    log_archive_max_age_days: 7,
    plugin_dir: 'plugins',
    plugin_dir_py: 'plugins_py',
    plugins: {},
    ui: { fg: 'white', bg: 'black', border: 'cyan', fontsize: 13 }
};

const CONFIG_PATH = runtimePath('config.json');

// ==================== 配置加载 ====================
function loadConfig() {
    if (!fs.existsSync(CONFIG_PATH)) {
        try {
            fs.writeFileSync(CONFIG_PATH, JSON.stringify(DEFAULT_CONFIG, null, 2));
            cliWrite(`已生成默认配置文件: ${CONFIG_PATH}`, 'yellow');
        } catch (e) {
            cliWrite(`生成配置文件失败: ${e.message}`, 'red');
        }
        return JSON.parse(JSON.stringify(DEFAULT_CONFIG));
    }
    try {
        const raw = JSON.parse(fs.readFileSync(CONFIG_PATH, 'utf-8'));
        const cfg = Object.assign({}, DEFAULT_CONFIG, raw);
        cfg.server = Object.assign({}, DEFAULT_CONFIG.server, raw.server || {});
        cfg.ui = Object.assign({}, DEFAULT_CONFIG.ui, raw.ui || {});

        try {
            const baseLog = cfg.log_file || 'logs/bot.log';
            if (!baseLog.includes(`_${INSTANCE_ID}`)) {
                const dir = path.dirname(baseLog);
                const ext = path.extname(baseLog);
                const name = path.basename(baseLog, ext);
                cfg.log_file = path.join(dir, `${name}_${INSTANCE_ID}${ext}`);
            }
            const baseJson = cfg.json_log || 'success_log.json';
            if (!baseJson.includes(`_${INSTANCE_ID}`)) {
                cfg.json_log = baseJson.replace(/\.json$/, `_${INSTANCE_ID}.json`);
            }
        } catch (e) { }

        return validateConfig(cfg);
    } catch (e) {
        cliWrite(`配置文件损坏，使用默认: ${e.message}`, 'red');
        const fallback = JSON.parse(JSON.stringify(DEFAULT_CONFIG));
        try { fs.writeFileSync(CONFIG_PATH, JSON.stringify(fallback, null, 2)); } catch (_) { }
        return fallback;
    }
}

function saveConfig(cfg) {
    try {
        fs.writeFileSync(CONFIG_PATH, JSON.stringify(cfg, null, 2));
        return true;
    } catch (e) {
        cliWrite(`保存配置失败: ${e.message}`, 'red');
        return false;
    }
}

function validateConfig(cfg) {
    if (!cfg.server || typeof cfg.server.ip !== 'string') {
        cliWrite('server.ip 无效，已重置为 127.0.0.1', 'yellow');
        cfg.server = cfg.server || {};
        cfg.server.ip = '127.0.0.1';
    }

    let port = cfg.server.port;
    if (typeof port === 'string') port = parseInt(port, 10);
    if (typeof port !== 'number' || isNaN(port) || port < 1 || port > 65535) {
        cliWrite(`端口无效 (${cfg.server.port})，已重置为 25565`, 'yellow');
        port = 25565;
    }
    cfg.server.port = port;

    if (!Array.isArray(cfg.versions) || cfg.versions.length === 0) {
        cfg.versions = ['1.19.2'];
    }

    if (!Array.isArray(cfg.proxy_list)) cfg.proxy_list = [];
    if (typeof cfg.proxy_mode !== 'string') cfg.proxy_mode = 'none';
    if (typeof cfg.use_name_dict !== 'boolean') cfg.use_name_dict = false;
    if (typeof cfg.use_player_pool !== 'boolean') cfg.use_player_pool = false;
    if (cfg.player_pool_exhausted !== 'stop' && cfg.player_pool_exhausted !== 'loop') {
        cfg.player_pool_exhausted = 'stop';
    }

    return cfg;
}

// ==================== 工具函数 ====================
function ensureDir(dir) {
    if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
}

function cleanupOldArchives(archiveDir, maxAgeDays = 7) {
    try {
        if (!fs.existsSync(archiveDir)) return;
        const now = Date.now();
        const maxAgeMs = maxAgeDays * 24 * 3600 * 1000;
        for (const file of fs.readdirSync(archiveDir)) {
            if (!file.endsWith('.gz') && !file.endsWith('.zip')) continue;
            const fp = path.join(archiveDir, file);
            try {
                if (now - fs.statSync(fp).mtimeMs > maxAgeMs) {
                    fs.unlinkSync(fp);
                    cliWrite(`清理过期归档: ${file}`, 'gray');
                }
            } catch (e) { }
        }
    } catch (e) { }
}

function rotateAndArchive(filePath, maxSizeBytes, archiveDir) {
    try {
        if (!fs.existsSync(filePath)) return;
        const stats = fs.statSync(filePath);
        if (stats.size < maxSizeBytes) return;
        ensureDir(archiveDir);
        const ts = new Date().toISOString().replace(/[:\-T]/g, '').slice(0, 15);
        const archivedPath = path.join(archiveDir, `${path.basename(filePath)}.${ts}.gz`);
        fs.writeFileSync(archivedPath, zlib.gzipSync(fs.readFileSync(filePath)));
        fs.writeFileSync(filePath, '');
        cliWrite(`日志归档: ${archivedPath} (${(stats.size / 1024).toFixed(1)} KB)`, 'gray');
    } catch (e) {
        cliWrite(`归档失败: ${e.message}`, 'red');
    }
}

function createLogger(logFile, maxSizeMB, archiveDir, allowArchive = true) {
    const maxSizeBytes = maxSizeMB * 1024 * 1024;
    ensureDir(path.dirname(logFile));
    if (archiveDir) ensureDir(archiveDir);

    let stream = fs.createWriteStream(logFile, { flags: 'a' });
    const writeQueue = [];
    let isProcessing = false;
    let lastRotateCheck = 0;

    const processQueue = () => {
        if (isProcessing || writeQueue.length === 0) return;
        isProcessing = true;
        try {
            while (writeQueue.length > 0) {
                const line = writeQueue.shift();
                try {
                    if (!stream || !stream.writable) {
                        stream = fs.createWriteStream(logFile, { flags: 'a' });
                    }
                    stream.write(line);
                } catch (e) {
                    emitLog('ERROR', `[日志写入失败] ${e.message}`);
                }
            }
        } finally {
            isProcessing = false;
        }
    };

    const checkAndRotate = () => {
        if (!allowArchive) return;
        const now = Date.now();
        if (now - lastRotateCheck < 1000) return;
        lastRotateCheck = now;
        try {
            if (!fs.existsSync(logFile)) return;
            if (fs.statSync(logFile).size >= maxSizeBytes) {
                if (stream) { try { stream.end(); } catch (e) { } stream = null; }
                rotateAndArchive(logFile, maxSizeBytes, archiveDir);
                stream = fs.createWriteStream(logFile, { flags: 'a' });
            }
        } catch (e) { }
    };

    const writeLine = (level, args) => {
        const text = args.map(safeStringify).join(' ');
        const line = `[${nowStr()}] [${level}] ${text}\n`;
        checkAndRotate();
        writeQueue.push(line);
        processQueue();
        emitLog(level, text);
    };

    const close = () => {
        const waitDrain = () => {
            if (writeQueue.length > 0) {
                setTimeout(waitDrain, 20);
            } else if (stream) {
                const s = stream;
                stream = null;
                try { s.end(); } catch (e) { }
            }
        };
        waitDrain();
    };

    return {
        info: (...args) => writeLine('INFO', args),
        warn: (...args) => writeLine('WARN', args),
        error: (...args) => writeLine('ERROR', args),
        debug: (...args) => writeLine('DEBUG', args),
        close
    };
}

// ==================== 玩家名生成 ====================
function randomNameFromDict(prefix = '', minLen = 3, maxLen = 16) {
    const dict = loadNameDict();
    const name = dict[Math.floor(Math.random() * dict.length)];
    const num = Math.floor(Math.random() * 90) + 10;
    const MAX = Math.min(maxLen, 16);

    let result;
    if (prefix) {
        let pfx = prefix;
        if (pfx.length > MAX - 4) pfx = pfx.slice(0, MAX - 4);

        const base = `${pfx}_${name}`;
        if (base.length + 2 <= MAX) {
            result = `${base}${num}`;
        } else if (base.length <= MAX) {
            result = base;
        } else {
            result = name.length + 2 <= MAX ? `${name}${num}` : name;
        }
    } else {
        result = name.length + 2 <= MAX ? `${name}${num}` : name;
    }

    if (result.length < minLen) {
        result = result + Math.floor(Math.random() * 90 + 10);
    }

    if (/^[0-9]/.test(result)) result = 'A' + result.slice(1);
    return result.slice(0, 16);
}

function randomNameRandom(prefix = '', minLen = 3, maxLen = 16) {
    const chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_';
    if (prefix) {
        const minTotal = Math.max(minLen, prefix.length + 1);
        const maxTotal = Math.max(maxLen, minTotal);
        let totalLen = Math.floor(Math.random() * (maxTotal - minTotal + 1)) + minTotal;
        totalLen = Math.min(totalLen, 16);
        if (prefix.length > maxLen) prefix = prefix.slice(0, maxLen - 1);
        const suffixLen = Math.max(0, totalLen - prefix.length);
        let suffix = '';
        for (let i = 0; i < suffixLen; i++) suffix += chars[Math.floor(Math.random() * chars.length)];
        let name = prefix + suffix;
        if (name.length > 0 && /^[0-9]/.test(name[0])) name = 'A' + name.slice(1);
        return name.slice(0, 16);
    } else {
        const length = Math.floor(Math.random() * (maxLen - minLen + 1)) + minLen;
        const firstChars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ';
        let name = firstChars[Math.floor(Math.random() * firstChars.length)];
        for (let i = 1; i < length; i++) name += chars[Math.floor(Math.random() * chars.length)];
        return name;
    }
}

// ==================== 玩家池 ====================
let _playerPool = null;
const _playerPoolUsed = new Set();

function getPlayerPoolPath() {
    const p = config && config.player_pool_path ? config.player_pool_path : 'player_pool.txt';
    return path.isAbsolute(p) ? p : path.join(RUNTIME_DIR, p);
}

function loadPlayerPool(forceReload = false) {
    if (_playerPool && !forceReload) return _playerPool;

    try {
        const poolPath = getPlayerPoolPath();

        // 文件已存在 → 读取
        if (fs.existsSync(poolPath)) {
            const content = fs.readFileSync(poolPath, 'utf-8');
            const names = content
                .split(/\r?\n/)
                .map(l => l.trim())
                .filter(l => l && !l.startsWith('#'))
                .map(l => l.replace(/[^a-zA-Z0-9_]/g, ''))
                .filter(l => l.length >= 2 && l.length <= 16);

            const uniq = Array.from(new Set(names));
            _playerPool = uniq;

            if (uniq.length === 0) {
                cliWrite(`[玩家池] 文件为空，请编辑后输入 reload-pool 重载`, 'yellow');
            } else {
                cliWrite(`[玩家池] 已加载 ${uniq.length} 个名字（${poolPath}）`, 'green');
            }
            return _playerPool;
        }

        // 文件不存在 → 生成空白模板
        const defaultContent = [
            '# 玩家名池',
            '# 每行一个名字，井号开头的行是注释',
            '# 名字规则：2-16 位，只允许字母、数字、下划线',
            '# 使用规则：每个名字用一次，用完根据设置停止或循环',
            '#',
            '# 请在下面填入你的玩家名（每行一个）：',
            '#'
        ].join('\n') + '\n';

        ensureDir(path.dirname(poolPath));
        fs.writeFileSync(poolPath, defaultContent, 'utf-8');

        cliWrite(`[玩家池] 已生成空白模板: ${poolPath}`, 'green');
        cliWrite(`[玩家池] 请编辑该文件填入玩家名，然后输入 reload-pool 重载`, 'yellow');

        _playerPool = [];
        return _playerPool;
    } catch (e) {
        cliWrite(`[玩家池] 读取失败: ${e.message}`, 'red');
    }

    _playerPool = [];
    return _playerPool;
}

function pickNameFromPool() {
    const pool = loadPlayerPool();
    if (!pool || pool.length === 0) return null;

    for (let i = 0; i < pool.length; i++) {
        if (!_playerPoolUsed.has(pool[i])) {
            _playerPoolUsed.add(pool[i]);
            return pool[i];
        }
    }

    const action = (config && config.player_pool_exhausted) || 'stop';
    if (action === 'loop') {
        cliWrite(`[玩家池] 池已耗尽（${pool.length} 个），循环重头开始`, 'yellow');
        resetPlayerPool();
        _playerPoolUsed.add(pool[0]);
        return pool[0];
    }

    return null;
}

function resetPlayerPool() {
    _playerPoolUsed.clear();
}

function playerPoolRemaining() {
    const pool = _playerPool || [];
    return pool.length - _playerPoolUsed.size;
}

function randomName(prefix = '', minLen = 3, maxLen = 16, useDict = false) {
    if (config && config.use_player_pool) {
        return pickNameFromPool();
    }
    if (useDict) return randomNameFromDict(prefix, minLen, maxLen);
    return randomNameRandom(prefix, minLen, maxLen);
}

// ==================== 插件 GUI（占位）====================
class GuiConsoleManager {
    constructor(logger) { this.logger = logger; }
    async create(pluginName) {
        this.logger.error(`[GUI ${pluginName}] 原生子窗口暂未实现`);
        return { success: false, error: 'NotImplemented' };
    }
    write(pluginName) { return false; }
    close(pluginName) { }
    closeAll() { }
}

// ==================== Node.js 插件管理器 ====================
class PluginManager {
    constructor(config, logger) {
        this.pluginsDir = runtimePath(config.plugin_dir || 'plugins');
        this.globalPluginSettings = config.plugins || {};
        this.loadedPlugins = new Map();
        this.hookHandlers = {};
        this.logger = logger;
        this.debug = config.debug || false;
        this.guiManager = new GuiConsoleManager(logger);
        ensureDir(this.pluginsDir);
        this.centralConfig = this.loadOrCreateCentralConfig();
    }

    loadOrCreateCentralConfig() {
        const centralPath = runtimePath('plugins.json');
        if (fs.existsSync(centralPath)) {
            try { return JSON.parse(fs.readFileSync(centralPath, 'utf-8')); }
            catch (e) { this.logger.error('[插件] plugins.json 格式错误，重新生成'); }
        }
        const defaultCfg = {};
        if (fs.existsSync(this.pluginsDir)) {
            for (const item of fs.readdirSync(this.pluginsDir, { withFileTypes: true })) {
                if (item.isDirectory()) {
                    const jsPath = path.join(this.pluginsDir, item.name, `${item.name}.js`);
                    if (fs.existsSync(jsPath)) defaultCfg[item.name] = { enabled: true, permission: 0 };
                }
            }
        }
        try { fs.writeFileSync(centralPath, JSON.stringify(defaultCfg, null, 2)); } catch (e) { }
        return defaultCfg;
    }

    getPluginSettings(pluginName) {
        const central = this.centralConfig && this.centralConfig[pluginName];
        if (central) return { enabled: central.enabled !== false, permission: central.permission || 0 };
        const global = this.globalPluginSettings && this.globalPluginSettings[pluginName];
        if (global) return { enabled: global.enabled !== false, permission: global.permission || 0 };
        return { enabled: true, permission: 0 };
    }

    loadAll() {
        if (!fs.existsSync(this.pluginsDir)) return;
        for (const item of fs.readdirSync(this.pluginsDir, { withFileTypes: true })) {
            if (item.isDirectory()) {
                const jsPath = path.join(this.pluginsDir, item.name, `${item.name}.js`);
                if (fs.existsSync(jsPath)) this.loadPlugin(jsPath, item.name);
            }
        }
    }

    loadPlugin(jsPath, pluginName) {
        try {
            const absolutePath = path.resolve(jsPath);
            delete require.cache[require.resolve(absolutePath)];
            const module = require(absolutePath);

            const meta = {
                name: module.name || pluginName,
                version: module.version || '0.0.0',
                author: module.author || 'Unknown',
                description: module.description || '',
                usage: module.usage || '',
                requiredPermission: module.requiredPermission || 0,
                defaultConfig: module.defaultConfig || {}
            };

            if (meta.name !== pluginName) {
                this.logger.error(`[插件] 跳过 ${pluginName}：元数据 name 不匹配 (${meta.name})`);
                return false;
            }

            const settings = this.getPluginSettings(meta.name);
            if (!settings.enabled) { this.logger.info(`[插件] 跳过 ${meta.name}（已禁用）`); return false; }
            if (settings.permission < meta.requiredPermission) {
                this.logger.error(`[插件] 跳过 ${meta.name}：需要权限 ${meta.requiredPermission}，当前 ${settings.permission}`);
                return false;
            }

            const pluginDir = path.dirname(jsPath);
            const configDir = path.join(pluginDir, 'config');
            const logDir = path.join(pluginDir, 'log');
            ensureDir(configDir);
            ensureDir(logDir);

            const configPath = path.join(configDir, `${meta.name}.json`);
            let pluginConfig = {};
            let compressLog = true;

            if (fs.existsSync(configPath)) {
                try {
                    const raw = JSON.parse(fs.readFileSync(configPath, 'utf-8'));
                    pluginConfig = raw.config || {};
                    compressLog = raw.compress_log !== undefined ? raw.compress_log : true;
                } catch (e) { }
            } else {
                const defaultContent = { enabled: true, permission: settings.permission, compress_log: true, config: meta.defaultConfig };
                try { fs.writeFileSync(configPath, JSON.stringify(defaultContent, null, 2)); } catch (e) { }
                pluginConfig = meta.defaultConfig || {};
            }

            const logFile = path.join(logDir, `${meta.name}.log`);
            const pluginLogger = createLogger(logFile, 1, path.join(logDir, 'archive'), compressLog);

            if (typeof module.init !== 'function') {
                this.logger.error(`[插件] 跳过 ${meta.name}：未导出 init`);
                pluginLogger.close();
                return false;
            }

            const hooks = this.createHookRegistrar(meta.name);
            const logger = {
                info: (...args) => pluginLogger.info(`[${meta.name}]`, ...args),
                warn: (...args) => pluginLogger.warn(`[${meta.name}]`, ...args),
                error: (...args) => pluginLogger.error(`[${meta.name}]`, ...args),
                debug: (...args) => {
                    if (this.debug) pluginLogger.debug(`[${meta.name}] DEBUG:`, ...args);
                },
                close: () => pluginLogger.close()
            };

            const pluginRef = { permission: settings.permission };
            const api = {
                createConsole: () => ({ success: false, error: 'NotImplemented' }),
                writeToConsole: () => false,
                closeConsole: () => { }
            };

            module.init(pluginConfig, hooks, logger, api);

            this.loadedPlugins.set(meta.name, {
                meta,
                instance: module,
                config: pluginConfig,
                permission: settings.permission,
                permissionRef: pluginRef,
                logger: pluginLogger,
                logFile: logFile,
                jsPath: absolutePath
            });
            this.logger.info(`[插件] ✅ 加载成功: ${meta.name} v${meta.version} (权限 ${settings.permission})`);
            return true;

        } catch (err) {
            this.logger.error(`[插件] ❌ 加载 ${pluginName} 失败: ${err.message}`);
            return false;
        }
    }

    unloadPlugin(pluginName, reason = 'user_unload') {
        const data = this.loadedPlugins.get(pluginName);
        if (!data) return { success: false, error: '插件未加载' };
        try {
            this.guiManager.close(pluginName);

            if (typeof data.instance.unload === 'function') {
                try { data.instance.unload(reason, data.logger); }
                catch (e) { this.logger.error(`[插件] ${pluginName} unload 异常: ${e.message}`); }
            }

            for (const event of Object.keys(this.hookHandlers)) {
                this.hookHandlers[event] = this.hookHandlers[event].filter(h => h.plugin !== pluginName);
            }

            data.logger.info(`[卸载] 插件被卸载，原因: ${reason}`);
            setTimeout(() => { try { data.logger.close(); } catch (e) { } }, 100);
            this.loadedPlugins.delete(pluginName);
            this.logger.info(`[插件] 卸载成功: ${pluginName} (原因: ${reason})`);
            return { success: true };
        } catch (e) {
            this.logger.error(`[插件] 卸载 ${pluginName} 失败: ${e.message}`);
            return { success: false, error: e.message };
        }
    }

    reloadPlugin(pluginName) {
        const data = this.loadedPlugins.get(pluginName);
        if (!data) return { success: false, error: '插件未加载' };

        const jsPath = data.jsPath;

        const result = this.unloadPlugin(pluginName, 'plugin_reload');
        if (!result.success) return result;

        setTimeout(() => {
            if (fs.existsSync(jsPath)) this.loadPlugin(jsPath, pluginName);
            else this.logger.error(`[插件] 重启失败：找不到 ${jsPath}`);
        }, 500);
        return { success: true };
    }

    unloadAll(reason = 'program_close') {
        for (const name of Array.from(this.loadedPlugins.keys())) {
            this.unloadPlugin(name, reason);
        }
    }

    closeAll() { this.guiManager.closeAll(); }

    createHookRegistrar(pluginName) {
        return {
            register: (event, handler) => {
                if (typeof handler !== 'function') return;
                if (!this.hookHandlers[event]) this.hookHandlers[event] = [];
                this.hookHandlers[event].push({ plugin: pluginName, handler });
            }
        };
    }

    async trigger(event, ...args) {
        if (!this.hookHandlers[event]) return;
        for (const { plugin, handler } of this.hookHandlers[event]) {
            try {
                await Promise.race([
                    Promise.resolve(handler(...args)),
                    new Promise((_, reject) => setTimeout(() => reject(new Error('钩子超时')), 5000))
                ]);
            } catch (err) {
                this.logger.error(`[插件] ${plugin} 钩子 ${event} 异常: ${err.message}`);
            }
        }
    }

    hasListener(event) {
        return !!(this.hookHandlers[event] && this.hookHandlers[event].length > 0);
    }

    getPluginInfo() {
        const info = [];
        for (const [name, data] of this.loadedPlugins) {
            info.push({
                name, version: data.meta.version, author: data.meta.author,
                description: data.meta.description, usage: data.meta.usage,
                permission: data.permission
            });
        }
        return info;
    }
}

// ==================== Python 插件管理器 ====================
class PythonPluginManager {
    constructor(config, logger) {
        this.pluginsDir = runtimePath(config.plugin_dir_py || 'plugins_py');
        this.loadedPlugins = new Map();
        this.botContext = {};
        this.logger = logger;
        this.guiManager = new GuiConsoleManager(logger);
        ensureDir(this.pluginsDir);
    }

    loadAll() {
        if (!fs.existsSync(this.pluginsDir)) return;
        for (const item of fs.readdirSync(this.pluginsDir, { withFileTypes: true })) {
            if (item.isDirectory()) this.loadPlugin(path.join(this.pluginsDir, item.name), item.name);
        }
    }

    loadPlugin(pluginPath, name) {
        try {
            const metaPath = path.join(pluginPath, 'plugin.json');
            if (!fs.existsSync(metaPath)) {
                this.logger.error(`[Python插件] 跳过 ${name}：缺少 plugin.json`);
                return false;
            }
            const meta = JSON.parse(fs.readFileSync(metaPath, 'utf-8'));
            for (const field of ['name', 'version', 'author', 'description', 'usage']) {
                if (!meta[field]) { this.logger.error(`[Python插件] 跳过 ${name}：缺少 ${field}`); return false; }
            }
            const entryPath = path.join(pluginPath, meta.main || 'main.py');
            if (!fs.existsSync(entryPath)) {
                this.logger.error(`[Python插件] 跳过 ${name}：找不到 ${entryPath}`);
                return false;
            }

            const configPath = path.join(pluginPath, 'config.json');
            let pluginConfig = {};
            let enabled = true;
            let permission = 0;
            if (fs.existsSync(configPath)) {
                try {
                    const raw = JSON.parse(fs.readFileSync(configPath, 'utf-8'));
                    pluginConfig = raw.config || {};
                    enabled = raw.enabled !== false;
                    permission = raw.permission || 0;
                } catch (e) { }
            } else {
                const defaultContent = { enabled: true, permission: 0, config: meta.defaultConfig || {} };
                try { fs.writeFileSync(configPath, JSON.stringify(defaultContent, null, 2)); } catch (e) { }
                pluginConfig = meta.defaultConfig || {};
            }

            if (!enabled) { this.logger.info(`[Python插件] 跳过 ${name}（已禁用）`); return false; }
            if (permission < (meta.requiredPermission || 0)) {
                this.logger.error(`[Python插件] 跳过 ${name}：需要权限 ${meta.requiredPermission}，当前 ${permission}`);
                return false;
            }

            const logDir = path.join(pluginPath, 'log');
            ensureDir(logDir);
            const logFile = path.join(logDir, `${meta.name}.log`);

            const { spawn } = require('child_process');
            const pythonCmd = process.platform === 'win32' ? 'python' : 'python3';
            const pythonProcess = spawn(pythonCmd, [entryPath], { stdio: ['pipe', 'pipe', 'pipe'] });

            const pluginInfo = {
                meta, config: pluginConfig, process: pythonProcess,
                permission, name: meta.name, logFile, logDir
            };
            this.loadedPlugins.set(meta.name, pluginInfo);

            pythonProcess.stdout.on('data', (data) => {
                for (const line of data.toString().split('\n').filter(l => l.trim())) {
                    try { this.handlePluginMessage(JSON.parse(line), pluginInfo); } catch (e) { }
                }
            });

            pythonProcess.stderr.on('data', (data) => {
                this.logger.error(`[Python插件 ${meta.name}] stderr: ${data.toString().trim()}`);
            });

            pythonProcess.on('exit', (code, signal) => {
                this.logger.info(`[Python插件 ${meta.name}] 进程退出 (code=${code}, signal=${signal})`);
                this.guiManager.close(meta.name);
                this.loadedPlugins.delete(meta.name);
            });

            pythonProcess.on('error', (err) => {
                this.logger.error(`[Python插件 ${meta.name}] 进程错误: ${err.message}`);
                this.loadedPlugins.delete(meta.name);
            });

            this.sendToPlugin(meta.name, { type: 'init', config: pluginConfig, pluginName: meta.name, permission });
            this.logger.info(`[Python插件] ✅ 加载成功: ${meta.name} v${meta.version} (权限 ${permission})`);
            return true;

        } catch (err) {
            this.logger.error(`[Python插件] ❌ 加载 ${name} 失败: ${err.message}`);
            return false;
        }
    }

    async unloadPlugin(pluginName, reason = 'user_unload') {
        const plugin = this.loadedPlugins.get(pluginName);
        if (!plugin) return { success: false, error: '插件未加载' };

        try {
            this.guiManager.close(pluginName);
            this.sendToPlugin(pluginName, { type: 'shutdown', reason });
            await new Promise(resolve => setTimeout(resolve, 2000));

            if (plugin.process && !plugin.process.killed) {
                try { plugin.process.kill('SIGTERM'); } catch (e) { }
                setTimeout(() => {
                    try { plugin.process.kill('SIGKILL'); } catch (e) { }
                }, 1000);
            }

            this.loadedPlugins.delete(pluginName);
            this.logger.info(`[Python插件] 卸载成功: ${pluginName} (原因: ${reason})`);
            return { success: true };
        } catch (e) {
            this.logger.error(`[Python插件] 卸载 ${pluginName} 失败: ${e.message}`);
            return { success: false, error: e.message };
        }
    }

    async reloadPlugin(pluginName) {
        const plugin = this.loadedPlugins.get(pluginName);
        if (!plugin) return { success: false, error: '插件未加载' };
        const pluginPath = path.join(this.pluginsDir, pluginName);
        await this.unloadPlugin(pluginName, 'plugin_reload');
        await new Promise(resolve => setTimeout(resolve, 500));
        if (fs.existsSync(pluginPath)) this.loadPlugin(pluginPath, pluginName);
        return { success: true };
    }

    async unloadAll(reason = 'program_close') {
        for (const name of Array.from(this.loadedPlugins.keys())) {
            await this.unloadPlugin(name, reason);
        }
    }

    closeAll() { this.guiManager.closeAll(); }

    sendToPlugin(pluginName, message) {
        const plugin = this.loadedPlugins.get(pluginName);
        if (!plugin || !plugin.process) return false;
        try {
            plugin.process.stdin.write(JSON.stringify(message) + '\n');
            return true;
        } catch (e) { return false; }
    }

    handlePluginMessage(msg, pluginInfo) {
        if (msg.type === 'action') {
            switch (msg.action) {
                case 'chat': this._actionChat(msg.data, pluginInfo); break;
                case 'setConfig': this._actionSetConfig(msg.data, pluginInfo); break;
                case 'getBots': this._actionGetBots(msg.data, pluginInfo); break;
                case 'setAdaptiveDelay': this._actionSetAdaptiveDelay(msg.data, pluginInfo); break;
                default:
                    this.logger.error(`[Python插件 ${pluginInfo.name}] 未知 action: ${msg.action}`);
            }
        } else if (msg.type === 'log') {
            const level = (msg.level || 'info').toLowerCase();
            const text = `[Python插件 ${pluginInfo.name}] ${msg.message}`;
            if (level === 'error') this.logger.error(text);
            else if (level === 'warn' || level === 'warning') this.logger.warn(text);
            else if (level === 'debug') this.logger.debug(text);
            else this.logger.info(text);
        }
    }

    _actionChat(data, pluginInfo) {
        if (pluginInfo.permission < 1) {
            this.logger.error(`[插件 ${pluginInfo.name}] 权限不足（需要 1）`);
            return;
        }
        const { botId, message } = data;
        if (typeof message !== 'string' || message.length > 256) return;
        const bot = this.botContext.getBotById ? this.botContext.getBotById(botId) : null;
        if (bot && bot._client && bot._client.state === 'play') bot.chat(message);
    }

    _actionSetConfig(data, pluginInfo) {
        if (pluginInfo.permission < 2) {
            this.logger.error(`[插件 ${pluginInfo.name}] 权限不足（需要 2）`);
            return;
        }
        if (!this.botContext.setRuntimeConfig || !data || typeof data.key !== 'string') return;
        this.botContext.setRuntimeConfig(data.key, data.value);
    }

    _actionGetBots(data, pluginInfo) {
        const bots = [];
        if (this.botContext.getBotById && this.botContext.getAllBots) {
            for (const b of this.botContext.getAllBots()) {
                bots.push({ botId: b.id, username: b.username || '', connected: b.connected });
            }
        }
        this.sendToPlugin(pluginInfo.name, {
            type: 'response',
            requestId: data && data.requestId,
            data: bots
        });
    }

    _actionSetAdaptiveDelay(data, pluginInfo) {
        if (pluginInfo.permission < 1) {
            this.logger.warn(`[插件 ${pluginInfo.name}] setAdaptiveDelay 需要权限 1，当前 ${pluginInfo.permission}`);
            return;
        }
        if (!this.botContext.setRuntimeConfig || !data) return;
        const { retryDelay, interval } = data;
        if (typeof retryDelay === 'number') {
            this.botContext.setRuntimeConfig('retryDelay', retryDelay);
        }
        if (typeof interval === 'number') {
            this.botContext.setRuntimeConfig('connectionInterval', interval);
        }
    }

    trigger(event, data) {
        for (const [name] of this.loadedPlugins) {
            this.sendToPlugin(name, { type: 'hook', event, data });
        }
    }

    setBotContext(context) { this.botContext = context; }
    hasListener() { return this.loadedPlugins.size > 0; }
}

// ==================== 主控制器 ====================
class BotController {
    constructor(config) {
        this.config = config;
        this.validateVersionConfig();
        const archiveDir = runtimePath(config.log_archive_dir || 'logs');
        ensureDir(archiveDir);
        const logFile = runtimePath(config.log_file);
        this.logger = createLogger(logFile, config.log_max_size_mb || 1, archiveDir, true);

        this.retryDelay = config.retry_delay || 5000;
        this.connectionInterval = config.interval_between_connections || 3000;

        this.pluginManager = new PluginManager(config, this.logger);
        this.pluginManager.loadAll();

        this.pythonPluginManager = new PythonPluginManager(config, this.logger);
        this.pythonPluginManager.setBotContext({
            getBotById: (id) => this.botMap ? this.botMap.get(id) : null,
            getAllBots: () => {
                const result = [];
                if (this.botMap) {
                    for (const [id, bot] of this.botMap) {
                        result.push({
                            id,
                            username: bot.username || '',
                            connected: bot._client && bot._client.state === 'play'
                        });
                    }
                }
                return result;
            },
            setRuntimeConfig: (key, value) => this.setRuntimeConfig(key, value)
        });
        this.pythonPluginManager.loadAll();

        this.successCount = 0;
        this.failureCount = 0;
        this.totalAttempts = 0;
        this.consecutiveTimeouts = 0;
        this.botIdCounter = 0;
        this.activeConnections = 0;
        this.stopped = false;
        this.paused = false;
        this.started = false;
        this.startTime = performance.now();
        this.running = 0;
        this.botMap = new Map();
        this.concurrentLimit = config.concurrency || 1;
        this.maxTimeouts = config.max_timeouts || 20;
        this.maxRetries = config.max_retries || 2;
        this.retriableErrors = ['ECONNRESET', 'Timeout', 'throttled', 'Throttled', 'Disconnected without login', 'Final timeout'];

        if (config.use_name_dict) {
            const dict = loadNameDict();
            this.logger.info(`[玩家名] 字典模式已启用，共 ${dict.length} 个名字（来自 names.txt）`);
        }

        if (config.use_player_pool) {
            loadPlayerPool(true);
        }

        this.proxyPool = new ProxyPool(config.proxy_list || [], config.proxy_mode || 'none');
        if (this.proxyPool.isEnabled()) {
            this.logger.info(`[代理] 已启用，模式: ${this.proxyPool.mode}，代理数: ${this.proxyPool.size}`);
        }

        this.archiveCleanupTimer = setInterval(() => {
            cleanupOldArchives(archiveDir, config.log_archive_max_age_days || 7);
        }, 24 * 3600 * 1000);
        if (this.archiveCleanupTimer.unref) this.archiveCleanupTimer.unref();

        this.statusTimer = setInterval(() => this.broadcastStatus(), 1000);
        if (this.statusTimer.unref) this.statusTimer.unref();
    }

    broadcastStatus() {
        send({
            type: 'status',
            started: this.started,
            stopped: this.stopped,
            paused: this.paused,
            success: this.successCount,
            failure: this.failureCount,
            attempts: this.totalAttempts,
            active: this.activeConnections,
            retryDelay: this.retryDelay,
            connectionInterval: this.connectionInterval,
            nameDictEnabled: this.config.use_name_dict,
            playerPoolEnabled: this.config.use_player_pool
        });
    }

    setRuntimeConfig(key, value) {
        const num = Number(value);
        if (!Number.isFinite(num) || num < 0) {
            this.logger.error(`[配置] 非法值: ${value}`);
            return;
        }
        switch (key) {
            case 'retryDelay':
                this.retryDelay = Math.max(1000, num);
                this.logger.info(`[配置] 重试延迟 → ${this.retryDelay}ms`);
                break;
            case 'connectionInterval':
                this.connectionInterval = Math.max(0, num);
                this.logger.info(`[配置] 连接间隔 → ${this.connectionInterval}ms`);
                break;
            default: this.logger.error(`[配置] 未知运行时配置项: ${key}`);
        }
    }

    validateVersionConfig() {
        const cfg = this.config;
        if (cfg.fixed_version && cfg.fixed_version.trim() !== '') {
            if (!cfg.versions || cfg.versions.length === 0) this.config.versions = [cfg.fixed_version.trim()];
        } else {
            if (!cfg.versions || cfg.versions.length === 0) this.config.versions = ['1.19.2'];
        }
    }

    getVersionForBot() {
        const cfg = this.config;
        if (cfg.fixed_version && cfg.fixed_version.trim() !== '') return cfg.fixed_version.trim();
        return cfg.versions[Math.floor(Math.random() * cfg.versions.length)];
    }

    isRetriable(errorMsg) {
        return this.retriableErrors.some(type => errorMsg && errorMsg.includes(type));
    }

    async start() {
        if (this.started && !this.stopped) {
            this.logger.error('已经启动过了');
            return;
        }
        this.started = true;
        this.stopped = false;
        this.paused = false;
        this.startTime = performance.now();

        this.logger.info('=== Minecraft 批量登录客户端启动 ===');
        this.logger.info(`目标服务器: ${this.config.server.ip}:${this.config.server.port}`);
        this.logger.info(`并发数: ${this.concurrentLimit}`);
        this.logger.info(`重试延迟: ${this.retryDelay}ms | 连接间隔: ${this.connectionInterval}ms`);

        if (this.config.use_player_pool) {
            const remaining = playerPoolRemaining();
            const ex = this.config.player_pool_exhausted === 'loop' ? '循环' : '停止';
            this.logger.info(`玩家名模式: 玩家池（剩余 ${remaining} 个，耗尽后${ex}）`);
        } else if (this.config.use_name_dict) {
            this.logger.info(`玩家名模式: 英文名字典`);
        } else {
            this.logger.info(`玩家名模式: 随机字符串`);
        }

        const nodePlugins = this.pluginManager.getPluginInfo();
        if (nodePlugins.length) this.logger.info(`已加载 ${nodePlugins.length} 个 Node.js 插件`);
        else this.logger.info('未加载 Node.js 插件');

        const pyPlugins = Array.from(this.pythonPluginManager.loadedPlugins.keys());
        if (pyPlugins.length) this.logger.info(`已加载 ${pyPlugins.length} 个 Python 插件`);
        else this.logger.info('未加载 Python 插件');

        await this.runLoop();
    }

    async runLoop() {
        const maxAttempts = this.config.infinite ? Infinity : this.config.count;

        while (this.totalAttempts < maxAttempts && !this.stopped) {
            while (this.paused && !this.stopped) {
                await new Promise(resolve => setTimeout(resolve, 200));
            }
            if (this.stopped) break;

            if (this.consecutiveTimeouts >= this.maxTimeouts) {
                this.logger.info(`连续 ${this.consecutiveTimeouts} 次失败，自动停止`);
                break;
            }

            while (this.running >= this.concurrentLimit) {
                await new Promise(resolve => setTimeout(resolve, 50));
            }
            if (this.stopped || this.paused) continue;

            this.totalAttempts++;
            this.botIdCounter++;
            const botId = this.botIdCounter;
            const minLen = this.config.player_name_min_length || 3;
            const maxLen = this.config.player_name_max_length || 16;
            const playerName = randomName(
                this.config.player_name_prefix || '',
                minLen, maxLen,
                this.config.use_name_dict === true
            );

            if (this.config.use_player_pool && !playerName) {
                const poolSize = (_playerPool || []).length;
                if (poolSize === 0) {
                    this.logger.error('[玩家池] 池为空，请编辑 player_pool.txt 后输入 reload-pool 重载');
                } else {
                    this.logger.info('[玩家池] 池已耗尽，停止任务');
                }
                this.stopped = true;
                break;
            }

            const version = this.getVersionForBot();

            this.logger.info(`[Bot ${botId}] 开始连接 (尝试 ${this.totalAttempts})... 版本: ${version} 玩家: ${playerName}`);

            this.running++;
            this.activeConnections++;

            this.spawnBotAsync(botId, playerName, version).catch(err => {
                this.logger.error(`[Bot ${botId}] spawnBot 异常: ${err && err.message ? err.message : err}`);
            });

            if (this.totalAttempts < maxAttempts && !this.stopped && !this.paused) {
                await new Promise(resolve => setTimeout(resolve, this.connectionInterval));
            }
        }

        while (this.activeConnections > 0 || this.running > 0) {
            await new Promise(resolve => setTimeout(resolve, 200));
        }

        this.logger.info('=== 任务结束 ===');
        this.stopped = true;

        send({
            type: 'run-finished',
            totalAttempts: this.totalAttempts,
            successCount: this.successCount,
            failureCount: this.failureCount
        });
    }

    async spawnBotAsync(botId, playerName, version) {
        let attemptCount = 0;
        let success = false;
        let lastError = '';
        let lastErrorType = '';
        let lastDuration = 0;
        let lastReconnectCount = 0;
        let slotHeld = true;

        try {
            while (attemptCount <= this.maxRetries && !success && !this.stopped) {
                attemptCount++;
                const result = await this.connectOnce(botId, playerName, version);
                const duration = result.duration || 0;
                const wasSuccess = result.success || false;
                const errorType = result.errorType || 'Unknown';
                lastDuration = duration;
                lastReconnectCount = result.reconnectCount || 0;

                if (this.pythonPluginManager.hasListener('connectionResult') ||
                    this.pluginManager.hasListener('connectionResult')) {
                    const payload = { success: wasSuccess, duration, errorType, botId };
                    this.pluginManager.trigger('connectionResult', payload).catch(() => { });
                    this.pythonPluginManager.trigger('connectionResult', payload);
                }

                if (wasSuccess) {
                    success = true;
                    this.successCount++;
                    this.consecutiveTimeouts = 0;
                } else {
                    lastError = result.error || '';
                    lastErrorType = errorType;
                    const nonRetriable = ['Kicked', 'VersionMismatch', 'Disconnected without login'];
                    if (nonRetriable.includes(errorType)) {
                        this.failureCount++;
                        this.consecutiveTimeouts++;
                        break;
                    }
                    if (attemptCount <= this.maxRetries && this.isRetriable(lastError)) {
                        const delay = this.retryDelay;
                        this.logger.info(`[Bot ${botId}] 重试 ${attemptCount}/${this.maxRetries} (${errorType})，等待 ${delay}ms...`);

                        if (slotHeld) {
                            this.running--;
                            this.activeConnections--;
                            slotHeld = false;
                        }
                        await new Promise(resolve => setTimeout(resolve, delay));

                        while (this.running >= this.concurrentLimit && !this.stopped) {
                            await new Promise(resolve => setTimeout(resolve, 50));
                        }
                        if (this.stopped) break;

                        this.running++;
                        this.activeConnections++;
                        slotHeld = true;
                        continue;
                    }
                    this.failureCount++;
                    this.consecutiveTimeouts++;
                    break;
                }
            }

            if (!success) {
                this.logger.error(`[Bot ${botId}] 最终失败，错误类型: ${lastErrorType || 'Unknown'}`);
            }

            send({
                type: 'bot-result',
                botId: botId,
                username: playerName,
                botSuccess: success,
                botErrorType: lastErrorType || '',
                botError: lastError || '',
                botDurationMs: Math.round(lastDuration || 0),
                reconnectCount: lastReconnectCount || 0
            });
        } finally {
            if (slotHeld) {
                this.running--;
                this.activeConnections--;
            }
        }
    }

    async connectOnce(botId, playerName, version) {
        const config = this.config;
        const maxReconnects = config.auto_reconnect ? (config.max_reconnects || 3) : 0;
        const reconnectDelay = config.reconnect_delay || 5000;

        let reconnectCount = 0;
        const totalStart = performance.now();

        while (true) {
            const result = await this.connectOnceSingle(botId, playerName, version);

            if (result.success && result.disconnected &&
                config.auto_reconnect && config.stay_connected &&
                reconnectCount < maxReconnects) {
                reconnectCount++;
                this.logger.info(`[Bot ${botId}] 掉线，${reconnectDelay}ms 后重连 (第 ${reconnectCount}/${maxReconnects} 次)`);
                await new Promise(r => setTimeout(r, reconnectDelay));
                continue;
            }

            result.duration = performance.now() - totalStart;
            result.reconnectCount = reconnectCount;
            return result;
        }
    }

    async connectOnceSingle(botId, playerName, version) {
        const config = this.config;
        const context = { botId, playerName, version };

        return new Promise((resolve) => {
            const startTime = performance.now();
            let loggedIn = false;
            let userEnded = false;
            let disconnectTimer = null;
            let timeoutHandle = null;
            let finalTimeoutHandle = null;
            let resolved = false;

            const resolveOnce = (result) => {
                if (!resolved) {
                    resolved = true;
                    if (disconnectTimer) clearTimeout(disconnectTimer);
                    if (timeoutHandle) clearTimeout(timeoutHandle);
                    if (finalTimeoutHandle) clearTimeout(finalTimeoutHandle);
                    this.botMap.delete(botId);
                    result.duration = performance.now() - startTime;
                    resolve(result);
                }
            };

            let proxyUrl = null;
            let proxyAgent = null;
            if (this.proxyPool && this.proxyPool.isEnabled()) {
                proxyUrl = this.proxyPool.next();
                proxyAgent = createProxyAgent(proxyUrl);
                if (proxyUrl && proxyAgent) {
                    this.logger.info(`[Bot ${botId}] 走代理: ${proxyUrl}`);
                } else {
                    this.logger.error(`[Bot ${botId}] 代理不可用，直连`);
                    proxyUrl = null;
                }
            }

            let botConfig = {
                host: config.server.ip, port: config.server.port,
                username: playerName, version: version,
                connectTimeout: config.connection_timeout * 1000,
                brand: config.send_brand ? 'vanilla' : undefined
            };
            if (proxyAgent) botConfig.agent = proxyAgent;

            Promise.all([
                this.pluginManager.trigger('beforeConnect', botConfig, context).catch(() => { }),
                Promise.resolve(this.pythonPluginManager.trigger('beforeConnect', context))
            ]).then(() => {
                let bot;
                try { bot = mineflayer.createBot(botConfig); }
                catch (e) {
                    this.logger.error(`[Bot ${botId}] 创建失败: ${e.message}`);
                    if (proxyUrl && this.proxyPool) this.proxyPool.markBad(proxyUrl);
                    resolveOnce({ success: false, error: 'CreateBotError', errorType: 'CreateBotError' });
                    return;
                }
                this.botMap.set(botId, bot);

                bot.once('connect', () => {
                    this.pluginManager.trigger('afterConnect', bot, context).catch(() => { });
                    try { this.pythonPluginManager.trigger('afterConnect', context); } catch (e) { }
                });

                bot.once('login', () => {
                    loggedIn = true;
                    if (timeoutHandle) { clearTimeout(timeoutHandle); timeoutHandle = null; }
                    if (finalTimeoutHandle) { clearTimeout(finalTimeoutHandle); finalTimeoutHandle = null; }

                    if (proxyUrl && this.proxyPool) this.proxyPool.markGood(proxyUrl);

                    const elapsed = (performance.now() - startTime).toFixed(0);
                    this.logger.info(`[Bot ${botId}] ★ 登录成功! 玩家: ${playerName} 耗时 ${elapsed}ms`);
                    fs.appendFile(runtimePath(config.json_log),
                        JSON.stringify({ timestamp: Date.now() / 1000, player: playerName, version, server: `${config.server.ip}:${config.server.port}` }) + '\n',
                        () => { });
                    this.pluginManager.trigger('onLogin', bot, context).catch(() => { });
                    try { this.pythonPluginManager.trigger('onLogin', context); } catch (e) { }

                    if (config.stay_connected) {
                        const raw = config.auto_disconnect_after;
                        const adSec = (typeof raw === 'number' && raw > 0) ? raw : 0;

                        if (adSec > 0) {
                            disconnectTimer = setTimeout(() => {
                                if (bot && bot._client && bot._client.state !== 'closed') {
                                    userEnded = true;
                                    bot.end();
                                    this.logger.info(`[Bot ${botId}] 自动断开 (${adSec}s)`);
                                }
                            }, adSec * 1000);
                        } else {
                            this.logger.info(`[Bot ${botId}] 保持登录（不自动断开）`);
                        }
                    } else {
                        if (bot && bot._client && bot._client.state !== 'closed') {
                            userEnded = true;
                            bot.end();
                        }
                    }
                });

                bot.once('kicked', (reason) => {
                    const reasonStr = typeof reason === 'string' ? reason : JSON.stringify(reason);
                    const isThrottled = /throttle/i.test(reasonStr);
                    const isVersionMismatch = /version|outdated/i.test(reasonStr);

                    let errorType = isThrottled ? 'Throttled'
                        : isVersionMismatch ? 'VersionMismatch'
                            : 'Kicked';

                    this.logger.error(`[Bot ${botId}] [${errorType}] 被踢出: ${reasonStr}`);
                    if (proxyUrl && this.proxyPool) this.proxyPool.markBad(proxyUrl);

                    this.pluginManager.trigger('onKick', reason, context).catch(() => { });
                    try { this.pythonPluginManager.trigger('onKick', { reason, ...context }); } catch (e) { }
                    resolveOnce({ success: false, error: `${errorType}: ${reasonStr}`, errorType });
                });

                bot.once('error', (err) => {
                    if (!loggedIn) {
                        if (proxyUrl && this.proxyPool) this.proxyPool.markBad(proxyUrl);

                        let errorType = 'Unknown';
                        if (err.code === 'ECONNRESET') errorType = 'ECONNRESET';
                        else if (err.code === 'ECONNREFUSED') errorType = 'ECONNREFUSED';
                        else if (err.message && /timeout/i.test(err.message)) errorType = 'Timeout';
                        else if (err.message && /version/i.test(err.message)) errorType = 'VersionMismatch';
                        this.logger.error(`[Bot ${botId}] [${errorType}] ${err.message}`);
                        this.pluginManager.trigger('onError', err, context).catch(() => { });
                        try { this.pythonPluginManager.trigger('onError', { error: err.message, ...context }); } catch (e) { }
                        resolveOnce({ success: false, error: `${errorType}: ${err.message}`, errorType });
                    }
                });

                timeoutHandle = setTimeout(() => {
                    if (!loggedIn && !resolved) {
                        if (bot && bot._client && bot._client.state !== 'closed') try { bot.end(); } catch (e) { }
                        resolveOnce({ success: false, error: 'Timeout', errorType: 'Timeout' });
                    }
                }, (config.connection_timeout + 1) * 1000);

                finalTimeoutHandle = setTimeout(() => {
                    if (!loggedIn && !resolved) {
                        if (bot && bot._client && bot._client.state !== 'closed') try { bot.end(); } catch (e) { }
                        resolveOnce({ success: false, error: 'Final timeout', errorType: 'Timeout' });
                    }
                }, (config.connection_timeout + 3) * 1000);

                bot.once('end', (reason) => {
                    this.pluginManager.trigger('onDisconnect', reason, context).catch(() => { });
                    try { this.pythonPluginManager.trigger('onDisconnect', { reason, ...context }); } catch (e) { }
                    if (!loggedIn && !resolved) {
                        resolveOnce({ success: false, error: 'Disconnected without login', errorType: 'Disconnected without login' });
                    } else if (loggedIn && !resolved) {
                        const isDisconnect = !userEnded;
                        resolveOnce({
                            success: true,
                            player: playerName,
                            version,
                            disconnected: isDisconnect,
                            reason: reason
                        });
                    }
                });
            }).catch(err => {
                this.logger.error(`[Bot ${botId}] 插件异常: ${err.message}`);
                resolveOnce({ success: false, error: 'PluginError', errorType: 'PluginError' });
            });
        });
    }

    async stop(reason = 'program_close') {
        if (this.stopped) return;
        this.stopped = true;
        if (this.statusTimer) clearInterval(this.statusTimer);

        this.logger.info('[系统] 正在关闭所有登录进程...');
        for (const [id, bot] of this.botMap) {
            try { bot.end(); } catch (e) { }
        }
        await new Promise(resolve => setTimeout(resolve, 1500));

        this.logger.info('[系统] 正在卸载插件...');
        await this.pythonPluginManager.unloadAll(reason);
        this.pluginManager.unloadAll(reason);
        await new Promise(resolve => setTimeout(resolve, 1000));

        const elapsed = ((performance.now() - this.startTime) / 1000).toFixed(2);
        const total = this.totalAttempts;
        const rate = total > 0 ? ((this.successCount / total) * 100).toFixed(2) : 0;
        this.logger.info('=== 统计报告 ===');
        this.logger.info(`总尝试: ${total} | 成功: ${this.successCount} | 失败: ${this.failureCount} | 成功率: ${rate}% | 耗时: ${elapsed}s`);
        this.logger.close();
    }
}

// ==================== 命令处理 ====================
let controller = null;
let config = null;

function sendPluginList() {
    if (!controller) return;
    send({
        type: 'plugin-list',
        node: controller.pluginManager.getPluginInfo(),
        python: Array.from(controller.pythonPluginManager.loadedPlugins.entries()).map(([name, p]) => ({
            name, version: p.meta.version, author: p.meta.author,
            description: p.meta.description, permission: p.permission
        }))
    });
}

function sendConfigState() {
    if (!config) return;
    try {
        send({
            type: 'config-state',
            cfg: {
                ip: (config.server && config.server.ip) || '127.0.0.1',
                port: (config.server && config.server.port) || 25565,
                version: config.fixed_version ||
                    (Array.isArray(config.versions) && config.versions.length > 0 ? config.versions[0] : '1.19.2'),
                count: config.count !== undefined ? config.count : 100,
                concurrency: config.concurrency !== undefined ? config.concurrency : 1,
                retryDelay: config.retry_delay !== undefined ? config.retry_delay : 5000,
                connectionInterval: config.interval_between_connections !== undefined ? config.interval_between_connections : 3000,
                connectionTimeout: config.connection_timeout !== undefined ? config.connection_timeout : 5,
                prefix: config.player_name_prefix || '',
                stayConnected: config.stay_connected === true,
                autoDisconnectAfter: config.auto_disconnect_after !== undefined ? config.auto_disconnect_after : 10,
                proxyMode: config.proxy_mode || 'none',
                proxyList: Array.isArray(config.proxy_list) ? config.proxy_list : [],
                useNameDict: config.use_name_dict === true,
                usePlayerPool: config.use_player_pool === true,
                playerPoolExhausted: config.player_pool_exhausted || 'stop'
            }
        });
    } catch (e) { }
}

// ==================== 调试命令处理 ====================
function sendDebugResult(message, level) {
    send({
        type: 'debug-result',
        level: level || 'info',
        message: String(message)
    });
}

function sendDebugDone() {
    send({ type: 'debug-done' });
}

async function handleDebugCommand(input) {
    const raw = String(input || '').trim();
    if (!raw) { sendDebugDone(); return; }

    if (/[\u4e00-\u9fff]/.test(raw)) {
        sendDebugResult('只允许英文指令，请勿输入中文。', 'error');
        sendDebugDone();
        return;
    }

    const parts = raw.split(/\s+/);
    const cmd = parts[0].toLowerCase();
    const args = parts.slice(1);

    try {
        switch (cmd) {
            case 'help': {
                sendDebugResult('═════ 调试命令列表 ═════', 'info');
                sendDebugResult('  help                    显示帮助', 'info');
                sendDebugResult('  version                 显示版本', 'info');
                sendDebugResult('  status                  显示运行状态', 'info');
                sendDebugResult('  bots                    列出所有在线假人', 'info');
                sendDebugResult('  bot <id>                查看单个假人详情', 'info');
                sendDebugResult('  kick <id>               踢掉指定假人', 'info');
                sendDebugResult('  kickall                 踢掉所有假人', 'info');
                sendDebugResult('  proxies                 显示代理池健康状态', 'info');
                sendDebugResult('  memory                  显示内存占用', 'info');
                sendDebugResult('  gc                      强制垃圾回收', 'info');
                sendDebugResult('  uptime                  显示运行时长', 'info');
                sendDebugResult('  cmd <command>           直接向后端发控制命令', 'info');
                sendDebugResult('  raw <json>              直接发原始 JSON', 'info');
                sendDebugResult('  clear                   清空输出（本地）', 'info');
                break;
            }

            case 'version': {
                sendDebugResult(`Minecraft Bot Backend`, 'ok');
                sendDebugResult(`  Version : 7.0.0`, 'info');
                sendDebugResult(`  Node    : ${process.version}`, 'info');
                sendDebugResult(`  Platform: ${process.platform} ${process.arch}`, 'info');
                sendDebugResult(`  Instance: ${INSTANCE_ID}`, 'info');
                break;
            }

            case 'status': {
                if (!controller) { sendDebugResult('controller 未初始化', 'error'); break; }
                sendDebugResult(`started    : ${controller.started}`, 'info');
                sendDebugResult(`stopped    : ${controller.stopped}`, 'info');
                sendDebugResult(`paused     : ${controller.paused}`, 'info');
                sendDebugResult(`success    : ${controller.successCount}`, 'ok');
                sendDebugResult(`failure    : ${controller.failureCount}`, 'warn');
                sendDebugResult(`attempts   : ${controller.totalAttempts}`, 'info');
                sendDebugResult(`active     : ${controller.activeConnections}`, 'info');
                sendDebugResult(`running    : ${controller.running}`, 'info');
                sendDebugResult(`concurrent : ${controller.concurrentLimit}`, 'info');
                if (config && config.use_player_pool) {
                    sendDebugResult(`playerPool : 剩余 ${playerPoolRemaining()}`, 'info');
                }
                break;
            }

            case 'bots': {
                if (!controller || !controller.botMap || controller.botMap.size === 0) {
                    sendDebugResult('当前没有在线假人', 'warn');
                    break;
                }
                sendDebugResult(`共 ${controller.botMap.size} 个在线假人:`, 'info');
                for (const [id, bot] of controller.botMap) {
                    const name = (bot && bot.username) || '?';
                    const state = bot && bot._client ? bot._client.state : 'unknown';
                    sendDebugResult(`  [${id}] ${name}  state=${state}`, 'info');
                }
                break;
            }

            case 'bot': {
                const id = parseInt(args[0]);
                if (isNaN(id)) { sendDebugResult('用法: bot <id>', 'error'); break; }
                if (!controller || !controller.botMap.has(id)) {
                    sendDebugResult(`未找到 bot ${id}`, 'error');
                    break;
                }
                const bot = controller.botMap.get(id);
                sendDebugResult(`Bot ${id}`, 'ok');
                sendDebugResult(`  username : ${bot.username || '?'}`, 'info');
                sendDebugResult(`  state    : ${bot._client ? bot._client.state : 'unknown'}`, 'info');
                if (bot.entity && bot.entity.position) {
                    sendDebugResult(`  position : ${bot.entity.position.x.toFixed(1)}, ${bot.entity.position.y.toFixed(1)}, ${bot.entity.position.z.toFixed(1)}`, 'info');
                }
                sendDebugResult(`  health   : ${bot.health !== undefined ? bot.health : '?'}`, 'info');
                sendDebugResult(`  food     : ${bot.food !== undefined ? bot.food : '?'}`, 'info');
                sendDebugResult(`  ping     : ${bot.player && bot.player.ping !== undefined ? bot.player.ping : '?'} ms`, 'info');
                break;
            }

            case 'kick': {
                const id = parseInt(args[0]);
                if (isNaN(id)) { sendDebugResult('用法: kick <id>', 'error'); break; }
                if (!controller || !controller.botMap.has(id)) {
                    sendDebugResult(`未找到 bot ${id}`, 'error');
                    break;
                }
                try {
                    controller.botMap.get(id).quit('kicked by debug');
                    sendDebugResult(`Bot ${id} 已踢出`, 'ok');
                } catch (e) {
                    sendDebugResult(`踢出失败: ${e.message}`, 'error');
                }
                break;
            }

            case 'kickall': {
                if (!controller || !controller.botMap || controller.botMap.size === 0) {
                    sendDebugResult('没有在线假人', 'warn');
                    break;
                }
                let n = 0;
                for (const [id, bot] of controller.botMap) {
                    try { bot.quit('kickall by debug'); n++; } catch (e) { }
                }
                sendDebugResult(`已踢出 ${n} 个假人`, 'ok');
                break;
            }

            case 'proxies': {
                if (!controller || !controller.proxyPool) {
                    sendDebugResult('代理池未初始化', 'error');
                    break;
                }
                const pool = controller.proxyPool;
                sendDebugResult(`模式: ${pool.mode}  总数: ${pool.entries.length}`, 'info');
                if (pool.entries.length === 0) {
                    sendDebugResult('  代理列表为空', 'warn');
                    break;
                }
                for (const url of pool.entries) {
                    const h = pool.health.get(url) || { success: 0, fail: 0, bannedUntil: 0 };
                    const banned = h.bannedUntil > Date.now() ? ' [冷却中]' : '';
                    sendDebugResult(`  ${url}  成功=${h.success} 失败=${h.fail}${banned}`, banned ? 'warn' : 'info');
                }
                break;
            }

            case 'memory': {
                const m = process.memoryUsage();
                const fmt = (b) => (b / 1024 / 1024).toFixed(2) + ' MB';
                sendDebugResult(`rss        : ${fmt(m.rss)}`, 'info');
                sendDebugResult(`heapTotal  : ${fmt(m.heapTotal)}`, 'info');
                sendDebugResult(`heapUsed   : ${fmt(m.heapUsed)}`, 'info');
                sendDebugResult(`external   : ${fmt(m.external)}`, 'info');
                break;
            }

            case 'gc': {
                if (global.gc) {
                    global.gc();
                    sendDebugResult('已强制垃圾回收', 'ok');
                } else {
                    sendDebugResult('需要以 --expose-gc 启动 node 才能强制 GC', 'warn');
                }
                break;
            }

            case 'uptime': {
                if (!controller) { sendDebugResult('controller 未初始化', 'error'); break; }
                const sec = ((performance.now() - controller.startTime) / 1000).toFixed(2);
                sendDebugResult(`本次任务运行时长: ${sec} 秒`, 'info');
                sendDebugResult(`Node 进程运行时长: ${process.uptime().toFixed(2)} 秒`, 'info');
                break;
            }

            case 'cmd': {
                if (args.length === 0) { sendDebugResult('用法: cmd <command>', 'error'); break; }
                const fullCmd = args.join(' ');
                sendDebugResult(`转发命令: ${fullCmd}`, 'info');
                await handleCommand(fullCmd);
                break;
            }

            case 'raw': {
                const json = raw.substring(4).trim();
                if (!json) { sendDebugResult('用法: raw <json>', 'error'); break; }
                try {
                    const obj = JSON.parse(json);
                    send(obj);
                    sendDebugResult('已发送原始消息', 'ok');
                } catch (e) {
                    sendDebugResult(`JSON 解析失败: ${e.message}`, 'error');
                }
                break;
            }

            case 'clear':
                sendDebugResult('（清空请用对话框里的【清空】按钮）', 'info');
                break;

            default:
                sendDebugResult(`未知命令: ${cmd}。输入 help 查看所有命令。`, 'error');
        }
    } catch (e) {
        sendDebugResult(`执行异常: ${e.message}`, 'error');
    }

    sendDebugDone();
}

async function handleCommand(input) {
    const parts = input.trim().split(/\s+/);
    const cmd = parts[0].toLowerCase();
    const args = parts.slice(1);

    emitLog('INFO', `> ${input}`);

    switch (cmd) {
        case 'help':
            cliWrite('═══════════════════ 命令列表 ═══════════════════', 'yellow');
            cliWrite('  help / status / clear / exit', 'white');
            cliWrite('  config show / ip / port / count / concurrency / prefix / version', 'white');
            cliWrite('  config retry-delay / connection-interval / connection-timeout', 'white');
            cliWrite('  config stay-connected / auto-disconnect / name-dict / save', 'white');
            cliWrite('  config player-pool / player-pool-exhausted', 'white');
            cliWrite('  plugins / reload <名> / reloadall / unload <名>', 'white');
            cliWrite('  permission list / <名> <0-3> / <名> reload / <名> info', 'white');
            cliWrite('  probe-plugins <host> [port] [version]', 'white');
            cliWrite('  reload-names / reload-pool / packlog', 'white');
            cliWrite('═══════════════════════════════════════════════', 'yellow');
            break;

        case 'exit':
            cliWrite('正在退出...', 'yellow');
            if (controller) await controller.stop('program_close');
            process.exit(0);
            break;

        case 'stop':
            if (controller) { controller.paused = true; cliWrite('程序已暂停', 'yellow'); }
            break;

        case 'run':
            if (controller) { controller.paused = false; cliWrite('程序已恢复运行', 'green'); }
            break;

        case 'status':
            if (controller) {
                cliWrite(`状态: ${controller.stopped ? '已停止' : (controller.paused ? '暂停' : '运行中')}`, 'green');
                cliWrite(`成功: ${controller.successCount} | 失败: ${controller.failureCount} | 尝试: ${controller.totalAttempts}`, 'white');
                if (config.use_player_pool) {
                    cliWrite(`玩家名模式: 玩家池（剩余 ${playerPoolRemaining()}）`, 'white');
                } else {
                    cliWrite(`玩家名模式: ${config.use_name_dict ? '英文名字典' : '随机字符串'}`, 'white');
                }
            }
            break;

        case 'clear':
            send({ type: 'clear' });
            break;

        case 'config':
            await handleConfigCommand(args);
            break;

        case 'plugins':
            sendPluginList();
            break;

        case 'permission':
            await handlePermissionCommand(args);
            break;

        case 'probe-plugins': {
            const host = args[0];
            const port = parseInt(args[1]) || 25565;
            const ver = args[2] || '1.19.2';

            if (!host) { cliWrite('用法: probe-plugins <host> [port] [version]', 'red'); break; }

            cliWrite(`开始探测插件列表: ${host}:${port} 版本 ${ver}（需登录，约 10~20 秒）`, 'yellow');

            const result = await probePluginsViaTabComplete(host, port, ver);

            if (!result.success) {
                cliWrite(`❌ 探测失败: ${result.error}`, 'red');
            } else {
                cliWrite(`✅ 探测完成，共 ${result.plugins.length} 个插件（扫描 ${result.cmdCount} 条命令）`, 'green');
                for (const p of result.plugins) cliWrite(`    · ${p}`, 'white');
            }

            send({
                type: 'plugin-probe-result',
                host: host,
                port: port,
                probeSuccess: result.success,
                plugins: result.plugins || [],
                cmdCount: result.cmdCount || 0,
                probeError: result.error || ''
            });
            break;
        }

        case 'reload': {
            if (args.length === 0) { cliWrite('用法: reload <插件名>', 'red'); break; }
            const name = args[0];
            let result;
            if (controller.pluginManager.loadedPlugins.has(name)) {
                result = controller.pluginManager.reloadPlugin(name);
            } else if (controller.pythonPluginManager.loadedPlugins.has(name)) {
                result = await controller.pythonPluginManager.reloadPlugin(name);
            } else { cliWrite(`未找到插件 ${name}`, 'red'); break; }
            cliWrite(result.success ? `插件 ${name} 重启成功` : `重启失败: ${result.error}`, result.success ? 'green' : 'red');
            sendPluginList();
            break;
        }

        case 'reloadall': {
            cliWrite('正在重启所有插件...', 'yellow');
            for (const n of Array.from(controller.pluginManager.loadedPlugins.keys())) {
                controller.pluginManager.reloadPlugin(n);
                await new Promise(r => setTimeout(r, 300));
            }
            for (const n of Array.from(controller.pythonPluginManager.loadedPlugins.keys())) {
                await controller.pythonPluginManager.reloadPlugin(n);
                await new Promise(r => setTimeout(r, 300));
            }
            cliWrite('所有插件重启完成', 'green');
            sendPluginList();
            break;
        }

        case 'unload': {
            if (args.length === 0) { cliWrite('用法: unload <插件名>', 'red'); break; }
            const name = args[0];
            let result;
            if (controller.pluginManager.loadedPlugins.has(name)) {
                result = controller.pluginManager.unloadPlugin(name, 'user_unload');
            } else if (controller.pythonPluginManager.loadedPlugins.has(name)) {
                result = await controller.pythonPluginManager.unloadPlugin(name, 'user_unload');
            } else { cliWrite(`未找到插件 ${name}`, 'red'); break; }
            cliWrite(result.success ? `插件 ${name} 已卸载` : `卸载失败: ${result.error}`, result.success ? 'green' : 'red');
            sendPluginList();
            break;
        }

        case 'packlog':
            await packLogs();
            break;

        case 'reload-names': {
            const dict = loadNameDict(true);
            cliWrite(`✅ 已重新加载 names.txt，共 ${dict.length} 个名字`, 'green');
            break;
        }

        case 'reload-pool': {
            loadPlayerPool(true);
            resetPlayerPool();
            cliWrite(`✅ 已重新加载玩家池，共 ${(_playerPool || []).length} 个名字，已清空使用记录`, 'green');
            break;
        }

        default:
            cliWrite(`未知命令: ${cmd}，输入 help 查看帮助`, 'red');
    }
}

async function handlePermissionCommand(args) {
    if (args.length === 0 || args[0] === 'list') {
        cliWrite('═════ 插件权限列表 ═════', 'yellow');
        for (const [name, data] of controller.pluginManager.loadedPlugins) {
            cliWrite(`  [Node] ${name.padEnd(25)} 权限: ${data.permission}`, 'white');
        }
        for (const [name, data] of controller.pythonPluginManager.loadedPlugins) {
            cliWrite(`  [Py]   ${name.padEnd(25)} 权限: ${data.permission}`, 'white');
        }
        return;
    }

    const pluginName = args[0];
    const sub = args[1];

    let pluginData = controller.pluginManager.loadedPlugins.get(pluginName);
    let isPython = false;
    if (!pluginData) {
        pluginData = controller.pythonPluginManager.loadedPlugins.get(pluginName);
        isPython = true;
    }
    if (!pluginData) { cliWrite(`未找到插件: ${pluginName}`, 'red'); return; }

    if (sub === 'info') {
        cliWrite(`插件: ${pluginName}`, 'green');
        cliWrite(`  类型: ${isPython ? 'Python' : 'Node.js'} | 版本: ${pluginData.meta.version} | 权限: ${pluginData.permission}`, 'white');
        return;
    }

    if (sub === 'reload') {
        let result;
        if (isPython) result = await controller.pythonPluginManager.reloadPlugin(pluginName);
        else result = controller.pluginManager.reloadPlugin(pluginName);
        cliWrite(result.success ? `插件 ${pluginName} 已重载` : `重载失败: ${result.error}`, result.success ? 'green' : 'red');
        return;
    }

    const level = parseInt(sub);
    if (isNaN(level) || level < 0 || level > 3) { cliWrite('权限等级必须 0-3', 'red'); return; }

    pluginData.permission = level;
    if (pluginData.permissionRef) pluginData.permissionRef.permission = level;

    const centralPath = runtimePath('plugins.json');
    try {
        const cfg = fs.existsSync(centralPath) ? JSON.parse(fs.readFileSync(centralPath, 'utf-8')) : {};
        cfg[pluginName] = cfg[pluginName] || {};
        cfg[pluginName].permission = level;
        fs.writeFileSync(centralPath, JSON.stringify(cfg, null, 2));
    } catch (e) { cliWrite(`保存权限失败: ${e.message}`, 'red'); }

    cliWrite(`插件 ${pluginName} 权限已设为 ${level}`, 'green');
    sendPluginList();
}

async function handleConfigCommand(args) {
    if (args.length === 0 || args[0] === 'show') {
        cliWrite('═════ 当前配置 ═════', 'green');
        cliWrite(`  服务器: ${config.server.ip}:${config.server.port}`, 'white');
        cliWrite(`  版本: ${config.fixed_version || '随机'} [${config.versions.join(', ')}]`, 'white');
        cliWrite(`  次数: ${config.count} | 并发: ${config.concurrency}`, 'white');
        cliWrite(`  前缀: ${config.player_name_prefix || '(无)'}`, 'white');
        if (config.use_player_pool) {
            cliWrite(`  玩家名模式: 玩家池（剩余 ${playerPoolRemaining()}）`, 'white');
            cliWrite(`  玩家池耗尽后: ${config.player_pool_exhausted === 'loop' ? '循环' : '停止'}`, 'white');
        } else {
            cliWrite(`  玩家名模式: ${config.use_name_dict ? '英文名字典' : '随机字符串'}`, 'white');
        }
        cliWrite(`  代理模式: ${config.proxy_mode} | 代理数: ${(config.proxy_list || []).length}`, 'white');
        return;
    }

    const sub = args[0];
    const value = args.slice(1).join(' ');
    let changed = false;

    switch (sub) {
        case 'ip':
            if (!value) { cliWrite('用法: config ip <IP>', 'red'); return; }
            config.server.ip = value; changed = true; break;
        case 'port': {
            const port = parseInt(value);
            if (isNaN(port) || port < 1 || port > 65535) { cliWrite('端口无效', 'red'); return; }
            config.server.port = port; changed = true; break;
        }
        case 'count': {
            const count = parseInt(value);
            if (isNaN(count) || count < 0) { cliWrite('次数无效', 'red'); return; }
            config.count = count; changed = true; break;
        }
        case 'concurrency': {
            const conc = parseInt(value);
            if (isNaN(conc) || conc < 1 || conc > 1000) { cliWrite('并发数范围 1-1000', 'red'); return; }
            config.concurrency = conc;
            if (controller) controller.concurrentLimit = conc;
            changed = true; break;
        }
        case 'prefix':
            config.player_name_prefix = value; changed = true; break;
        case 'version':
            if (!value) { cliWrite('用法: config version <版本>', 'red'); return; }
            config.fixed_version = value; changed = true; break;
        case 'name-dict': {
            const on = /^(on|true|1|yes|enable|启用)$/i.test(value);
            config.use_name_dict = on;
            changed = true;
            cliWrite(`英文名字典模式: ${on ? '已启用' : '已禁用'}`, on ? 'green' : 'yellow');
            break;
        }
        case 'player-pool': {
            const on = /^(on|true|1|yes|enable|启用)$/i.test(value);
            config.use_player_pool = on;
            if (on) {
                loadPlayerPool(true);
                resetPlayerPool();
            }
            changed = true;
            cliWrite(`玩家池模式: ${on ? '已启用' : '已禁用'}`, on ? 'green' : 'yellow');
            break;
        }
        case 'player-pool-exhausted': {
            const v = value.toLowerCase().trim();
            if (v !== 'stop' && v !== 'loop') {
                cliWrite('用法: config player-pool-exhausted <stop|loop>', 'red');
                return;
            }
            config.player_pool_exhausted = v;
            changed = true;
            cliWrite(`玩家池耗尽后: ${v === 'stop' ? '停止任务' : '循环重头'}`, 'green');
            break;
        }
        case 'retry-delay': {
            const n = parseInt(value);
            if (isNaN(n) || n < 1000) { cliWrite('重试延迟至少 1000 毫秒', 'red'); return; }
            config.retry_delay = n;
            if (controller) controller.retryDelay = n;
            changed = true; break;
        }
        case 'connection-interval': {
            const n = parseInt(value);
            if (isNaN(n) || n < 0) { cliWrite('连接间隔不能为负', 'red'); return; }
            config.interval_between_connections = n;
            if (controller) controller.connectionInterval = n;
            changed = true; break;
        }
        case 'connection-timeout': {
            const n = parseInt(value);
            if (isNaN(n) || n < 1 || n > 60) { cliWrite('连接超时范围 1~60 秒', 'red'); return; }
            config.connection_timeout = n;
            changed = true; break;
        }
        case 'stay-connected': {
            const on = /^(on|true|1|yes|enable|启用)$/i.test(value);
            config.stay_connected = on;
            changed = true; break;
        }
        case 'auto-disconnect': {
            const n = parseInt(value);
            if (isNaN(n) || n < 0) { cliWrite('自动断开秒数不能为负', 'red'); return; }
            config.auto_disconnect_after = n;
            changed = true; break;
        }
        case 'save':
            saveConfig(config); cliWrite('配置已保存', 'green'); return;
        default:
            cliWrite(`未知配置项: ${sub}`, 'red'); return;
    }

    if (changed) {
        saveConfig(config);
        cliWrite(`配置已更新: ${sub} = ${value}`, 'green');
        sendConfigState();
    }
}

async function packLogs() {
    return new Promise((resolve) => {
        const ts = new Date().toISOString().replace(/[:\-T]/g, '').slice(0, 15);
        const packDir = runtimePath('logs_pack');
        ensureDir(packDir);
        const outputPath = path.join(packDir, `logs_${ts}.zip`);
        const output = fs.createWriteStream(outputPath);
        const archive = archiver('zip', { zlib: { level: 9 } });

        output.on('close', () => {
            cliWrite(`日志打包完成: ${outputPath}`, 'green');
            resolve();
        });
        archive.on('error', (err) => { cliWrite(`打包失败: ${err.message}`, 'red'); resolve(); });
        archive.pipe(output);

        const logDir = runtimePath(config.log_archive_dir || 'logs');
        const nodePluginDir = runtimePath(config.plugin_dir || 'plugins');
        const pyPluginDir = runtimePath(config.plugin_dir_py || 'plugins_py');

        if (fs.existsSync(logDir)) archive.directory(logDir, 'main_logs');
        if (fs.existsSync(nodePluginDir)) archive.directory(nodePluginDir, 'node_plugins');
        if (fs.existsSync(pyPluginDir)) archive.directory(pyPluginDir, 'py_plugins');

        archive.finalize();
    });
}

// ==================== stdin 解析 ====================
const rl = readline.createInterface({ input: process.stdin, terminal: false });

rl.on('line', (line) => {
    let msg;
    try { msg = JSON.parse(line); }
    catch (e) { emitLog('ERROR', '无效 JSON: ' + line); return; }

    switch (msg.type) {
        case 'cmd':
            handleCommand(msg.text).catch(err => emitLog('ERROR', `命令异常: ${err.message}`));
            break;
        case 'debug':
            handleDebugCommand(msg.text).catch(err => emitLog('ERROR', `调试命令异常: ${err.message}`));
            break;
        case 'start':
            handleStartFromGui(msg.params).catch(err => emitLog('ERROR', `启动异常: ${err.message}`));
            break;
        case 'exit':
            (async () => {
                if (controller) await controller.stop('program_close');
                process.exit(0);
            })();
            break;
    }
});

async function handleStartFromGui(params) {
    if (!controller) { emitLog('ERROR', '控制器未初始化'); return; }
    if (controller.started && !controller.stopped) {
        emitLog('WARN', '任务已在运行，忽略启动请求');
        return;
    }
    if (params) {
        if (params.ip) config.server.ip = params.ip;
        if (params.port && !isNaN(params.port)) config.server.port = params.port;
        if (params.version) config.fixed_version = params.version;
        if (!isNaN(params.count)) config.count = params.count;
        if (!isNaN(params.concurrency)) {
            config.concurrency = params.concurrency;
            controller.concurrentLimit = params.concurrency;
        }
        if (typeof params.proxyMode === 'string') config.proxy_mode = params.proxyMode;
        if (Array.isArray(params.proxyList)) {
            config.proxy_list = params.proxyList.filter(s => typeof s === 'string' && s.trim());
        }
        if (typeof params.useNameDict === 'boolean') {
            config.use_name_dict = params.useNameDict;
        }
        if (typeof params.usePlayerPool === 'boolean') {
            config.use_player_pool = params.usePlayerPool;
            if (params.usePlayerPool) {
                loadPlayerPool(true);
                resetPlayerPool();
            }
        }
        if (typeof params.playerPoolExhausted === 'string') {
            const v = params.playerPoolExhausted.toLowerCase().trim();
            if (v === 'stop' || v === 'loop') {
                config.player_pool_exhausted = v;
            }
        }
        if (typeof params.stayConnected === 'boolean') {
            config.stay_connected = params.stayConnected;
        }
        if (typeof params.autoDisconnectAfter === 'number') {
            config.auto_disconnect_after = params.autoDisconnectAfter;
        }

        controller.proxyPool = new ProxyPool(config.proxy_list, config.proxy_mode);
        if (controller.proxyPool.isEnabled()) {
            emitLog('INFO', `[代理] 模式: ${config.proxy_mode}，代理数: ${controller.proxyPool.size}`);
        } else {
            emitLog('INFO', '[代理] 未启用');
        }

        if (config.use_player_pool) {
            const ex = config.player_pool_exhausted === 'loop' ? '循环' : '停止';
            emitLog('INFO', `[玩家名] 玩家池模式已启用，剩余 ${playerPoolRemaining()} 个名字（耗尽后${ex}）`);
        } else if (config.use_name_dict) {
            const dict = loadNameDict();
            emitLog('INFO', `[玩家名] 字典模式已启用，共 ${dict.length} 个名字`);
        } else {
            emitLog('INFO', '[玩家名] 随机字符串模式');
        }

        saveConfig(config);
        sendConfigState();
    }

    if (controller.stopped) {
        controller.stopped = false;
        controller.started = false;
        controller.successCount = 0;
        controller.failureCount = 0;
        controller.totalAttempts = 0;
        controller.consecutiveTimeouts = 0;
        controller.botIdCounter = 0;
        controller.activeConnections = 0;
        controller.running = 0;
        if (!controller.statusTimer) {
            controller.statusTimer = setInterval(() => controller.broadcastStatus(), 1000);
            if (controller.statusTimer.unref) controller.statusTimer.unref();
        }
    }
    controller.start().catch(err => emitLog('ERROR', `启动失败: ${err.message}`));
}

// ==================== 启动 ====================
process.on('SIGINT', async () => {
    if (controller) await controller.stop('program_close');
    process.exit(0);
});
process.on('SIGTERM', async () => {
    if (controller) await controller.stop('program_close');
    process.exit(0);
});

(async () => {
    try {
        config = loadConfig();
    } catch (e) {
        emitLog('ERROR', `[配置错误] ${e.message}`);
        process.exit(1);
    }

    try {
        controller = new BotController(config);
    } catch (e) {
        emitLog('ERROR', `[控制器初始化失败] ${e.message}`);
        process.exit(1);
    }

    emitLog('INFO', 'Node 后端已就绪');
    emitLog('INFO', '输入 help 查看命令，或点击"开始"运行任务');
    send({ type: 'ready' });
    controller.broadcastStatus();
    sendPluginList();
    sendConfigState();
})();