using System;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Filters;
using SchoolMS.Core.Entities;          // AuditLog class ke liye
using SchoolMS.Infrastructure.Data;    // AppDbContext ke liye

namespace SchoolMS.API.Filters
{
    // 1. Isko 'public' rakhna hai taake Program.cs isey access kar sake
    public class AuditLogFilter : IActionFilter
    {
        private readonly AppDbContext _dbContext;

        public AuditLogFilter(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Request aane par abhi hume kuch nahi karna
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var httpMethod = context.HttpContext.Request.Method;

            if (httpMethod == "POST" || httpMethod == "PUT" || httpMethod == "DELETE")
            {
                if (context.Exception == null)
                {
                    var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
                    var actionName = context.RouteData.Values["action"]?.ToString() ?? "Unknown";

                    // 1. Pehle normal token se nikalne ki koshish karein (Baqi saare controllers ke liye)
                    var userIdClaim = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    int? userId = string.IsNullOrEmpty(userIdClaim) ? null : int.Parse(userIdClaim);

                    var userName = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                                   ?? context.HttpContext.User.FindFirst("unique_name")?.Value
                                   ?? "Anonymous";

                    // 🛑 SPECIAL CHECK FOR LOGIN: Agar Auth/Login chal raha hai aur user anonymous hai
                    if (controllerName.Equals("Auth", StringComparison.OrdinalIgnoreCase) &&
                        actionName.Equals("Login", StringComparison.OrdinalIgnoreCase))
                    {
                        // Agar login successful raha hai, toh controller ObjectResult (Ok) return karta hai
                        if (context.Result is Microsoft.AspNetCore.Mvc.ObjectResult objectResult && objectResult.Value != null)
                        {
                            // Yahan check karein ke kya response mein aapka LoginResponseDto aaya hai?
                            // Chunki aapka object direct mapping mein ho sakta hai, hum dynamic ya properties read kar sakte hain.
                            var responseData = objectResult.Value;

                            // Reflection ke zariye LoginResponseDto se properties nikalien
                            var userIdProp = responseData.GetType().GetProperty("UserId")?.GetValue(responseData, null);
                            var fullNameProp = responseData.GetType().GetProperty("FullName")?.GetValue(responseData, null);

                            if (userIdProp != null) userId = Convert.ToInt32(userIdProp);
                            if (fullNameProp != null) userName = fullNameProp.ToString() ?? "Anonymous";
                        }
                    }

                    var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A";
                    var routeDataJson = System.Text.Json.JsonSerializer.Serialize(context.RouteData.Values);

                    // Time fix as per Pakistan Standard Time
                    TimeZoneInfo pkTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
                    DateTime pkTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, pkTimeZone);

                    bool isLoginRequest = controllerName.Equals("Auth", StringComparison.OrdinalIgnoreCase) &&
                      actionName.Equals("Login", StringComparison.OrdinalIgnoreCase);

                    var auditLog = new AuditLog
                    {
                        UserId = userId,
                        UserName = userName,
                        // 🛑 Action ko handle karein: Agar login hai toh 'Login' likhein, warna Create/Update/Delete
                        Action = isLoginRequest ? "Login" : (httpMethod == "POST" ? "Create" : httpMethod == "PUT" ? "Update" : "Delete"),

                        // 🛑 Entity ko handle karein: Agar login hai toh 'Authentication' ya 'Auth' likhein
                        Entity = isLoginRequest ? "Authentication" : controllerName,
                        Details = $"Executed Action: {actionName} | RouteData: {routeDataJson}",
                        IpAddress = ipAddress,
                        CreatedAt = pkTime
                    };

                    _dbContext.AuditLogs.Add(auditLog);
                    _dbContext.SaveChanges();
                }
            }
        }
    }
}