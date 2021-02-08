using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace AuthenticationAuthorization
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication("Cookies").AddCookie();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    string content = "<h1>ASP.NET Core 인증과 권한 초간단 코드</h1>";
                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(content);
                });

                endpoints.MapGet("/Login", async context => 
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, "User Name")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, "Cookies");

                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    await context.SignInAsync("Cookies", claimsPrincipal);

                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                    await context.Response.WriteAsync("<h3>로그인 완료</h3>");
                });

                endpoints.MapGet("/Info", async context => {
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
            });
        }
    }
}
