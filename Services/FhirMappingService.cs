using schedule_slot.Models; // 👈 引用 Models 房間

namespace schedule_slot.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

public class FhirMappingService
{
    public Slot MapToFhirSlot(TeleERCsvRecord row, string scheduleId)
    {
        // 1. 合併時間並補上台灣時區 (+08:00)
        var startDateTime = DateTimeOffset.Parse($"{row.Slot_Date} {row.Time_Start} +08:00");
        var endDateTime = DateTimeOffset.Parse($"{row.Slot_Date} {row.Time_End} +08:00");

        // 2. 建立一個全新的 Slot 物件
        var slot = new Slot
        {
            Status = Slot.SlotStatus.Free, // 預設為可預約
            StartElement = new Instant(startDateTime),
            EndElement = new Instant(endDateTime),
            
            // 連結回排班表 (Schedule)
            Schedule = new ResourceReference($"Schedule/{scheduleId}"),
            
            // 設定諮詢等級 (例如：急症)
            AppointmentType = new CodeableConcept("http://terminology.hl7.org/CodeSystem/v2-0276", "EMERGENCY")
        };

        // 3. 貼上「擴充標籤」：通訊管道 (視訊/電話)
        slot.Extension.Add(new Extension(FhirConstants.UrlChannel, new FhirString(row.Channel)));

        // 4. 貼上「擴充標籤」：掛號人數上限
        slot.Extension.Add(new Extension(FhirConstants.UrlCapacity, new Integer(row.Capacity)));

        return slot;
    }
}
