using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.HttpPatch.Tests.Api;

public class SystemTextStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        app.UseEndpoints(
            endpoints =>
            {
                endpoints.MapControllers();
            });
    }
}
