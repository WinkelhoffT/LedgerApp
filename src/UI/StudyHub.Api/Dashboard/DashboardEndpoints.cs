using StudyHub.Logic.Business.Dashboard;

namespace StudyHub.Api.Dashboard;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/dashboard");

        group.MapGet("/semester-progress", (IDashboardManagement dashboardManagement, CancellationToken cancellationToken)
            => dashboardManagement.GetSemesterProgressAsync(cancellationToken));

        return endpoints;
    }
}
