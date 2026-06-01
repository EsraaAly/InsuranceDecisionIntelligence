using Hangfire;
using InsuranceDecisionIntelligence.Application.Abstractions.Data;
using InsuranceDecisionIntelligence.Application.Abstractions.File;
using InsuranceDecisionIntelligence.Application.Abstractions.Persistence;
using InsuranceDecisionIntelligence.Application.Abstractions.Queue;
using InsuranceDecisionIntelligence.Application.Configuration;
using InsuranceDecisionIntelligence.Application.Contracts.Event_Driven;
using InsuranceDecisionIntelligence.Application.Services.Datasets;
using InsuranceDecisionIntelligence.Application.Services.Uploads;
using InsuranceDecisionIntelligence.Infrastructure.Data.Import;
using InsuranceDecisionIntelligence.Infrastructure.Data.Uploads;
using InsuranceDecisionIntelligence.Infrastructure.FileStorage;
using InsuranceDecisionIntelligence.Infrastructure.FileStorage.Readers;
using InsuranceDecisionIntelligence.Infrastructure.Queue;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InsuranceDecisionIntelligence.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(configuration.GetSection("FileStorageSettings"));
        services.Configure<DatabaseConnectionOptions>(configuration.GetSection("ConnectionStrings"));

        services.AddScoped<IFileStorageService, LocalDiskFileStorageService>();
        services.AddScoped<IFileUploadImportService, FileUploadImportService>();
        services.AddScoped<ITabularFileReader, CsvExcelTabularFileReader>();
        services.AddScoped<IDataReaderSqlBulkImporter, DataReaderSqlBulkImporter>();
        services.AddScoped<IDataTableSqlBulkImporter, DataTableSqlBulkImporter>();
        services.AddScoped<IUploadDatasetMetadataReader, SqlUploadDatasetMetadataReader>();
        services.AddScoped<IImportedDatasetQueryService, ImportedDatasetQueryService>();
        services.AddScoped<IImportedDatasetPageRepository, SqlImportedDatasetPageRepository>();
        services.AddScoped<IDatasetChartQueryService, DatasetChartQueryService>();

        services.AddSingleton<IQueueService, QueueService>();
        services.AddHostedService<FileImportBackgroundWorker>();

        services.AddHangfire(config => config
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(configuration.GetConnectionString("HangfireConnection")));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 1;
        });

        services.AddMassTransit(x =>
        {
            x.AddConsumer<FileUploadedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("localhost", "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                cfg.ReceiveEndpoint("insurance-file-import-queue", e =>
                {
                    //e.ConcurrentMessageLimit = 1;
                    e.ConfigureConsumer<FileUploadedConsumer>(context);
                });
            });
        });

        return services;
    }
}
