// https://www.memoengine.com/labs/aspnet-core-8-0-getting-started/
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// [1] Startup.ConfigureServices 영역 
// 서비스 추가
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();

var app = builder.Build();

// [2] Startup.Configure 영역 
// 개발 환경 설정
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization(); // [Authorize] 특성 사용

#region 0. Menu
// 엔드포인트 및 라우트 설정
app.MapGet("/", async context =>
{
    // 메뉴 HTML 문자열을 생성하여 응답
    string content = "<h1>ASP.NET Core 인증과 권한 초간단 코드</h1>";
    content += "<a href=\"/Login\">로그인</a><br />";
    content += "<a href=\"/Login/User\">로그인(User)</a><br />";
    content += "<a href=\"/Login/Administrator\">로그인(Administrator)</a><br />";
    content += "<a href=\"/Info\">정보</a><br />";
    content += "<a href=\"/InfoDetails\">정보(Details)</a><br />";
    content += "<a href=\"/InfoJson\">정보(JSON)</a><br />";
    content += "<a href=\"/Logout\">로그아웃</a><br />";
    content += "<hr /><a href=\"/Landing\">랜딩페이지</a><br />";
    content += "<a href=\"/Greeting\">환영페이지</a><br />";
    content += "<a href=\"/Dashboard\">관리페이지</a><br />";
    content += "<a href=\"/api/AuthService\">로그인 정보(JSON)</a><br />";

    // 응답 헤더 설정
    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
    await context.Response.WriteAsync(content);
});
#endregion

#region 1. Login
// "/Login" 경로로 GET 요청을 처리합니다.
app.MapGet("/Login", async context =>
{
    // 클레임(Claim) 리스트를 생성하고 사용자 아이디 클레임을 추가합니다.
    var claims = new List<Claim>
    {
            new Claim(ClaimTypes.Name, "아이디") // 사용자 이름(아이디) 클레임
    };

    // 쿠키 인증 스키마를 사용하여 클레임 아이덴티티를 생성합니다.
    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

    // 클레임 아이덴티티를 사용하여 클레임 프린시펄을 생성합니다.
    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

    // 사용자를 로그인 처리하고 쿠키 인증 스키마를 통해 인증합니다.
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

    // 응답 헤더의 "Content-Type"을 설정하여 한글 문자가 올바르게 표시되도록 합니다.
    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";

    // 클라이언트에 "로그인 완료" 메시지를 출력합니다.
    await context.Response.WriteAsync("<h3>로그인 완료</h3>");
});
#endregion

#region 2. Info
// "/Info" 경로로 GET 요청을 처리합니다.
app.MapGet("/Info", async context =>
{
    string result = "";

    // 사용자가 인증되었는지 확인
    if (context.User.Identity is not null && context.User.Identity.IsAuthenticated)
    {
        result += $"<h3>로그인 이름: {context.User.Identity.Name}</h3>";
    }
    else
    {
        result += "<h3>로그인하지 않았습니다.</h3>";
    }

    // 응답 헤더 설정
    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
    await context.Response.WriteAsync(result, Encoding.Default);
});
#endregion

#region 3. InfoJson
// "/InfoJson" 경로로 GET 요청을 처리합니다.
app.MapGet("/InfoJson", async context =>
{
    string json = "";

    // 사용자가 인증되었는지 확인
    if (context.User.Identity is not null && context.User.Identity.IsAuthenticated)
    {
        // 사용자 클레임 정보를 JSON으로 직렬화
        var claims = context.User.Claims.Select(c => new ClaimDto { Type = c.Type, Value = c.Value });
        json += JsonSerializer.Serialize<IEnumerable<ClaimDto>>(
            claims,
            new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    }
    else
    {
        json += "{}";
    }

    // MIME 타입을 JSON 형식으로 변경
    context.Response.Headers["Content-Type"] = "application/json; charset=utf-8";
    await context.Response.WriteAsync(json);
});
#endregion

#region 4. Logout
// "/Logout" 경로로 GET 요청을 처리합니다.
app.MapGet("/Logout", async context =>
{
    // 사용자를 로그아웃 처리합니다.
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    // 응답 헤더 설정
    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
    await context.Response.WriteAsync("<h3>로그아웃 완료</h3>");
});
#endregion

#region 5. Login/{Username}
// "/Login/{Username}" 경로로 GET 요청을 처리합니다.
app.MapGet("/Login/{Username}", async context =>
{
    // URL에서 사용자 이름을 가져옵니다.
    var username = context.Request.RouteValues["Username"]?.ToString() ?? string.Empty;

    // 사용자 클레임 리스트를 생성합니다.
    var claims = new List<Claim>
    {
            new Claim(ClaimTypes.NameIdentifier, username),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Email, username.ToLower() + "@youremail.com"),
            new Claim(ClaimTypes.Role, "Users"), // 기본적으로 "Users" 역할 부여
            new Claim("원하는 이름", "원하는 값")
    };

    // 만약 사용자 이름이 "Administrator"라면 "Administrators" 역할을 추가합니다.
    if (username == "Administrator")
    {
        claims.Add(new Claim(ClaimTypes.Role, "Administrators"));
    }

    // 쿠키 인증 스키마를 사용하여 클레임 아이덴티티 및 프린시펄을 생성합니다.
    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

    // 사용자를 로그인 처리하고 쿠키 인증 스키마를 통해 인증합니다.
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, new AuthenticationProperties { IsPersistent = true });

    // 응답 헤더 설정
    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";

    // 클라이언트에 "로그인 완료" 메시지를 출력합니다.
    await context.Response.WriteAsync("<h3>로그인 완료</h3>");
});
#endregion

#region 6. InfoDetails
// "/InfoDetails" 경로로 GET 요청을 처리합니다.
app.MapGet("/InfoDetails", async context =>
{
    string result = "";

    // 사용자가 인증되었는지 확인
    if (context.User.Identity is not null && context.User.Identity.IsAuthenticated)
    {
        result += $"<h3>로그인 이름: {context.User.Identity.Name}</h3>";

        // 사용자 클레임 정보를 출력
        foreach (var claim in context.User.Claims)
        {
            result += $"{claim.Type} = {claim.Value}<br />";
        }

        // 사용자가 "Administrators"와 "Users" 역할을 모두 가지고 있는지 확인
        if (context.User.IsInRole("Administrators") && context.User.IsInRole("Users"))
        {
            result += "<br />Administrators + Users 권한이 있습니다.<br />";
        }
    }
    else
    {
        result += "<h3>로그인하지 않았습니다.</h3>";
    }

    // 응답 헤더 설정
    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
    await context.Response.WriteAsync(result, Encoding.Default);
});
#endregion

// 컨트롤러 맵핑
app.MapControllers(); // app.MapDefaultControllerRoute();
app.Run();

#region DTO
// 컨트롤러 및 DTO 클래스
public class ClaimDto
{
    public string Type { get; set; }
    public string Value { get; set; }
}
#endregion

#region 7. MVC Controller
// MVC 컨트롤러 정의
[AllowAnonymous]
[Route("/Landing")]
public class LandingController : Controller
{
    [HttpGet]
    public IActionResult Index() => Content("누구나 접근 가능");

    [Authorize]
    [HttpGet("/Greeting")]
    public IActionResult Greeting()
    {
        var roleName = HttpContext.User.IsInRole("Administrators") ? "관리자" : "사용자";
        return Content($"<em>{roleName}</em> 님, 반갑습니다.", "text/html", Encoding.Default);
    }
}

[Authorize(Roles = "Administrators")]
[Route("/Dashboard")]
public class DashboardController : Controller
{
    [HttpGet]
    public IActionResult Index() => Content("관리자 님, 반갑습니다.");
}
#endregion

#region 8. Web API Controller
// Web API 컨트롤러 정의
[ApiController]
[Route("api/[controller]")]
public class AuthServiceController : ControllerBase
{
    [Authorize]
    [HttpGet]
    public IEnumerable<ClaimDto> Get() =>
        HttpContext.User.Claims.Select(c => new ClaimDto { Type = c.Type, Value = c.Value });
}
#endregion
