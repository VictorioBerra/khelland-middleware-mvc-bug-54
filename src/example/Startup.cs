using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Http;
using WebApiContrib.Core.Formatter.Csv;

namespace example
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment HostingEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProblemDetails(options => {
                // This is the default behavior; only include exception details in a development environment.
                options.IncludeExceptionDetails = ctx => HostingEnvironment.IsDevelopment();

                // Because exceptions are handled polymorphically, this will act as a "catch all" mapping, which is why it's added last.
                options.Map<Exception>(ex => new ExceptionProblemDetails(ex, StatusCodes.Status500InternalServerError));
            });

            services
                .AddRouting(
                    options =>
                    {
                        // All generated URL's should be lower-case.
                        options.LowercaseUrls = true;
                    })
                .AddMvcCore()
                 .AddCsvSerializerFormatters()
                // Using `SetCompatibilityVersion()` is recommended for Core 2.1 and onward.
                // https://blogs.msdn.microsoft.com/webdev/2018/02/27/introducing-compatibility-version-in-mvc/
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonFormatters()
                .AddMvcOptions(options =>
                {
                    // Remove string and stream output formatters. These are not useful for an API serving JSON or XML.
                    options.OutputFormatters.RemoveType<StreamOutputFormatter>();
                    options.OutputFormatters.RemoveType<StringOutputFormatter>();

                    var jsonInputFormatterMediaTypes = options
                        .InputFormatters
                        .OfType<JsonInputFormatter>()
                        .First()
                        .SupportedMediaTypes;
                    var jsonOutputFormatterMediaTypes = options
                        .OutputFormatters
                        .OfType<JsonOutputFormatter>()
                        .First()
                        .SupportedMediaTypes;

                    // Add Problem Details media type (application/problem+json) to the JSON output formatters.
                    // See https://tools.ietf.org/html/rfc7807
                    jsonOutputFormatterMediaTypes.Insert(0, "application/problem+json");

                    // Add RESTful JSON media type (application/vnd.restful+json) to the JSON input and output formatters.
                    // See http://restfuljson.org/
                    jsonInputFormatterMediaTypes.Insert(0, "application/vnd.restful+json");
                    jsonOutputFormatterMediaTypes.Insert(0, "application/vnd.restful+json");

                    // Returns a 406 Not Acceptable if the MIME type in the Accept HTTP header is not valid.
                    options.ReturnHttpNotAcceptable = true;

                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseProblemDetails();
            
            if (env.IsDevelopment())
            {
                // This prevents the error.
                // app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.Use((context, next) =>
            {
                context.Request.Scheme = "https";
                return next();
            });

            app.UseMvc();
        }
    }
}
