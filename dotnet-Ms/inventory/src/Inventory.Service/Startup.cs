
using System;
using System.Net.Http;
using Common.MassTransit;
using Common.MongoDB;
using Inventory.Service.clients;
using Inventory.Service.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Timeout;

namespace Inventory.Service
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
            services.AddMongo()
                    .AddMongoRepository<InventoryItem>("inventoryitems")
                    .AddMongoRepository<CatalogItem>("catalogitems")
                    .AddMassTransitWithRabbitMQ();


            AddCatalogClient(services);

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Inventory.Service", Version = "v1" });
            });
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory.Service v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private static void AddCatalogClient(IServiceCollection services)
        {
            Random jitterer = new Random();
            services.AddHttpClient<CatalogClient>(client =>
            {

                client.BaseAddress = new System.Uri("https://localhost:5001");

            })
            .AddTransientHttpErrorPolicy(builder => builder.Or<TimeoutRejectedException>().WaitAndRetryAsync(
                5,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)),
                onRetry: (outcome, timespan, retryAttempt) =>
                {
                    var serviceprovider = services.BuildServiceProvider();
                    serviceprovider.GetService<ILogger<CatalogClient>>()?
                    .LogWarning($"Delaying for {timespan.TotalSeconds} seconds, then making retry {retryAttempt}");
                }
            ))
            .AddTransientHttpErrorPolicy(builder => builder.Or<TimeoutRejectedException>().CircuitBreakerAsync(
                3, TimeSpan.FromSeconds(15),

                onBreak: (outcome, timespan) =>
                {
                    var serviceprovider = services.BuildServiceProvider();
                    serviceprovider.GetService<ILogger<CatalogClient>>()?
                    .LogWarning($"Opening the circuit for {timespan.TotalSeconds} Seconds..... ");
                },
                onReset: () =>
                {
                    var serviceprovider = services.BuildServiceProvider();
                    serviceprovider.GetService<ILogger<CatalogClient>>()?
                    .LogWarning($"Closing the Circuit....");
                }
            ))
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
        }

    }
}
