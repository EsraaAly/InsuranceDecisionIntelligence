using InsuranceDecisionIntelligence.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using InsuranceDecisionIntelligence.Application.Interfaces.Data;
using InsuranceDecisionIntelligence.Application.FileStorage.Services;
using InsuranceDecisionIntelligence.Infrastructure.FileStorage.Services;
using InsuranceDecisionIntelligence.Infrastructure.Data.Bulk;
using InsuranceDecisionIntelligence.Application.Interfaces.File;
using InsuranceDecisionIntelligence.Application.Services.File;
using InsuranceDecisionIntelligence.Application.Interfaces.Repositories;
using InsuranceDecisionIntelligence.Infrastructure.Repositories;
using InsuranceDecisionIntelligence.Application.Services.Data;
using InsuranceDecisionIntelligence.Infrastructure.Data;

namespace InsuranceDecisionIntelligence.Infrastructure
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FileProviderSettings>(configuration.GetSection("FileStorageSettings"));
            services.Configure<ConnectionSettings>(configuration.GetSection("ConnectionStrings"));

            services.AddScoped<IFileProvider, FileProviderService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IFileReader, CSVReaderService>();
            services.AddScoped<IBulkInsertByIDataReaderService, BulkInsertByIDataReaderService>();
            services.AddScoped<IBulkInsertByDataTableService, BulkInsertByDataTableService>();
            services.AddScoped<IDatabaseMetaDataService, DatabaseMetaDataService>();
            services.AddScoped<IDataQueryService, DataQueryService>();
            services.AddScoped<IPolicyRepository, PolicyRepository>();

            return services;
        }
    }
}
