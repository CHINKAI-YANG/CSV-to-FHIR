using Microsoft.AspNetCore.Mvc;
using schedule_slot.Services; 
using schedule_slot.Models;   
using CsvHelper;
using System.Globalization;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using CsvHelper.Configuration;

namespace schedule_slot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduleUploadController : ControllerBase
    {
        private readonly FhirMappingService _mappingService;

        public ScheduleUploadController(FhirMappingService mappingService)
        {
            _mappingService = mappingService;
        }

        [HttpPost("upload-single")]
[HttpPost("upload")]
public IActionResult UploadCsv(IFormFile file)
{
    if (file == null || file.Length == 0) return BadRequest("請選擇檔案");

    try 
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true, // 確保你的 CSV 有標題列
            PrepareHeaderForMatch = args => args.Header.Trim(),
            HeaderValidated = null,
            MissingFieldFound = null
        };

        using var csv = new CsvHelper.CsvReader(reader, config);
        
        // 取得第一筆資料
        var firstRecord = csv.GetRecords<TeleERCsvRecord>().FirstOrDefault();

        if (firstRecord == null) 
        {
            return BadRequest("CSV 檔案內沒有發現有效的資料列。");
        }

        // 轉換為單一 Slot 物件 (不包裝 Bundle)
        var slot = _mappingService.MapToFhirSlot(firstRecord, firstRecord.Sched_ID);

        // 回傳單一 JSON 物件
        var serializer = new Hl7.Fhir.Serialization.FhirJsonSerializer();
        return Ok(serializer.SerializeToString(slot));
    }
    catch (Exception ex)
    {
        // 發生錯誤時回傳具體的錯誤訊息，方便前端顯示
        return BadRequest($"轉換失敗：{ex.Message}");
    }
}
    }
}