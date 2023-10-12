using Microsoft.AspNetCore.Builder;

namespace LogOtter.Azure.AppServices.RequestMiddleware;

public static class ConfigureExtensions
{
    public static WebApplication UseRestoreRawRequestPathMiddleware(this WebApplication webApplication)
    {
        webApplication.UseMiddleware<RestoreRawRequestPathMiddleware>();
        webApplication.UseRouting();

        return webApplication;
    }
}
