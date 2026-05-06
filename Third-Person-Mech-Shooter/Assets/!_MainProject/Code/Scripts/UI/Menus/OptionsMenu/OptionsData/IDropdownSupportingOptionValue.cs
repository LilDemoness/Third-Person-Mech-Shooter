using System.Collections.Generic;

public interface IDropdownSupportingOptionValue
{
    public void SetValue(int dropdownIndex);
    public int GetSelectedOptionIndex();
    public List<string> GetDropdownOptions();
}