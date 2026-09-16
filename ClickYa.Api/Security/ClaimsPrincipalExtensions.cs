using System.Security.Claims;

namespace ClickYa.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static bool CanAccess(this ClaimsPrincipal user, string role, int resourceId)
    {
        if (user.IsInRole(SecurityDefaults.AdminRole)) return true;
        if (!user.IsInRole(role)) return false;
        return int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var subjectId) &&
               subjectId == resourceId;
    }
}
