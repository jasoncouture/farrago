using Farrago.Core;
using Farrago.Core.KeyValueStore;
using Farrago.Protocol.Tcp.Server;
using k8s.Util.Informer.Cache;

var builder = WebApplication.CreateBuilder(args);
builder.Host.AddFarrago();
builder.Services.AddMvc();
builder.Services.AddControllers();
builder.Services.AddNativeTcpServer(builder.Configuration.GetSection("Servers:NativeTcp"));

var app = builder.Build();
app.UseFarrago();
app.UseRouting();
app.UseEndpoints(eb => eb.MapControllers());

app.MapGet("/readiness", () => "Ok");
app.MapGet("/liveness", () => "Ok");
await app.RunAsync();