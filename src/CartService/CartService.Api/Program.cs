using CartService.Application.Interface;
using CartService.Application.Service;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//-------------------------REDIS
//var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

// 🔹 Đăng ký Redis Multiplexer vào DI container
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    return ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis"));
});



builder.Services.AddScoped<ICartServices, CartServices>();


// 4. Redis
// var redisConnection = configuration.GetConnectionString("Redis");
// builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));
// builder.Services.AddScoped<ICacheService, RedisCacheService>();
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? Environment.GetEnvironmentVariable("ConnectionStrings__Redis");

if (redisConnection.StartsWith("redis://"))
{
    redisConnection = redisConnection.Replace("redis://", "");
}

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    try
    {
        return ConnectionMultiplexer.Connect(redisConnection);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Redis connection failed: {ex.Message}");
        throw;
    }
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
