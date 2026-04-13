using ATEA_test.API.AutoMapperSetup;
using ATEA_test.API.DependencyInjection;
using ATEA_test.API.Gateways;
using ATEA_test.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAutoMapper([typeof(ProfileAssemblyInfo)]);
builder.Services.AddSingleton<IOrderService, OrderService>();
builder.Services.AddSingleton<IPaymentGateway, AlphaPaymentGateway>();
builder.Services.AddSingleton<IPaymentGateway, BetaPaymentGateway>();
builder.Services.AddSingleton<IPaymentGateway, GammaPaymentGateway>();
builder.Services.AddSingleton<IPaymentGatewayRegistry, PaymentGatewayRegistry>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
var app = builder.Build();

DIFactory.Initialize(app.Services);
app.UseCors("MyPolicy");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();
