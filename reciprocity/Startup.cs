using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dapper;
using reciprocity.Data.Default;
using reciprocity.Data;
using reciprocity.Services;
using reciprocity.Services.Default;

namespace reciprocity
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add
        // services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            });

            SqlMapper.AddTypeHandler(new TimeSpanTypeHandler());
            SqlMapper.AddTypeHandler(new BearerTokenTypeHandler());

            services.AddSingleton<IConnectionFactory>(new ConnectionStringConnectionFactory(Configuration.GetConnectionString("Main")));

            services.AddSingleton<IDataService, DataService>();
        }

        // This method gets called by the runtime. Use this method to configure
        // the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            app.UseStaticFiles();
            app.UseStatusCodePagesWithRedirects("/error/{0}");
            app.UseMvc();
        }
    }
}
