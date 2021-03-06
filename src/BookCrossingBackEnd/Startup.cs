using System;
using System.IO;
using System.Text;
using Application.Services.Implementation;
using Application.Services.Interfaces;
using AutoMapper;
using BookCrossingBackEnd.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using BookCrossingBackEnd.Validators;
using Domain;
using FluentValidation.AspNetCore;
using RequestService = Application.Services.Implementation.RequestService;
using Infrastructure.NoSQL;
using Domain.NoSQL;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using EmailConfiguration = Application.Dto.Email.EmailConfiguration;
using Application;
using Hangfire;
using BookCrossingBackEnd.ServiceExtension;

namespace BookCrossingBackEnd
{
    public class Startup
    {
        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            _logger = logger;
        }

        private IConfiguration Configuration { get; }
        private readonly ILogger _logger;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddDbContext(Configuration);


            services.AddMongoSettings(Configuration);

            services.AddEmailService(Configuration);

            services.AddMapper();

            services.AddScoped<ICommentOwnerMapper, CommentOwnerMapper>();

            services.AddControllers().AddNewtonsoftJson(options =>
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            );

            services.AddRepositories();

            services.AddCustomServices();

            services.AddLogging();

            services.AddApplicationInsightsTelemetry();

            services.AddCorsSettings();

            services.AddMVCWithFluentValidatoin();

            services.AddJWTAuthenticatoin(Configuration);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SoftServe BookCrossing", Version = "v1" });
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles(new StaticFileOptions()
            {
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
                },
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot")),
                RequestPath = new PathString("")
            });

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseCors("CorsPolicy");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseHangfireDashboard();
            //app.UseHangfireDashboard("/hangfire", new DashboardOptions
            //{
            //    Authorization = new[] { new HangfireAuthorizationFilter() },
            //});
            app.UseHangfireServer(new BackgroundJobServerOptions
            {
                WorkerCount = 1
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SoftServe BookCrossing");
            });

            if (env.IsDevelopment())
            {
                _logger.LogInformation("Configuring for Development environment");
                app.UseDeveloperExceptionPage();
            }
            else
            {
                _logger.LogInformation("Configuring for Production environment");
            }

        }

    }
}