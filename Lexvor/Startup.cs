using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.API;
using Lexvor.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lexvor.Data;
using Lexvor.Models;
using Lexvor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Stripe;

namespace Lexvor {
    public class Startup {
        public IHostingEnvironment Env { get; set; }

        public Startup(IHostingEnvironment env) {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            if (env.IsDevelopment()) {
                builder.AddUserSecrets<Startup>();
                builder.AddApplicationInsightsSettings(developerMode: true);
            }

            Env = env;

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            // Adds services required for using options.
            services.AddOptions();

            services.AddApplicationInsightsTelemetry(Configuration);

            // Register the IConfiguration instance which MyOptions binds against.
            services.Configure<ConnectionStrings>(Configuration.GetSection("ConnectionStrings"));
            services.Configure<OtherSettings>(Configuration.GetSection("OtherSettings"));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>(options => {
                options.SignIn.RequireConfirmedEmail = false;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 0;
                options.User.RequireUniqueEmail = true;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(24);
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<SecurityStampValidatorOptions>(options => {
                // enables immediate logout, after updating the user's stat.
                options.ValidationInterval = TimeSpan.FromSeconds(30);
            });

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddMvc(options => { options.EnableEndpointRouting = false; }).AddSessionStateTempDataProvider();
            services.AddMemoryCache();
            services.AddSession();

            var builder = services.AddRazorPages();
#if DEBUG
            if (Env.IsDevelopment()) {
                builder.AddRazorRuntimeCompilation();
            }
#endif
            services.AddControllers();

            services.Configure<MessageSenderOptions>(Configuration.GetSection("MessageSenderOptions"));

            services.AddScoped<ExceptionCatcher>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
				// TODO: This doesnt work
				//app.UseDeveloperExceptionPage();
				//app.UseBrowserLink();
				//app.UseDatabaseErrorPage();
				app.UseExceptionHandler(builder => {
                    builder.Run(async context => {
                        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
                        if (error != null) {
                            var service = context.Features.Get<IServiceProvidersFeature>();
                            var settingsManager = service?.RequestServices.GetService(typeof(IOptions<OtherSettings>));
                            var settings = (settingsManager as OptionsManager<OtherSettings>)?.Value;
							if (settings != null) {
								ErrorHandler.Capture(settings.SentryDSN, error.Error, context, customData: new Dictionary<string, string>() {
									{"DEBUGGING", "true" }
								});
							}
						}
                    });
                });
            } else {
                app.UseExceptionHandler("/Home/Error");
			}
            app.UseCors("AllowAll");
			//app.UseCors(builder => builder.WithOrigins(
			//	"https://localhost:44328",
			//	"https://lexvorwireless.com",
			//	"https://stage.lexvorwireless.com",
			//	"https://test.lexvorwireless.com",
			//	"https://lexvor-stage.azurewebsites.net",
			//	"https://sandbox.plaid.com",
			//	"https://plaid.com",
			//	"https://analytics.plaid.com",
			//	"https://cdn.ravenjs.com",
			//	"https://api-cf.affirm.com",
			//	"https://cdn1.affirm.com",
			//	"https://www.affirm.com"));

			app.UseStaticFiles();

            app.UseAuthentication();
            //app.UseMiddleware(typeof(ErrorHandlingMiddleware), Configuration.GetSection("OtherSettings"));

            app.UseSession();

            app.UseMvc(routes => {
                routes.MapRoute("areaRoute", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapRoute(
                    "Error",
                    "{*.}",
                    new { controller = "Error", action = "NotFound" });
            });

            app.UseRouting();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });

            //var key = Configuration["StripeSecretKey"];
            var key = Environment.GetEnvironmentVariable("StripeSecretKey");
            if (string.IsNullOrEmpty(key)) {
                key = Environment.GetEnvironmentVariable("APPSETTING_StripeSecretKey");
            }
            StripeConfiguration.ApiKey = key;
            //ApplicationDbContext.Seed(app);
        }
    }
}
