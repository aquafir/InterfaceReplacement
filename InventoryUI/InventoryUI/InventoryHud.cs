using ACE.DatLoader.FileTypes;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using UtilityBelt.Common.Enums;
using UtilityBelt.Scripting.Interop;
using UtilityBelt.Scripting.Lib;
using UtilityBelt.Service.Lib.Settings;
using UtilityBelt.Service.Views;
using WattleScript.Interpreter;

namespace InventoryUI;

public class InventoryHud
{
    private const int PLAYER_ICON = 0x0600127E;
    ScriptHudManager sHud = new();
    Game game = new();
    //Hud hud;
    //List<PropertyFilter> filters = new();


    bool ShowBags;
    bool ShowIcons;
    bool ShowExtraFilters;

    string FilterText = "";
    int FilterObjectType = 0;
    bool UseFilterType = false;
    int FilterIntId = 0;
    string FilterIntText = "";
    bool UseFilterInt = false;
    int FilterStringId = 0;
    string FilterStringText = "";
    bool UsePropertyFilters = true;
    bool UseFilterString = true;
    Vector2 IconSize = new(24, 24);

    private float DrawItemIndex = 0;

    uint SelectedBag = 0;// game.CharacterId,

    ImGuiTableColumnFlags[] columnFlags = {
        ImGuiTableColumnFlags.WidthFixed,
        ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.IsSorted,
        ImGuiTableColumnFlags.None,
        ImGuiTableColumnFlags.None
    };


    public InventoryHud()
    {
        //this.hud = hud;
    }

    public void Render()
    {
        try
        {
            DrawOptions();
            DrawFilter();
            DrawInventory();
        }
        catch (Exception ex)
        {
            PluginCore.Log(ex);
        }
    }

    Regex FilterRegex = new("", RegexOptions.IgnoreCase | RegexOptions.IgnoreCase);

    private void DrawFilter()
    {
        //Basic name filter
        if (ImGui.InputText("Filter", ref FilterText, 512))
        {
            FilterRegex = new(FilterText, RegexOptions.IgnoreCase | RegexOptions.IgnoreCase);

        }

        //Extra filter section
        if (!ShowExtraFilters) return;

        var comboWidth = 150;
        var filterWidth = 200;

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

    private void DrawInventory()
    {
        DrawItemIndex = 0;

        if (ShowBags)
        {
            //Create a 2 - column table for bags and inventory
            ImGui.BeginTable("layout", 2, ImGuiTableFlags.BordersInner);
            ImGui.TableSetupColumn("bags", ImGuiTableColumnFlags.NoHeaderLabel | ImGuiTableColumnFlags.WidthFixed, IconSize.X);
            ImGui.TableSetupColumn("items", ImGuiTableColumnFlags.NoHeaderLabel);
            ImGui.TableNextColumn();

            //Draw player and containers
            DrawBagIcon(game.Character.Weenie);
            foreach (var bag in game.Character.Containers)
            {
                DrawBagIcon(bag);
            }

            //Move to next column and render selected bag
            ImGui.TableNextColumn();
            var wo = game.World.Get(SelectedBag);
            ImGui.Text($"Selected Container: {wo}");

            DrawBagItems(wo.Items);

            ImGui.EndTable();
        }
        else
        {
            //Render all items
            DrawBagItems(game.Character.Weenie.AllItems);
        }
    }

    private void DrawBagIcon(WorldObject wo)
    {
        if (ImGui.TextureButton($"{wo.Id}", GetOrCreateTexture(wo), IconSize))
            SelectedBag = wo.Id;
        DrawBagItemTooltip(wo);
        DrawBagContextMenu(wo);
    }

    void DrawBagContextMenu(WorldObject wo)
    {
        if (ImGui.BeginPopupContextItem())
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
        }

        ImGui.EndPopup();
    }

    private void BeginBagTable()
    {
        if (true)
            return;

        //local flags = IM.ImGuiTableFlags.None + IM.ImGuiTableFlags.BordersInner + IM.ImGuiTableFlags.Resizable +
        //    IM.ImGuiTableFlags.RowBg + IM.ImGuiTableFlags.Reorderable + IM.ImGuiTableFlags.Hideable +
        //    IM.ImGuiTableFlags.ScrollY + IM.ImGuiTableFlags.Sortable
        //ImGui.BeginTable("items-table", 4, flags, ImGui.GetContentRegionAvail())
        //ImGui.TableSetupColumn("###Icon", s.columnFlags[1], 16)
        //ImGui.TableSetupColumn("Name", s.columnFlags[2])
        //ImGui.TableSetupColumn("Value", s.columnFlags[3])
        //ImGui.TableSetupColumn("ObjectClass", s.columnFlags[4])
        //ImGui.TableHeadersRow()

        //for colIndex = 0, ImGui.TableGetColumnCount(), 1 do
        //            s.columnFlags[colIndex + 1] = ImGui.TableGetColumnFlags(colIndex)
        //end

        //local specs = ImGui.TableGetSortSpecs()
        //-- todo: needs tri sort support
        //if specs ~= nil and specs.SortDirection ~= IM.ImGuiSortDirection.None then
        //    local asc = specs.SortDirection == IM.ImGuiSortDirection.Ascending
        //    local cIndex = specs.ColumnIndex
        //    table.sort(wos, function(a, b)
        //        if cIndex == 0 then
        //            return Sort(asc, a.Value(DataId.Icon), b.Value(DataId.Icon))
        //        elseif cIndex == 1 then
        //            return Sort(asc, a.Name, b.Name)
        //        elseif cIndex == 2 then
        //            return Sort(asc, a.Value(IntId.Value), b.Value(IntId.Value))
        //        elseif cIndex == 3 then
        //            return Sort(asc, tostring(a.ObjectClass), tostring(b.ObjectClass))
        //        end
        //    end)
        //end
    }

    //Returns true if an object is filtered given the current options and filters
    private bool IsFiltered(WorldObject wo)
    {
        if (!wo.HasAppraisalData)
            wo.Appraise();

        //    --Filter by regex
        //    if s.FilterRegex ~= nil then return not s.FilterRegex.IsMatch(wo.Name) end

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

    /// <summary>
    /// Draw a bag contents in either table or bag layout
    /// </summary>
    private void DrawBagItems(IList<WorldObject> wos)
    {
        //Start the content area for bags
        if (!ShowIcons)
            BeginBagTable();
        else
            ImGui.BeginChild("items", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.None);

        int i = 0;
        foreach (var wo in wos)
        {
            if (!IsFiltered(wo))
            {
                if (ShowIcons)
                {
                    DrawItemIcon(i++, wo);
                }
                else
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ManagedTexture texture = GetOrCreateTexture(wo);
                    ImGui.TextureButton(wo.Id.ToString(), texture, new(16, 16));

                    DrawItemContextMenu(wo);

                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(wo.Name);
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(wo.Value(IntId.Value).ToString());
                    ImGui.TableSetColumnIndex(3);
                    ImGui.Text(wo.ObjectClass.ToString());
                }
            }

            if (!ShowIcons)
                ImGui.EndTable();
        }

        if (!ShowIcons)
            ImGui.EndTable();
        else
            ImGui.EndChild();
    }

    void DrawItemIcon(int index, WorldObject wo)
    {
        var pad = 4;
        var availWidth = ImGui.GetContentRegionAvail().X - ((pad * 2));
        var currentWidth = (DrawItemIndex * (IconSize.X + pad * 2)) + IconSize.X;

        if (index != 1 && (currentWidth + pad * 2 + IconSize.X) < availWidth)
        {
            if (true)
                ImGui.SameLine();
            else
                DrawItemIndex = -1;
        }

        var texture = GetOrCreateTexture(wo);
        ImGui.TextureButton($"{wo.Id}", texture, IconSize);

        DrawItemTooltip(wo);
        DrawItemContextMenu(wo);

        DrawItemIndex++;
    }


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

    /// <summary>
    /// Draw context menu for WorldObject
    /// </summary>
    /// <param name="wo"></param>
    private void DrawItemContextMenu(WorldObject wo)
    {
        if (ImGui.BeginPopupContextItem())
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
        }
        ImGui.EndPopup();
    }
}


