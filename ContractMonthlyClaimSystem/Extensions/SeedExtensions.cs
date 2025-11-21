using Microsoft.AspNetCore.Http;
using System;

namespace ContractMonthlyClaimSystem.Extensions
{
    public static class SessionExtensions
    {
        public static string GetUserRole(this ISession session)
        {
            return session.GetString("UserRole") ?? string.Empty;
        }

        public static string GetUserId(this ISession session)
        {
            return session.GetString("UserId") ?? string.Empty;
        }

        public static string GetUserName(this ISession session)
        {
            return session.GetString("UserName") ?? string.Empty;
        }

        public static bool IsInRole(this ISession session, string role)
        {
            return session.GetUserRole().Equals(role, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAuthenticated(this ISession session)
        {
            return !string.IsNullOrEmpty(session.GetUserId());
        }
    }
}