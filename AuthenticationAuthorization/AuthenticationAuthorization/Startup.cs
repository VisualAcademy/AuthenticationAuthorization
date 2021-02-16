using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AuthenticationAuthorization
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            //services.AddAuthentication("Cookies").AddCookie();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization(); 

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute(); 

                #region Menu
                endpoints.MapGet("/", async context =>
                {
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

                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(content);
                }); 
                #endregion

                #region Login/{Username}
                endpoints.MapGet("/Login/{Username}", async context =>
                {
                    var username = context.Request.RouteValues["Username"].ToString(); 
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, username),
                        new Claim(ClaimTypes.Name, username),
                        new Claim(ClaimTypes.Email, username + "@a.com"),
                        new Claim(ClaimTypes.Role, "Users"),
                        new Claim("원하는 이름", "원하는 값")
                    };

                    if (username == "Administrator")
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "Administrators"));
                    }

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    await context.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        claimsPrincipal,
                        new AuthenticationProperties { IsPersistent = true });

                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                    await context.Response.WriteAsync("<h3>로그인 완료</h3>");
                }); 
                #endregion

                #region Login
                endpoints.MapGet("/Login", async context =>
                {
                    var claims = new List<Claim>
                    {
                                //new Claim(ClaimTypes.Name, "User Name")
                                new Claim(ClaimTypes.Name, "아이디")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                    await context.Response.WriteAsync("<h3>로그인 완료</h3>");
                });
                #endregion

                #region InfoDetails
                endpoints.MapGet("/InfoDetails", async context =>
                {
                    string result = "";

                    if (context.User.Identity.IsAuthenticated)
                    {
                        result += $"<h3>로그인 이름: {context.User.Identity.Name}</h3>";
                        foreach (var claim in context.User.Claims)
                        {
                            result += $"{claim.Type} = {claim.Value}<br />";
                        }
                        if (context.User.IsInRole("Administrators") && context.User.IsInRole("Users"))
                        {
                            result += "<br />Administrators + Users 권한이 있습니다.<br />";
                        }
                    }
                    else
                    {
                        result += "<h3>로그인하지 않았습니다.</h3>";
                    }

                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(result, Encoding.Default);
                });
                #endregion

                #region Info
                endpoints.MapGet("/Info", async context =>
                {
                    string result = "";

                    if (context.User.Identity.IsAuthenticated)
                    {
                        result += $"<h3>로그인 이름: {context.User.Identity.Name}</h3>";
                    }
                    else
                    {
                        result += "<h3>로그인하지 않았습니다.</h3>";
                    }

                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(result, Encoding.Default);
                });
                #endregion

                #region InfoJson
                endpoints.MapGet("/InfoJson", async context =>
                {
                    string json = "";

                    if (context.User.Identity.IsAuthenticated)
                    {
                        //json += "{ \"type\": \"Name\", \"value\": \"User Name\" }";
                        var claims = context.User.Claims.Select(c => new ClaimDto { Type = c.Type, Value = c.Value });
                        json += JsonSerializer.Serialize<IEnumerable<ClaimDto>>(
                            claims,
                            new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                    }
                    else
                    {
                        json += "{}";
                    }

                    // MIME 타입
                    context.Response.Headers["Content-Type"] = "application/json; charset=utf-8";
                    await context.Response.WriteAsync(json);
                });
                #endregion

                #region Logout
                endpoints.MapGet("/Logout", async context =>
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                    await context.Response.WriteAsync("<h3>로그아웃 완료</h3>");
                });
                #endregion
            });
        }
    }

    #region DTO
    /// <summary>
    /// Data Transfer Object 
    /// </summary>
    public class ClaimDto
    {
        public string Type { get; set; }
        public string Value { get; set; }
    } 
    #endregion

    #region MVC Controller
    [AllowAnonymous]
    public class LandingController : Controller
    {
        public IActionResult Index() => Content("누구나 접근 가능");

        [Authorize]
        [Route("/Greeting")]
        public IActionResult Greeting()
        {
            var roleName = HttpContext.User.IsInRole("Administrators") ? "관리자" : "사용자";
            return Content($"<em>{roleName}</em> 님, 반갑습니다.", "text/html", Encoding.Default);
        }
    }

    [Authorize(Roles = "Administrators")]
    public class DashboardController : Controller
    {
        public IActionResult Index() => Content("관리자 님, 반갑습니다.");
    }
    #endregion

    #region Web API Controller
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
}
