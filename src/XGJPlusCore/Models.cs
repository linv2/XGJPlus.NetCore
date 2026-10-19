using System.Text.Json;
using System.Text.Json.Serialization;

namespace XGJPlus;

// 缓存数据模型
public class CacheData
{
    [JsonPropertyName("jwttoken")]
    public string JwtToken { get; set; } = "";

    [JsonPropertyName("imprint")]
    public string Imprint { get; set; } = "";

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}

// 版本信息
public class VersionInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("build_number")]
    public string BuildNumber { get; set; } = "";
}

// 登录相关
public class QrCodeResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("msg")]
    public string Msg { get; set; } = "";

    [JsonPropertyName("data")]
    public QrCodeData? Data { get; set; }
}

public class QrCodeData
{
    [JsonPropertyName("ticket")]
    public string Ticket { get; set; } = "";

    [JsonPropertyName("random")]
    public JsonElement Random { get; set; }

    public string RandomStr =>
        Random.ValueKind == JsonValueKind.Number ? Random.GetInt64().ToString() : Random.GetString() ?? "";
}

public class LoginStatusResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("msg")]
    public string Msg { get; set; } = "";

    [JsonPropertyName("data")]
    public string? Data { get; set; }
}

// 用户/子儿信息
public class UserResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("data")]
    public UserData? Data { get; set; }
}

public class UserData
{
    [JsonPropertyName("currentUser")]
    public CurrentUser? CurrentUser { get; set; }
}

public class CurrentUser
{
    [JsonPropertyName("child_class_list")]
    public List<ChildClass> ChildClassList { get; set; } = new();
}

public class ChildClass
{
    [JsonPropertyName("member_id")]
    public string MemberId { get; set; } = "";

    [JsonPropertyName("class_id")]
    public string ClassId { get; set; } = "";
}

// 家长/父信息
public class ParentInfo
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("cls")]
    public string Cls { get; set; } = "";

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("creator_wx_name")]
    public string CreatorWxName { get; set; } = "";

    [JsonPropertyName("creator_wx_openid")]
    public string CreatorWxOpenid { get; set; } = "";

    [JsonPropertyName("score")]
    public string Score { get; set; } = "";

    [JsonPropertyName("attach")]
    public AttachInfo? Attach { get; set; }
}

public class AttachInfo
{
    [JsonPropertyName("headers")]
    public List<string> Headers { get; set; } = new();

    [JsonPropertyName("detail")]
    public List<StudentDetail> Detail { get; set; } = new();
}

public class StudentDetail
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("infos")]
    public List<StudentInfoItem> Infos { get; set; } = new();
}

public class StudentInfoItem
{
    [JsonPropertyName("latest")]
    public string Latest { get; set; } = "";
}

// 班级信息
public class ClassInfo
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("class_name")]
    public string ClassName { get; set; } = "";
}

// 班级成员
public class ClassMembersResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("data")]
    public ClassMembersData? Data { get; set; }
}

public class ClassMembersData
{
    [JsonPropertyName("members")]
    public List<Member> Members { get; set; } = new();
}

public class Member
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = "";

    [JsonPropertyName("wx_name")]
    public string WxName { get; set; } = "";

    [JsonPropertyName("wx_avatar")]
    public string WxAvatar { get; set; } = "";

    [JsonPropertyName("teach_role")]
    public int TeachRole { get; set; } = -1;

    [JsonPropertyName("teach_role_str")]
    public string TeachRoleStr { get; set; } = "";

    [JsonPropertyName("family")]
    public List<FamilyMember> Family { get; set; } = new();
}

public class FamilyMember
{
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = "";

    [JsonPropertyName("wx_name")]
    public string WxName { get; set; } = "";

    [JsonPropertyName("wx_avatar")]
    public string WxAvatar { get; set; } = "";
}

// 班级花名册
public class ClassDetailResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("data")]
    public ClassDetailData? Data { get; set; }
}

public class ClassDetailData
{
    [JsonPropertyName("class")]
    public ClassDetail? Class { get; set; }
}

public class ClassDetail
{
    [JsonPropertyName("rosters")]
    public List<Roster> Rosters { get; set; } = new();
}

public class Roster
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

// 学生成绩
public class ScoreResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("data")]
    public ScoreData? Data { get; set; }
}

public class ScoreData
{
    [JsonPropertyName("studentScore")]
    public StudentScore? StudentScore { get; set; }
}

public class StudentScore
{
    [JsonPropertyName("score_detail")]
    public List<ScoreDetail> ScoreDetail { get; set; } = new();
}

public class ScoreDetail
{
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = "";

    [JsonPropertyName("score")]
    public string Score { get; set; } = "";
}

// 请求配置
public class RequestConfig
{
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = "app_web";

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "app";

    [JsonPropertyName("app_info")]
    public AppInfo AppInfo { get; set; } = new();

    [JsonPropertyName("device_info")]
    public DeviceInfo DeviceInfo { get; set; } = new();
}

public class AppInfo
{
    [JsonPropertyName("app_vcode")]
    public string AppVcode { get; set; } = "";

    [JsonPropertyName("app_vname")]
    public string AppVname { get; set; } = "";
}

public class DeviceInfo
{
    [JsonPropertyName("os_name")]
    public string? OsName { get; set; }

    [JsonPropertyName("os_version")]
    public string? OsVersion { get; set; }

    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("screen_height")]
    public int? ScreenHeight { get; set; }

    [JsonPropertyName("screen_width")]
    public int? ScreenWidth { get; set; }

    [JsonPropertyName("device_id")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("network_type")]
    public string NetworkType { get; set; } = "WiFi";
}

// 浏览器信息
public class BrowserInfo
{
    public string UserAgent { get; set; } = "";
    public string SecChUa { get; set; } = "";
    public string SecChUaPlatform { get; set; } = "";
    public string SecChUaMobile { get; set; } = "";
}
