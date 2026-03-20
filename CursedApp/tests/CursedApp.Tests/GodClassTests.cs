using System;
using Xunit;

namespace CursedApp.Tests;

/// <summary>
/// The entire test suite for the entire application.
/// 7 tests for ~2000 lines of production code.
/// Test coverage: "yes" (we have tests).
/// </summary>
public class GodClassTests
{
    [Fact]
    public void GodClass_CanBeCreated()
    {
        var god = new GodClass();
        Assert.NotNull(god);
    }

    [Fact]
    public void GetOrder_ReturnsNull_WhenNotFound()
    {
        DataAccess.ConnectionString = "not-a-real-connection";
        var god = new GodClass();
        var order = god.GetOrder("nonexistent");
        Assert.Null(order);
    }

    [Fact]
    public void Cache_SetAndGet_Works()
    {
        var god = new GodClass();
        god.SetCache("test-key", "test-value", TimeSpan.FromMinutes(5));
        var result = god.GetFromCache<string>("test-key");
        Assert.Equal("test-value", result);
    }

    [Fact]
    public void Cache_ExpiredEntry_ReturnsNull()
    {
        var god = new GodClass();
        god.SetCache("expired-key", "value", TimeSpan.FromMilliseconds(1));
        System.Threading.Thread.Sleep(10);
        var result = god.GetFromCache<string>("expired-key");
        Assert.Null(result);
    }

    [Fact]
    public void CreateSession_ReturnsSessionId()
    {
        var god = new GodClass();
        var sessionId = god.CreateSession("user-1", "Admin");
        Assert.NotNull(sessionId);
        Assert.NotEmpty(sessionId);
    }

    [Fact]
    public void GetSession_ReturnsSession_WhenExists()
    {
        var god = new GodClass();
        var sessionId = god.CreateSession("user-1", "Admin");
        var session = god.GetSession(sessionId);
        Assert.NotNull(session);
        Assert.Equal("user-1", session!.UserId);
        Assert.Equal("Admin", session.Role);
    }

    [Fact]
    public void Helpers_Hash_ReturnsMd5()
    {
        var hash = Helpers.Hash("test");
        Assert.Equal("098f6bcd4621d373cade4e832627b4f6", hash);
    }

    [Fact]
    public void Helpers_IsValidEmail_ValidatesCorrectly()
    {
        Assert.True(Helpers.IsValidEmail("test@example.com"));
        Assert.False(Helpers.IsValidEmail("not-an-email"));
        Assert.False(Helpers.IsValidEmail(""));
    }

    [Fact]
    public void Helpers_Slugify_ConvertsCorrectly()
    {
        Assert.Equal("hello-world", Helpers.Slugify("Hello World"));
        Assert.Equal("test-123", Helpers.Slugify("Test 123"));
    }

    [Fact]
    public void Helpers_MaskCreditCard_MasksCorrectly()
    {
        Assert.Equal("************1234", Helpers.MaskCreditCard("4111111111111234"));
        Assert.Equal("****", Helpers.MaskCreditCard(""));
    }

    [Fact]
    public void Helpers_CalculateTax_ReturnsCorrectRate()
    {
        Assert.Equal(7.25m, Helpers.CalculateTax(100m, "CA"));
        Assert.Equal(0m, Helpers.CalculateTax(100m, "OR"));
        Assert.Equal(5.00m, Helpers.CalculateTax(100m, "ZZ")); // Default rate
    }

    [Fact]
    public void Helpers_GenerateId_Returns12Chars()
    {
        var id = Helpers.GenerateId();
        Assert.Equal(12, id.Length);
    }
}
