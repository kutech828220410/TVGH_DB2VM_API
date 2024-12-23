using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Reflection;
//using IGeekFan.AspNetCore.Knife4jUI;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Swashbuckle.AspNetCore.Swagger;

namespace DB2VM
{
    public class Startup
    {
        private readonly IWebHostEnvironment _environment;
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _environment = environment;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder
                        .SetIsOriginAllowed(origin => true)
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });

            });
            services.AddControllers();
            services.AddSignalR();

            services.AddSwaggerGen(c =>
            {
                // API �A��²��
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Pharmacy Sysytem API",
                    Description = "Pharmacy Sysytem API",
                    License = new OpenApiLicense { Name = "�E�˴����ަ������q ���v�Ҧ�" }
                });
                c.CustomOperationIds(apiDesc =>
                {
                    var controllerAction = apiDesc.ActionDescriptor as ControllerActionDescriptor;
                    return controllerAction.ControllerName + "-" + controllerAction.ActionName;
                });

                // Ū�� XML �ɮײ��� API ����
                //var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                //var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                //var xmlFile_HIS_DB_Lib = $"HIS_DB_Lib.xml";
                //var xmlPath_HIS_DB_Lib = Path.Combine(AppContext.BaseDirectory, xmlFile_HIS_DB_Lib);

                //c.IncludeXmlComments(xmlPath);
                //c.IncludeXmlComments(xmlPath_HIS_DB_Lib);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCors(builder =>
            {
                builder.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials();
            });
            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseCors(); // �ҥ�CORS
            app.UseAuthorization();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                //endpoints.MapHub<ChatHub>("/chatHub");
            });
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                //c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });
            //app.UseKnife4UI(c =>
            //{
            //    c.RoutePrefix = string.Empty;
            //    c.SwaggerEndpoint($"/swagger/v1/swagger.json", "h.swagger.webapi v1");
            //});
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapSwagger("{documentName}/swagger.json");
            });
        }
    }
}
