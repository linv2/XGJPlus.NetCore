# XGJPlus (.NET Core 版)

班级小管家工具 —— 从多个接口获取班级学生信息、成绩等数据，并生成 Excel 文件。

## 项目由来

本项目是 Node.js 版 [XGJPlus](https://github.com/Sunwuyuan/XGJPlus) 的 .NET Core 移植版本。

- **原项目作者**: Sunwuyuan
- **原项目地址**: https://github.com/Sunwuyuan/XGJPlus
- **原项目许可证**: AGPL-3.0-only
- **源码**: TypeScript/Node.js（ESM）
- **本版本**: C# / .NET 10 控制台应用
- **移植说明**: 保留原项目的功能逻辑、API 调用流程和许可证约束，仅将技术栈从 Node.js 转换为 .NET。

## AI 辅助说明

本项目的代码转换工作由 **Seed-Code** AI 模型（Trae IDE 内置）在人工指导下完成，包括：

- Node.js → C# 的逐文件逻辑移植
- API 返回结构调试与兼容性修复
- EPPlus / QRCoder 等 .NET 包选型与集成
- ASCII 二维码宽高比统一问题的半高字符方案
- `dotnet new` 项目脚手架搭建与 Release 构建验证

### 依赖对照

| Node.js 包 | .NET 替代 |
|---|---|
| `axios` | `HttpClient` (内置) |
| `exceljs` | `EPPlus` |
| `qrcode-terminal` | `QRCoder` |
| `progress` | 自定义 `ProgressBar` |
| `readline` | `Console.ReadLine()` |
| `user-agents` | 内置随机 UA 列表 |
| `iconv-lite` | 不需要（.NET 编码支持更好） |

## 系统要求

- .NET 10 SDK 或更高版本

## 运行

```bash
dotnet run --project src/XGJPlusCore
```

首次运行会生成 `cache.json` 缓存登录令牌，24 小时内再次运行无需重新扫码。

## 构建

```bash
dotnet build src/XGJPlusCore/XGJPlus.csproj -c Release
```

## 功能

- 二维码扫码登录班级小管家
- 获取孩子关联的所有班级和家长信息
- 选择导出：
  - **所有班级成员信息**（老师 / 学生 / 家庭成员）
  - **成绩单**（逐个获取每位学生的各科成绩）
  - **学生信息表**
- 生成 `.xlsx` Excel 文件到当前目录

## 目录结构

```
src/XGJPlusCore/
├── XGJPlus.csproj       # 项目文件
├── Program.cs           # 主入口、登录流程、数据处理调度
├── ApiClient.cs         # 所有 HTTP API 调用
├── CacheManager.cs      # cache.json 读写
├── ExcelGenerator.cs    # Excel 导出（EPPlus）
├── ProgressBar.cs       # 控制台进度条
└── Models.cs            # API 请求/响应数据模型
```

## 许可证

本项目继承原项目 [AGPL-3.0-only](https://www.gnu.org/licenses/agpl-3.0.html) 许可证。
