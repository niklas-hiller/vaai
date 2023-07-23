using VAAI.Server.Hubs;

namespace VAAI.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddSignalR();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
        }

        app.UseHttpsRedirection();

        app.MapHub<MainHub>("Main");

        app.MapGet("/", () => "Hello World!");

        app.Run();
    }
}