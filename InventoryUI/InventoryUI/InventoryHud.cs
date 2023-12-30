using ACE.DatLoader.FileTypes;
using ACEditor;
using ACEditor.Props;
using ACEditor.Table;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using ImGuiNET;
using InventoryUI.Comparison;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using UtilityBelt.Common.Enums;
using UtilityBelt.Scripting.Actions;
using UtilityBelt.Scripting.Interop;
using UtilityBelt.Scripting.Lib;
using UtilityBelt.Service.Lib.Settings;
using UtilityBelt.Service.Views;
using WattleScript.Interpreter;
using Hud = UtilityBelt.Service.Views.Hud;
using WorldObject = UtilityBelt.Scripting.Interop.WorldObject;

namespace InventoryUI;

public class InventoryHud : IDisposable
{
    //State
    ScriptHudManager sHud = new();
    readonly Hud hud;
    Game game = new();
    private float Index = 0;
    uint SelectedBag = 0;// game.CharacterId,
    List<WorldObject> filteredItems = new();   //Filtered items to be drawn

    //Options
    bool ShowBags = false;
    bool ShowIcons;
    bool ShowExtraFilter = true;

    //Filters
    //Standard name (maybe more?) filter
    string FilterText = "";
    //Custom filter
    string[] filterTypes =
    {
        PropType.Bool.ToString(),
        PropType.Float.ToString(),
        PropType.Int.ToString(),
        PropType.Int64.ToString(),
        PropType.String.ToString(),
    };
    int filterComboIndex = 2;
    PropertyFilter propFilter = new(PropType.Int);
    PropType propType = PropType.Int;

    //Setup for icon textures
    readonly Vector2 IconSize = new(24, 24);
    const int ICON_PAD = 8;
    const int ICON_COL_WIDTH = 24 + ICON_PAD;
    const int PLAYER_ICON = 0x0600127E;
    Vector4 SELECTED_COLOR = new(200, 200, 0, 255);
    Vector4 UNSELECTED_COLOR = new(0, 0, 0, 0);

    //Setup for item table
    const ImGuiTableFlags TABLE_FLAGS =
    ImGuiTableFlags.BordersInner | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable;
    //ImGuiTableFlags.ContextMenuInBody | ImGuiTableFlags.Sortable;
    readonly Dictionary<ItemColumn, ImGuiTableColumnFlags> COLUMN_FLAGS = new()
    {
        [ItemColumn.Icon] = ImGuiTableColumnFlags.WidthFixed,
        [ItemColumn.Name] = ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthStretch,
        [ItemColumn.Value] = ImGuiTableColumnFlags.WidthFixed,
    };
    public enum ItemColumn
    {
        Icon, Name, Value
    }

    public InventoryHud(Hud hud)
    {
        SelectedBag = game.CharacterId;
        this.hud = hud;

        hud.OnShow += Hud_OnShow;
        //game.World.OnObjectCreated
        game.Messages.Incoming.Qualities_UpdateInstanceID += Incoming_Qualities_UpdateInstanceID;
        game.Messages.Incoming.Qualities_PrivateUpdateInstanceID += Incoming_Qualities_PrivateUpdateInstanceID;
    }

    #region Events
    private void Hud_OnShow(object sender, EventArgs e)
    {
        SetFilteredItems();
    }

    private void Incoming_Qualities_PrivateUpdateInstanceID(object sender, UtilityBelt.Common.Messages.Events.Qualities_PrivateUpdateInstanceID_S2C_EventArgs e)
    {
        SetFilteredItems();
    }

    private void Incoming_Qualities_UpdateInstanceID(object sender, UtilityBelt.Common.Messages.Events.Qualities_UpdateInstanceID_S2C_EventArgs e)
    {
        SetFilteredItems();
    }
    #endregion

    public void Render()
    {
        try
        {
            CheckHotkeys();
            DrawOptions();
            DrawFilters();
            DrawInventory();
        }
        catch (Exception ex)
        {
            PluginCore.Log(ex);
        }
    }

    private void CheckHotkeys()
    {
        //var io = ImGui.GetIO();
        if(ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
        {
            if (ImGui.IsKeyPressed(ImGuiKey.B))
                ShowBags = !ShowBags;
            if (ImGui.IsKeyPressed(ImGuiKey.I))
                ShowIcons = !ShowIcons;
            if (ImGui.IsKeyPressed(ImGuiKey.E))
                ShowExtraFilter = !ShowExtraFilter;


            if (ImGui.IsKeyPressed(ImGuiKey.F)) {
                C.Chat("f");
            }

        }
    }

    #region Draw Item Icons/Table
    private void DrawInventory()
    {
        Index = 0;

        if (ShowBags)
        {
            //Create a 2 - column table for bags and inventory
            if (ImGui.BeginTable("layout", 2, ImGuiTableFlags.BordersInner | ImGuiTableFlags.ContextMenuInBody))
            {
                ImGui.TableSetupColumn("bags", ImGuiTableColumnFlags.NoHeaderLabel | ImGuiTableColumnFlags.WidthFixed, IconSize.X);
                ImGui.TableSetupColumn("items", ImGuiTableColumnFlags.NoHeaderLabel);
                ImGui.TableNextColumn();

                //Draw player and containers
                DrawBagIcon(game.Character.Weenie);
                foreach (var bag in game.Character.Containers)
                    DrawBagIcon(bag);

                //Move to next column and render selected bag
                ImGui.TableNextColumn();
                //var wo = game.World.Get(SelectedBag);
                //ImGui.Text($"Selected Container: {wo}");

                DrawItems();

                ImGui.EndTable();
            }
        }
        else
        {
            //Render all items
            DrawItems();
        }
    }

    /// <summary>
    /// Draw a bag contents in either table or bag layout
    /// </summary>
    private void DrawItems()
    {
        //Start the content area for items based on whether icons or a table is used
        if (ShowIcons)
            DrawItemsAsIcons();
        else
            DrawItemsAsTable();
    }

    private void DrawBagIcon(WorldObject wo)
    {
        if (ImGui.TextureButton($"{wo.Id}", GetOrCreateTexture(wo), IconSize, 0, SelectedBag == wo.Id ? SELECTED_COLOR : UNSELECTED_COLOR))
        {
            //Store selected bag
            SelectedBag = wo.Id;

            //Set items to that bags contents?
            //Could use AllItems for player to include containers?            
            SetFilteredItems();
        }

        DrawBagItemTooltip(wo);
        DrawBagContextMenu(wo);
    }

    private void DrawItemsAsIcons()
    {
        ImGui.BeginChild("items", ImGui.GetContentRegionAvail(), false);

        //Setup dimensions
        //Available width divided into columns
        Index = 0;
        var width = ImGui.GetContentRegionAvail().X - IconSize.X / 2;
        int columns = (int)(width / ICON_COL_WIDTH);

        int index = 0;
        foreach (var wo in filteredItems)
        {
            if (IsFiltered(wo))
                continue;

            //Move on to next row?
            if (index % columns != 0)
                ImGui.SameLine();

            DrawItemIcon(wo);
            index++;
        }

        ImGui.EndChild();
    }

    private void DrawItemsAsTable()
    {
        BeginBagTable();

        foreach (var wo in filteredItems)
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex((int)ItemColumn.Icon);
            ImGui.TextureButton(wo.Id.ToString(), GetOrCreateTexture(wo), IconSize);

            ImGui.TableSetColumnIndex((int)ItemColumn.Name);
            ImGui.Text(wo.Name);
            DrawItemContextMenu(wo);

            ImGui.TableSetColumnIndex((int)ItemColumn.Value);
            if (ShowExtraFilter && propFilter.EnumIndex != null)
                ImGui.Text(propFilter.FindValue(wo) ?? "");
            else
                ImGui.Text(wo.Value(IntId.Value).ToString());
        }

        ImGui.EndTable();
    }

    private void BeginBagTable()
    {
        ImGui.BeginTable("items-table", COLUMN_FLAGS.Count, TABLE_FLAGS, ImGui.GetContentRegionAvail());

        ImGui.TableSetupColumn("###Icon", COLUMN_FLAGS[ItemColumn.Icon], IconSize.X + ICON_PAD, (int)ItemColumn.Icon);
        ImGui.TableSetupColumn("Name", COLUMN_FLAGS[ItemColumn.Name], 0, (int)ItemColumn.Name);

        if (ShowExtraFilter && propFilter.EnumIndex != null)
            ImGui.TableSetupColumn(propFilter.Selection, COLUMN_FLAGS[ItemColumn.Value], 60, (int)ItemColumn.Value);
        else
            ImGui.TableSetupColumn("Value", COLUMN_FLAGS[ItemColumn.Value], 60, (int)ItemColumn.Value);

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        //Sort if needed?
        //Checked to make sure after headers is fine
        SortItems();
    }
    #endregion

    #region Menu
    private void DrawOptions()
    {
        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("Options###InvOpt"))
            {
                ImGui.MenuItem("Show Bags", "", ref ShowBags);
                ImGui.MenuItem("Show Icons", "", ref ShowIcons);
                ImGui.MenuItem("Show Extra Filters", "", ref ShowExtraFilter);

                ImGui.EndMenu();
            }
            ImGui.EndMenuBar();
        }
    }

    void DrawItemIcon(WorldObject wo)
    {
        var texture = GetOrCreateTexture(wo);
        ImGui.TextureButton($"{wo.Id}", texture, IconSize);

        DrawItemTooltip(wo);
        DrawItemContextMenu(wo);

        Index++;
    }
    #endregion

    #region Context Menu / Tooltips
    /// <summary>
    /// Draws hovered details for Container
    /// </summary>
    private void DrawBagItemTooltip(WorldObject wo)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(wo.Name);
            ImGui.Text($"Value: {wo.Value(IntId.Value)}");
            ImGui.Text($"Burden: {wo.Burden}");
            ImGui.Text($"Capacity: {wo.Items.Count}/{wo.IntValues[IntId.ItemsCapacity]}");
            ImGui.EndTooltip();
        }
    }

    /// <summary>
    /// Draws hovered details for WorldObject
    /// </summary>
    private void DrawItemTooltip(WorldObject wo)
    {
        var texture = GetOrCreateTexture(wo);

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextureButton($"{wo.Id}", texture, IconSize);
            ImGui.SameLine();
            ImGui.Text(wo.Name);
            ImGui.Text($"Value: {wo.Value(IntId.Value)}");
            ImGui.Text($"ObjectClass: {wo.ObjectClass}");
            ImGui.EndTooltip();
        }
    }

    void DrawBagContextMenu(WorldObject wo)
    {
        if (ImGui.BeginPopupContextItem($"###{wo.Id}", ImGuiPopupFlags.MouseButtonRight))
        {
            if (ImGui.MenuItem("Drop"))
                wo.Drop();
            //if ImGui.MenuItem("Give Selected") then
            //    if game.World.Selected ~= nil then
            //        wo.Give(game.World.Selected.Id)
            //    else
            //        print('Nothing selected')
            //    end
            //end
            //if ImGui.MenuItem("Give Player") then
            //    if game.World.Selected ~= nil and game.World.Selected.ObjectClass == ObjectClass.Player then
            //        wo.Give(game.World.Selected.Id)
            //    else
            //        wo.Give(game.World.GetNearest(ObjectClass.Player).Id)
            //    end
            //end
            //if ImGui.MenuItem("Give Vendor") then
            //    if game.World.Selected ~= nil and game.World.Selected.ObjectClass == ObjectClass.Vendor then
            //        wo.Give(game.World.Selected.Id)
            //    else
            //        wo.Give(game.World.GetNearest(ObjectClass.Vendor).Id)
            //    end

            ImGui.EndPopup();
        }
    }

    /// <summary>
    /// Draw context menu for WorldObject
    /// </summary>
    /// <param name="wo"></param>
    private void DrawItemContextMenu(WorldObject wo)
    {
        if (ImGui.BeginPopupContextItem($"P{wo.Id}"))
        {
            if (ImGui.MenuItem("Split?"))
            { }
            //    if ImGui.MenuItem("Select") then wo.Select() end
            //    if ImGui.MenuItem("Drop") then wo.Drop() end
            //    if ImGui.MenuItem("Use") then wo.Use() end
            //    if ImGui.MenuItem("Use Self") then wo.UseOn(game.CharacterId) end
            //    if ImGui.MenuItem("Give Selected") then
            //        if game.World.Selected ~= nil then
            //            wo.Give(game.World.Selected.Id)
            //        else
            //print('Nothing selected')
            //        end
            //    if ImGui.MenuItem("Give NPC") then
            //        if game.World.Selected ~= nil and game.World.Selected.ObjectClass == ObjectClass.Npc then
            //            wo.Give(game.World.Selected.Id)
            //        else
            //wo.Give(game.World.GetNearest(ObjectClass.Npc).Id)
            //        end
            //    end
            //    if ImGui.MenuItem("Give Player") then
            //        if game.World.Selected ~= nil and game.World.Selected.ObjectClass == ObjectClass.Player then
            //            wo.Give(game.World.Selected.Id)
            //        else
            //wo.Give(game.World.GetNearest(ObjectClass.Player).Id)
            //        end
            //    end
            //    -- if ImGui.MenuItem("Give Vendor") then
            //    --     if game.World.Selected ~= nil and game.World.Selected.ObjectClass == ObjectClass.Vendor then
            //    --         wo.Give(game.World.Selected.Id)
            //    --     else
            //--wo.Give(game.World.GetNearest(ObjectClass.Vendor).Id)
            //--     end
            //-- end
            //    if ImGui.MenuItem("Salvage") then
            //        --game.Actions.Salvage()
            //        game.Actions.SalvageAdd(wo.Id)
            //    end
            ImGui.EndPopup();
        }
    }
    #endregion

    #region Filters
    Regex FilterRegex = new("", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    string customFilterText = "";
    Regex CustomFilterRegex = new("", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    const string valueReqPattern = @"^(>=?|<=?|!=\?{0,2}|==|\?{1,2}|!B|B)(.*)";
    Regex ValueReqRegex = new(valueReqPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    ValueRequirement valueRequirement;
    private void DrawFilters()
    {
        //Basic name filter
        if (ImGui.InputText("Filter", ref FilterText, 512))
        {
            FilterRegex = new(FilterText, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            SetFilteredItems();
        }

        //Extra filter section
        if (!ShowExtraFilter) return;

        bool changed = false;

        //Render PropType selector
        ImGui.SetNextItemWidth(80);
        if (ImGui.Combo("Prop", ref filterComboIndex, filterTypes, filterTypes.Length))
        {
            //Parse PropType from combo.  Do here to prevent PropFilter change refresh
            if (Enum.TryParse<PropType>(filterTypes[filterComboIndex], out propType))
            {
                propFilter = new PropertyFilter(propType);
                SetFilteredItems();
                C.Chat($"Custom filter set to: {propType}");
            }

            changed = true;
        }

        ImGui.SameLine();

        propFilter.Render();

        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.InputText("Value###CFilter", ref customFilterText, 50, ImGuiInputTextFlags.AllowTabInput | ImGuiInputTextFlags.AutoSelectAll))
            changed = true;

        if (propFilter.Changed)
        {
            changed = true;
            propFilter.Changed = false;
        }

        if (changed)
            UpdateFilters();
    }


    private void UpdateFilters()
    {
        //Rebuild basic filter regex
        FilterRegex = new(FilterText, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        //ExtraFilter stuff
        if (ShowExtraFilter)
        {
            //Set up custom filter for either a string or a ValueRequirement
            if (propType == PropType.String)
            {
                CustomFilterRegex = new(customFilterText, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                C.Chat($"Built regex for {propFilter.Selection}");
            }
            else
            {
                //Try to parse a comparison and value
                var match = ValueReqRegex.Match(customFilterText);

                if (match.Success && double.TryParse(match.Groups[2].Value, out var result) && CompareExtensions.TryParse(match.Groups[1].Value, out var comparison))
                {
                    valueRequirement = new()
                    {
                        PropType = propType,
                        Type = comparison,
                        TargetValue = result,
                        PropKey = propFilter.EnumIndex ?? 0,
                    };
                    C.Chat($"Parsed value requirement: {propType} - {comparison} - {result}");

                }
                else
                {
                    valueRequirement = null;
                    //C.Chat($"{match.Success}");
                }
            }
        }
        //Todo: null if extra filters unused?

        SetFilteredItems();

        //propFilter.Changed = false;
    }


    //Returns true if an object is filtered given the current options and filters
    private bool IsFiltered(WorldObject wo)
    {
        if (!wo.HasAppraisalData)
            wo.Appraise();

        //Filter by regex
        if (!String.IsNullOrEmpty(FilterText))
        {
            if (!FilterRegex.IsMatch(wo.Name))
                return true;
        }

        //Filter by prop
        if (!ShowExtraFilter)
            return false;

        //Skip missing?
        //Todo: check logic
        if (propFilter.Props.Length < 1 || propFilter.EnumIndex is null)
            return false;

        var key = propFilter.EnumIndex;

        //C.Chat($"{propType} - {key} - {propFilter.Selection}");

        switch (propType)
        {
            case PropType.String:
                //Require value
                if (!wo.StringValues.TryGetValue((StringId)key, out var value) || !CustomFilterRegex.IsMatch(value))
                    return true;
                break;
            default:
                //If there's a value req try to satisfy it
                if (valueRequirement is null)
                    return false;

                return !valueRequirement.VerifyRequirement(wo);
                break;
        }

        return false;
    }

    //Filter list
    private void SetFilteredItems()
    {
        var bag = game.World.Get(SelectedBag);
        var items = bag is null ? game.Character.Inventory : bag.Items;

        filteredItems = items.Where(x => !IsFiltered(x)).ToList();
        //C.Chat($"Rebuild filter {items.Count}->{filteredItems.Count}");
    }
    #endregion

    #region Sorting
    //Sort table based on column/direction
    private uint sortColumn = 0; // Currently sorted column index
    private ImGuiSortDirection sortDirection = ImGuiSortDirection.Ascending;

    //Sort if needed
    private void SortItems()
    {
        //Check if a sort is needed
        var tableSortSpecs = ImGui.TableGetSortSpecs();
        if (!tableSortSpecs.SpecsDirty)
            return;

        //Find column/direction
        sortDirection = tableSortSpecs.Specs.SortDirection;
        sortColumn = tableSortSpecs.Specs.ColumnUserID;

        //C.Chat($"Dirty: {sortDirection} - {tableSortSpecs.Specs.ColumnUserID}");

        //Handle sorting
        if (sortDirection == ImGuiSortDirection.Ascending)
        {
            filteredItems = sortColumn switch
            {
                1 => filteredItems.OrderBy(x => x.Name).ToList(),
                //Default to value
                2 when !ShowExtraFilter => filteredItems.OrderBy(x => x.Value(IntId.Value)).ToList(),
                //StringProp
                2 when valueRequirement is null => filteredItems.OrderBy(x => propFilter.FindValue(x) ?? "").ToList(),
                //Value requirement available
                2 => filteredItems.OrderBy(x => valueRequirement.GetNormalizeValue(x)).ToList(),
                _ => filteredItems,
            };
        }
        else
        {
            filteredItems = sortColumn switch
            {
                1 => filteredItems.OrderByDescending(x => x.Name).ToList(),
                //Default to value
                2 when !ShowExtraFilter => filteredItems.OrderByDescending(x => x.Value(IntId.Value)).ToList(),
                //StringProp
                2 when valueRequirement is null => filteredItems.OrderByDescending(x => propFilter.FindValue(x) ?? "").ToList(),
                //Value requirement available
                2 => filteredItems.OrderByDescending(x => valueRequirement.GetNormalizeValue(x)).ToList(),
                _ => filteredItems,
            };
        }

        //Data has been sorted
        tableSortSpecs.SpecsDirty = false;
    }
    #endregion

    #region Utility
    readonly Dictionary<uint, ManagedTexture> _woTextures = new();
    /// <summary>
    /// Get or create a managed texture for a world object
    /// </summary>
    private ManagedTexture GetOrCreateTexture(WorldObject wo)
    {
        if (!_woTextures.TryGetValue(wo.WeenieClassId, out var texture))
        {
            if (wo.Id == game.Character.Id)
                texture = sHud.GetIconTexture(PLAYER_ICON);
            else
                texture = sHud.GetIconTexture(wo.Value(DataId.Icon));

            _woTextures.AddOrUpdate(wo.WeenieClassId, texture);
        }

        return texture;
    }
    #endregion

    public void Dispose()
    {
        try
        {

        }
        catch (Exception)
        {

            throw;
        }
    }
}


