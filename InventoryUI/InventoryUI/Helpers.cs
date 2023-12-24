using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ImGuiNET;


namespace ACEditor;

public class Helpers
{

    private static Dictionary<string, SearchData> searchData;
    private static string currentlyActiveId;

    /// <summary>
    /// Begins a table with filters and the ability to search through the data.
    /// Uses the ImGui Table API.
    /// </summary>
    /// <param name="id">The ID for the table.</param>
    /// <param name="type">The type of the class from which the data should be extracted.</param>
    /// <param name="options">Use a preset form of options to determine behaviour and appearance of the table.</param>
    public static bool BeginSearchTable(string id, Type type, SearchOptions? options = null)
    {
        options ??= new SearchOptions();
        searchData ??= new Dictionary<string, SearchData>();
        if (!searchData.ContainsKey(id))
        {
            var search = new SearchData();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            search.fieldNames = new string[fields.Length];
            search.fieldTypes = new Type[fields.Length];
            search.fieldWidths = new float[fields.Length];
            search.fieldRelationship = new Type[fields.Length];
            search.fieldRelationshipFieldName = new string[fields.Length];
            search.fieldFilters = new object[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                search.fieldNames[i] = fields[i].Name;
                search.fieldTypes[i] = fields[i].FieldType;
                if (options.Options.ContainsKey(search.fieldNames[i]))
                {
                    var fieldOptions = options.Options[search.fieldNames[i]];
                    search.fieldWidths[i] = fieldOptions.Width;
                    if (fieldOptions.RelationshipType != null)
                    {
                        search.fieldRelationship[i] = fieldOptions.RelationshipType;
                        search.fieldRelationshipFieldName[i] = fieldOptions.RelationshipFieldName;
                    }
                }
                else
                {
                    search.fieldWidths[i] = 150f;
                }


            }
            searchData.Add(id, search);
        }

        var data = searchData[id];
        for (int i = 0; i < data.fieldNames.Length; i++)
        {
            ImGui.TableSetupColumn(data.fieldNames[i], ImGuiTableColumnFlags.None, data.fieldWidths[i]);
        }

        ImGuiTableFlags flags = ImGuiTableFlags.None;
        if (options.CanOrderColumns)
            flags |= ImGuiTableFlags.Reorderable;

        flags |= ImGuiTableFlags.Resizable;
        flags |= ImGuiTableFlags.Sortable;
        flags |= ImGuiTableFlags.ContextMenuInBody;

        var isOpen = ImGui.BeginTable(id, data.fieldNames.Length, flags);
        if (isOpen)
        {
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
            ImGui.TableNextRow();

            for (int i = 0; i < data.fieldFilters.Length; i++)
            {
                var filterType = data.fieldTypes[i];
                if (filterType == typeof(string))
                {
                    string value = string.Empty;
                    if (data.fieldFilters[i] != null)
                    {
                        value = data.fieldFilters[i].ToString();
                    }
                    ImGui.InputText("##Filter" + i, ref value, 256);
                    data.fieldFilters[i] = value;
                }
                else if (filterType == typeof(int))
                {
                    int value = 0;
                    if (data.fieldFilters[i] != null)
                    {
                        value = (int)data.fieldFilters[i];
                    }
                    ImGui.InputInt("##Filter" + i, ref value);
                    data.fieldFilters[i] = value;
                }
                else if (filterType == typeof(float))
                {
                    float value = 0;
                    if (data.fieldFilters[i] != null)
                    {
                        value = (float)data.fieldFilters[i];
                    }
                    ImGui.InputFloat("##Filter" + i, ref value);
                    data.fieldFilters[i] = value;
                }
                else if (filterType == typeof(double))
                {
                    double value = 0;
                    if (data.fieldFilters[i] != null)
                    {
                        value = (double)data.fieldFilters[i];
                    }
                    ImGui.InputDouble("##Filter" + i, ref value);
                    data.fieldFilters[i] = value;
                }
                else if (filterType == typeof(bool))
                {
                    bool value = false;
                    if (data.fieldFilters[i] != null)
                    {
                        value = (bool)data.fieldFilters[i];
                    }
                    ImGui.Checkbox("##Filter" + i, ref value);
                    data.fieldFilters[i] = value;
                }


                ImGui.TableNextColumn();
            }

            currentlyActiveId = id;
        }

        return isOpen;
    }

    /// <summary>
    /// The data to use for populating this table. It is best to pre-load data before rendering.
    /// Specify the range of the data to display. If using thousands of records, it is best to
    /// specify start and end for efficiency.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data">The data to render into the table.</param>
    /// <param name="start">The beginning of the range to acquire from data.</param>
    /// <param name="end">The end of the range to acquire from data.</param>
    public static void SearchTableNeedsData<T>(IEnumerable<T> data, int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            var item = data.ElementAt(i);
            ImGui.TableNextRow();
        }
    }
}


public class SearchData
{

    public string[] fieldNames;
    public float[] fieldWidths;
    public Type[] fieldTypes;
    public Type[] fieldRelationship;
    public string[] fieldRelationshipFieldName;
    public object[] fieldFilters;

    public SearchData()
    {
        fieldNames = Array.Empty<string>();
        fieldWidths = Array.Empty<float>();
        fieldTypes = Array.Empty<Type>();
        fieldRelationship = Array.Empty<Type>();
        fieldRelationshipFieldName = Array.Empty<string>();
        fieldFilters = Array.Empty<object>();
    }


}

public class OptionData
{

    public float Width;
    public Type? RelationshipType;
    public string RelationshipFieldName;

    public OptionData()
    {
        RelationshipFieldName = string.Empty;
        Width = 150f;
    }

}
public class SearchOptions
{

    private Dictionary<string, OptionData> options;
    private bool noOrdering;

    public bool CanOrderColumns { get => !noOrdering; }

    public IDictionary<string, OptionData> Options { get => options; }

    public SearchOptions()
    {
        options = new Dictionary<string, OptionData>();
    }

    /// <summary>
    /// Set the width of the specified column.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <param name="width">The display width of the given field in search.</param>
    public void SetColumnWidth(string fieldName, float width)
    {
        if (options.ContainsKey(fieldName))
        {
            options[fieldName].Width = width;
        }
        else
        {
            options.Add(fieldName, new OptionData() { Width = width });
        }
    }

    /// <summary>
    /// Setup a relationship between a given field and the field to display from another type.
    /// </summary>
    /// <param name="fieldName">The name of the field to assign the relationship.</param>
    /// <param name="relType">The type that represents the relationship.</param>
    /// <param name="relFieldName">The field used as the display value from the relationship type.</param>
    public void SetRelationship(string fieldName, Type relType, string relFieldName)
    {
        if (options.ContainsKey(fieldName))
        {
            options[fieldName].RelationshipType = relType;
            options[fieldName].RelationshipFieldName = relFieldName;
        }
        else
        {
            options.Add(fieldName, new OptionData() { RelationshipType = relType, RelationshipFieldName = relFieldName });
        }
    }

    /// <summary>
    /// Prevent manual ordering of columns.
    /// </summary>
    public void DisableOrdering()
    {
        noOrdering = true;
    }

}