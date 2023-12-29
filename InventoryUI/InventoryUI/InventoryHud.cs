using ACE.DatLoader.FileTypes;
using ACEditor;
using ACEditor.Table;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using UtilityBelt.Common.Enums;
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
    bool ShowExtraFilters;

    //Filters?
    string FilterText = "";
    //List<PropertyFilter> filters = new();

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
    }

    #region Events
    private void Hud_OnShow(object sender, EventArgs e)
    {
        SetFilteredItems();
    }

    #endregion

    public void Render()
    {
        try
        {
            //DrawOptions();
            //DrawFilter();
            DrawInventory();
        }
        catch (Exception ex)
        {
            PluginCore.Log(ex);
        }
    }

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

    private void BeginBagTable()
    {
        ImGui.BeginTable("items-table", COLUMN_FLAGS.Count, TABLE_FLAGS, ImGui.GetContentRegionAvail());

        ImGui.TableSetupColumn("###Icon", COLUMN_FLAGS[ItemColumn.Icon], IconSize.X + ICON_PAD);
        ImGui.TableSetupColumn("Name", COLUMN_FLAGS[ItemColumn.Name]);
        ImGui.TableSetupColumn("Value", COLUMN_FLAGS[ItemColumn.Value], 60);
        //ImGui.TableSetupColumn("ObjectClass", COLUMN_FLAGS[3]);

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        //Sort if needed?
        //Checked to make sure after headers is fine
        SortItems();
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
        //BeginBagTable();

        //if (ImGui.BeginTable("items-table", COLUMN_FLAGS.Count, TABLE_FLAGS, ImGui.GetContentRegionAvail()))
        //if (ImGui.BeginTable("items", COLUMN_FLAGS.Count, ImGuiTableFlags.Sortable, ImGui.GetContentRegionAvail()))
        //{
        //    //ImGui.TableSetupColumn("###Icon", COLUMN_FLAGS[ItemColumn.Icon], IconSize.X + ICON_PAD);
        //    //ImGui.TableSetupColumn("Name", COLUMN_FLAGS[ItemColumn.Name]);
        //    //ImGui.TableSetupColumn("Value", COLUMN_FLAGS[ItemColumn.Value], 60);
        //    ImGui.TableSetupColumn("Icon");//, COLUMN_FLAGS[ItemColumn.Icon], IconSize.X + ICON_PAD);
        //    ImGui.TableSetupColumn("Name");//, COLUMN_FLAGS[ItemColumn.Name]);
        //    ImGui.TableSetupColumn("Value");//, COLUMN_FLAGS[ItemColumn.Value], 60);

        //    //ImGui.TableSetupScrollFreeze(0, 1);
        //    ImGui.TableHeadersRow();
        C.Chat("Draw as table");
        return;

        //Sort if needed?
        //Checked to make sure after headers is fine
        //SortItems();
        if (ImGui.BeginTable("table1", 3, ImGuiTableFlags.Sortable))
        {
            ImGui.TableSetupColumn("test");
            ImGui.TableSetupColumn("foo");
            ImGui.TableSetupColumn("bar");
            ImGui.TableHeadersRow();

            var tableSortSpecs = ImGui.TableGetSortSpecs();
            if (tableSortSpecs.SpecsDirty)
            {
                CoreManager.Current.Actions.AddChatText($"Sort Specs changed", 1);
                // do sorting...
                tableSortSpecs.SpecsDirty = false;
            }

            for (int row = 0; row < 4; row++)
            {
                ImGui.TableNextRow();
                for (int column = 0; column < 3; column++)
                {
                    ImGui.TableSetColumnIndex(column);
                    ImGui.Text($"Row {row} Column {column}");
                }
            }
            ImGui.EndTable();

            //foreach (var wo in filteredItems)
            //{
            //    ImGui.TableNextRow();

            //    ImGui.TableSetColumnIndex((int)ItemColumn.Icon);
            //    ImGui.TextureButton(wo.Id.ToString(), GetOrCreateTexture(wo), IconSize);

            //    ImGui.TableSetColumnIndex((int)ItemColumn.Name);
            //    ImGui.Text(wo.Name);
            //    DrawItemContextMenu(wo);

            //    ImGui.TableSetColumnIndex((int)ItemColumn.Value);
            //    ImGui.Text(wo.Value(IntId.Value).ToString());
            //}

            //ImGui.EndTable();
        }
    }

    #region Menu
    private void DrawOptions()
    {
        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("Options###InvOpt"))
            {
                ImGui.MenuItem("Show Bags", "", ref ShowBags);
                ImGui.MenuItem("Show Icons", "", ref ShowIcons);
                ImGui.MenuItem("Show Extra Filters", "", ref ShowExtraFilters);

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
    Regex FilterRegex = new("", RegexOptions.IgnoreCase | RegexOptions.IgnoreCase);
    private void DrawFilter()
    {
        //Basic name filter
        if (ImGui.InputText("Filter", ref FilterText, 512))
        {
            FilterRegex = new(FilterText, RegexOptions.IgnoreCase | RegexOptions.IgnoreCase);
            C.Chat($"Rebuild filter: {FilterText}");

            SetFilteredItems();
        }

        //Extra filter section
        //if (!ShowExtraFilters) return;

        //var comboWidth = 150;
        //var filterWidth = 200;

        //    for index, filter in ipairs(propFilters) do
        //        ImGui.SetNextItemWidth(comboWidth)
        //        -- print('test', value:TypeName())
        //        --         local didChange, newValue = ImGui.InputText("###IntFilter", s.FilterIntText, 512)
        //        -- if didChange then s.FilterIntText = newValue end

        //        local didChange, newValue = ImGui.InputText("###Filter"..index, filter.valueText, 300)
        //        if didChange then
        //            --Todo decide where to keep text/input.Being lazy and adding it to filters
        //            filter.valueText = newValue
        //            if newValue == "" then
        //                filter.valueRegex = nil
        //            else
        //                filter.valueRegex = Regex.new (newValue, RegexOptions.Compiled + RegexOptions.IgnoreCase)
        //            end
        //        end
        //        ImGui.SameLine()
        //        filter:DrawCombo()
        //    end
        //    --ObjectType
        //    ImGui.SetNextItemWidth(comboWidth)
        //    local didChange, newValue = ImGui.Combo("###ObjectTypeCombo", s.FilterObjectType - 1, otypeProps, #otypeProps)
        //    if didChange then s.FilterObjectType = newValue + 1 end
        //    ImGui.SameLine()
        //    --ImGui.SameLine(comboWidth + filterWidth + 24)
        //    if ImGui.Checkbox('Class', s.UseFilterType) then s.UseFilterType = not s.UseFilterType end
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

        //    --Filter by ObjectType
        //    if s.ShowExtraFilters and s.UseFilterType and wo.ObjectType ~= s.FilterObjectType then return true end

        //    --Filter by Properties
        //    if s.ShowExtraFilters then-- and s.UsePropertyFilters--just using them if showing
        //        for index, filter in ipairs(propFilters) do
        //            --print(filter:TypeName())
        //            if filter.Enabled and filter.valueRegex ~= nil then
        //                local val = filter:Value(wo)
        //                if val == nil or not filter.valueRegex.IsMatch(val) then return true end
        //            end
        //        end
        //    end

        return false;
    }

    //Filter list
    private void SetFilteredItems()
    {
        var bag = game.World.Get(SelectedBag);
        var items = bag is null ? game.Character.Inventory : bag.Items;

        filteredItems = items.Where(x => !IsFiltered(x)).ToList();
        C.Chat($"Rebuild filter {items.Count}->{filteredItems.Count}");
    }
    #endregion

    #region Sorting
    //Sort table based on column/direction
    private uint sortColumn = 0; // Currently sorted column index
    private ImGuiSortDirection sortDirection = ImGuiSortDirection.Ascending;

    //Sort if needed
    private void SortItems()
    {
        var tableSortSpecs = ImGui.TableGetSortSpecs();
        //return;

        //Check if a sort is needed
        if (!tableSortSpecs.SpecsDirty)
            return;

        tableSortSpecs.SpecsDirty = false;
        C.Chat("Dirty");
        return;

        //Find column/direction
        sortDirection = tableSortSpecs.Specs.SortDirection;
        sortColumn = tableSortSpecs.Specs.ColumnUserID;

        //Console.WriteLine($"Dirty: {sortDirection} - {tableSortSpecs.Specs.ColumnUserID}");

        if (sortDirection == ImGuiSortDirection.Ascending)
        {
            filteredItems = sortColumn switch
            {
                1 => filteredItems.OrderBy(x => x.Name).ToList(),
                2 => filteredItems.OrderBy(x => x.Value(IntId.Value)).ToList(),
                _ => filteredItems,
            };
        }
        else
        {
            filteredItems = sortColumn switch
            {
                1 => filteredItems.OrderByDescending(x => x.Name).ToList(),
                2 => filteredItems.OrderByDescending(x => x.Value(IntId.Value)).ToList(),
                _ => filteredItems,
            };
        }

        //filteredItems = sortDirection == ImGuiSortDirection.Descending ?
        //    sortColumn switch
        //    {
        //        _ => throw new global::System.NotImplementedException(),
        //    } :
        //                sortColumn switch
        //                {
        //                    _ => throw new global::System.NotImplementedException(),
        //                };
        //ImGuiSortDirection.Ascending? filteredItems.OrderBy(x => CompareTableRows(x.Comp)


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


