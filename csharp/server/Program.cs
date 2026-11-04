/*
   Data de criação: 03/11/2026
   Aluna: Caroline Marques Lau
   Arquivo de conexão com o servidor
*/

using System.Net;                                  // IPAddress
using Mflix;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;    // HttpProtocols
using Microsoft.Extensions.DependencyInjection;
using Serilog;

DotNetEnv.Env.TraversePath().Load();

string mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI")
?? throw new InvalidOperationException("MONGODB_URI não configurada.");

string databaseName = Environment.GetEnvironmentVariable("MONGODB_DB")
    ?? throw new InvalidOperationException("MONGODB_DB não configurada.");

string collectionName = Environment.GetEnvironmentVariable("MONGODB_COLLECTION")
    ?? throw new InvalidOperationException("MONGODB_COLLECTION não configurada.");

Log.Logger = new LoggerConfiguration()
// para diferenciar os clientes
.WriteTo.Console()
.CreateLogger();

string serverAddress = Environment.GetEnvironmentVariable("SERVER_ADDR")
?? throw new InvalidOperationException("SERVER_ADDR não configurada.");

string[] addressParts = serverAddress.Split(':');

if (addressParts.Length != 2 ||
    !IPAddress.TryParse(addressParts[0], out IPAddress? ipAddress) ||
    !int.TryParse(addressParts[1], out int port) ||
    port < 1 || port > 65535)
{
    Log.Error("SERVER_ADDR inválida: {Address}", serverAddress);
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(ipAddress!, port, listen => listen.Protocols = HttpProtocols.Http2);
});

builder.Services.AddGrpc();
builder.Services.AddSingleton(new MongoConnection(mongoUri, databaseName, collectionName));
builder.Services.AddSingleton<MovieRepository>();

var app = builder.Build();

app.MapGrpcService<MovieServiceImpl>();

Log.Information("Servidor gRPC iniciado em {Address}", serverAddress);
app.Run();