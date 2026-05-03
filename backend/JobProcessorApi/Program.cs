var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Serve static files from the frontend directory
var env = app.Services.GetRequiredService<IWebHostEnvironment>();
var frontendPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", ".."));
Console.WriteLine($"Serving static files from: {frontendPath}");
Console.WriteLine($"Index.html exists: {System.IO.File.Exists(Path.Combine(frontendPath, "index.html"))}");

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendPath),
    RequestPath = ""
});

// Map controllers for API endpoints
app.MapControllers();

// Fallback to index.html for client-side routing
app.MapFallback(async context =>
{
    var filePath = Path.Combine(frontendPath, "index.html");
    if (System.IO.File.Exists(filePath))
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(filePath);
    }
    else
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Not found");
    }
});





app.Run();


