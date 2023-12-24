using ACEditor.Props;
using Decal.Adapter;

//using Decal.Adapter;
//using Decal.Adapter.Wrappers;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using UtilityBelt.Scripting.Interop;


namespace ACEditor.Table;

public class PropertyFilter
{
    public string Name { get; set; } = "";
    public string Label { get; set; }

    public PropType Type { get; set; } = PropType.Unknown;
    public PropertyData Target { get; set; } = new();

    public bool ShowName { get; set; } = true;

    public bool ShowIncludeMissing { get; set; } = true;
    public bool IncludeMissing = false;

    public bool UseFilter { get; set; } = true;
    //public bool UseRegex { get; set; } = true;

    public int SelectedIndex = 0;
    public string Selection => SelectedIndex < Props.Length ? Props[SelectedIndex] : null;

    //Name of Property Enum keys
    public string[] Props { get; set; } = new string[0];
    //Value of Property Enum
    public int[] PropKeys { get; set; } = new int[0];

    public string FilterText = "";

    public bool Changed { get; set; } = false;

    public PropertyFilter(PropType type)
    {
        Type = type;
        Label = Type.ToString();
        Name = Label;
    }

    public void Render()
    {
        //Todo: when to reset?
        Changed = false;

        if (ShowName)
        {
            ImGui.LabelText($"###{Label}", $"{Name} ({Props.Length})");
            ImGui.SameLine();
        }

        if (ImGui.Combo($"###{Label}Combo", ref SelectedIndex, Props, Props.Length))
        {
            C.Chat(Selection ?? "");
        }

        if (UseFilter)
        {
            ImGui.SetNextItemWidth(200);
            ImGui.SameLine();

            if (ImGui.InputText($"{Name}###{Label}Filter", ref FilterText, 256))
            {
                UpdateFilter();
            }
        }

        if (ShowIncludeMissing)
        {
            ImGui.SameLine();
            if (ImGui.Checkbox($"Include Missing?###{Label}IncMiss", ref IncludeMissing))
                UpdateFilter();
        }
    }

    public void UpdateFilter()
    {
        Changed = true;

        //Get Target props
        Props = Target is null || IncludeMissing ? Type.GetProps() : Type.GetProps(Target);
        PropKeys = Target is null || IncludeMissing ? Type.GetPropKeys() : Type.GetPropKeys(Target);

        //Apply filter
        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            var regex = new Regex(FilterText ?? "", RegexOptions.IgnoreCase);

            Props = Props.Where(x => regex.IsMatch(x)).ToArray();
        }

        //C.Chat("Filter changed");
    }

    internal void SetTarget(PropertyData target)
    {
        Target = target;
        UpdateFilter();
    }
}
