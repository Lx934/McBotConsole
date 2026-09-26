// ============================================================================
// updater.cpp - McBotConsole patch updater
// Usage: updater.exe <job.json path>
// Build (MinGW): g++ updater.cpp -o updater.exe -std=c++11 -static -mwindows -lshell32
// ============================================================================

#include <windows.h>
#include <shellapi.h>
#include <string>
#include <vector>
#include <cstdio>
#include <cstring>
#include <cctype>
#include <ctime>
#include <cwchar>

// ==================== Encoding ====================

static std::wstring U8toW(const std::string &s) {
	if (s.empty())
		return L"";
	int len = MultiByteToWideChar(CP_UTF8, 0, s.c_str(), (int)s.size(), nullptr, 0);
	if (len <= 0)
		return L"";
	std::wstring w(len, L'\0');
	MultiByteToWideChar(CP_UTF8, 0, s.c_str(), (int)s.size(), &w[0], len);
	return w;
}

static std::string WtoU8(const std::wstring &w) {
	if (w.empty())
		return "";
	int len = WideCharToMultiByte(CP_UTF8, 0, w.c_str(), (int)w.size(),
	                              nullptr, 0, nullptr, nullptr);
	if (len <= 0)
		return "";
	std::string s(len, '\0');
	WideCharToMultiByte(CP_UTF8, 0, w.c_str(), (int)w.size(),
	                    &s[0], len, nullptr, nullptr);
	return s;
}

// ==================== Path helpers ====================

static std::wstring JoinPath(const std::wstring &dir, const std::wstring &rel) {
	if (dir.empty())
		return rel;
	if (rel.empty())
		return dir;
	wchar_t last = dir[dir.size() - 1];
	if (last == L'\\' || last == L'/')
		return dir + rel;
	return dir + L"\\" + rel;
}

static std::wstring DirOf(const std::wstring &path) {
	size_t pos = path.find_last_of(L"\\/");
	if (pos == std::wstring::npos)
		return L".";
	if (pos == 0)
		return path.substr(0, 1);
	return path.substr(0, pos);
}

// ==================== Files ====================

static bool FileExistsW(const std::wstring &path) {
	DWORD attrs = GetFileAttributesW(path.c_str());
	return attrs != INVALID_FILE_ATTRIBUTES && !(attrs &FILE_ATTRIBUTE_DIRECTORY);
}

static bool DirExistsW(const std::wstring &path) {
	DWORD attrs = GetFileAttributesW(path.c_str());
	return attrs != INVALID_FILE_ATTRIBUTES && (attrs &FILE_ATTRIBUTE_DIRECTORY);
}

static bool EnsureDirW(const std::wstring &path) {
	if (DirExistsW(path))
		return true;
	size_t pos = path.find_last_of(L"\\/");
	if (pos != std::wstring::npos && pos > 2) {
		EnsureDirW(path.substr(0, pos));
	}
	if (CreateDirectoryW(path.c_str(), nullptr))
		return true;
	return GetLastError() == ERROR_ALREADY_EXISTS;
}

static bool ReadAllBytes(const std::wstring &path, std::string &out) {
	FILE* fp = _wfopen(path.c_str(), L"rb");
	if (!fp)
		return false;
	fseek(fp, 0, SEEK_END);
	long sz = ftell(fp);
	fseek(fp, 0, SEEK_SET);
	if (sz < 0) {
		fclose(fp);
		return false;
	}
	out.resize((size_t)sz);
	if (sz > 0) {
		size_t rd = fread(&out[0], 1, (size_t)sz, fp);
		if (rd != (size_t)sz) {
			fclose(fp);
			return false;
		}
	}
	fclose(fp);
	return true;
}

static bool AppendText(const std::wstring &path, const std::string &content) {
	FILE* fp = _wfopen(path.c_str(), L"ab");
	if (!fp)
		return false;
	if (!content.empty())
		fwrite(content.data(), 1, content.size(), fp);
	fclose(fp);
	return true;
}

static bool CopyFileRetry(const std::wstring &src, const std::wstring &dst, int retries = 15) {
	std::wstring dir = DirOf(dst);
	if (!dir.empty())
		EnsureDirW(dir);

	for (int i = 0; i < retries; i++) {
		if (CopyFileW(src.c_str(), dst.c_str(), FALSE))
			return true;
		Sleep(200);
	}
	return false;
}

// ==================== Log ====================

static std::wstring g_logPath;

static void Log(const std::wstring &msg) {
	time_t t = time(nullptr);
	struct tm lt = { 0 };
	localtime_s(&lt, &t);
	wchar_t ts[32] = { 0 };
	wcsftime(ts, 32, L"%Y-%m-%d %H:%M:%S", &lt);

	std::wstring line = std::wstring(L"[") + ts + L"] " + msg + L"\r\n";

	if (!g_logPath.empty()) {
		FILE* fp = _wfopen(g_logPath.c_str(), L"ab");
		if (fp) {
			std::string u8 = WtoU8(line);
			fwrite(u8.data(), 1, u8.size(), fp);
			fclose(fp);
		}
	}
	OutputDebugStringW(line.c_str());
}

// ==================== Minimal JSON ====================

static void SkipWs(const std::string &s, size_t &i) {
	while (i < s.size()) {
		char c = s[i];
		if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
			i++;
		else
			break;
	}
}

static bool ParseJsonStr(const std::string &s, size_t &i, std::string &out) {
	if (i >= s.size() || s[i] != '"')
		return false;
	i++;
	out.clear();
	while (i < s.size() && s[i] != '"') {
		if (s[i] == '\\' && i + 1 < s.size()) {
			i++;
			switch (s[i]) {
				case '"':
					out += '"';
					break;
				case '\\':
					out += '\\';
					break;
				case '/':
					out += '/';
					break;
				case 'n':
					out += '\n';
					break;
				case 'r':
					out += '\r';
					break;
				case 't':
					out += '\t';
					break;
				case 'b':
					out += '\b';
					break;
				case 'f':
					out += '\f';
					break;
				case 'u':
					if (i + 4 < s.size())
						i += 4;
					break;
				default:
					out += s[i];
					break;
			}
			i++;
		} else {
			out += s[i++];
		}
	}
	if (i >= s.size())
		return false;
	i++;
	return true;
}

static std::string JsonGetString(const std::string &json, const std::string &key) {
	std::string search = "\"" + key + "\"";
	size_t pos = json.find(search);
	if (pos == std::string::npos)
		return "";
	pos += search.size();
	SkipWs(json, pos);
	if (pos >= json.size() || json[pos] != ':')
		return "";
	pos++;
	SkipWs(json, pos);
	std::string result;
	if (!ParseJsonStr(json, pos, result))
		return "";
	return result;
}

static long long JsonGetInt(const std::string &json, const std::string &key, long long def = 0) {
	std::string search = "\"" + key + "\"";
	size_t pos = json.find(search);
	if (pos == std::string::npos)
		return def;
	pos += search.size();
	SkipWs(json, pos);
	if (pos >= json.size() || json[pos] != ':')
		return def;
	pos++;
	SkipWs(json, pos);
	std::string num;
	while (pos < json.size() && (isdigit((unsigned char)json[pos]) || json[pos] == '-')) {
		num += json[pos++];
	}
	if (num.empty())
		return def;
	try {
		return std::stoll(num);
	} catch (...) {
		return def;
	}
}

static std::vector<std::string> JsonGetStringArray(const std::string &json, const std::string &key) {
	std::vector<std::string> result;
	std::string search = "\"" + key + "\"";
	size_t pos = json.find(search);
	if (pos == std::string::npos)
		return result;
	pos += search.size();
	SkipWs(json, pos);
	if (pos >= json.size() || json[pos] != ':')
		return result;
	pos++;
	SkipWs(json, pos);
	if (pos >= json.size() || json[pos] != '[')
		return result;
	pos++;
	while (pos < json.size()) {
		SkipWs(json, pos);
		if (pos < json.size() && json[pos] == ']')
			break;
		if (pos < json.size() && json[pos] == ',') {
			pos++;
			continue;
		}
		if (pos < json.size() && json[pos] == '"') {
			std::string s;
			if (ParseJsonStr(json, pos, s))
				result.push_back(s);
			else
				break;
		} else {
			break;
		}
	}
	return result;
}

// ==================== Path safety ====================

static bool IsPathSafe(const std::string &rel) {
	if (rel.empty())
		return false;
	if (rel[0] == '/' || rel[0] == '\\')
		return false;
	if (rel.size() >= 2 && rel[1] == ':')
		return false;
	if (rel.find("..") != std::string::npos)
		return false;
	if (rel.find('*') != std::string::npos)
		return false;
	if (rel.find('?') != std::string::npos)
		return false;
	if (rel.find('"') != std::string::npos)
		return false;
	if (rel.find('<') != std::string::npos)
		return false;
	if (rel.find('>') != std::string::npos)
		return false;
	if (rel.find('|') != std::string::npos)
		return false;
	return true;
}

// ==================== JSON escape ====================

static std::string JsonEscape(const std::string &s) {
	std::string out;
	out.reserve(s.size() + 8);
	for (size_t i = 0; i < s.size(); i++) {
		char c = s[i];
		switch (c) {
			case '"':
				out += "\\\"";
				break;
			case '\\':
				out += "\\\\";
				break;
			case '\n':
				out += "\\n";
				break;
			case '\r':
				out += "\\r";
				break;
			case '\t':
				out += "\\t";
				break;
			default:
				if ((unsigned char)c < 0x20) {
					char buf[8];
					snprintf(buf, sizeof(buf), "\\u%04x", (unsigned char)c);
					out += buf;
				} else {
					out += c;
				}
		}
	}
	return out;
}

// ==================== Job ====================

struct Job {
	std::wstring programDir;
	std::wstring payloadDir;
	std::wstring backupDir;
	std::wstring appliedJsonPath;
	std::wstring mainExePath;
	DWORD parentPid = 0;
	std::string patchId;
	std::string nonce;
	std::vector<std::wstring> files;
};

static bool ParseJob(const std::string &json, Job &job) {
	job.programDir = U8toW(JsonGetString(json, "programDir"));
	job.payloadDir = U8toW(JsonGetString(json, "payloadDir"));
	job.backupDir = U8toW(JsonGetString(json, "backupDir"));
	job.appliedJsonPath = U8toW(JsonGetString(json, "appliedJsonPath"));
	job.mainExePath = U8toW(JsonGetString(json, "mainExePath"));
	job.parentPid = (DWORD)JsonGetInt(json, "parentPid", 0);
	job.patchId = JsonGetString(json, "patchId");
	job.nonce = JsonGetString(json, "nonce");

	auto files = JsonGetStringArray(json, "files");
	for (size_t i = 0; i < files.size(); i++) {
		job.files.push_back(U8toW(files[i]));
	}

	if (job.programDir.empty())
		return false;
	if (job.payloadDir.empty())
		return false;
	if (job.files.empty())
		return false;
	return true;
}

// ==================== Wait for parent ====================

static void WaitForParentExit(DWORD pid, DWORD timeoutMs) {
	if (pid == 0)
		return;
	HANDLE h = OpenProcess(SYNCHRONIZE, FALSE, pid);
	if (!h)
		return;
	WaitForSingleObject(h, timeoutMs);
	CloseHandle(h);
}

// ==================== Apply with rollback ====================

static bool ApplyFiles(const Job &job, std::vector<std::wstring> &copied) {
	EnsureDirW(job.backupDir);

	for (size_t k = 0; k < job.files.size(); k++) {
		const std::wstring &rel = job.files[k];
		std::string relNarrow = WtoU8(rel);

		if (!IsPathSafe(relNarrow)) {
			Log(L"[ERR] Unsafe path, aborting: " + rel);
			for (size_t r = 0; r < copied.size(); r++) {
				std::wstring b = JoinPath(job.backupDir, copied[r]);
				std::wstring d = JoinPath(job.programDir, copied[r]);
				if (FileExistsW(b))
					CopyFileRetry(b, d);
			}
			return false;
		}

		std::wstring src = JoinPath(job.payloadDir, rel);
		std::wstring dst = JoinPath(job.programDir, rel);
		std::wstring bak = JoinPath(job.backupDir, rel);

		if (!FileExistsW(src)) {
			Log(L"[ERR] Missing in payload: " + src);
			for (size_t r = 0; r < copied.size(); r++) {
				std::wstring b = JoinPath(job.backupDir, copied[r]);
				std::wstring d = JoinPath(job.programDir, copied[r]);
				if (FileExistsW(b))
					CopyFileRetry(b, d);
			}
			return false;
		}

		if (FileExistsW(dst)) {
			if (!CopyFileRetry(dst, bak)) {
				Log(L"[ERR] Backup failed: " + dst);
				for (size_t r = 0; r < copied.size(); r++) {
					std::wstring b = JoinPath(job.backupDir, copied[r]);
					std::wstring d = JoinPath(job.programDir, copied[r]);
					if (FileExistsW(b))
						CopyFileRetry(b, d);
				}
				return false;
			}
		}

		if (!CopyFileRetry(src, dst)) {
			Log(L"[ERR] Copy failed: " + src + L" -> " + dst);
			for (size_t r = 0; r < copied.size(); r++) {
				std::wstring b = JoinPath(job.backupDir, copied[r]);
				std::wstring d = JoinPath(job.programDir, copied[r]);
				if (FileExistsW(b))
					CopyFileRetry(b, d);
			}
			return false;
		}

		copied.push_back(rel);
		Log(L"[OK] Replaced: " + rel);
	}

	return true;
}

// ==================== Write applied.jsonl ====================

static bool WriteAppliedRecord(const Job &job) {
	if (job.appliedJsonPath.empty())
		return false;

	time_t t = time(nullptr);

	std::string line = "{";
	line += "\"patchId\":\"" + JsonEscape(job.patchId) + "\",";
	line += "\"nonce\":\"" + JsonEscape(job.nonce) + "\",";
	line += "\"appliedAt\":" + std::to_string((long long)t) + ",";

	line += "\"files\":[";
	for (size_t i = 0; i < job.files.size(); i++) {
		if (i > 0)
			line += ",";
		line += "\"" + JsonEscape(WtoU8(job.files[i])) + "\"";
	}
	line += "],";

	line += "\"backupDir\":\"" + JsonEscape(WtoU8(job.backupDir)) + "\"";
	line += "}\n";

	std::wstring dir = DirOf(job.appliedJsonPath);
	if (!dir.empty())
		EnsureDirW(dir);

	return AppendText(job.appliedJsonPath, line);
}

// ==================== Cleanup temp ====================

static void CleanupTempDir(const std::wstring &payloadDir) {
	std::wstring tempRoot = DirOf(payloadDir);
	if (tempRoot.empty() || tempRoot == L".")
		return;

	std::wstring name = tempRoot;
	size_t slash = tempRoot.find_last_of(L"\\/");
	if (slash != std::wstring::npos)
		name = tempRoot.substr(slash + 1);
	if (name.find(L"mcpatch-") != 0) {
		Log(L"[WARN] Temp dir name mismatch, skip cleanup: " + tempRoot);
		return;
	}

	Log(L"[CLEANUP] Scheduled removal: " + tempRoot);

	std::wstring cmd = L"cmd.exe /c timeout /t 2 /nobreak > nul & rmdir /s /q \""
	                   + tempRoot + L"\"";

	STARTUPINFOW si = { 0 };
	si.cb = sizeof(si);
	si.dwFlags = STARTF_USESHOWWINDOW;
	si.wShowWindow = SW_HIDE;
	PROCESS_INFORMATION pi = { 0 };

	std::wstring cmdBuf = cmd;
	if (CreateProcessW(nullptr, &cmdBuf[0], nullptr, nullptr, FALSE,
	                   CREATE_NO_WINDOW, nullptr, nullptr, &si, &pi)) {
		CloseHandle(pi.hProcess);
		CloseHandle(pi.hThread);
	}
}

// ==================== Start main exe ====================

static bool StartMainExe(const std::wstring &exePath, const std::wstring &workDir) {
	std::wstring cmd = L"\"" + exePath + L"\"";
	std::wstring cwd = workDir.empty() ? DirOf(exePath) : workDir;

	STARTUPINFOW si = { 0 };
	si.cb = sizeof(si);
	PROCESS_INFORMATION pi = { 0 };

	std::wstring cmdBuf = cmd;
	if (CreateProcessW(
	            exePath.c_str(),
	            &cmdBuf[0],
	            nullptr, nullptr, FALSE,
	            0, nullptr,
	            cwd.empty() ? nullptr : cwd.c_str(),
	            &si, &pi)) {
		CloseHandle(pi.hProcess);
		CloseHandle(pi.hThread);
		return true;
	}
	return false;
}

// ==================== Main ====================

int main() {
	int argc = 0;
	LPWSTR* argv = CommandLineToArgvW(GetCommandLineW(), &argc);

	if (!argv || argc < 2) {
		if (argv)
			LocalFree(argv);
		return 1;
	}

	std::wstring jobPath = argv[1];
	LocalFree(argv);

	std::wstring jobDir = DirOf(jobPath);
	g_logPath = JoinPath(jobDir, L"updater.log");

	Log(L"=== updater started ===");
	Log(L"job.json: " + jobPath);

	std::string json;
	if (!ReadAllBytes(jobPath, json)) {
		Log(L"[FATAL] Cannot read job.json");
		return 2;
	}

	Job job;
	if (!ParseJob(json, job)) {
		Log(L"[FATAL] job.json parse failed or missing fields");
		return 3;
	}

	Log(L"programDir: " + job.programDir);
	Log(L"payloadDir: " + job.payloadDir);
	Log(L"backupDir: " + job.backupDir);
	Log(L"appliedJsonPath: " + job.appliedJsonPath);
	Log(L"mainExePath: " + job.mainExePath);
	Log(L"parentPid: " + std::to_wstring((unsigned long long)job.parentPid));
	Log(L"patchId: " + U8toW(job.patchId));
	Log(L"nonce: " + U8toW(job.nonce));
	Log(L"files count: " + std::to_wstring((unsigned long long)job.files.size()));

	if (job.parentPid != 0) {
		Log(L"Waiting for parent process to exit...");
		WaitForParentExit(job.parentPid, 60000);
		Sleep(800);
		Log(L"Parent process exited");
	}

	std::vector<std::wstring> copied;
	if (!ApplyFiles(job, copied)) {
		Log(L"[FATAL] Apply failed, rolled back");
		return 4;
	}

	if (!WriteAppliedRecord(job)) {
		Log(L"[WARN] applied.jsonl write failed (continuing)");
	} else {
		Log(L"[OK] applied.jsonl updated");
	}

	Log(L"[OK] Done, replaced " + std::to_wstring((unsigned long long)copied.size()) + L" file(s)");

	CleanupTempDir(job.payloadDir);

	if (!job.mainExePath.empty() && FileExistsW(job.mainExePath)) {
		Log(L"Starting main exe: " + job.mainExePath);
		if (!StartMainExe(job.mainExePath, job.programDir)) {
			Log(L"[WARN] Failed to start main exe, start it manually");
		}
	}

	Log(L"=== updater exited ===");
	return 0;
}