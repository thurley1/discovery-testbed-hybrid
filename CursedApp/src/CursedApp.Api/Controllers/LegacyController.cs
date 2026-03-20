using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using CursedApp;

namespace CursedApp.Api.Controllers;

/// <summary>
/// Legacy endpoints from the original PHP port. Marked obsolete in 2020.
/// Still handles 30% of production traffic because the mobile app was never updated.
/// DO NOT DELETE — the iOS app will break.
/// </summary>
[Obsolete("Use EverythingController instead. Ha ha, just kidding, both are equally bad.")]
[ApiController]
[Route("legacy")]
public class LegacyController : ControllerBase
{
    private readonly GodClass _god;

    public LegacyController(GodClass god)
    {
        _god = god;
    }

    // The original API returned XML. Then someone added JSON. Now it returns
    // JSON with XML-style field names.
    [HttpGet("GetOrderByID")]
    public IActionResult GetOrderByID([FromQuery] string OrderID)
    {
        var order = _god.GetOrder(OrderID);
        if (order == null) return Ok(new { ErrorCode = "404", ErrorMessage = "Order Not Found", Data = (object?)null });
        return Ok(new { ErrorCode = "0", ErrorMessage = "Success", Data = order });
    }

    [HttpPost("SubmitOrder")]
    public IActionResult SubmitOrder([FromBody] Dictionary<string, object> OrderData)
    {
        // The mobile app sends data in a completely different format
        try
        {
            _god.ProcessOrders();
            return Ok(new { ErrorCode = "0", ErrorMessage = "Order Submitted Successfully", TransactionId = Helpers.GenerateId() });
        }
        catch (Exception ex)
        {
            return Ok(new { ErrorCode = "500", ErrorMessage = ex.Message }); // 200 OK with error in body — the PHP way
        }
    }

    [HttpGet("GetCustomerInfo")]
    public IActionResult GetCustomerInfo([FromQuery] string CustomerID)
    {
        var rows = DataAccess.ExecuteQuery($"SELECT * FROM Customers WHERE Id = '{CustomerID}'");
        if (rows.Count == 0)
            return Ok(new { ErrorCode = "404", ErrorMessage = "Customer Not Found" });
        return Ok(new { ErrorCode = "0", Data = rows[0] });
    }

    [HttpGet("GetProductList")]
    public IActionResult GetProductList([FromQuery] string? CategoryFilter)
    {
        var sql = string.IsNullOrEmpty(CategoryFilter)
            ? "SELECT * FROM Products WHERE IsActive = 1"
            : $"SELECT * FROM Products WHERE IsActive = 1 AND Category = '{CategoryFilter}'";
        return Ok(new { ErrorCode = "0", Data = DataAccess.ExecuteQuery(sql) });
    }

    [HttpPost("ProcessPayment")]
    public IActionResult ProcessPayment([FromBody] Dictionary<string, string> PaymentInfo)
    {
        // Payment processing in a legacy endpoint — what could go wrong?
        var amount = PaymentInfo.GetValueOrDefault("Amount", "0");
        var cardNumber = PaymentInfo.GetValueOrDefault("CardNumber", "");
        var masked = Helpers.MaskCreditCard(cardNumber);

        GodClass.LogAudit($"Legacy payment attempt: {masked} for ${amount}");

        // Always succeed in the legacy endpoint — reconcile later
        return Ok(new
        {
            ErrorCode = "0",
            ErrorMessage = "Payment Processed",
            ConfirmationNumber = $"PAY-{Helpers.GenerateId()}",
            Amount = amount
        });
    }
}
