using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore;
using PlatformService.Data;
using PlatformService.SyncDataService.Http;


var builder = WebApplication.CreateBuilder(args);

// we dependencyinject => If someone asks for IPlatformRepo he will get our PlatformRepo implementation
builder.Services.AddScoped<IPlatformRepo, PlatformRepo>();

// we inject our Database context
builder.Services.AddDbContext<AppDbContext>(opts => {
    opts.UseInMemoryDatabase("InMem");
});

// we inject Automapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// we inject our "httpClientFactory" we use to send a simple http post for ever new Platform Created directly to CommandsClient
builder.Services.AddHttpClient<ICommandDataClient, HttpCommandDataClient>();

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

app.MapControllers();

// we manually (for testing/quick-development) inject some fake data into our db
PrepDb.PrepPopulation(app);

Console.WriteLine($"--> config[CommandService] endpoint: {app.Configuration["CommandService"]}");

app.Run();
