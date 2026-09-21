using Application.Modules.Catalog.UnitOfMeasure.CreateUnitOfMeasure;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Modules.Catalog.UnitOfMeasure.CreateUnitOfMeasure;

namespace Infrastructure.Modules.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddScoped<ICreateUnitOfMeasureStore, EfCreateUnitOfMeasureStore>();
        services.AddScoped<ICreateUnitOfMeasureHandler, CreateUnitOfMeasureHandler>();

        return services;
    }
}