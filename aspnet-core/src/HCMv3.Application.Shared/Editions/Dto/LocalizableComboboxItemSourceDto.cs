using System.Collections.ObjectModel;

namespace HCMv3.Editions.Dto;

//Mapped in CustomDtoMapper
public class LocalizableComboboxItemSourceDto
{
    public Collection<LocalizableComboboxItemDto> Items { get; set; }
}

