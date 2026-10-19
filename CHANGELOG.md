# 2025-10-19

- 将 Node.js 版 XGJPlus (https://github.com/Sunwuyuan/XGJPlus) 完整移植到 .NET 10 控制台应用
- 项目结构: `src/XGJPlusCore/`
- 依赖替换: `axios` → HttpClient, `exceljs` → EPPlus 7.5.2, `qrcode-terminal` → QRCoder 1.8.1, `progress` → 自定义 ProgressBar
- 修复 API 兼容性问题: `data.random` 字段为数字类型而非字符串, `getParent` 返回带 `code/data` 包装结构
- 使用半高 Unicode 字符 (▀▄█) + 控制台 UTF-8 输出实现宽高比统一的 ASCII 二维码
- 版本信息 / 缓存登录 / 成绩导出 / 学生信息 / 班级成员 功能全部保留
- 许可证: AGPL-3.0-only (继承原项目)
