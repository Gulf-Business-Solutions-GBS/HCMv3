using System.Collections.Generic;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Plain response-model POCOs for the vendor "Biotime"-style time-attendance cloud API (ported verbatim,
/// property-for-property, from the nested classes at the bottom of legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.TimeAttendanceIntegrationService</c>). These are simple JSON
/// deserialization targets (via <c>Newtonsoft.Json</c>/<c>System.Text.Json</c>) for the vendor's actual HTTP
/// responses -- they carry no business logic of their own, hence "Dto"-like POCOs living alongside the
/// domain service that consumes them rather than under Application.Shared (they are never returned to our
/// own clients, only consumed internally by <see cref="ITimeAttendanceIntegrationService"/>).
/// </summary>
public class TokenResponse
{
    public string token { get; set; }
}

public class ApiErrorResponse
{
    public ErrorDetail error { get; set; }

    public class ErrorDetail
    {
        public string code { get; set; }
        public string message { get; set; }
        public InnerError innererror { get; set; }

        public class InnerError
        {
            public string message { get; set; }
            public string type { get; set; }
            public string stacktrace { get; set; }
        }
    }
}

/// <summary>Ported from legacy nested <c>CheckInOut</c> class (a single punch record in the API response).</summary>
public class CheckInOutRecord
{
    public long id { get; set; }
    public string emp_code { get; set; }
    public string punch_time { get; set; }
    public string punch_state { get; set; }
    public int verify_type { get; set; }
    public string work_code { get; set; }
    public string terminal_sn { get; set; }
    public string terminal_alias { get; set; }
    public string area_alias { get; set; }
    public object longitude { get; set; }
    public object latitude { get; set; }
    public object gps_location { get; set; }
    public object mobile { get; set; }
    public int source { get; set; }
    public int purpose { get; set; }
    public string crc { get; set; }
    public object is_attendance { get; set; }
    public object reserved { get; set; }
    public string upload_time { get; set; }
    public object sync_status { get; set; }
    public object sync_time { get; set; }
    public object temperature { get; set; }
    public object mask_flag { get; set; }
    public int timeevents_sync_status { get; set; }
    public object timeevents_sync_time { get; set; }
    public string company { get; set; }
    public string emp { get; set; }
    public int terminal { get; set; }
}

/// <summary>Ported from legacy nested <c>Header</c> class -- a paged page of <see cref="CheckInOutRecord"/> data.</summary>
public class CheckInOutHeader
{
    public int count { get; set; }
    public string next { get; set; }
    public string previous { get; set; }
    public string msg { get; set; }
    public int code { get; set; }
    public List<CheckInOutRecord> data { get; set; }
}

public class ApiArea
{
    public int id { get; set; }
    public string area_code { get; set; }
    public string area_name { get; set; }
}

/// <summary>Ported from legacy nested <c>Device</c> class.</summary>
public class ApiDevice
{
    public int id { get; set; }
    public string sn { get; set; }
    public string ip_address { get; set; }
    public string alias { get; set; }
    public string terminal_name { get; set; }
    public string fw_ver { get; set; }
    public string push_ver { get; set; }
    public string state { get; set; }
    public int terminal_tz { get; set; }
    public ApiArea area { get; set; }
    public string last_activity { get; set; }
    public int user_count { get; set; }
    public int fp_count { get; set; }
    public int face_count { get; set; }
    public int palm_count { get; set; }
    public int transaction_count { get; set; }
    public string push_time { get; set; }
    public string transfer_time { get; set; }
    public int transfer_interval { get; set; }
    public int is_attendance { get; set; }
    public string area_name { get; set; }
}

/// <summary>
/// Ported from legacy nested <c>Devices</c> class. NOTE: legacy typed <c>next</c>/<c>previous</c> as
/// <c>object</c> here (vs. <c>string</c> on <see cref="CheckInOutHeader"/>) -- preserved as-is since the
/// paging-follow logic in <c>InsertTransactionFromApiAsync</c> just calls <c>.ToString()</c> on it.
/// </summary>
public class ApiDevicesPage
{
    public int count { get; set; }
    public object next { get; set; }
    public object previous { get; set; }
    public string msg { get; set; }
    public int code { get; set; }
    public List<ApiDevice> data { get; set; }
}

public class ApiEmployeeAttendanceFlags
{
    public int id { get; set; }
    public bool enable_attendance { get; set; }
    public bool enable_overtime { get; set; }
    public bool enable_holiday { get; set; }
    public bool enable_schedule { get; set; }
}

public class ApiDepartment
{
    public int id { get; set; }
    public string dept_code { get; set; }
    public string dept_name { get; set; }
}

public class ApiPosition
{
    public int id { get; set; }
    public string position_code { get; set; }
    public string position_name { get; set; }
}

/// <summary>Ported from legacy nested <c>EmployeeData</c> class.</summary>
public class ApiEmployeeData
{
    public int id { get; set; }
    public string emp_code { get; set; }
    public string first_name { get; set; }
    public object last_name { get; set; }
    public object nickname { get; set; }
    public string format_name { get; set; }
    public string photo { get; set; }
    public string device_password { get; set; }
    public object card_no { get; set; }
    public ApiDepartment department { get; set; }
    public ApiPosition position { get; set; }
    public string hire_date { get; set; }
    public string gender { get; set; }
    public object birthday { get; set; }
    public int? verify_mode { get; set; }
    public object emp_type { get; set; }
    public object contact_tel { get; set; }
    public object office_tel { get; set; }
    public object mobile { get; set; }
    public object national { get; set; }
    public object city { get; set; }
    public object address { get; set; }
    public object postcode { get; set; }
    public object email { get; set; }
    public string enroll_sn { get; set; }
    public object ssn { get; set; }
    public object religion { get; set; }
    public ApiEmployeeAttendanceFlags attemployee { get; set; }
    public int dev_privilege { get; set; }
    public List<ApiArea> area { get; set; }
    public int app_status { get; set; }
    public int app_role { get; set; }
    public string update_time { get; set; }
    public string fingerprint { get; set; }
    public string face { get; set; }
    public string palm { get; set; }
    public object vl_face { get; set; }
}

/// <summary>Ported from legacy nested <c>EmployeeHeader</c> class.</summary>
public class ApiEmployeeHeader
{
    public int count { get; set; }
    public object next { get; set; }
    public object previous { get; set; }
    public string msg { get; set; }
    public int code { get; set; }
    public List<ApiEmployeeData> data { get; set; }
}
