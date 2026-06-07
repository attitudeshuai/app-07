using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PointsMall.BackgroundServices;
using PointsMall.Data;
using PointsMall.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PointsMall API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var useInMemoryDb = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (useInMemoryDb)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("PointsMallDB"));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        var serverVersionStr = builder.Configuration["MySqlServerVersion"];
        ServerVersion? serverVersion = null;
        if (!string.IsNullOrEmpty(serverVersionStr))
        {
            serverVersion = ServerVersion.Parse(serverVersionStr);
        }
        else
        {
            try
            {
                serverVersion = ServerVersion.AutoDetect(connectionString);
            }
            catch
            {
                serverVersion = ServerVersion.Parse("8.0.33-mysql");
            }
        }
        options.UseMySql(connectionString, serverVersion);
    });
}

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddScoped<IMemberLevelService, MemberLevelService>();
builder.Services.AddScoped<IFlashSaleService, FlashSaleService>();
builder.Services.AddScoped<IFlashSaleReservationService, FlashSaleReservationService>();
builder.Services.AddScoped<ICheckInService, CheckInService>();
builder.Services.AddScoped<ILogisticsService, LogisticsService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPointsService, PointsService>();

builder.Services.AddHostedService<OrderAutoCompleteService>();
builder.Services.AddHostedService<PointsExpiryBackgroundService>();
builder.Services.AddHostedService<FlashSaleReminderBackgroundService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    
    if (!context.Users.Any(u => u.Username == "admin"))
    {
        var admin = new PointsMall.Models.User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            Role = "Admin",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        context.Users.Add(admin);
        context.SaveChanges();
        Console.WriteLine("Admin account created successfully: admin / 123456");
    }

    if (!context.MemberUsers.Any())
    {
        var sampleUsers = new List<PointsMall.Models.MemberUser>
        {
            new PointsMall.Models.MemberUser
            {
                Username = "zhangsan",
                Nickname = "张三",
                Phone = "13800138001",
                Email = "zhangsan@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Points = 2500,
                TotalPoints = 3000,
                Status = "Active"
            },
            new PointsMall.Models.MemberUser
            {
                Username = "lisi",
                Nickname = "李四",
                Phone = "13900139002",
                Email = "lisi@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Points = 1800,
                TotalPoints = 1800,
                Status = "Active"
            },
            new PointsMall.Models.MemberUser
            {
                Username = "wangwu",
                Nickname = "王五",
                Phone = "13700137003",
                Email = "wangwu@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Points = 500,
                TotalPoints = 1500,
                Status = "Active"
            },
            new PointsMall.Models.MemberUser
            {
                Username = "zhaoliu",
                Nickname = "赵六",
                Phone = "13600136004",
                Email = "zhaoliu@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Points = 0,
                TotalPoints = 0,
                Status = "Inactive"
            }
        };

        foreach (var user in sampleUsers)
        {
            if (user.Points > 0)
            {
                user.PointsRecords.Add(new PointsMall.Models.PointsRecord
                {
                    Type = "Income",
                    Points = user.Points,
                    Balance = user.Points,
                    Source = "System",
                    Remark = "初始积分赠送",
                    ExpireAt = DateTime.Now.AddDays(365),
                    AvailablePoints = user.Points,
                    IsExpired = false
                });
            }
            context.MemberUsers.Add(user);
        }
        context.SaveChanges();
        Console.WriteLine("Sample member users created successfully");
    }

    if (!context.MemberLevels.Any())
    {
        var defaultLevels = new List<PointsMall.Models.MemberLevel>
        {
            new PointsMall.Models.MemberLevel
            {
                Name = "青铜",
                MinPoints = 0,
                DiscountRate = 1.0m,
                Description = "初始会员等级，享受基础权益",
                SortOrder = 1,
                IsActive = true
            },
            new PointsMall.Models.MemberLevel
            {
                Name = "白银",
                MinPoints = 1000,
                DiscountRate = 0.95m,
                Description = "白银会员，享受95折优惠",
                SortOrder = 2,
                IsActive = true
            },
            new PointsMall.Models.MemberLevel
            {
                Name = "黄金",
                MinPoints = 5000,
                DiscountRate = 0.9m,
                Description = "黄金会员，享受9折优惠",
                SortOrder = 3,
                IsActive = true
            },
            new PointsMall.Models.MemberLevel
            {
                Name = "钻石",
                MinPoints = 10000,
                DiscountRate = 0.8m,
                Description = "钻石会员，享受8折优惠",
                SortOrder = 4,
                IsActive = true
            }
        };

        context.MemberLevels.AddRange(defaultLevels);
        context.SaveChanges();
        Console.WriteLine("Default member levels created successfully");
    }

    if (!context.Categories.Any())
    {
        var digitalCategory = new PointsMall.Models.Category
        {
            Name = "数码产品",
            ParentId = null,
            SortOrder = 1,
            IsActive = true
        };

        var homeCategory = new PointsMall.Models.Category
        {
            Name = "家居用品",
            ParentId = null,
            SortOrder = 2,
            IsActive = true
        };

        var officeCategory = new PointsMall.Models.Category
        {
            Name = "办公用品",
            ParentId = null,
            SortOrder = 3,
            IsActive = true
        };

        context.Categories.AddRange(digitalCategory, homeCategory, officeCategory);
        context.SaveChanges();

        var earphoneCategory = new PointsMall.Models.Category
        {
            Name = "耳机",
            ParentId = digitalCategory.Id,
            SortOrder = 1,
            IsActive = true
        };

        var phoneCategory = new PointsMall.Models.Category
        {
            Name = "手机",
            ParentId = digitalCategory.Id,
            SortOrder = 2,
            IsActive = true
        };

        var cupCategory = new PointsMall.Models.Category
        {
            Name = "水杯",
            ParentId = homeCategory.Id,
            SortOrder = 1,
            IsActive = true
        };

        var umbrellaCategory = new PointsMall.Models.Category
        {
            Name = "雨具",
            ParentId = homeCategory.Id,
            SortOrder = 2,
            IsActive = true
        };

        var notebookCategory = new PointsMall.Models.Category
        {
            Name = "笔记本",
            ParentId = officeCategory.Id,
            SortOrder = 1,
            IsActive = true
        };

        context.Categories.AddRange(earphoneCategory, phoneCategory, cupCategory, umbrellaCategory, notebookCategory);
        context.SaveChanges();
        Console.WriteLine("Default categories created successfully");
    }

    if (!context.Products.Any())
    {
        var cupCategory = context.Categories.FirstOrDefault(c => c.Name == "水杯" && c.ParentId != null);
        var notebookCategory = context.Categories.FirstOrDefault(c => c.Name == "笔记本" && c.ParentId != null);
        var umbrellaCategory = context.Categories.FirstOrDefault(c => c.Name == "雨具" && c.ParentId != null);
        var earphoneCategory = context.Categories.FirstOrDefault(c => c.Name == "耳机" && c.ParentId != null);
        var digitalCategory = context.Categories.FirstOrDefault(c => c.Name == "数码产品" && c.ParentId == null);

        var sampleProducts = new List<PointsMall.Models.Product>
        {
            new PointsMall.Models.Product
            {
                Name = "精美水杯",
                Description = "高品质不锈钢保温杯，500ml容量",
                PointsRequired = 500,
                Stock = 100,
                ImageUrl = "https://via.placeholder.com/300",
                IsActive = true,
                CategoryId = cupCategory != null ? cupCategory.Id : null
            },
            new PointsMall.Models.Product
            {
                Name = "商务笔记本",
                Description = "A5尺寸，优质纸张，100页",
                PointsRequired = 300,
                Stock = 200,
                ImageUrl = "https://via.placeholder.com/300",
                IsActive = true,
                CategoryId = notebookCategory != null ? notebookCategory.Id : null
            },
            new PointsMall.Models.Product
            {
                Name = "品牌雨伞",
                Description = "全自动折叠伞，防紫外线",
                PointsRequired = 800,
                Stock = 50,
                ImageUrl = "https://via.placeholder.com/300",
                IsActive = true,
                CategoryId = umbrellaCategory != null ? umbrellaCategory.Id : null
            },
            new PointsMall.Models.Product
            {
                Name = "无线耳机",
                Description = "蓝牙耳机，降噪功能",
                PointsRequired = 2000,
                Stock = 20,
                ImageUrl = "https://via.placeholder.com/300",
                IsActive = true,
                CategoryId = earphoneCategory != null ? earphoneCategory.Id : null
            },
            new PointsMall.Models.Product
            {
                Name = "运动手环",
                Description = "智能手环，心率监测",
                PointsRequired = 1500,
                Stock = 30,
                ImageUrl = "https://via.placeholder.com/300",
                IsActive = false,
                CategoryId = digitalCategory != null ? digitalCategory.Id : null
            }
        };

        context.Products.AddRange(sampleProducts);
        context.SaveChanges();
        Console.WriteLine("Sample products created successfully");
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PointsMall API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
