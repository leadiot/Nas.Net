# Nas.Net

## 项目介绍
您是否有这样的问题： 
- 手机设备更新换代越来越快，一些重要的文件无法进行多设备之间的同步？ 
- 手机设备上的照片越来越多，却找不到一个很好的备份方案？ 
- 一些企业网盘虽然能满足文件的备份，但是一些重要资料和私密资料却不能放置于公网？ 
- 家里的台式机、笔记本等电脑设备长时间空闲，得不到很好的利用？ 
 
Nas.Net就是这样一款针对个人、家庭及小团队的私有云存储软件，它可以直接运行于已有的多种设备上（台式机、笔记本等），让您的老旧成为您的个云存储中心，同时将您多个设备上的重要文件进行同步管理，不再为重要文件的备份担忧。

Nas.Net基于【Scm.Net】项目开发，它的目标是为用户提供一个简单、方便、安全的私有云存储解决方案。软件目前还在快速完善中，如果您有好的建议或意见，欢迎联系我们。  

## 软件特色
- 致力于充分利用已有的设备资源（台式机、笔记本等）；
- 服务端支持Windows、Linux、MacOS等平台；
- 客户端支持Windows、Linux、MacOS、Android、Hamonary等平台；
- 后端支持多种插件，用于为服务端提供更加多样化的能力；
- 前端支持多种扩展，用于为用户端提供更加个性化的服务。

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
- 解压缩文件：解压缩下载的文件，将【Nas.Web.zip】文件解压到指定目录（如：D:\Nas）；
- 运行服务端：打开命令行，进入【D:\Nas\Nas.Web】目录，直接运行【.\Scm.Api.exe】即可，或者直接双击【Scm.Api.exe】文件；
- 访问服务端：打开浏览器，访问【http://localhost】即可访问后台。

### 服务端（MacOS）
- 安装运行时：直接下载【ASP.NET Core 运行时】，默认安装即可；
- 解压缩文件：解压缩下载的文件，将【Nas.Web.zip】文件解压到指定目录（如：~/Nas）；
- 运行服务端：打开命令行，进入【~/Nas/Nas.Web】目录，直接运行【./Scm.Api.exe】即可；
- 访问服务端：打开浏览器，访问【http://localhost】即可访问后台。

### 服务端（Linux，以Ubuntu为例）
- 安装运行时：直接执行以下命令：
``sudo apt-get update && sudo apt-get install -y aspnetcore-runtime-10.0``
- 解压缩文件：解压缩下载的文件，将【Nas.Web.zip】文件解压到指定目录（如：~/Nas）；
- 运行服务端：打开命令行，进入【~/Nas/Nas.Web】目录，直接运行【./Scm.Api.exe】即可；
- 访问服务端：打开浏览器，访问【http://localhost】即可访问后台。

### 客户端（Windows）
- 安装运行时：直接下载【.NET 桌面运行时】，默认安装即可；
- 解压缩文件：解压缩下载的文件，将【Nas.Net.zip】文件解压到指定目录（如：D:\Nas\Nas.Net）；
- 运行客户端：直接运行【Nas.Net.exe】文件即可，系统会打开客户端。

### 注意事项
- 服务端和客户端目录建议不要太深，受操作系统的限制，文件路径长度不能超过256个字符；
- 目前仅支持不超过5M大小的文件同步，超大文件支持还有待完善。

## 演示地址
- **登录地址**：[点击登录](http://www.c-scm.net)
- **管理账号**：
  - 用户名：admin@demo
  - 密码：123456
- **普通账号**：
  - 用户名：user@demo
  - 密码：123456

## 下载地址
[服务端下载](https://www.c-scm.net/data/nas.web.zip)  
[电脑端下载](https://www.c-scm.net/data/nas.net.zip)  
 
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