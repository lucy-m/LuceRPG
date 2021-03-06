using LuceRPG.Adapters;
using LuceRPG.Models;
using LuceRPG.Samples;
using LuceRPG.Server;
using LuceRPG.Server.Core;
using LuceRPG.Server.Processors;
using LuceRPG.Server.Storer;
using LuceRPG.Utility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace LuceRPGServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IntentionQueue>();
            services.AddSingleton(PrimordiaVille.collection);
            services.AddSingleton<WorldEventsStorer>();
            services.AddSingleton<LastPingStorer>();
            services.AddSingleton<BehaviourMapStorer>();
            services.AddSingleton<ICredentialService, CredentialService>();
            services.AddSingleton<IntentionProcessor>();
            services.AddSingleton<StaleClientProcessor>();
            services.AddSingleton<BehaviourProcessor>();
            services.AddSingleton<ITimestampProvider, TimestampProvider>();
            services.AddSingleton<ICsvLogService, CsvLogService>();

            services.AddHostedService<IntentionProcessorService>();
            services.AddHostedService<StaleClientProcessorService>();
            services.AddHostedService<BehaviourProcessorService>();
            services.AddHostedService<LogFlusher>();

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "LuceRPGServer", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LuceRPGServer v1"));
            }

            app.UseRouting();
            app.UseCors(policy =>
            {
                policy.AllowAnyOrigin();
                policy.AllowAnyHeader();
                policy.AllowAnyMethod();
            });

            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // sets up log directory
            app.ApplicationServices.GetService<ICsvLogService>();
        }
    }
}
