using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // public void ConfigureServices(IServiceCollection services)
        // {
        //     // use SqlServer as DB provider
        //     services.AddDbContext<DataContext>(x => {
        //         // set the connection string and ignore EF warnings for prod
        //         var connectionString = Configuration.GetConnectionString("DefaultConnection");
        //         x.UseSqlServer(connectionString)
        //             .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.IncludeIgnoredWarning));
        //     });
            
        //     services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
        //         .AddJsonOptions(opt => {
        //             opt.SerializerSettings.ReferenceLoopHandling = 
        //                 Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        //         });
        //     services.AddCors();
        //     services.Configure<CloudinarySettings>(Configuration.GetSection("CloudinarySettings"));
        //     services.AddAutoMapper();
        //     services.AddTransient<Seed>();
        //     services.AddScoped<IAuthRepository, AuthRepository>();
        //     services.AddScoped<IDatingRepository, DatingRepository>();
        //     services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        //         .AddJwtBearer(options => {
        //             options.TokenValidationParameters = new TokenValidationParameters
        //             {
        //                 ValidateIssuerSigningKey = true,
        //                 IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII
        //                     .GetBytes(Configuration.GetSection("AppSettings:Token").Value)),
        //                 ValidateIssuer = false,
        //                 ValidateAudience = false
        //             };
        //         });
        //     services.AddScoped<LogUserActivity>();
        // }

        public void ConfigureDevelopmentServices(IServiceCollection services)
        {
            // use SqlLite as DB provider
            services.AddDbContext<DataContext>(x => x.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));
            
            #region Identity and Authentication services 

            // customize the identity
            IdentityBuilder builder = services.AddIdentityCore<User>(opt => {
                opt.Password.RequireDigit = false;
                opt.Password.RequiredLength = 4;
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequireUppercase = false;
            });

            // new builder using the previous configuration, to set up persistence about roles
            builder = new IdentityBuilder(builder.UserType, typeof(Role), builder.Services);
            builder.AddEntityFrameworkStores<DataContext>();
            builder.AddRoleValidator<RoleValidator<Role>>();
            builder.AddRoleManager<RoleManager<Role>>();
            builder.AddSignInManager<SignInManager<User>>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII
                            .GetBytes(Configuration.GetSection("AppSettings:Token").Value)),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });

            #endregion

            // I require all controller to be authenticated, So we can avoid [Authorize] attribute upon them
            services.AddMvc(options => {
                var policy = new AuthorizationPolicyBuilder()
                                .RequireAuthenticatedUser()
                                .Build();
                options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddJsonOptions(opt => {
                    opt.SerializerSettings.ReferenceLoopHandling = 
                        Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                });
            services.AddCors();
            services.Configure<CloudinarySettings>(Configuration.GetSection("CloudinarySettings"));
            Mapper.Reset();
            services.AddAutoMapper();
            services.AddTransient<Seed>();
            services.AddScoped<IDatingRepository, DatingRepository>();   
            services.AddScoped<LogUserActivity>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, Seed seeder)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(builder => {
                    builder.Run(async context => {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                        var error = context.Features.Get<IExceptionHandlerFeature>();
                        if (error != null) 
                        {
                            context.Response.AddApplicationError(error.Error.Message);
                            await context.Response.WriteAsync(error.Error.Message);
                        }
                    });
                });
                // app.UseHsts();
            }

            // app.UseHttpsRedirection();
            // fill db with some test data
            seeder.SeedUsers();
            app.UseCors(x => x.WithOrigins("http://localhost:4200")
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                 .AllowCredentials());
            app.UseAuthentication();
            // look for default files, such index.html, default.html, etc
            app.UseDefaultFiles();
            // enable static files to host the angular app
            app.UseStaticFiles();
            // tell to MVC that angular is taking care of some routes
            app.UseMvc(routes => {
                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new { controller = "Fallback", action = "Index"}
                );
            });

        }
    }
}
