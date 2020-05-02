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
            string localConnection = Configuration.GetConnectionString("DefaultConnection");
            // Please download appsettings.json for connecting to Azure DB
            string azureConnection = Configuration.GetConnectionString("AzureConnection");
            services.AddDbContext<Infrastructure.RDBMS.BookCrossingContext>(options =>
                options.UseSqlServer(azureConnection, x => x.MigrationsAssembly("BookCrossingBackEnd")));

         
            // requires using Microsoft.Extensions.Options
            services.Configure<MongoSettings>(
                Configuration.GetSection(nameof(MongoSettings)));

            services.AddSingleton<IMongoSettings>(sp =>
                sp.GetRequiredService<IOptions<MongoSettings>>().Value);

            services.AddHangfire(config =>
                config.UseSqlServerStorage(Configuration.GetConnectionString("AzureConnection")));
            services.AddHangfireServer(options=> options.SchedulePollingInterval = TimeSpan.FromSeconds(10));

            var emailConfig = Configuration
                .GetSection("EmailConfiguration")
                .Get<EmailConfiguration>();
            services.AddSingleton(emailConfig);

            var mappingConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new Application.Mapper());
            });

            IMapper mapper = mappingConfig.CreateMapper();
            services.AddSingleton(mapper);
            services.AddScoped<ICommentOwnerMapper, CommentOwnerMapper>();

            services.AddControllers().AddNewtonsoftJson(options =>
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            );

            services.AddScoped(typeof(Domain.NoSQL.IChildRepository<,>), typeof(Infrastructure.NoSQL.BaseChildRepository<,>));
            services.AddScoped(typeof(Domain.NoSQL.IRootRepository<>), typeof(Infrastructure.NoSQL.BaseRootRepository<>));
            services.AddScoped(typeof(Domain.RDBMS.IRepository<>), typeof(Infrastructure.RDBMS.BaseRepository<>));
            services.AddScoped<IBookChildCommentService, BookChildCommentService>();
            services.AddScoped<IBookRootCommentService, BookRootCommentService>();
            services.AddScoped<ILocationService, LocationService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUserService, UsersService>();
            services.AddScoped<IEmailSenderService, EmailSenderService>();
            services.AddScoped<IRequestService, RequestService>();
            services.AddScoped<IAuthorService, AuthorService>();
            services.AddScoped<IBookService, BookService>();
            services.AddScoped<IUserResolverService,UserResolverService>();
            services.AddScoped<IGenreService, GenreService>();
            services.AddScoped<IHangfireJobScheduleService, HangfireJobSchedulerService>();
            services.AddLogging();
            services.AddApplicationInsightsTelemetry();

            services.AddSingleton<IImageService, ImageService>();
            services.AddSingleton<IPaginationService, PaginationService>();

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().Build());
            });


            services.AddMvc(options =>
            {
                options.Filters.Add(new ModelValidationFilter());
            })
            .AddFluentValidation(cfg =>
            {
                cfg.RegisterValidatorsFromAssemblyContaining<AuthorValidator>();
            });

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Configuration["Jwt:Issuer"],
                        ValidAudience = Configuration["Jwt:Issuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
                    };

                });


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
