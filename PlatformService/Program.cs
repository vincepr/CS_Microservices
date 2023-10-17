using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore;
using PlatformService.AsyncDataServices;
using PlatformService.Data;
using PlatformService.SyncDataService.Http;
using PlatformService.SyncDataService.Http.Grpc;


var builder = WebApplication.CreateBuilder(args);

// we dependencyinject => If someone asks for IPlatformRepo he will get our PlatformRepo implementation
builder.Services.AddScoped<IPlatformRepo, PlatformRepo>();

// we inject our Database context
builder.Services.AddDbContext<AppDbContext>(opts => {
    if (builder.Environment.IsProduction()) {
        Console.WriteLine("--> CASE PRODUCTION - using SqlServerDb");
        opts.UseSqlServer(builder.Configuration.GetConnectionString("PlatformsConn"));
    } else {
        Console.WriteLine("--> CASE DEV - using InMemoryDB");
        opts.UseInMemoryDatabase("InMem");
    }
});

// we inject Automapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// we inject our "httpClientFactory" we use to send a simple http post for ever new Platform Created directly to CommandsClient
builder.Services.AddHttpClient<ICommandDataClient, HttpCommandDataClient>();
builder.Services.AddGrpc();
builder.Services.AddSingleton<IMessageBusClient, MessageBusClient>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();   // disabling http while inside our cluster

app.UseAuthorization();

app.MapControllers();   // this maps all our Controllers by default
app.MapGrpcService<GrpcPlatformService>();  // the grpcService we have to Add manually
// we (this is optinal) serve the protobuf file to the client, so they could infer everyhing from it:
app.MapGet(
    "/protos/platforms.proto",
    async context => {
        await context.Response.WriteAsync(File.ReadAllText("Protos/platforms.proto"));
    }
);

// we manually (for testing/quick-development) inject some fake data into our db
PrepDb.PrepPopulation(app, app.Environment.IsProduction());

Console.WriteLine($"--> config[CommandService] endpoint: {app.Configuration["CommandService"]}");

app.Run();
