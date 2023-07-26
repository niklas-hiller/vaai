using Microsoft.AspNetCore.SignalR;
using VAAI.Server.Filters;
using VAAI.Server.Hubs;
using VAAI.Server.Services;

namespace VAAI.Server;

internal class Program
{
    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddSignalR(hubOptions =>
        {
            hubOptions.AddFilter<GroupFilter<VAAIHub>>();
            hubOptions.MaximumReceiveMessageSize = long.MaxValue;
        });
        builder.Services.AddSingleton<GroupFilter<VAAIHub>>();
        builder.Services.AddSingleton<ISessionService<VAAIHub>, SessionService<VAAIHub>>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
        }

        app.UseHttpsRedirection();

        app.MapHub<VAAIHub>("VAAI");

        app.MapGet("/", () => "Hello World!");

        app.Run();
    }
}