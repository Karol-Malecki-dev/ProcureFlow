using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.AddPurchaseRequestItem;
using Application.Modules.PurchaseRequests.CancelPurchaseRequest;
using Application.Modules.PurchaseRequests.CreatePurchaseRequest;
using Application.Modules.PurchaseRequests.GetPurchaseRequestDetails;
using Application.Modules.PurchaseRequests.ListMyPurchaseRequests;
using Application.Modules.PurchaseRequests.RemovePurchaseRequestItem;
using Application.Modules.PurchaseRequests.SubmitPurchaseRequest;
using Application.Modules.PurchaseRequests.UpdatePurchaseRequestItemQuantity;
using Infrastructure.Modules.PurchaseRequests.AddPurchaseRequestItem;
using Infrastructure.Modules.PurchaseRequests.CancelPurchaseRequest;
using Infrastructure.Modules.PurchaseRequests.CreatePurchaseRequest;
using Infrastructure.Modules.PurchaseRequests.Draft;
using Infrastructure.Modules.PurchaseRequests.GetPurchaseRequestDetails;
using Infrastructure.Modules.PurchaseRequests.ListMyPurchaseRequests;
using Infrastructure.Modules.PurchaseRequests.PurchaseRequestMembershipReader;
using Infrastructure.Modules.PurchaseRequests.RemovePurchaseRequestItem;
using Infrastructure.Modules.PurchaseRequests.SubmitPurchaseRequest;
using Infrastructure.Modules.PurchaseRequests.UpdatePurchaseRequestItemQuantity;
using Infrastructure.Modules.PurchaseRequests.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Modules.PurchaseRequests;

/// <summary>
/// Registers all PF3 purchase-request draft ports and handlers.
/// </summary>
public static class PurchaseRequestsModule
{
    public static IServiceCollection AddPurchaseRequestsModule(this IServiceCollection services)
    {
        services.AddScoped<IPurchaseRequestMembershipReader, EfPurchaseRequestMembershipReader>();
        services.AddScoped<IPurchaseRequestDraftStore, EfPurchaseRequestDraftStore>();
        services.AddScoped<IPurchaseRequestWorkflowStore, EfPurchaseRequestWorkflowStore>();

        services.AddScoped<ICreatePurchaseRequestStore, EfCreatePurchaseRequestStore>();
        services.AddScoped<ICreatePurchaseRequestHandler, CreatePurchaseRequestHandler>();

        services.AddScoped<IAddPurchaseRequestItemHandler, AddPurchaseRequestItemHandler>();
        services.AddScoped<IUpdatePurchaseRequestItemQuantityHandler, UpdatePurchaseRequestItemQuantityHandler>();
        services.AddScoped<IRemovePurchaseRequestItemHandler, RemovePurchaseRequestItemHandler>();
        services.AddScoped<ISubmitPurchaseRequestHandler, SubmitPurchaseRequestHandler>();
        services.AddScoped<ICancelPurchaseRequestHandler, CancelPurchaseRequestHandler>();

        services.AddScoped<IGetPurchaseRequestDetailsStore, EfGetPurchaseRequestDetailsStore>();
        services.AddScoped<IGetPurchaseRequestDetailsHandler, GetPurchaseRequestDetailsHandler>();

        services.AddScoped<IListMyPurchaseRequestsStore, EfListMyPurchaseRequestsStore>();
        services.AddScoped<IListMyPurchaseRequestsHandler, ListMyPurchaseRequestsHandler>();

        return services;
    }
}
