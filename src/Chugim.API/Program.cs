using Chugim.Application;
using Chugim.Infrastructure;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// NLog מחליף את ספקי הלוג המובנים. הקוד ממשיך להשתמש ב-ILogger<T> הרגיל.
// RemoveLoggerFactoryFilter = false: רמות הלוג נקבעות ב-appsettings, לא ב-nlog.config.
builder.Logging.ClearProviders();
builder.Host.UseNLog(new NLogAspNetCoreOptions { RemoveLoggerFactoryFilter = false });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
