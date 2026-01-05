# Scm.Net

## 项目介绍
Scm.Net 是一款基于 **.Net 10.0** 及 **Vue 3.0** 构建的企业中后台快速开发框架。该框架适用于多种异构应用场景，旨在帮助开发者快速搭建系统。目前已用于开发多个企业级应用，包括 **OMS**（订单管理系统）、**WMS**（仓储管理系统）、**TMS**（运输管理系统）、**DMS**（配送管理系统）、**BMS**（计费管理系统）、**YMS**（园区管理系统）、**EAM**（资产管理系统）和 **IOT**（物联网管理系统）等。

## 软件架构
- **前后端分离架构**：前端采用 **Vue 3.0**，后端基于 **.Net 10.0**。
- **兼容性强**：支持多平台（Windows、MacOS、Linux、HarmonyOS），并兼容多种设备终端（电脑、平板、手机）。
- **依赖精简**：
  - 后端：**SqlSugarCore**（ORM 工具）、**Newtonsoft.Json**（JSON 处理）、**ImageSharp**（图像处理）。
  - 前端：**Element Plus**（UI 组件库）、**Axios**（HTTP 请求库）。
- **响应式布局**：支持多设备访问。

## 设计原则
- 数据库仅用于**数据存储**，不依赖特定数据库特性，确保可平滑迁移。
- 数据库操作以**单表为主**，允许有限冗余以提升性能。
- 数据交互基于**JSON**，提升扩展性和兼容性。
- **DTO**（数据传输对象）统一采用**蛇形命名法**，便于维护。

## 主要功能
1. **多登录方式**：支持账户、手机、邮件、三方登录。
2. **多数据库引擎**：支持 MySQL、SQL Server、Oracle、SQLite、MariaDB、PostgreSQL、Firebird、MongoDB。
3. **缓存机制**：支持 MemoryCache、Redis、Map 等。
4. **日志管理**：记录登录日志、操作日志，并保存用户环境信息（如浏览器、操作系统、IP等）。
5. **大模型集成**：支持 DeepSeek、华为盘古、阿里通义千问、腾讯元宝、百度文心一言、豆包、ChatGPT 等。
6. **代码自动生成**：支持自定义模板，快速生成基础代码。
7. **ID生成器**：支持雪花ID、序列ID、格式ID等多种方式。
8. **权限管理**：支持多级权限（公司、部门、岗位、用户、角色）。
9. **全局配置与数据字典**：统一管理全局参数和字典。
10. **用户留言与实时反馈**：支持用户互动。
11. **自定义审批流程**：灵活配置审批规则。

## 项目特色
- **模块化设计**：功能模块可按需引入。
- **前后端分离**：前端与后端可部署在不同域名下。
- **无需二次开发**：后台系统可直接发布使用。
- **多租户支持**：可扩展为多组织架构系统。
- **完善示例**：提供详细的文档和操作说明。

## 浏览器兼容性
| 平台        | Chrome | Firefox | Safari | Android Browser | Edge |
|-------------|--------|---------|--------|-----------------|------|
| **iOS**     | ✅ 支持 | ✅ 支持  | ✅ 支持 | N/A             | ✅ 支持 |
| **Android** | ✅ 支持 | ✅ 支持  | N/A    | ✅ Android v5.0+ | ✅ 支持 |
| **Windows** | ✅ 支持 | ✅ 支持  | ✅ 支持 | ✅ 支持           | ✅ 支持 |
| **MacOS**   | ✅ 支持 | ✅ 支持  | ✅ 支持 | N/A             | N/A   |
| **Linux**   | ✅ 支持 | ✅ 支持  | ✅ 支持 | N/A             | N/A   |

## 开发环境搭建
1. 安装 [.NET SDK](https://dotnet.microsoft.com)（10.0 或更高）。
2. 安装 [Visual Studio](https://visualstudio.microsoft.com)（2026 或更高）。
3. 安装 [MariaDB](https://mariadb.org)（10.3 或更高）。
4. 获取项目代码：[Scm.NET](https://gitee.com/openscm/scm.net)
5. 项目文档：[环境搭建教程](https://gitee.com/openscm/scm.net/wikis/环境搭建教程)、[数据库配置](https://gitee.com/openscm/scm.net/wikis/数据库配置)

## 演示地址
- **登录地址**：[点击登录](http://www.c-scm.net)
- **管理账号**：
  - 用户名：admin@demo
  - 密码：123456
- **普通账号**：
  - 用户名：user@demo
  - 密码：123456

## 开源协议
该项目采用开源协议，请查看：[LICENSE](https://gitee.com/openscm/scm.net/blob/master/LICENSE)

## 贡献代码
欢迎有兴趣的开发者参与项目开发，共同完善框架。贡献方式：
- Fork 项目。
- 创建新分支。
- 提交 Pull Request。

## 项目截图
- **PC 端**：
  - [后台首页](https://gitee.com/openscm/scm.net/raw/master/screenshots/pc-home.png)
  - [接口日志](https://gitee.com/openscm/scm.net/raw/master/screenshots/pc-logapi.png)
  - [在线SQL](https://gitee.com/openscm/scm.net/raw/master/screenshots/pc-sql.png)
  - [任务调度](https://gitee.com/openscm/scm.net/raw/master/screenshots/pc-task.png)
  - [系统监控](https://gitee.com/openscm/scm.net/raw/master/screenshots/pc-monitor.png)
- **移动端**：
  - [用户登录](https://gitee.com/openscm/scm.net/raw/master/screenshots/mp-login.jpg)
  - [用户首页](https://gitee.com/openscm/scm.net/raw/master/screenshots/mp-home.jpg)
  - [系统菜单](https://gitee.com/openscm/scm.net/raw/master/screenshots/mp-menu.jpg)

## 特别鸣谢
- **[SqlSugar](https://gitee.com/dotnetchina/SqlSugar)**
- **[Panda.DynamicWebApi](https://gitee.com/mirrors/Panda.DynamicWebApi)**

## QQ 交流群
![QQ](https://img.shields.io/badge/QQ-415872667-green.svg?logo=tencent%20qq&logoColor=red)  
加入交流群：415872667

## 支持作者
如果本项目对您有帮助，请支持作者以鼓励更多开源贡献：

![微信支付](https://gitee.com/openscm/scm.net/raw/master/wepay.jpg)