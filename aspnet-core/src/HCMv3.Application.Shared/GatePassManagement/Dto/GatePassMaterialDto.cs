using System;
using Abp.Application.Services.Dto;

namespace HCMv3.GatePassManagement.Dto;

public class GatePassMaterialDto : EntityDto<int>
{
    public int GatePassId { get; set; }

    public string ItemCode { get; set; }

    public string ItemName { get; set; }

    public int Quantity { get; set; }

    public DateTime ExpectedReturnDate { get; set; }

    public int ReturnedQuantity { get; set; }
}
