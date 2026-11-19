using AlunosTech.NewSistem.Data;
using AlunosTech.NewSistem.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Conexão com o SQL Server: ajuste "DefaultConnection" em appsettings.json (ou appsettings.Development.json).
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("A connection string 'DefaultConnection' não foi encontrada em appsettings.json.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

builder.Services.AddHealthChecks();

builder.Services.AddControllersWithViews();

// A sessão (AddSession) precisa de um IDistributedCache registrado.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IForumService, ForumService>();

var app = builder.Build();

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var auth = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
    SeedData.Initialize(db, auth);
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex,
        "Não foi possível inicializar o banco AlunosTech_Moderno durante a inicialização. " +
        "A aplicação continuará disponível e o seed poderá ser executado novamente quando o SQL Server estiver acessível.");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.MapHealthChecks("/health");

app.Run();
