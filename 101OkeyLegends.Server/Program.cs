using _101OkeyLegends.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

// SignalR servisini ekleyin.
builder.Services.AddSignalR();

var app = builder.Build();

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<GameHub>("/gamehub");
});

app.Run();