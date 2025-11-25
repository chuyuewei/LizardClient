package main

import (
	"crypto/sha256"
	"crypto/subtle"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net/http"
	"os"
	"path/filepath"
	"sort"
	"strings"
	"time"
)

const (
	Port          = "51000"
	ManifestsDir  = "./manifests"
	DownloadsDir  = "./downloads"
	ChangelogsDir = "./changelogs"
	PanelDir      = "./panel"
)

var (
	// 管理员认证配置 - 生产环境应从配置文件或环境变量读取
	AdminUsername = "chuyuewei"
	AdminPassword = "CYW@1008.com" // 建议修改为强密码
)

// Statistics 统计数据
type Statistics struct {
	TotalDownloads   int64            `json:"totalDownloads"`
	FileDownloads    map[string]int64 `json:"fileDownloads"`
	StorageUsage     int64            `json:"storageUsage"`
	TotalFiles       int              `json:"totalFiles"`
	LastUpdate       time.Time        `json:"lastUpdate"`
	RecentActivities []ActivityLog    `json:"recentActivities"`
}

// ActivityLog 活动日志
type ActivityLog struct {
	Timestamp time.Time `json:"timestamp"`
	Action    string    `json:"action"`
	Details   string    `json:"details"`
}

var stats = &Statistics{
	FileDownloads:    make(map[string]int64),
	RecentActivities: make([]ActivityLog, 0),
}

// UpdateManifest 更新清单结构
type UpdateManifest struct {
	ManifestVersion string       `json:"manifestVersion"`
	LatestVersion   string       `json:"latestVersion"`
	MinimumVersion  string       `json:"minimumVersion"`
	Channel         string       `json:"channel"`
	LastUpdated     time.Time    `json:"lastUpdated"`
	UpdateServerUrl string       `json:"updateServerUrl"`
	Updates         []UpdateInfo `json:"updates"`
}

// UpdateInfo 更新信息
type UpdateInfo struct {
	Version                  string    `json:"version"`
	ReleaseDate              time.Time `json:"releaseDate"`
	DownloadUrl              string    `json:"downloadUrl"`
	FileSize                 int64     `json:"fileSize"`
	FileHash                 string    `json:"fileHash"`
	IsMandatory              bool      `json:"isMandatory"`
	IsCritical               bool      `json:"isCritical"`
	Changelog                string    `json:"changelog"`
	MinimumCompatibleVersion string    `json:"minimumCompatibleVersion"`
	Dependencies             []string  `json:"dependencies"`
	ReleaseNotesUrl          string    `json:"releaseNotesUrl"`
}

// HealthResponse 健康检查响应
type HealthResponse struct {
	Status    string    `json:"status"`
	Timestamp time.Time `json:"timestamp"`
	Version   string    `json:"version"`
}

// FileInfo 文件信息
type FileInfo struct {
	Name     string    `json:"name"`
	Size     int64     `json:"size"`
	Hash     string    `json:"hash"`
	Modified time.Time `json:"modified"`
}

func main() {
	// 创建必要的目录
	createDirectories()

	// 加载统计数据
	loadStatistics()

	// 注册路由
	// 公开端点
	http.HandleFunc("/health", healthHandler)
	http.HandleFunc("/manifest-stable.json", manifestHandler("stable"))
	http.HandleFunc("/manifest-beta.json", manifestHandler("beta"))
	http.HandleFunc("/manifest-dev.json", manifestHandler("dev"))
	http.HandleFunc("/downloads/", downloadHandler)
	http.HandleFunc("/changelog/", changelogHandler)
	http.HandleFunc("/mods/", modHandler)

	// 管理面板（需要认证）
	http.HandleFunc("/admin", basicAuth(panelHandler))
	http.HandleFunc("/admin/", basicAuth(servePanel))

	// API端点（需要认证）
	http.HandleFunc("/api/upload", basicAuth(uploadHandler))
	http.HandleFunc("/api/manifests", basicAuth(manifestsAPIHandler))
	http.HandleFunc("/api/manifests/", basicAuth(updateManifestHandler))
	http.HandleFunc("/api/files", basicAuth(filesListHandler))
	http.HandleFunc("/api/files/", basicAuth(deleteFileHandler))
	http.HandleFunc("/api/statistics", basicAuth(statisticsHandler))
	http.HandleFunc("/api/hash", basicAuth(hashHandler))

	// 启动服务器
	addr := ":" + Port
	log.Printf("==============================================")
	log.Printf("   LizardClient Update Server v2.0")
	log.Printf("==============================================")
	log.Printf("")
	log.Printf("Server starting on http://localhost:%s", Port)
	log.Printf("")
	log.Printf("Public Endpoints:")
	log.Printf("  - GET  /health                    服务器健康检查")
	log.Printf("  - GET  /manifest-{channel}.json   获取更新清单")
	log.Printf("  - GET  /downloads/<filename>      下载更新文件")
	log.Printf("")
	log.Printf("Admin Panel:")
	log.Printf("  - GET  /admin                     管理面板")
	log.Printf("  - Username: %s", AdminUsername)
	log.Printf("")
	log.Printf("API Endpoints (需要认证):")
	log.Printf("  - POST /api/upload                上传文件")
	log.Printf("  - GET  /api/manifests             获取所有清单")
	log.Printf("  - PUT  /api/manifests/{channel}   更新清单")
	log.Printf("  - GET  /api/files                 文件列表")
	log.Printf("  - DEL  /api/files/{filename}      删除文件")
	log.Printf("  - GET  /api/statistics            统计数据")
	log.Printf("")
	log.Printf("==============================================")
	log.Printf("")

	if err := http.ListenAndServe(addr, logMiddleware(http.DefaultServeMux)); err != nil {
		log.Fatalf("Server failed to start: %v", err)
	}
}

// createDirectories 创建必要的目录
func createDirectories() {
	dirs := []string{ManifestsDir, DownloadsDir, ChangelogsDir, PanelDir, filepath.Join(DownloadsDir, "mods")}
	for _, dir := range dirs {
		if err := os.MkdirAll(dir, 0755); err != nil {
			log.Fatalf("Failed to create directory %s: %v", dir, err)
		}
	}
	log.Printf("Directories initialized: %v", dirs)
}

// basicAuth HTTP基础认证中间件
func basicAuth(handler http.HandlerFunc) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		username, password, ok := r.BasicAuth()
		if !ok {
			w.Header().Set("WWW-Authenticate", `Basic realm="Admin Panel"`)
			http.Error(w, "Unauthorized", http.StatusUnauthorized)
			return
		}

		// 使用constant-time比较防止时序攻击
		usernameMatch := subtle.ConstantTimeCompare([]byte(username), []byte(AdminUsername)) == 1
		passwordMatch := subtle.ConstantTimeCompare([]byte(password), []byte(AdminPassword)) == 1

		if !usernameMatch || !passwordMatch {
			w.Header().Set("WWW-Authenticate", `Basic realm="Admin Panel"`)
			http.Error(w, "Unauthorized", http.StatusUnauthorized)
			return
		}

		handler(w, r)
	}
}

// logMiddleware 日志中间件
func logMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		start := time.Now()
		next.ServeHTTP(w, r)
		log.Printf("%s %s %s", r.Method, r.RequestURI, time.Since(start))
	})
}

// healthHandler 健康检查处理器
func healthHandler(w http.ResponseWriter, r *http.Request) {
	response := HealthResponse{
		Status:    "ok",
		Timestamp: time.Now(),
		Version:   "2.0.0",
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(response)
}

// manifestHandler 清单处理器工厂函数
func manifestHandler(channel string) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		manifestPath := filepath.Join(ManifestsDir, fmt.Sprintf("manifest-%s.json", channel))

		// 如果清单文件不存在，创建默认清单
		if _, err := os.Stat(manifestPath); os.IsNotExist(err) {
			log.Printf("Manifest not found, creating default: %s", manifestPath)
			createDefaultManifest(manifestPath, channel)
		}

		// 读取清单文件
		data, err := os.ReadFile(manifestPath)
		if err != nil {
			http.Error(w, "Failed to read manifest", http.StatusInternalServerError)
			log.Printf("Error reading manifest: %v", err)
			return
		}

		w.Header().Set("Content-Type", "application/json")
		w.Header().Set("Access-Control-Allow-Origin", "*")
		w.Write(data)
	}
}

// createDefaultManifest 创建默认清单
func createDefaultManifest(path string, channel string) {
	serverUrl := fmt.Sprintf("http://localhost:%s", Port)

	manifest := UpdateManifest{
		ManifestVersion: "1.0.0",
		LatestVersion:   "1.0.0",
		MinimumVersion:  "1.0.0",
		Channel:         channel,
		LastUpdated:     time.Now(),
		UpdateServerUrl: serverUrl,
		Updates: []UpdateInfo{
			{
				Version:                  "1.0.0",
				ReleaseDate:              time.Now(),
				DownloadUrl:              fmt.Sprintf("%s/downloads/LizardClient_v1.0.0.zip", serverUrl),
				FileSize:                 0,
				FileHash:                 "",
				IsMandatory:              false,
				IsCritical:               false,
				Changelog:                "Initial release",
				MinimumCompatibleVersion: "1.0.0",
				Dependencies:             []string{},
				ReleaseNotesUrl:          fmt.Sprintf("%s/changelog/1.0.0.md", serverUrl),
			},
		},
	}

	data, err := json.MarshalIndent(manifest, "", "  ")
	if err != nil {
		log.Printf("Error creating default manifest: %v", err)
		return
	}

	if err := os.WriteFile(path, data, 0644); err != nil {
		log.Printf("Error writing default manifest: %v", err)
	}
}

// downloadHandler 下载处理器
func downloadHandler(w http.ResponseWriter, r *http.Request) {
	filename := filepath.Base(r.URL.Path)
	if filename == "downloads" || filename == "" {
		http.Error(w, "Filename required", http.StatusBadRequest)
		return
	}

	filePath := filepath.Join(DownloadsDir, filename)

	fileInfo, err := os.Stat(filePath)
	if os.IsNotExist(err) {
		http.Error(w, "File not found", http.StatusNotFound)
		log.Printf("File not found: %s", filePath)
		return
	}

	file, err := os.Open(filePath)
	if err != nil {
		http.Error(w, "Failed to open file", http.StatusInternalServerError)
		log.Printf("Error opening file: %v", err)
		return
	}
	defer file.Close()

	// 更新统计
	stats.FileDownloads[filename]++
	stats.TotalDownloads++
	addActivity("download", fmt.Sprintf("Downloaded: %s", filename))
	saveStatistics()

	w.Header().Set("Content-Type", "application/zip")
	w.Header().Set("Content-Disposition", fmt.Sprintf("attachment; filename=%s", filename))
	w.Header().Set("Content-Length", fmt.Sprintf("%d", fileInfo.Size()))
	w.Header().Set("Accept-Ranges", "bytes")

	if r.Header.Get("Range") != "" {
		http.ServeFile(w, r, filePath)
		return
	}

	io.Copy(w, file)
	log.Printf("File downloaded: %s (%d bytes)", filename, fileInfo.Size())
}

// changelogHandler 更新日志处理器
func changelogHandler(w http.ResponseWriter, r *http.Request) {
	filename := filepath.Base(r.URL.Path)
	if filename == "changelog" || filename == "" {
		http.Error(w, "Version required", http.StatusBadRequest)
		return
	}

	changelogPath := filepath.Join(ChangelogsDir, filename)

	if _, err := os.Stat(changelogPath); os.IsNotExist(err) {
		defaultChangelog := fmt.Sprintf("# Version %s\n\nNo changelog available.\n", filename)
		w.Header().Set("Content-Type", "text/markdown; charset=utf-8")
		w.Write([]byte(defaultChangelog))
		return
	}

	data, err := os.ReadFile(changelogPath)
	if err != nil {
		http.Error(w, "Failed to read changelog", http.StatusInternalServerError)
		log.Printf("Error reading changelog: %v", err)
		return
	}

	w.Header().Set("Content-Type", "text/markdown; charset=utf-8")
	w.Write(data)
}

// modHandler 模组信息处理器
func modHandler(w http.ResponseWriter, r *http.Request) {
	path := r.URL.Path[len("/mods/"):]
	parts := strings.Split(path, "/")

	if len(parts) < 2 {
		http.Error(w, "Invalid mod URL", http.StatusBadRequest)
		return
	}

	modId := parts[0]
	modInfoPath := filepath.Join(DownloadsDir, "mods", modId, "latest.json")

	if _, err := os.Stat(modInfoPath); os.IsNotExist(err) {
		http.Error(w, "Mod not found", http.StatusNotFound)
		log.Printf("Mod not found: %s", modId)
		return
	}

	data, err := os.ReadFile(modInfoPath)
	if err != nil {
		http.Error(w, "Failed to read mod info", http.StatusInternalServerError)
		log.Printf("Error reading mod info: %v", err)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	w.Header().Set("Access-Control-Allow-Origin", "*")
	w.Write(data)
}

// panelHandler 管理面板主页
func panelHandler(w http.ResponseWriter, r *http.Request) {
	http.ServeFile(w, r, filepath.Join(PanelDir, "index.html"))
}

// servePanel 提供面板静态文件
func servePanel(w http.ResponseWriter, r *http.Request) {
	path := r.URL.Path[len("/admin/"):]
	if path == "" {
		http.ServeFile(w, r, filepath.Join(PanelDir, "index.html"))
		return
	}

	filePath := filepath.Join(PanelDir, path)
	http.ServeFile(w, r, filePath)
}

// uploadHandler 文件上传处理器
func uploadHandler(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	// 解析multipart表单（最大32MB）
	if err := r.ParseMultipartForm(32 << 20); err != nil {
		http.Error(w, "Failed to parse form", http.StatusBadRequest)
		return
	}

	file, header, err := r.FormFile("file")
	if err != nil {
		http.Error(w, "Failed to get file", http.StatusBadRequest)
		return
	}
	defer file.Close()

	// 创建目标文件
	filename := header.Filename
	destPath := filepath.Join(DownloadsDir, filename)

	dest, err := os.Create(destPath)
	if err != nil {
		http.Error(w, "Failed to create file", http.StatusInternalServerError)
		return
	}
	defer dest.Close()

	// 复制文件并计算哈希
	hash := sha256.New()
	writer := io.MultiWriter(dest, hash)

	size, err := io.Copy(writer, file)
	if err != nil {
		http.Error(w, "Failed to save file", http.StatusInternalServerError)
		return
	}

	hashString := hex.EncodeToString(hash.Sum(nil))

	// 返回文件信息
	response := FileInfo{
		Name:     filename,
		Size:     size,
		Hash:     hashString,
		Modified: time.Now(),
	}

	addActivity("upload", fmt.Sprintf("Uploaded: %s (%d bytes)", filename, size))
	updateStorageStats()

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(response)

	log.Printf("File uploaded: %s (%d bytes, hash: %s)", filename, size, hashString)
}

// manifestsAPIHandler 获取所有清单
func manifestsAPIHandler(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	manifests := make(map[string]interface{})
	channels := []string{"stable", "beta", "dev"}

	for _, channel := range channels {
		manifestPath := filepath.Join(ManifestsDir, fmt.Sprintf("manifest-%s.json", channel))
		if data, err := os.ReadFile(manifestPath); err == nil {
			var manifest UpdateManifest
			if err := json.Unmarshal(data, &manifest); err == nil {
				manifests[channel] = manifest
			}
		}
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(manifests)
}

// updateManifestHandler 更新清单
func updateManifestHandler(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPut {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	channel := filepath.Base(r.URL.Path)
	if channel != "stable" && channel != "beta" && channel != "dev" {
		http.Error(w, "Invalid channel", http.StatusBadRequest)
		return
	}

	var manifest UpdateManifest
	if err := json.NewDecoder(r.Body).Decode(&manifest); err != nil {
		http.Error(w, "Invalid JSON", http.StatusBadRequest)
		return
	}

	manifest.LastUpdated = time.Now()
	manifestPath := filepath.Join(ManifestsDir, fmt.Sprintf("manifest-%s.json", channel))

	data, err := json.MarshalIndent(manifest, "", "  ")
	if err != nil {
		http.Error(w, "Failed to encode manifest", http.StatusInternalServerError)
		return
	}

	if err := os.WriteFile(manifestPath, data, 0644); err != nil {
		http.Error(w, "Failed to save manifest", http.StatusInternalServerError)
		return
	}

	addActivity("manifest", fmt.Sprintf("Updated manifest: %s", channel))

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]string{"status": "success"})

	log.Printf("Manifest updated: %s", channel)
}

// filesListHandler 获取文件列表
func filesListHandler(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	files, err := os.ReadDir(DownloadsDir)
	if err != nil {
		http.Error(w, "Failed to read directory", http.StatusInternalServerError)
		return
	}

	var fileList []FileInfo
	for _, file := range files {
		if file.IsDir() {
			continue
		}

		info, err := file.Info()
		if err != nil {
			continue
		}

		filePath := filepath.Join(DownloadsDir, file.Name())
		hash, _ := calculateFileHash(filePath)

		fileList = append(fileList, FileInfo{
			Name:     file.Name(),
			Size:     info.Size(),
			Hash:     hash,
			Modified: info.ModTime(),
		})
	}

	// 按修改时间降序排序
	sort.Slice(fileList, func(i, j int) bool {
		return fileList[i].Modified.After(fileList[j].Modified)
	})

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(fileList)
}

// deleteFileHandler 删除文件
func deleteFileHandler(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodDelete {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	filename := filepath.Base(r.URL.Path)
	filePath := filepath.Join(DownloadsDir, filename)

	if err := os.Remove(filePath); err != nil {
		http.Error(w, "Failed to delete file", http.StatusInternalServerError)
		return
	}

	addActivity("delete", fmt.Sprintf("Deleted: %s", filename))
	updateStorageStats()

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]string{"status": "success"})

	log.Printf("File deleted: %s", filename)
}

// statisticsHandler 统计数据处理器
func statisticsHandler(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	updateStorageStats()

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(stats)
}

// hashHandler 计算文件哈希
func hashHandler(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var req struct {
		Filename string `json:"filename"`
	}

	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "Invalid JSON", http.StatusBadRequest)
		return
	}

	filePath := filepath.Join(DownloadsDir, req.Filename)
	hash, err := calculateFileHash(filePath)
	if err != nil {
		http.Error(w, "Failed to calculate hash", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]string{"hash": hash})
}

// calculateFileHash 计算文件SHA256哈希
func calculateFileHash(filePath string) (string, error) {
	file, err := os.Open(filePath)
	if err != nil {
		return "", err
	}
	defer file.Close()

	hash := sha256.New()
	if _, err := io.Copy(hash, file); err != nil {
		return "", err
	}

	return hex.EncodeToString(hash.Sum(nil)), nil
}

// addActivity 添加活动日志
func addActivity(action, details string) {
	activity := ActivityLog{
		Timestamp: time.Now(),
		Action:    action,
		Details:   details,
	}

	stats.RecentActivities = append([]ActivityLog{activity}, stats.RecentActivities...)
	if len(stats.RecentActivities) > 50 {
		stats.RecentActivities = stats.RecentActivities[:50]
	}

	saveStatistics()
}

// updateStorageStats 更新存储统计
func updateStorageStats() {
	var totalSize int64
	var fileCount int

	files, err := os.ReadDir(DownloadsDir)
	if err != nil {
		return
	}

	for _, file := range files {
		if file.IsDir() {
			continue
		}

		info, err := file.Info()
		if err != nil {
			continue
		}

		totalSize += info.Size()
		fileCount++
	}

	stats.StorageUsage = totalSize
	stats.TotalFiles = fileCount
	stats.LastUpdate = time.Now()
}

// loadStatistics 加载统计数据
func loadStatistics() {
	statsPath := "./stats.json"
	data, err := os.ReadFile(statsPath)
	if err != nil {
		log.Printf("No existing statistics found, starting fresh")
		return
	}

	if err := json.Unmarshal(data, stats); err != nil {
		log.Printf("Error loading statistics: %v", err)
	}
}

// saveStatistics 保存统计数据
func saveStatistics() {
	statsPath := "./stats.json"
	data, err := json.MarshalIndent(stats, "", "  ")
	if err != nil {
		log.Printf("Error encoding statistics: %v", err)
		return
	}

	if err := os.WriteFile(statsPath, data, 0644); err != nil {
		log.Printf("Error saving statistics: %v", err)
	}
}
