using schedule_slot.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. 註冊基礎 API 功能
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 2. 註冊 Swagger (這裡不用寫 using，它會自動抓)
builder.Services.AddSwaggerGen();

// 3. 註冊你的翻譯官
builder.Services.AddScoped<FhirMappingService>();

var app = builder.Build();
app.UseDefaultFiles(); // 自動尋找 index.html
app.UseStaticFiles();  // 允許讀取 wwwroot 裡的檔案
// 4. 設定 Swagger 介面
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    });
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();