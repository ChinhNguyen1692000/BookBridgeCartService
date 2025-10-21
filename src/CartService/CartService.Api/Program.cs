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
var redisConnection = builder.Configuration.GetConnectionString("Redis")
                      ?? Environment.GetEnvironmentVariable("ConnectionStrings__Redis")
                      ?? ""; // Cung cấp một giá trị mặc định rỗng

if (redisConnection.StartsWith("redis://"))
{
    redisConnection = redisConnection.Replace("redis://", "");
}

// Thêm kiểm tra null/rỗng trước khi Connect
if (string.IsNullOrEmpty(redisConnection))
{
    // Hoặc ghi log và thoát nếu kết nối Redis là bắt buộc
    throw new InvalidOperationException("Redis connection string is missing.");
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

// 6. Configure Kestrel to use Render's provided PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
