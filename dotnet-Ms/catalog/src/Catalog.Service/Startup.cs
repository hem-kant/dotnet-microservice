

using Calatlog.Metric.Collector;
using Catalog.Metric.Middleware;

using Catalog.Service.Entities;
using Common.MassTransit;
using Common.MongoDB;
using Common.Settings;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Prometheus;
namespace Catalog.Service
{
    public class Startup
    {
        private ServiceSettings serviceSettings;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {


            serviceSettings = Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();

            services.AddMongo()
                .AddMongoRepository<Item>("item")
                .AddMassTransitWithRabbitMQ();

            services.AddSingleton<MetricCollector>();
            //services.AddMassTransitHostedService();

            services.AddControllers(options =>
            {
                options.SuppressAsyncSuffixInActionNames = false;
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog.Service", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog.Service v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseMetricServer();
            app.UseMiddleware<MetricMiddleware>();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/api/health",
                                    new HealthCheckOptions()
                                    {
                                        Predicate = _ => true,
                                        ResponseWriter = UIResponseWriter.
                                                    WriteHealthCheckUIResponse
                                    });
            });


        }
    }
}
