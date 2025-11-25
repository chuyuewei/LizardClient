// 全局变量
let currentManifest = null;
let currentChannel = 'stable';

// 页面加载时初始化
document.addEventListener('DOMContentLoaded', function() {
    initializeUpload();
    loadStatistics();
    loadFiles();
    loadManifest();
    
    // 每30秒刷新一次统计
    setInterval(loadStatistics, 30000);
});

// ============ 文件上传 ============

function initializeUpload() {
    const uploadArea = document.getElementById('uploadArea');
    const fileInput = document.getElementById('fileInput');

    // 拖拽上传
    uploadArea.addEventListener('dragover', (e) => {
        e.preventDefault();
        uploadArea.classList.add('drag-over');
    });

    uploadArea.addEventListener('dragleave', () => {
        uploadArea.classList.remove('drag-over');
    });

    uploadArea.addEventListener('drop', (e) => {
        e.preventDefault();
        uploadArea.classList.remove('drag-over');
        
        const files = e.dataTransfer.files;
        if (files.length > 0) {
            uploadFile(files[0]);
        }
    });

    // 点击上传
    fileInput.addEventListener('change', (e) => {
        if (e.target.files.length > 0) {
            uploadFile(e.target.files[0]);
        }
    });
}

async function uploadFile(file) {
    const progressDiv = document.getElementById('uploadProgress');
    const resultDiv = document.getElementById('uploadResult');
    const progressFill = document.getElementById('progressFill');
    const uploadStatus = document.getElementById('uploadStatus');

    // 显示进度条
    progressDiv.style.display = 'block';
    resultDiv.style.display = 'none';
    progressFill.style.width = '0%';
    uploadStatus.textContent = '上传中...';

    const formData = new FormData();
    formData.append('file', file);

    try {
        const xhr = new XMLHttpRequest();

        // 进度监听
        xhr.upload.addEventListener('progress', (e) => {
            if (e.lengthComputable) {
                const percent = (e.loaded / e.total) * 100;
                progressFill.style.width = percent + '%';
                uploadStatus.textContent = `上传中... ${percent.toFixed(1)}%`;
            }
        });

        // 完成监听
        xhr.addEventListener('load', () => {
            if (xhr.status === 200) {
                const response = JSON.parse(xhr.responseText);
                showUploadResult(response);
                loadStatistics();
                loadFiles();
            } else {
                showError('上传失败: ' + xhr.statusText);
            }
            progressDiv.style.display = 'none';
        });

        xhr.addEventListener('error', () => {
            showError('上传失败，请检查网络连接');
            progressDiv.style.display = 'none';
        });

        xhr.open('POST', '/api/upload');
        xhr.send(formData);

    } catch (error) {
        console.error('Upload error:', error);
        showError('上传失败: ' + error.message);
        progressDiv.style.display = 'none';
    }
}

function showUploadResult(data) {
    const resultDiv = document.getElementById('uploadResult');
    resultDiv.style.display = 'block';
    resultDiv.innerHTML = `
        <div class="message-success">
            <strong>✓ 上传成功!</strong><br>
            文件名: ${data.name}<br>
            大小: ${formatBytes(data.size)}<br>
            SHA256: <span class="hash">${data.hash}</span>
        </div>
    `;
}

// ============ 统计数据 ============

async function loadStatistics() {
    try {
        const response = await fetch('/api/statistics');
        const stats = await response.json();

        document.getElementById('totalDownloads').textContent = stats.totalDownloads || 0;
        document.getElementById('totalFiles').textContent = stats.totalFiles || 0;
        document.getElementById('storageUsage').textContent = formatBytes(stats.storageUsage || 0);
        document.getElementById('lastUpdate').textContent = stats.lastUpdate ? 
            new Date(stats.lastUpdate).toLocaleString('zh-CN') : '-';

        // 更新活动日志
        updateActivityLog(stats.recentActivities || []);

    } catch (error) {
        console.error('Failed to load statistics:', error);
    }
}

function updateActivityLog(activities) {
    const logDiv = document.getElementById('activityLog');
    
    if (activities.length === 0) {
        logDiv.innerHTML = '<p class="loading">暂无活动记录</p>';
        return;
    }

    logDiv.innerHTML = activities.slice(0, 10).map(activity => `
        <div class="activity-item">
            <div class="activity-time">${new Date(activity.timestamp).toLocaleString('zh-CN')}</div>
            <div>
                <span class="activity-action">${activity.action}</span>
                <span class="activity-details">${activity.details}</span>
            </div>
        </div>
    `).join('');
}

// ============ 清单编辑 ============

async function loadManifest() {
    const channel = document.getElementById('channelSelect').value;
    currentChannel = channel;

    try {
        const response = await fetch(`/manifest-${channel}.json`);
        const manifest = await response.json();
        currentManifest = manifest;

        document.getElementById('manifestEditor').value = JSON.stringify(manifest, null, 2);
    } catch (error) {
        console.error('Failed to load manifest:', error);
        showError('加载清单失败: ' + error.message);
    }
}

async function saveManifest() {
    const channel = document.getElementById('channelSelect').value;
    const editorContent = document.getElementById('manifestEditor').value;

    try {
        const manifest = JSON.parse(editorContent);

        const response = await fetch(`/api/manifests/${channel}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: editorContent
        });

        if (response.ok) {
            showSuccess('清单已保存');
            currentManifest = manifest;
            loadStatistics();
        } else {
            showError('保存失败');
        }
    } catch (error) {
        showError('JSON 格式错误: ' + error.message);
    }
}

// ============ 文件管理 ============

async function loadFiles() {
    const listDiv = document.getElementById('fileList');
    listDiv.innerHTML = '<p class="loading">加载中...</p>';

    try {
        const response = await fetch('/api/files');
        const files = await response.json();

        if (files.length === 0) {
            listDiv.innerHTML = '<p class="loading">暂无文件</p>';
            return;
        }

        listDiv.innerHTML = files.map(file => `
            <div class="file-item">
                <div class="file-info">
                    <div class="file-name">📄 ${file.name}</div>
                    <div class="file-meta">
                        大小: ${formatBytes(file.size)} | 
                        修改: ${new Date(file.modified).toLocaleString('zh-CN')}
                    </div>
                    ${file.hash ? `<div class="file-hash">SHA256: ${file.hash}</div>` : ''}
                </div>
                <div class="file-actions">
                    <button class="btn btn-secondary" onclick="copyHash('${file.hash}')">复制哈希</button>
                    <button class="btn btn-danger" onclick="deleteFile('${file.name}')">删除</button>
                </div>
            </div>
        `).join('');
    } catch (error) {
        console.error('Failed to load files:', error);
        listDiv.innerHTML = '<p class="loading">加载失败</p>';
    }
}

async function deleteFile(filename) {
    if (!confirm(`确定要删除文件 "${filename}" 吗？`)) {
        return;
    }

    try {
        const response = await fetch(`/api/files/${filename}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            showSuccess('文件已删除');
            loadFiles();
            loadStatistics();
        } else {
            showError('删除失败');
        }
    } catch (error) {
        showError('删除失败: ' + error.message);
    }
}

function copyHash(hash) {
    navigator.clipboard.writeText(hash).then(() => {
        showSuccess('哈希值已复制到剪贴板');
    }).catch(() => {
        showError('复制失败');
    });
}

// ============ 工具函数 ============

function formatBytes(bytes) {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return (bytes / Math.pow(k, i)).toFixed(2) + ' ' + sizes[i];
}

function showSuccess(message) {
    showMessage(message, 'success');
}

function showError(message) {
    showMessage(message, 'error');
}

function showMessage(message, type) {
    const messageDiv = document.createElement('div');
    messageDiv.className = `message message-${type}`;
    messageDiv.textContent = message;
    
    document.querySelector('.container').insertBefore(
        messageDiv, 
        document.querySelector('.header').nextSibling
    );

    setTimeout(() => {
        messageDiv.remove();
    }, 5000);
}

// 监听频道切换
document.getElementById('channelSelect').addEventListener('change', loadManifest);
