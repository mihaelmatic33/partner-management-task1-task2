using PartnerManagement.Web.Data;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DbOptions>(builder.Configuration.GetSection(DbOptions.SectionName));
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IPartnerRepository, PartnerRepository>();
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
    using var connection = connectionFactory.CreateConnection();
    connection.Execute(
        """
        IF COL_LENGTH('dbo.Partners', 'IsActive') IS NULL
        BEGIN
            ALTER TABLE dbo.Partners
            ADD IsActive BIT NOT NULL CONSTRAINT DF_Partners_IsActive DEFAULT (1);
        END
        """);

    connection.Execute(
        """
        IF EXISTS
        (
            SELECT 1
            FROM sys.key_constraints kc
            WHERE kc.name = 'UQ_Partners_CroatianPIN'
                AND kc.parent_object_id = OBJECT_ID('dbo.Partners')
        )
        BEGIN
            ALTER TABLE dbo.Partners DROP CONSTRAINT UQ_Partners_CroatianPIN;
        END

        IF NOT EXISTS
        (
            SELECT 1
            FROM sys.indexes i
            WHERE i.name = 'UX_Partners_CroatianPIN_NotNull'
                AND i.object_id = OBJECT_ID('dbo.Partners')
        )
        BEGIN
            CREATE UNIQUE INDEX UX_Partners_CroatianPIN_NotNull
                ON dbo.Partners (CroatianPIN)
                WHERE CroatianPIN IS NOT NULL;
        END
        """);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Partners}/{action=Index}/{id?}");

app.Run();
