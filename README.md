# Nas.Net - 您的轻量级私有云存储方案

#### 项目介绍
在万物互联的时代，您是否正面临以下困扰？

*   **终端爆炸**：手机、平板、笔记本、台式机、鸿蒙设备越来越多，文件在多设备间传输繁琐，版本混乱？
*   **数据焦虑**：手机相册动辄数万张照片，占用空间巨大，担心丢失却找不到安全的备份方式？
*   **隐私担忧**：公有云网盘限速、广告多，企业数据和个人隐私文件不敢随意上传？
*   **硬件闲置**：家里的旧电脑、闲置主机性能过剩，除了吃灰别无他用，购买专业NAS硬件成本又太高？

#### 解决方案
**Nas.Net** 是一款跨平台的私有云存储软件，致力于为个人、家庭及小团队提供简单、方便、安全的 **私有云存储服务**，其可以直接运行于已有的多种设备上，让您的老旧设备再次焕发新的机会，助您解决多终端时代的数据同步与安全备份难题！

它的核心使命是：**利用闲置设备，构建属于您自己的私有云中心。**

无需购买昂贵的硬件NAS，只需将 Nas.Net 部署在您现有的 Windows 或 Linux 电脑上，即可瞬间将其变身为功能完备的私有云服务器。

#### 核心功能
1. **📱 全终端文件同步**：支持 Android、HarmonyOS、Windows、Linux 等多平台，照片、文档、视频自动同步，随时随地访问。
2. **🔒 私有化部署，数据安全**：所有文件存储在您自己的设备上，远离公有云泄露风险，企业级数据安全由自己掌控。
3. **💻 盘活闲置硬件**：让老旧的台式机、笔记本发挥余热，零成本搭建个人存储中心。
4. **⚡ 轻量高效，基于 .NET**：依托 .NET 跨平台技术，性能优异，资源占用低，运行稳定可靠。

#### 功能特性详解
1. **智能文件同步**
   - 实时双向同步：本地与云端文件自动保持一致
   - 增量同步：只传输变化的部分，节省带宽和时间
   - 断点续传：网络中断后自动恢复，确保大文件传输可靠性
   - 选择性同步：可按需选择同步目录，灵活控制存储空间

2. **安全与隐私**
   - 数据加密：传输过程采用 HTTPS 加密，保护数据安全
   - 权限管理：精细的用户权限控制，确保数据访问安全
   - 本地存储：所有数据存储在您自己的设备上，完全掌控数据主权
   - 访问控制：支持 IP 白名单、登录验证等多重安全机制

3. **便捷管理**
   - 网页管理：通过浏览器随时随地管理文件和用户
   - 终端管理：统一管理所有同步设备，监控同步状态
   - 日志记录：详细的操作日志，便于问题排查和审计
   - 多用户支持：支持多用户注册和使用，适合家庭和小团队

4. **扩展与集成**
   - 自定义存储路径：可将文件存储在任意磁盘分区或外部存储
   - 数据库灵活配置：默认使用 SQLite，可根据需要切换到其他数据库
   - 跨平台支持：服务端支持 Windows、Linux、MacOS，客户端覆盖主流操作系统

#### 关于项目
Nas.Net 基于 **【[Scm.Net](https://gitee.com/openscm/scm.net)】** 项目深度开发，目标是为用户提供一个**简单、稳定、安全、易用**的私有云存储解决方案。

🚀 **软件目前正处于快速迭代完善阶段**，我们非常期待您的反馈。如果您有好的建议或意见，欢迎随时联系我们，一起共建更好的 Nas.Net！

## 环境依赖
### 服务端
- 操作系统：无依赖，Windows、Linux、MacOS等均可直接使用；
- 运行环境：依赖于【Asp.Net Core 运行时】10.0，[下载地址](https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0)；
- 数据库：无依赖，默认使用【SQLite】，可以根据需要调整；

### 客户端
- Windows客户端：依赖于【.NET 桌面运行时】10.0，[下载地址](https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0)；
- Android客户端，无依赖，直接安装即可；

## 系统安装
### 服务端（Windows）
- 安装运行时：直接下载【ASP.NET Core 运行时】，默认安装即可；
- 解压缩文件：将【Nas.Web.zip】文件解压到指定目录（如：D:\Nas）；
- 运行服务端：打开命令行，进入【D:\Nas\Nas.Web】目录，直接运行【.\Scm.Api.exe】即可，或者直接双击【Scm.Api.exe】文件；
- 访问服务端：打开浏览器，访问【http://localhost:9999】即可访问后台。

### 服务端（MacOS）
- 安装运行时：直接下载【ASP.NET Core 运行时】，默认安装即可；
- 解压缩文件：将【Nas.Web.zip】文件解压到指定目录（如：~/Nas）；
- 运行服务端：打开命令行，进入【~/Nas/Nas.Web】目录，直接运行【./Scm.Api.exe】即可；
- 访问服务端：打开浏览器，访问【http://localhost:9999】即可访问后台。

### 服务端（Linux，以Ubuntu为例）
- 安装运行时：直接执行以下命令：
``sudo apt-get update && sudo apt-get install -y aspnetcore-runtime-10.0``
- 解压缩文件：将【Nas.Web.zip】文件解压到指定目录（如：~/Nas）；
- 运行服务端：打开命令行，进入【~/Nas/Nas.Web】目录，直接运行【./Scm.Api.exe】即可；
- 访问服务端：打开浏览器，访问【http://localhost:9999】即可访问后台。

### 客户端（Windows）
- 安装运行时：直接下载【.NET 桌面运行时】，默认安装即可；
- 解压缩文件：将【Nas.Net.zip】文件解压到指定目录（如：D:\Nas\Nas.Net）；
- 运行客户端：直接运行【Nas.Net.exe】文件即可，系统会打开客户端。

### 注意事项
- 服务端和客户端目录建议不要太深，受操作系统的限制，文件路径长度不能超过256个字符；
- 目前仅支持不超过5M大小的文件同步，超大文件支持还有待完善。
- 默认登录用户：admin，默认登录密码：123456，您可以登录后自行修改：

## 演示地址
- **登录地址**：[点击登录](http://www.c-scm.net)
- **管理账号**：
  - 用户名：admin@demo
  - 密码：123456
- **普通账号**：
  - 用户名：user@demo
  - 密码：123456

## 下载地址
[服务端下载](http://www.c-scm.net/download/nas.web.zip)  
[电脑端下载](http://www.c-scm.net/download/nas.net.zip)  
 
## 软件截图
- **环境配置**：  
  - 本地目录
![本地目录](https://gitee.com/openscm/nas.net/raw/master/screenshots/env-01-path.png)  
  - Web端主程序
![Web端主程序](https://gitee.com/openscm/nas.net/raw/master/screenshots/env-02-main.png)  
  - Web端启动成功
![Web端启动成功](https://gitee.com/openscm/nas.net/raw/master/screenshots/env-03-web.png)  
  - 桌面端主程序
![桌面端主程序](https://gitee.com/openscm/nas.net/raw/master/screenshots/env-04-net.png)  
  
- **网页端**：  
  - 用户登录
![用户登录](https://gitee.com/openscm/nas.net/raw/master/screenshots/web-01-login.png)
  - 用户首页
![用户首页](https://gitee.com/openscm/nas.net/raw/master/screenshots/web-02-home.png)
  - 文件管理
![文件管理](https://gitee.com/openscm/nas.net/raw/master/screenshots/web-03-file.png)  
  - 终端管理
![终端管理](https://gitee.com/openscm/nas.net/raw/master/screenshots/web-04-terminal.png)  

- **桌面端**：  
  - 设备绑定
![设备绑定](https://gitee.com/openscm/nas.net/raw/master/screenshots/win-01-bind.png)  
  - 用户首页
![用户首页](https://gitee.com/openscm/nas.net/raw/master/screenshots/win-02-home.png)  
  - 添加同步目录  
![添加同步目录](https://gitee.com/openscm/nas.net/raw/master/screenshots/win-03-folder.png)  
  - 同步目录
![同步目录](https://gitee.com/openscm/nas.net/raw/master/screenshots/win-04-folder.png)  
  - 同步日志
![同步日志](https://gitee.com/openscm/nas.net/raw/master/screenshots/win-05-sync.png)    
  - 本地文件
![本地文件](https://gitee.com/openscm/nas.net/raw/master/screenshots/win-06-native.png)    
  - 远端文件
![远端文件](https://gitee.com/openscm/nas.net/raw/master/screenshots/win-07-remote.png)    
  - 关于软件
![关于软件](https://gitee.com/openscm/nas.net/raw/master/screenshots/win-08-about.png)  

## QQ 交流群
![QQ](https://img.shields.io/badge/QQ-297679499-green.svg?logo=tencent%20qq&logoColor=red)  
加入交流群：297679499

## 许可证
本项目采用 [GNU Lesser General Public License v3.0](LICENSE) 许可证。

## 贡献指南
我们欢迎并感谢所有形式的贡献！如果您想参与项目开发，请遵循以下步骤：

1. **Fork 项目**：在 Gitee 上 fork 【Scm.Net】项目到您自己的账号
2. **克隆仓库**：将 fork 后的仓库克隆到本地
3. **创建分支**：基于 `master` 分支创建新的功能分支
4. **开发功能**：实现您的功能或修复 bug
5. **提交代码**：提交代码并推送到您的 fork 仓库
6. **创建 Pull Request**：向【Scm.Net】项目提交 Pull Request

## 技术架构
### 系统架构
- **服务端**：基于 ASP.NET Core 构建的 Web 服务，提供 RESTful API
- **客户端**：基于各平台原生开发的多平台客户端
- **存储层**：默认使用 SQLite 数据库，文件存储在本地文件系统，您可以根据需要调整为其他数据库（如：MySQL、PostgreSQL）
- **通信协议**：基于 HTTP/HTTPS 的 RESTful API

### 技术栈
- **后端**：C#、ASP.NET Core、SqlSugar
- **前端**：HTML、CSS、JavaScript、Vue
- **移动端**：各平台原生应用
- **数据库**：SQLite（默认）
- **部署**：跨平台支持，可在 Windows、Linux、MacOS 上运行

## 常见问题
### 1. 服务端启动失败怎么办？
- 检查是否安装了正确版本的 .NET 运行时
- 检查端口是否被占用
- 查看日志文件了解详细错误信息

### 2. 客户端无法连接服务端怎么办？
- 确认服务端是否正常运行
- 检查网络连接是否正常
- 确认服务端 IP 地址和端口是否正确
- 检查防火墙是否阻止了连接

### 3. 同步速度慢怎么办？
- 确保网络连接稳定
- 检查设备性能是否足够
- 考虑使用有线网络连接
- 调整同步设置，减少同时同步的文件数量

### 4. 如何备份数据？
- 定期备份存储目录中的文件
- 备份 SQLite 数据库文件
- 考虑使用外部存储设备进行备份

## 版本更新记录
### v1.0.0（2026-02-06）
- ✨ 初始版本发布
- 🔒 实现基本的用户认证和权限管理
- 💾 实现文件同步和管理功能
- 🎨 提供网页管理界面