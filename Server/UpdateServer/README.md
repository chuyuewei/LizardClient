# LizardClient Update Server v2.0

增强版更新服务器，带有Web管理面板。

## 新功能 v2.0

### 🔐 认证系统
- HTTP基础认证保护管理面板
- 默认用户名: `admin`
- 默认密码: `lizard2025` ⚠️ **建议修改**

### 📤 文件上传
- 拖拽上传支持
- 自动计算SHA256哈希
- 上传进度显示
- 文件大小限制: 32MB

### ✏️ 清单编辑
- Web界面编辑JSON清单
- 支持三个频道 (Stable/Beta/Dev)
- 实时JSON验证
- 一键保存

### 📊 统计面板
- 总下载量追踪
- 文件数量统计
- 存储空间使用
- 最近活动日志

### 📂 文件管理
- 文件列表展示
- 一键删除文件
- 复制哈希值
- 文件信息查看

## 快速开始

### 1. 启动服务器

```bash
cd UpdateServer
go run main.go
```

或使用批处理脚本:
```batch
.\start-update-server.bat
```

### 2. 访问管理面板

打开浏览器访问:
```
http://localhost:51000/admin
```

**登录信息:**
- 用户名: `admin`
- 密码: `lizard2025`

## API端点

### 公开端点
```
GET  /health                    # 健康检查
GET  /manifest-stable.json      # 稳定版清单
GET  /manifest-beta.json        # 测试版清单
GET  /manifest-dev.json         # 开发版清单
GET  /downloads/<filename>      # 下载文件
GET  /changelog/<version>.md    # 更新日志
```

### 管理API（需要认证）
```
GET   /admin                    # 管理面板
POST  /api/upload               # 上传文件
GET   /api/manifests            # 获取所有清单
PUT   /api/manifests/{channel}  # 更新清单
GET   /api/files                # 文件列表
DELETE /api/files/{filename}    # 删除文件
GET   /api/statistics           # 统计数据
POST  /api/hash                 # 计算文件哈希
```

## 目录结构

```
UpdateServer/
├── main.go                    # 服务器主程序
├── go.mod                     # Go模块
├── README.md                  # 文档
├── stats.json                 # 统计数据（自动创建）
├── manifests/                 # 更新清单
│   ├── manifest-stable.json
│   ├── manifest-beta.json
│   └── manifest-dev.json
├── downloads/                 # 更新文件
│   └── mods/                  # 模组文件
├── changelogs/               # 更新日志
└── panel/                    # 管理面板
    ├── index.html
    ├── style.css
    └── script.js
```

## 使用示例

### 上传新版本

1. 打开管理面板 `http://localhost:51000/admin`
2. 在"文件上传"区域拖拽或选择ZIP文件
3. 等待上传完成并记录SHA256哈希值
4. 在"清单编辑"中添加新版本信息

### 编辑清单

```json
{
  "manifestVersion": "1.0.0",
  "latestVersion": "1.2.0",
  "channel": "stable",
  "updates": [
    {
      "version": "1.2.0",
      "downloadUrl": "http://localhost:51000/downloads/LizardClient_v1.2.0.zip",
      "fileHash": "上传时获取的哈希值",
      "changelog": "更新内容"
    }
  ]
}
```

### 查看统计

统计面板实时显示:
- 总下载次数
- 文件数量
- 存储使用量
- 最近活动

## 安全建议

### 修改默认密码

编辑 `main.go` 文件:

```go
var (
    AdminUsername = "admin"
    AdminPassword = "your-strong-password-here"  // 修改这里
)
```

### 生产部署

1. **使用环境变量**
   ```bash
   export ADMIN_USER=admin
   export ADMIN_PASS=strong-password
   ```

2. **使用HTTPS**
   - 配置nginx反向代理
   - 启用SSL证书

3. **限制访问**
   - 使用防火墙限制IP
   - 配置nginx IP白名单

## 管理面板截图

管理面板包含:
- 📊 实时统计卡片
- 📤 拖拽上传界面
- ✏️ JSON清单编辑器
- 📂 文件管理列表
- 📋 活动日志

## 故障排除

### 无法访问管理面板

检查:
1. 服务器是否正在运行
2. 端口51000是否被占用
3. 浏览器是否启用JavaScript
4. 登录凭据是否正确

### 上传失败

检查:
1. 文件大小是否超过32MB
2. downloads目录权限
3. 磁盘空间是否充足

### 清单保存失败

检查:
1. JSON格式是否正确
2. manifests目录权限
3. 浏览器开发者工具查看错误

## 技术栈

- **后端**: Go 1.21+
- **前端**: Vanilla HTML/CSS/JavaScript
- **认证**: HTTP Basic Auth
- **存储**: 文件系统 + JSON

## 更新日志

### v2.0.0 (2025-11-25)
- ✨ 新增Web管理面板
- ✨ 文件上传功能
- ✨ 清单编辑器
- ✨ 统计面板
- ✨ 文件管理
- ✨ 认证系统
- ✨ 活动日志
- 🔒 SHA256文件验证

### v1.0.0
- 基础更新服务器功能

## 许可证

MIT License
