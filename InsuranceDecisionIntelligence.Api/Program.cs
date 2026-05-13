
using InsuranceDecisionIntelligence.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using System.Text;

namespace InsuranceDecisionIntelligence.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            builder.Services.AddControllers();
            builder.Services.AddMemoryCache();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "API", Version = "v1" });
            });

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = long.MaxValue;
            });

            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = long.MaxValue;
            });

            builder.Services.AddInfrastructureServices(builder.Configuration);

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
