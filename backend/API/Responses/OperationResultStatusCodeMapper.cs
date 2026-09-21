using Application.Features.Projects;
using Application.Modules.Catalog.UnitOfMeasure;
using Application.Modules.PurchaseRequests;
using Domain.Models.Organizations.Enums;
using Microsoft.AspNetCore.Http;

namespace API.Responses;

internal static class OperationResultStatusCodeMapper
{
    public static int Map(ProjectOperationStatus status) => status switch
    {
        ProjectOperationStatus.NotFound => StatusCodes.Status404NotFound,
        ProjectOperationStatus.ValidationError => StatusCodes.Status400BadRequest,
        ProjectOperationStatus.Conflict => StatusCodes.Status409Conflict,
        ProjectOperationStatus.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };

    public static int Map(UnitOfMeasureOperationStatus status) => status switch
    {
        UnitOfMeasureOperationStatus.NotFound => StatusCodes.Status404NotFound,
        UnitOfMeasureOperationStatus.Conflict => StatusCodes.Status409Conflict,
        UnitOfMeasureOperationStatus.ValidationError => StatusCodes.Status400BadRequest,
        UnitOfMeasureOperationStatus.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };

    public static int Map(PurchaseRequestOperationStatus status) => status switch
    {
        PurchaseRequestOperationStatus.NotFound => StatusCodes.Status404NotFound,
        PurchaseRequestOperationStatus.Conflict => StatusCodes.Status409Conflict,
        PurchaseRequestOperationStatus.ValidationError => StatusCodes.Status400BadRequest,
        PurchaseRequestOperationStatus.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };

    public static int Map(BranchOperationStatus status) => status switch
    {
        BranchOperationStatus.NotFound => StatusCodes.Status404NotFound,
        BranchOperationStatus.Conflict => StatusCodes.Status409Conflict,
        BranchOperationStatus.ValidationError => StatusCodes.Status400BadRequest,
        BranchOperationStatus.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };

    public static int Map(MembershipOperationStatus status) => status switch
    {
        MembershipOperationStatus.NotFound => StatusCodes.Status404NotFound,
        MembershipOperationStatus.Conflict => StatusCodes.Status409Conflict,
        MembershipOperationStatus.ValidationError => StatusCodes.Status400BadRequest,
        MembershipOperationStatus.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };
}