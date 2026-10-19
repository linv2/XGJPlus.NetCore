using System.Security.Cryptography;
using System.Text;
using QRCoder;

namespace XGJPlus;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine(@"
========================================
班级小管家工具 (XGJPlus .NET Core 版)

孙悟元 https://wuyuan.dev
项目使用AGPL-3.0-only协议开源，
开源地址：https://github.com/Sunwuyuan/XGJPlus
Copyright (C) 2025-2025 SunWuyuan <https://github.com/Sunwuyuan>.

========================================
");

        /*

        项目使用AGPL-3.0-only协议开源，因此，
        如需署名请添加新行，请勿删除或修改上述内容。
        请勿删除或修改这里的注释。

        */

        ExcelGenerator.SetupLicense();

        var api = new ApiClient();

        // Fetch version info before anything else
        await api.FetchVersionInfoAsync();

        // Load cache and check if we have a valid token
        var cache = CacheManager.LoadCache();
        if (CacheManager.IsTokenValid(cache))
        {
            Console.WriteLine("从缓存中获取到可用令牌");
            api.JwtToken = cache!.JwtToken;
            api.Imprint = cache.Imprint;

            try
            {
                // Test if the cached token is still valid
                await api.GetUserChildInfoAsync();
                Console.WriteLine("缓存中的令牌可用（如需退出请删除cache.json）");
            }
            catch
            {
                Console.WriteLine("缓存中的令牌已失效，重新登录...");
                api.JwtToken = "";
                api.Imprint = "";
            }
        }

        if (string.IsNullOrEmpty(api.JwtToken))
        {
            await LoginAsync(api);
        }

        // 成绩接口的 imprint 使用当前登录用户自己的 openid
        var tokenOpenid = GetOpenidFromJwt(api.JwtToken);
        if (!string.IsNullOrEmpty(tokenOpenid))
        {
            api.Imprint = tokenOpenid;
            Console.WriteLine("已从登录令牌解析到当前用户 openid（用作成绩接口的 imprint）");
        }
        else
        {
            Console.WriteLine("未能从登录令牌解析 openid，成绩接口将退回使用条目创建者的 openid");
        }

        var childList = await api.GetUserChildInfoAsync();
        var memberIds = childList.Select(c => c.MemberId).ToList();
        var parentInfo = await api.GetParentInfoAsync(memberIds);
        var classInfo = await api.GetClassByMemberIdAsync(memberIds);

        var classMap = new Dictionary<string, string>();
        foreach (var cls in classInfo)
        {
            classMap[cls.Id] = cls.ClassName;
        }

        Console.WriteLine("请选择要处理的数据：");
        Console.WriteLine("0. 处理所有班级成员信息");
        for (int i = 0; i < parentInfo.Count; i++)
        {
            var info = parentInfo[i];
            var className = classMap.GetValueOrDefault(info.Cls, "未知班级");
            var typeDesc = info.Type switch
            {
                4 => "成绩单",
                15 => "学生信息",
                _ => $"类型{info.Type} 暂不支持"
            };
            Console.WriteLine($"{i + 1}. {info.Title} ({className}) - {info.CreatorWxName} [{typeDesc}]");
        }

        Console.Write("请输入选择的编号：");
        var selectedIndexStr = Console.ReadLine()?.Trim();
        int selectedIndex = int.TryParse(selectedIndexStr, out var idx) ? idx : -1;

        if (selectedIndex == 0)
        {
            // 处理所有班级信息
            Console.WriteLine("开始处理所有班级信息...");
            foreach (var (cls, className) in classMap)
            {
                Console.WriteLine($"正在处理班级: {className}");
                var membersData = await api.GetClassMembersAsync(cls);
                var processedData = ProcessClassMemberData(membersData, classMap, cls);
                if (processedData != null)
                {
                    await ExcelGenerator.GenerateExcelAsync(processedData);
                }
            }
            Console.WriteLine("所有班级处理完成！");
        }
        else if (selectedIndex > 0 && selectedIndex <= parentInfo.Count)
        {
            var selectedInfo = parentInfo[selectedIndex - 1];
            if (string.IsNullOrEmpty(api.Imprint))
            {
                api.Imprint = selectedInfo.CreatorWxOpenid;
            }
            var processedData = await ProcessDataByTypeAsync(api, selectedInfo, classMap);
            if (processedData != null)
            {
                await ExcelGenerator.GenerateExcelAsync(processedData);
            }
        }
        else
        {
            Console.WriteLine("无效的选择");
        }
    }

    // 登录流程
    static async Task LoginAsync(ApiClient api)
    {
        var qrCodeData = await api.GetQrCodeAsync();
        if (qrCodeData.Data == null)
        {
            Console.Error.WriteLine("获取二维码失败");
            return;
        }

        var ticket = qrCodeData.Data.Ticket;
        var random = qrCodeData.Data.RandomStr;
        var qrUrl = $"https://mp.weixin.qq.com/cgi-bin/showqrcode?ticket={ticket}";

        Console.WriteLine("请扫描以下二维码登录：");
        Console.WriteLine($"或者在浏览器中打开以下链接：{qrUrl}");
        Console.WriteLine();

        // 生成终端二维码
        GenerateAsciiQr(qrUrl);

        var loginStatus = new LoginStatusResponse { Code = 0, Msg = "未扫码" };

        while (loginStatus.Code != 1)
        {
            Console.Write("\n按回车键尝试登录...");
            Console.ReadLine();
            loginStatus = await api.CheckLoginStatusAsync(random);
            if (loginStatus.Code == 1)
            {
                Console.WriteLine("登录成功！");
                api.JwtToken = loginStatus.Data ?? "";
                // Save cache after successful login
                CacheManager.SaveCache(new CacheData
                {
                    JwtToken = api.JwtToken,
                    Imprint = api.Imprint,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
            }
            else if (loginStatus.Msg != "未扫码")
            {
                Console.Error.WriteLine("登录失败：" + loginStatus.Msg);
                Environment.Exit(1);
            }
            else
            {
                Console.WriteLine("等待扫码...");
            }
        }
    }

    // 使用 QRCoder 生成宽高统一的 ASCII 二维码
    static void GenerateAsciiQr(string url)
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            var size = qrCodeData.ModuleMatrix.Count;
            var modules = qrCodeData.ModuleMatrix;

            // 用两行像素合并为一行，用半高块字符实现宽高比统一
            for (int row = 0; row < size; row += 2)
            {
                var line = new System.Text.StringBuilder();
                for (int col = 0; col < size; col++)
                {
                    bool top = modules[row][col];
                    bool bottom = row + 1 < size && modules[row + 1][col];
                    line.Append(top switch
                    {
                        true when bottom => "█",
                        true => "▀",
                        false when bottom => "▄",
                        _ => " "
                    });
                }
                Console.WriteLine(line);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[二维码生成失败: {ex.Message}]");
        }
    }

    // 从 JWT 解析 openid
    static string GetOpenidFromJwt(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return "";

            var payload = parts[1];
            // Base64URL → Base64
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("openid", out var openidEl) ? openidEl.GetString() ?? "" : "";
        }
        catch
        {
            return "";
        }
    }

    // 按类型处理数据
    static async Task<ExcelGenerator.ProcessedData?> ProcessDataByTypeAsync(ApiClient api, ParentInfo data, Dictionary<string, string> classMap)
    {
        if (data.Type == 0)
        {
            var membersData = await api.GetClassMembersAsync(data.Cls);
            return ProcessClassMemberData(membersData, classMap, data.Cls);
        }

        return data.Type switch
        {
            4 => await ProcessScoreDataAsync(api, data, classMap),
            15 => ProcessStudentInfoData(data, classMap),
            _ => null
        };
    }

    // 处理成绩数据
    static async Task<ExcelGenerator.ProcessedData?> ProcessScoreDataAsync(ApiClient api, ParentInfo data, Dictionary<string, string> classMap)
    {
        var studentNames = await api.GetClassByIdAsync(data.Cls);
        var results = new List<(string Name, List<ScoreDetail> ScoreDetail)>();
        var pb = new ProgressBar(studentNames.Count);

        foreach (var name in studentNames)
        {
            pb.Tick();
            var scoreDetail = await api.GetStudentScoreAsync(name, data.Score);
            if (scoreDetail != null && scoreDetail.Count > 0)
            {
                results.Add((name, scoreDetail));
                Console.WriteLine($"\r {results.Count}号 {name} - {scoreDetail.Count}科目成绩获取成功");
            }
            else if (scoreDetail != null)
            {
                results.Add((name, new List<ScoreDetail> { new() { Subject = "缺考", Score = "缺考" } }));
                Console.WriteLine($"\r {results.Count}号 {name} - 缺考");
            }
            else
            {
                results.Add((name, new List<ScoreDetail> { new() { Subject = "错误", Score = "错误" } }));
                Console.WriteLine($"\r {results.Count}号 {name} - 获取成绩失败");
            }
        }

        // 收集所有科目
        var subjects = results
            .SelectMany(r => r.ScoreDetail.Select(d => d.Subject))
            .Distinct()
            .ToList();

        var columns = new List<string> { "姓名" };
        columns.AddRange(subjects);

        var rows = new List<Dictionary<string, string>>();
        foreach (var (name, scoreDetail) in results)
        {
            var row = new Dictionary<string, string> { ["姓名"] = name };
            foreach (var detail in scoreDetail)
            {
                row[detail.Subject] = detail.Score;
            }
            rows.Add(row);
        }

        return new ExcelGenerator.ProcessedData
        {
            Type = "score",
            Rows = rows,
            Columns = columns,
            FileName = $"{data.Title}_{classMap.GetValueOrDefault(data.Cls, "")}_{data.CreatorWxName}_{results.Count}人"
        };
    }

    // 处理学生信息数据
    static ExcelGenerator.ProcessedData? ProcessStudentInfoData(ParentInfo data, Dictionary<string, string> classMap)
    {
        if (data.Attach == null || data.Attach.Headers.Count == 0 || data.Attach.Detail.Count == 0)
        {
            Console.WriteLine("数据格式不正确，缺少必要的信息");
            return null;
        }

        var headers = new List<string> { "姓名" };
        headers.AddRange(data.Attach.Headers);

        var rows = new List<Dictionary<string, string>>();
        foreach (var student in data.Attach.Detail)
        {
            var row = new Dictionary<string, string> { ["姓名"] = student.Name };
            for (int i = 0; i < data.Attach.Headers.Count; i++)
            {
                row[data.Attach.Headers[i]] = i < student.Infos.Count ? student.Infos[i].Latest : "";
            }
            rows.Add(row);
        }

        return new ExcelGenerator.ProcessedData
        {
            Type = "studentInfo",
            Rows = rows,
            Columns = headers,
            FileName = $"{data.Title}_{classMap.GetValueOrDefault(data.Cls, "")}_{data.CreatorWxName}_{rows.Count}人"
        };
    }

    // 处理班级成员数据
    static ExcelGenerator.ProcessedData? ProcessClassMemberData(ClassMembersResponse data, Dictionary<string, string> classMap, string currentCls)
    {
        var members = data.Data?.Members ?? new List<Member>();

        var headers = new List<string> { "姓名", "电话", "微信昵称", "微信头像", "身份" };
        var memberData = new List<Dictionary<string, string>>();

        var teachers = members.Where(m => m.TeachRole != -1).ToList();
        var students = members.Where(m => m.TeachRole == -1).ToList();

        // 处理老师数据
        foreach (var teacher in teachers)
        {
            var teacherIdentity = string.IsNullOrEmpty(teacher.TeachRoleStr) ? "老师" : $"{teacher.TeachRoleStr}老师";
            memberData.Add(new Dictionary<string, string>
            {
                ["姓名"] = teacher.Name ?? "",
                ["电话"] = teacher.Phone ?? "",
                ["微信昵称"] = teacher.WxName ?? "",
                ["微信头像"] = teacher.WxAvatar ?? "",
                ["身份"] = teacherIdentity
            });

            foreach (var familyMember in teacher.Family)
            {
                memberData.Add(new Dictionary<string, string>
                {
                    ["姓名"] = "",
                    ["电话"] = familyMember.Phone ?? "",
                    ["微信昵称"] = familyMember.WxName ?? "",
                    ["微信头像"] = familyMember.WxAvatar ?? "",
                    ["身份"] = $"{teacher.Name}的家长"
                });
            }
        }

        // 处理学生数据
        foreach (var student in students)
        {
            memberData.Add(new Dictionary<string, string>
            {
                ["姓名"] = student.Name ?? "",
                ["电话"] = student.Phone ?? "",
                ["微信昵称"] = student.WxName ?? "",
                ["微信头像"] = student.WxAvatar ?? "",
                ["身份"] = "学生"
            });

            foreach (var familyMember in student.Family)
            {
                memberData.Add(new Dictionary<string, string>
                {
                    ["姓名"] = "",
                    ["电话"] = familyMember.Phone ?? "",
                    ["微信昵称"] = familyMember.WxName ?? "",
                    ["微信头像"] = familyMember.WxAvatar ?? "",
                    ["身份"] = $"{student.Name}的家长"
                });
            }
        }

        return new ExcelGenerator.ProcessedData
        {
            Type = "classMember",
            Rows = memberData,
            Columns = headers,
            FileName = $"班级成员信息_{classMap.GetValueOrDefault(currentCls, "")}_{members.Count}人"
        };
    }
}
