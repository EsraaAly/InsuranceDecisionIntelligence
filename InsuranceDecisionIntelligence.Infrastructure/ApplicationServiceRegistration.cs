using InsuranceDecisionIntelligence.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using InsuranceDecisionIntelligence.Application.Interfaces.Data;
using InsuranceDecisionIntelligence.Application.Services.Data;
using InsuranceDecisionIntelligence.Application.FileStorage.Services;
using InsuranceDecisionIntelligence.Infrastructure.FileStorage.Services;
using InsuranceDecisionIntelligence.Infrastructure.Data;

namespace InsuranceDecisionIntelligence.Infrastructure
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IFileProvider, FileProviderService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IFileReader, ExcelReaderService>();
            services.AddScoped<IBulkInsertService, BulkInsertService>();


            services.Configure<FileProviderSettings>(configuration.GetSection("FileStorageSettings"));

            return services;
        }
    }
}
