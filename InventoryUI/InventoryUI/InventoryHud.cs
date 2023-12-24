using AcClient;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using UtilityBelt.Common.Enums;
using UtilityBelt.Scripting.Interop;
using UtilityBelt.Scripting.Lib;
using UtilityBelt.Service.Views;

namespace InventoryUI;

public class InventoryHud
{
    private const int PLAYER_ICON = 0x0600127E;
    ScriptHudManager sHud = new();
    Game game = new();
    Hud hud;
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

    int SelectedBag = 0;// game.CharacterId,

    ImGuiTableColumnFlags[] columnFlags = {
        ImGuiTableColumnFlags.WidthFixed,
        ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.IsSorted,
        ImGuiTableColumnFlags.None,
        ImGuiTableColumnFlags.None
    };


    public InventoryHud(Hud hud)
    {
        this.hud = hud;
    }

    public void Render()
    {
        try
        {
            //DrawOptions();
            //DrawFilter();
            //DrawInventory();
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
        if (ShowBags)
        {
            //        --Create a 2 - column table for bags and inventory
            ImGui.BeginTable("layout", 2, ImGuiTableFlags.BordersInner);
            //        ImGui.TableSetupColumn("bags", IM.ImGuiTableColumnFlags.NoHeaderLabel + IM.ImGuiTableColumnFlags.WidthFixed,
            //            s.IconSize.X)
            //        ImGui.TableSetupColumn("items", IM.ImGuiTableColumnFlags.NoHeaderLabel)
            //        ImGui.TableNextColumn()
            //        --Draw player and containers
            //        DrawBagIcon(s, game.Character.Weenie)
            //        for i, bag in pairs(game.Character.Containers) do
            //            DrawBagIcon(s, bag)
            //        end

            //        --Move to next column and render selected bag
            //        ImGui.TableNextColumn()
            //        local wo = game.World.Get(s.SelectedBag)
            //        ImGui.Text("Selected Container: "..tostring(wo))

            //        DrawBagItems(s, wo.Items)

            ImGui.EndTable();
        }
        else
        {
            //        --Render all items
            //        DrawBagItems(s, game.Character.Weenie.AllItems)
        }
        //function DrawInventory(s)
        //    s.DrawItemIndex = 0
    }



    private void BeginBagTable()
    {

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
    private void DrawBagItems(List<WorldObject> wos)
    {
        //Start the content area for bags
        if (!ShowIcons)
            BeginBagTable();
        else
            ImGui.BeginChild("items", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.None);

        foreach (var wo in wos)
        {
            if (!IsFiltered(wo))
            {
                if (ShowIcons)
                {
                    //DrawItemIcon(s, i, item)
                }
                else
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ManagedTexture texture = GetOrCreateTexture(wo);
                    ImGui.TextureButton(wo.Id.ToString(), texture, new(16, 16));

                    //DrawItemContextMenu(wo);

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
        if (!_woTextures.TryGetValue(wo.WeenieClassId, out var texture))
            return;

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


