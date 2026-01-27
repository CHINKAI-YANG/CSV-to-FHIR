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
                    HasHeaderRecord = true, // 👈 重要：改回 true 才能正確跳過第一行標題
                    PrepareHeaderForMatch = args => args.Header.Trim(), 
                    HeaderValidated = null, 
                    MissingFieldFound = null
                };

                using var csv = new CsvReader(reader, config);
                // 讀取資料，CsvHelper 會根據你在 Model 設好的 [Name] 自動對應
                var records = csv.GetRecords<TeleERCsvRecord>().ToList();

                // --- 🛡️ 防呆檢查開始 ---
                foreach (var doctorGroup in records.GroupBy(r => r.Pract_ID))
                {
                    // 確保同一個醫師的時間是按先後順序排列
                    var sortedSlots = doctorGroup.OrderBy(r => DateTimeOffset.Parse($"{r.Slot_Date} {r.Time_Start}")).ToList();
                    
                    for (int i = 0; i < sortedSlots.Count; i++)
                    {
                        var current = sortedSlots[i];
                        var start = DateTimeOffset.Parse($"{current.Slot_Date} {current.Time_Start}");
                        var end = DateTimeOffset.Parse($"{current.Slot_Date} {current.Time_End}");

                        // 1. 基本邏輯：結束不能早於開始
                        if (end <= start)
                        {
                            return BadRequest($"【資料錯誤】醫師 {current.Pract_ID} 在 {current.Slot_Date} 的時段 ({current.Time_Start} - {current.Time_End}) 結束時間不可早於開始。");
                        }

                        // 2. 衝突檢查：檢查與下一筆是否有重疊
                        if (i < sortedSlots.Count - 1)
                        {
                            var next = sortedSlots[i + 1];
                            var nextStart = DateTimeOffset.Parse($"{next.Slot_Date} {next.Time_Start}");
                            
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
                    bundle.AddResourceEntry(slot, $"Slot/{Guid.NewGuid()}");
                }

                var serializer = new FhirJsonSerializer();
                return Ok(serializer.SerializeToString(bundle));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"伺服器錯誤：{ex.Message}");
            }
        }
    }
}