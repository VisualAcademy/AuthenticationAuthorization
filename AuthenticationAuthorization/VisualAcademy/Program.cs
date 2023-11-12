using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ���� �߰�
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();

var app = builder.Build();

// ���� ȯ�� ����
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ��������Ʈ �� ���Ʈ ����
app.MapGet("/", async context =>
{
    string content = "<h1>ASP.NET Core ������ ���� �ʰ��� �ڵ�</h1>";
    content += "<a href=\"/Login\">�α���</a><br />";
    content += "<a href=\"/Login/User\">�α���(User)</a><br />";
    content += "<a href=\"/Login/Administrator\">�α���(Administrator)</a><br />";
    content += "<a href=\"/Info\">����</a><br />";
    content += "<a href=\"/InfoDetails\">����(Details)</a><br />";
    content += "<a href=\"/InfoJson\">����(JSON)</a><br />";
    content += "<a href=\"/Logout\">�α׾ƿ�</a><br />";
    content += "<hr /><a href=\"/Landing\">����������</a><br />";
    content += "<a href=\"/Greeting\">ȯ��������</a><br />";
    content += "<a href=\"/Dashboard\">����������</a><br />";
    content += "<a href=\"/api/AuthService\">�α��� ����(JSON)</a><br />";

    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
    await context.Response.WriteAsync(content);
});

app.MapGet("/Login/{Username}", async context =>
{
    var username = context.Request.RouteValues["Username"].ToString();
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, username),
        new Claim(ClaimTypes.Name, username),
        new Claim(ClaimTypes.Email, username.ToLower() + "@youremail.com"),
        new Claim(ClaimTypes.Role, "Users"),
        new Claim("���ϴ� �̸�", "���ϴ� ��")
    };

    if (username == "Administrator")
    {
        claims.Add(new Claim(ClaimTypes.Role, "Administrators"));
    }

    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, new AuthenticationProperties { IsPersistent = true });
    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
    await context.Response.WriteAsync("<h3>�α��� �Ϸ�</h3>");
});

app.MapGet("/Login", async context =>
{
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, "���̵�")
    };

    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
    await context.Response.WriteAsync("<h3>�α��� �Ϸ�</h3>");
});

app.MapGet("/InfoDetails", async context =>
{
    string result = "";

    if (context.User.Identity.IsAuthenticated)
    {
        result += $"<h3>�α��� �̸�: {context.User.Identity.Name}</h3>";
        foreach (var claim in context.User.Claims)
        {
            result += $"{claim.Type} = {claim.Value}<br />";
        }
        if (context.User.IsInRole("Administrators") && context.User.IsInRole("Users"))
        {
            result += "<br />Administrators + Users ������ �ֽ��ϴ�.<br />";
        }
    }
    else
    {
        result += "<h3>�α������� �ʾҽ��ϴ�.</h3>";
    }

    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
    await context.Response.WriteAsync(result, Encoding.Default);
});

app.MapGet("/Info", async context =>
{
    string result = "";

    if (context.User.Identity.IsAuthenticated)
    {
        result += $"<h3>�α��� �̸�: {context.User.Identity.Name}</h3>";
    }
    else
    {
        result += "<h3>�α������� �ʾҽ��ϴ�.</h3>";
    }

    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
    await context.Response.WriteAsync(result, Encoding.Default);
});

app.MapGet("/InfoJson", async context =>
{
    string json = "";

    if (context.User.Identity.IsAuthenticated)
    {
        var claims = context.User.Claims.Select(c => new ClaimDto { Type = c.Type, Value = c.Value });
        json += JsonSerializer.Serialize<IEnumerable<ClaimDto>>(
            claims,
            new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    }
    else
    {
        json += "{}";
    }

    context.Response.Headers["Content-Type"] = "application/json; charset=utf-8";
    await context.Response.WriteAsync(json);
});

app.MapGet("/Logout", async context =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
    await context.Response.WriteAsync("<h3>�α׾ƿ� �Ϸ�</h3>");
});

app.MapControllers();
app.Run();

// ��Ʈ�ѷ� �� DTO Ŭ����
public class ClaimDto
{
    public string Type { get; set; }
    public string Value { get; set; }
}

[AllowAnonymous]
[Route("/Landing")]
public class LandingController : Controller
{
    [HttpGet]
    public IActionResult Index() => Content("������ ���� ����");

    [Authorize]
    [HttpGet("/Greeting")]
    public IActionResult Greeting()
    {
        var roleName = HttpContext.User.IsInRole("Administrators") ? "������" : "�����";
        return Content($"<em>{roleName}</em> ��, �ݰ����ϴ�.", "text/html", Encoding.Default);
    }
}

[Authorize(Roles = "Administrators")]
[Route("/Dashboard")]
public class DashboardController : Controller
{
    [HttpGet]
    public IActionResult Index() => Content("������ ��, �ݰ����ϴ�.");
}

[ApiController]
[Route("api/[controller]")]
public class AuthServiceController : ControllerBase
{
    [Authorize]
    [HttpGet]
    public IEnumerable<ClaimDto> Get() =>
        HttpContext.User.Claims.Select(c => new ClaimDto { Type = c.Type, Value = c.Value });
}
