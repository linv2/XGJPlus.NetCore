using System.Text;
using System.Text.Json;

namespace XGJPlus;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private BrowserInfo _browserInfo;
    private VersionInfo? _versionInfo;
    private RequestConfig? _requestConfig;
    public string JwtToken { get; set; } = "";
    public string Imprint { get; set; } = "";

    public ApiClient()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _browserInfo = GenerateBrowserInfo();
    }

    private static readonly string[] UserAgents =
    [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 16_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.6 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (Linux; Android 13; SM-S918B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_1) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Safari/605.1.15"
    ];

    private static readonly Random _random = new();

    private BrowserInfo GenerateBrowserInfo()
    {
        var ua = UserAgents[_random.Next(UserAgents.Length)];
        var isMobile = ua.Contains("Mobile") || ua.Contains("iPhone") || ua.Contains("Android");
        var platform = ua.Contains("iPhone") || ua.Contains("Android") ? "\"iOS\"" : "\"Windows\"";
        return new BrowserInfo
        {
            UserAgent = ua,
            SecChUa = "\"Not A(Brand\";v=\"99\", \"Google Chrome\";v=\"120\"",
            SecChUaPlatform = platform,
            SecChUaMobile = isMobile ? "?1" : "?0"
        };
    }

    private Dictionary<string, string> GetHeaders(bool withAuth = true)
    {
        var headers = new Dictionary<string, string>
        {
            { "accept", "*/*" },
            { "accept-language", "zh-CN,zh;q=0.9,en;q=0.8" },
            { "app-info", _versionInfo != null ? $"1/{_versionInfo.Version}/{_versionInfo.BuildNumber}" : "1/3.3.7/814" },
            { "cache-control", "no-cache" },
            { "content-type", "application/json; charset=utf-8" },
            { "sec-ch-ua", _browserInfo.SecChUa },
            { "sec-ch-ua-mobile", _browserInfo.SecChUaMobile },
            { "sec-ch-ua-platform", _browserInfo.SecChUaPlatform },
            { "sec-fetch-dest", "empty" },
            { "sec-fetch-mode", "cors" },
            { "sec-fetch-site", "cross-site" },
            { "user-agent", _browserInfo.UserAgent },
            { "referer", "https://service.banjixiaoguanjia.com/" },
            { "Referrer-Policy", "strict-origin-when-cross-origin" }
        };

        if (withAuth && !string.IsNullOrEmpty(JwtToken))
        {
            headers["authorization"] = JwtToken;
        }

        return headers;
    }

    private async Task<string> GetAsync(string url, Dictionary<string, string>? headers = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        foreach (var h in headers ?? GetHeaders())
        {
            request.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> PostAsync(string url, object body, Dictionary<string, string>? headers = null)
    {
        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        foreach (var h in headers ?? GetHeaders())
        {
            request.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    // 获取版本信息
    public async Task<VersionInfo> FetchVersionInfoAsync()
    {
        try
        {
            var json = await GetAsync("https://service.banjixiaoguanjia.com/appweb/version.json", GetHeaders(false));
            _versionInfo = JsonSerializer.Deserialize<VersionInfo>(json) ?? new VersionInfo { Version = "3.3.7", BuildNumber = "814" };
            Console.WriteLine($"请求版本信息成功：1/{_versionInfo.Version}/{_versionInfo.BuildNumber}");
        }
        catch
        {
            _versionInfo = new VersionInfo { Version = "3.3.7", BuildNumber = "814" };
            Console.WriteLine($"请求版本信息失败，使用默认值：1/3.3.7/814");
        }

        _requestConfig = new RequestConfig
        {
            Channel = "app_web",
            Platform = "app",
            AppInfo = new AppInfo { AppVcode = _versionInfo.BuildNumber, AppVname = _versionInfo.Version },
            DeviceInfo = new DeviceInfo { NetworkType = "WiFi" }
        };

        Console.WriteLine($"使用随机浏览器信息：{_browserInfo.UserAgent}");
        return _versionInfo;
    }

    // 获取二维码
    public async Task<QrCodeResponse> GetQrCodeAsync()
    {
        var json = await PostAsync("https://b.welife001.com/app/auth/getQrCodeImg", _requestConfig!, GetHeaders(false));
        return JsonSerializer.Deserialize<QrCodeResponse>(json) ?? new QrCodeResponse();
    }

    // 检查登录状态
    public async Task<LoginStatusResponse> CheckLoginStatusAsync(string random)
    {
        var body = new { random, channel = _requestConfig!.Channel, platform = _requestConfig.Platform, app_info = _requestConfig.AppInfo, device_info = _requestConfig.DeviceInfo };
        var json = await PostAsync("https://b.welife001.com/app/auth/checkLoginStatusWithToken", body, GetHeaders(false));
        return JsonSerializer.Deserialize<LoginStatusResponse>(json) ?? new LoginStatusResponse();
    }

    // 获取用户子儿信息
    public async Task<List<ChildClass>> GetUserChildInfoAsync()
    {
        var json = await PostAsync("https://b.welife001.com/applet/getUser", new { });
        var response = JsonSerializer.Deserialize<UserResponse>(json);
        return response?.Data?.CurrentUser?.ChildClassList ?? new List<ChildClass>();
    }

    // 获取家长信息
    public async Task<List<ParentInfo>> GetParentInfoAsync(List<string> memberIds)
    {
        var allData = new List<ParentInfo>();
        int page = 0;
        bool hasMoreData = true;

        while (hasMoreData)
        {
            var members = string.Join("%3A", memberIds);
            var url = $"https://b.welife001.com/info/getParent?members={members}&type=-1&date=-1&page={page}&size=20&isRecent=false";
            var json = await GetAsync(url);

            // API 返回格式: { "code": 0, "data": [...] }
            using var doc = JsonDocument.Parse(json);
            var pageData = doc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == System.Text.Json.JsonValueKind.Array
                ? JsonSerializer.Deserialize<List<ParentInfo>>(dataEl.GetRawText())
                : null;

            if (pageData == null || pageData.Count == 0)
            {
                hasMoreData = false;
            }
            else
            {
                allData.AddRange(pageData);
                page++;
            }
        }

        return allData;
    }

    // 根据成员ID获取班级
    public async Task<List<ClassInfo>> GetClassByMemberIdAsync(List<string> memberIds)
    {
        var json = await PostAsync("https://b.welife001.com/applet/getClassByMemberId", new { member_ids = memberIds });
        var response = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        if (response != null && response.TryGetValue("data", out var data))
        {
            var dataJson = JsonSerializer.Serialize(data);
            return JsonSerializer.Deserialize<List<ClassInfo>>(dataJson) ?? new List<ClassInfo>();
        }
        return new List<ClassInfo>();
    }

    // 获取班级成员
    public async Task<ClassMembersResponse> GetClassMembersAsync(string cid)
    {
        var json = await PostAsync("https://b.welife001.com/applet/getMembersByCid", new { cid, updateclass = 1, all_status = true });
        return JsonSerializer.Deserialize<ClassMembersResponse>(json) ?? new ClassMembersResponse();
    }

    // 根据班级ID获取班级详情
    public async Task<List<string>> GetClassByIdAsync(string cid)
    {
        var json = await PostAsync("https://b.welife001.com/applet/getClassById", new { cid });
        var response = JsonSerializer.Deserialize<ClassDetailResponse>(json);
        return response?.Data?.Class?.Rosters?.Select(r => r.Name).ToList() ?? new List<string>();
    }

    // 获取学生成绩
    public async Task<List<ScoreDetail>?> GetStudentScoreAsync(string name, string scoreId)
    {
        try
        {
            var body = new { id = scoreId, preview = false, name, score_phone_verified = false };
            var headers = GetHeaders();
            if (!string.IsNullOrEmpty(Imprint))
                headers["imprint"] = Imprint;
            headers["x-rid"] = Guid.NewGuid().ToString();

            var json = await PostAsync("https://b.welife001.com/getStudentScoreById", body, headers);
            var response = JsonSerializer.Deserialize<ScoreResponse>(json);
            return response?.Data?.StudentScore?.ScoreDetail;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching score for {name}: {ex.Message}");
            return null;
        }
    }
}
