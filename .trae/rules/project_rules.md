# 新版本同步工作流

当上游 PunishXIV/Questionable 发布新版本时，按以下流程同步到 cn 分支：

## 步骤

### 1. 确保 upstream remote 存在
```bash
git remote add upstream https://github.com/PunishXIV/Questionable.git
```
如果是已有环境可跳过。

### 2. 拉取上游最新
```bash
git fetch upstream
```

### 3. 切换到 cn 分支
```bash
git checkout cn
```

### 4. 合并上游
```bash
git merge upstream/new-main --no-edit
```

### 5. 解决冲突（如有）
按以下原则处理：
- **代码逻辑** → 以上游为准
- **UI中文文本** → 保留cn分支翻译（`_L("中文")` 格式）
- **自动生成文件**（如 packages.lock.json） → 全部采用上游
- **pluginmaster.json** → 版本号与下载链接跟随上游，下载链接加 `-cn` 后缀

解决后：
```bash
git add <冲突文件>
git commit
```

### 6. 更新 pluginmaster.json 版本号
将 `AssemblyVersion`、`DownloadLinkInstall`、`DownloadLinkUpdate` 中的版本号替换为 `Directory.Build.targets` 中 `<Version>` 的值。

下载链接格式：
```
https://github.com/youdianfengbao/Questionable/releases/download/{版本号}-cn/latest.zip
```

### 7. 提交版本号更新
```bash
git add pluginmaster.json
git commit -m "更新 pluginmaster.json 至 v{版本号}"
```

### 8. 推送代码
```bash
git push origin cn
```

### 9. 打标签并推送
```bash
git tag {版本号}-cn
git push origin {版本号}-cn
```

## 注意事项

- 如果本地仓库是浅克隆，需先 `git fetch --unshallow origin cn` 获取完整历史，否则合并会报 `refusing to merge unrelated histories`
- 需要配置 git 用户信息：`git config user.email "517847596@qq.com"` 和 `git config user.name "youdianfengbao"`
- 版本号来源：`Directory.Build.targets` 中的 `<Version>` 字段
- 特殊改动清单详见 `.trae/context.md`
