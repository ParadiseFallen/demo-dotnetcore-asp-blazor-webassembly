using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Advanced.Models;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Identity;

namespace Advanced {
    public class Startup {

        public Startup(IConfiguration config) {
            Configuration = config;
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services) {
            services.AddDbContext<DataContext>(opts => {
                opts.UseSqlServer(Configuration[
                    "ConnectionStrings:PeopleConnection"]);
                opts.EnableSensitiveDataLogging(true);
            });
            services.AddControllersWithViews().AddRazorRuntimeCompilation();
            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddServerSideBlazor();
            services.AddSingleton<Services.ToggleService>();

            services.AddResponseCompression(opts => {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });

            services.AddDbContext<IdentityContext>(opts => opts.UseSqlServer(Configuration["ConnectionStrings:IdentityConnection"]));
            services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<IdentityContext>();
            services.Configure<IdentityOptions>(opts => {
              opts.Password.RequiredLength = 8;
              opts.Password.RequireNonAlphanumeric = false;
              opts.Password.RequireLowercase = false;
              opts.Password.RequireUppercase = false;
              opts.Password.RequireDigit = true;
              opts.User.RequireUniqueEmail = true;
              // opts.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyz";
              opts.User.AllowedUserNameCharacters = new Func<string>(() => {
                var sb = new System.Text.StringBuilder();
                for (ushort i = 65; i < 122; i++)
                {
                    if (i < 91 || i > 96)
                    {
                        System.Console.WriteLine((char)i);
                        sb.Append((char)i);
                    }
                }
                return sb.ToString();
              })();
              opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromSeconds(15);
              opts.Lockout.MaxFailedAccessAttempts = 3;
              opts.Lockout.AllowedForNewUsers = true;
            });
        }

        public void Configure(IApplicationBuilder app, DataContext context) {

            app.UseDeveloperExceptionPage();
            app.UseStaticFiles();
            app.UseRouting();
            // app.UseAuthentication();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute("controllers",
                    "controllers/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapDefaultControllerRoute();
                endpoints.MapRazorPages();
                endpoints.MapBlazorHub();

                endpoints.MapFallbackToClientSideBlazor<BlazorWebAssembly.Startup>
                    ("/webassembly/{*path:nonfile}", "index.html");

                endpoints.MapFallbackToPage("/_Host");
            });

            app.Map("/webassembly", opts =>
                opts.UseClientSideBlazorFiles<BlazorWebAssembly.Startup>());

            SeedData.SeedDatabase(context);
        }
    }
}
