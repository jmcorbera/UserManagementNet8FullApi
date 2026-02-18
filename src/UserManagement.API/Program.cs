using UserManagement.Application;
using UserManagement.Infrastructure;

namespace UserManagement.API;

/// <summary>
/// Entry point for WebApplicationFactory in integration tests.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        DotNetEnv.Env.Load();

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddApplication(builder.Configuration);
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddWebServices(builder.Configuration);


        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.Map("/", () => Results.Redirect("/swagger"));
        app.MapHealthChecks("/health");

        app.Run();
    }
}
