# 📅 Timetable CSV to FHIR Specification

---

## 1. CSV Data Field Definitions (Input)

| Field ID | Field Name | Type | Format Example | Description |
|----------|------------|------|---------------|------------|
| Sched_ID | Schedule Plan ID | String | ER-2026-001 | Unique identifier used to link multiple Slots |
| Pract_ID | Practitioner ID | String | DOC12345 | Maps to the Practitioner resource in the system |
| Dept_ID | Department Code | Code | 02 | Department code for the Emergency Department |
| Slot_Date | Consultation Date | Date | 2026-01-22 | YYYY-MM-DD |
| Time_Start | Start Time | Time | 20:00 | HH:mm (24-hour format) |
| Time_End | End Time | Time | 20:15 | HH:mm (24-hour format) |
| Slot_Type | Consultation Level | String | EMERGENCY | Indicates urgency/type of the appointment |
| Channel | Communication Channel | Code | Video | Video, Phone, or Chat |
| Capacity | Capacity | Int | 1 | Maximum number of bookings allowed for this slot |

---

## 2. FHIR Resource Mapping Table (Schedule)

| CSV Field | FHIR Element Path | Mapping Description |
|----------|------------------|-------------------|
| Sched_ID | Schedule.identifier | Sets the external unique identifier |
| Pract_ID | Schedule.actor | Reference: `Practitioner/DOC12345` |
| Dept_ID | Schedule.serviceType | Uses a CodeSystem to create a CodeableConcept |

---

## 3. Slot Resource Mapping

| FHIR Element | Source CSV Field | Transformation Logic & Remarks |
|-------------|----------------|------------------------------|
| id | Slot_UID | Format: `[Sched_ID]-[Seq]` to ensure uniqueness |
| status | (Fixed Value) | Default = `free`; set to `busy-unavailable` if doctor unavailable |
| start | Slot_Date + Time_Start | Convert to ISO 8601: `YYYY-MM-DDTHH:mm:ss+08:00` |
| end | Slot_Date + Time_End | Convert to ISO 8601: `YYYY-MM-DDTHH:mm:ss+08:00` |
| appointmentType | Slot_Type | Map to HL7 v2-0276 (e.g., EMERGENCY) |
| schedule | Sched_ID | Reference to corresponding Schedule |
| extension:channel | Channel | Distinguish Video / Phone / Chat |
| extension:capacity | Capacity | Defines booking limit (e.g., Emergency = 1) |
| comment | Remark | Additional notes or instructions |

---

## 4. System Conversion Flowchart

```mermaid
graph TD
    A[Start Reading CSV File] --> B{Parse Row by Row}
    B --> C[Data Format & Timezone Validation]
    C --> D{Determine Schedule Resource}
    
    D -- Does Not Exist --> E[Create New Schedule: Practitioner + Dept]
    D -- Exists --> F[Retrieve Existing Schedule ID]
    
    E --> G[Generate Slot Resource]
    F --> G
    
    G --> H[Merge Slot_Date + Time_Start to ISO 8601]
    H --> I[Encapsulate Channel & Capacity into Extensions]
    I --> J[Set Slot.schedule to point to Schedule ID]
    J --> K[Pack into Bundle Transaction]
    
    K --> L{More Rows?}
    L -- Yes --> B
    L -- No --> M[Send POST to FHIR Server]
    
    M --> N[End]
    
