using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.AddPurchaseRequestItem;
using Application.Modules.PurchaseRequests.Attachments;
using Application.Modules.PurchaseRequests.Attachments.CreatePurchaseRequestAttachment;
using Application.Modules.PurchaseRequests.Attachments.DeletePurchaseRequestAttachment;
using Application.Modules.PurchaseRequests.Attachments.DownloadPurchaseRequestAttachment;
using Application.Modules.PurchaseRequests.Attachments.ListPurchaseRequestAttachments;
using Application.Modules.PurchaseRequests.Approval.DecidePurchaseRequest;
using Application.Modules.PurchaseRequests.Approval.ListPurchaseRequestApprovalQueue;
using Application.Modules.PurchaseRequests.Budget.GetBranchMonthlyBudget;
using Application.Modules.PurchaseRequests.Budget.UpsertBranchMonthlyBudget;
using Application.Modules.PurchaseRequests.CancelPurchaseRequest;
using Application.Modules.PurchaseRequests.CreatePurchaseRequest;
using Application.Modules.PurchaseRequests.Fulfillment.ListPurchaseRequestFulfillmentQueue;
using Application.Modules.PurchaseRequests.Fulfillment.MarkPurchaseRequestDelivered;
using Application.Modules.PurchaseRequests.Fulfillment.MarkPurchaseRequestOrdered;
using Application.Modules.PurchaseRequests.GetPurchaseRequestDetails;
using Application.Modules.PurchaseRequests.ListMyPurchaseRequests;
using Application.Modules.PurchaseRequests.RemovePurchaseRequestItem;
using Application.Modules.PurchaseRequests.SubmitPurchaseRequest;
using Application.Modules.PurchaseRequests.UpdatePurchaseRequestItemQuantity;
using Infrastructure.Modules.PurchaseRequests.AddPurchaseRequestItem;
using Infrastructure.Modules.PurchaseRequests.Attachments;
using Infrastructure.Modules.PurchaseRequests.Approval;
using Infrastructure.Modules.PurchaseRequests.Budget;
using Infrastructure.Modules.PurchaseRequests.CancelPurchaseRequest;
using Infrastructure.Modules.PurchaseRequests.CreatePurchaseRequest;
using Infrastructure.Modules.PurchaseRequests.Draft;
using Infrastructure.Modules.PurchaseRequests.Fulfillment;
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
/// Registers purchase-request draft, budget and approval ports and handlers.
/// </summary>
public static class PurchaseRequestsModule
{
    public static IServiceCollection AddPurchaseRequestsModule(this IServiceCollection services)
    {
        services.AddScoped<IPurchaseRequestMembershipReader, EfPurchaseRequestMembershipReader>();
        services.AddScoped<IPurchaseRequestDraftStore, EfPurchaseRequestDraftStore>();
        services.AddScoped<IPurchaseRequestWorkflowStore, EfPurchaseRequestWorkflowStore>();
        services.AddScoped<IPurchaseRequestApprovalStore, EfPurchaseRequestApprovalStore>();
        services.AddScoped<IPurchaseRequestFulfillmentStore, EfPurchaseRequestFulfillmentStore>();
        services.AddScoped<IPurchaseRequestAttachmentStore, EfPurchaseRequestAttachmentStore>();
        services.AddScoped<IPurchaseRequestAttachmentCleanupProcessor, PurchaseRequestAttachmentCleanupProcessor>();

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

        services.AddScoped<IGetBranchMonthlyBudgetHandler, GetBranchMonthlyBudgetHandler>();
        services.AddScoped<IUpsertBranchMonthlyBudgetHandler, UpsertBranchMonthlyBudgetHandler>();
        services.AddScoped<IListPurchaseRequestApprovalQueueHandler, ListPurchaseRequestApprovalQueueHandler>();
        services.AddScoped<IDecidePurchaseRequestHandler, DecidePurchaseRequestHandler>();
        services.AddScoped<IListPurchaseRequestFulfillmentQueueHandler, ListPurchaseRequestFulfillmentQueueHandler>();
        services.AddScoped<IMarkPurchaseRequestOrderedHandler, MarkPurchaseRequestOrderedHandler>();
        services.AddScoped<IMarkPurchaseRequestDeliveredHandler, MarkPurchaseRequestDeliveredHandler>();
        services.AddScoped<ICreatePurchaseRequestAttachmentHandler, CreatePurchaseRequestAttachmentHandler>();
        services.AddScoped<IListPurchaseRequestAttachmentsHandler, ListPurchaseRequestAttachmentsHandler>();
        services.AddScoped<IDownloadPurchaseRequestAttachmentHandler, DownloadPurchaseRequestAttachmentHandler>();
        services.AddScoped<IDeletePurchaseRequestAttachmentHandler, DeletePurchaseRequestAttachmentHandler>();
        services.AddHostedService<PurchaseRequestAttachmentCleanupWorker>();

        return services;
    }
}
