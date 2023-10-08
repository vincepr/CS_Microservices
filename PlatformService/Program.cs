using Microsoft.EntityFrameworkCore;
using PlatformService.Data;

var builder = WebApplication.CreateBuilder(args);

// we dependencyinject => If someone asks for IPlatformRepo he will get our PlatformRepo implementation
builder.Services.AddScoped<IPlatformRepo, PlatformRepo>();

// we inject our Database context
builder.Services.AddDbContext<AppDbContext>(opts => {
    opts.UseInMemoryDatabase("InMem");
});

// we inject Automapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// we manually (for testing/quick-development) inject some fake data into our db
PrepDb.PrepPopulation(app);

app.Run();
