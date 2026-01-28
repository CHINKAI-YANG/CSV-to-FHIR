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

        [HttpPost("upload")]
public IActionResult UploadCsv(IFormFile file)
{
    if (file == null || file.Length == 0) return BadRequest("請選擇檔案");

    try 
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            PrepareHeaderForMatch = args => args.Header.Trim(), 
            HeaderValidated = null, 
            MissingFieldFound = null
        };

        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<TeleERCsvRecord>().ToList();

        // --- 🛡️ 防呆檢查開始 ---
        foreach (var doctorGroup in records.GroupBy(r => r.Pract_ID))
        {
            // 使用 InvariantCulture 確保日期解析穩定，解決「黃色警告」與解析失敗問題
            var sortedSlots = doctorGroup.OrderBy(r => 
                DateTimeOffset.Parse($"{r.Slot_Date} {r.Time_Start}", CultureInfo.InvariantCulture)).ToList();
            
            for (int i = 0; i < sortedSlots.Count; i++)
            {
                var current = sortedSlots[i];
                var start = DateTimeOffset.Parse($"{current.Slot_Date} {current.Time_Start}", CultureInfo.InvariantCulture);
                var end = DateTimeOffset.Parse($"{current.Slot_Date} {current.Time_End}", CultureInfo.InvariantCulture);

                if (end <= start)
                {
                    return BadRequest($"【資料錯誤】醫師 {current.Pract_ID} 在 {current.Slot_Date} 的時段結束不可早於開始。");
                }

                if (i < sortedSlots.Count - 1)
                {
                    var next = sortedSlots[i + 1];
                    var nextStart = DateTimeOffset.Parse($"{next.Slot_Date} {next.Time_Start}", CultureInfo.InvariantCulture);
                    
                    if (end > nextStart)
                    {
                        return BadRequest($"【時間衝突】醫師 {current.Pract_ID} 發生重疊！目前的結束時間 {current.Time_End} 已蓋過下一個時段的開始 {next.Time_Start}。");
                    }
                }
            }
        }
        // --- 🛡️ 防呆檢查結束 ---

        var bundle = new Bundle { Type = Bundle.BundleType.Transaction };
        foreach (var row in records)
        {
            var slot = _mappingService.MapToFhirSlot(row, row.Sched_ID);
            // 💡 Transaction 模式通常需要 Request 資訊，否則某些 Server 會拒絕
            var entry = bundle.AddResourceEntry(slot, $"Slot/{Guid.NewGuid()}");
            entry.Request = new Bundle.RequestComponent { Method = Bundle.HTTPVerb.POST, Url = "Slot" };
        }

        var serializer = new FhirJsonSerializer();
        return Ok(serializer.SerializeToString(bundle));
    }
    catch (Exception ex)
    {
        // 這樣寫能幫你在網頁上看到到底是哪一行出錯
        return BadRequest($"解析 CSV 時發生崩潰：{ex.Message}");
    }
}
    }
}