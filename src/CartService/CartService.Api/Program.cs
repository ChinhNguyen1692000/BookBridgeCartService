using CartService.Application.Interface;
using CartService.Application.Service;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddScoped<ICartServices, CartServices>();


// 4. Redis
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? Environment.GetEnvironmentVariable("ConnectionStrings__Redis");

if (!string.IsNullOrEmpty(redisConnection) && redisConnection.StartsWith("redis://"))
{
    redisConnection = redisConnection.Replace("redis://", "");
}
else if (string.IsNullOrEmpty(redisConnection))
{
    Console.WriteLine(" Redis connection string is null or empty!");
}

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    try
    {
        var config = ConfigurationOptions.Parse(redisConnection, true);
        config.AbortOnConnectFail = false;
        config.ConnectRetry = 3;
        config.ConnectTimeout = 5000;
        config.SyncTimeout = 5000;
        return ConnectionMultiplexer.Connect(config);
    }
    catch (Exception ex)
    {
        Console.WriteLine($" Redis connection failed: {ex.Message}");
        throw;
    }
});



// 5. Configure Kestrel to use Render's provided PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

builder.Services.AddHttpsRedirection(options => options.HttpsPort = null);


var app = builder.Build();

// Check Redis connection when startup
using (var scope = app.Services.CreateScope())
{
    var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
    if (redis.IsConnected)
        Console.WriteLine(" Redis connected successfully.");
    else
        Console.WriteLine(" Redis not connected.");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok("Healthy"));

app.UseAuthorization();

app.MapControllers();

app.Run();
