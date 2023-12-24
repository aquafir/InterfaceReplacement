using ImGuiNET;
using System;
using System.Numerics;
using UtilityBelt.Scripting.Interop;
using UtilityBelt.Service;
using UtilityBelt.Service.Views;

namespace InventoryUI;
internal class InventoryUI : IDisposable
{
    /// <summary>
    /// The UBService Hud
    /// </summary>
    readonly Hud hud;
    readonly Game g = new();

    Vector2 MIN_SIZE = new(200, 200);
    Vector2 MAX_SIZE = new(1000, 900);

    readonly InventoryHud backpack = new();

    public InventoryUI()
    {
        // Create a new UBService Hud
        hud = UBService.Huds.CreateHud("InventoryUI");
        hud.WindowSettings = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoScrollbar;

        // set to show our icon in the UBService HudBar
        hud.ShowInBar = true;
        hud.Visible = true;

        // subscribe to the hud render event so we can draw some controls
        hud.OnPreRender += Hud_OnPreRender;
        hud.OnRender += Hud_OnRender;
    }

    private void Hud_OnPreRender(object sender, EventArgs e)
    {
        ImGui.SetNextWindowSizeConstraints(MIN_SIZE, MAX_SIZE);
    }

    /// <summary>
    /// Called every time the ui is redrawing.
    /// </summary>
    private void Hud_OnRender(object sender, EventArgs e)
    {
        try
        {
            backpack.Render();
        }
        catch (Exception ex)
        {
            PluginCore.Log(ex);
        }
    }


    public void Dispose()
    {
        hud.Dispose();
    }
}


//---@type PropFilter[]
//local propFilters = { }
//-- table.insert(propFilters, pf.new (PropType.String))
//-- table.insert(propFilters, pf.new (PropType.Bool))
//-- table.insert(propFilters, pf.new (PropType.Int))
//-- table.insert(propFilters, pf.new (PropType.Float))
//-- table.insert(propFilters, pf.new (PropType.Int64))
//-- table.insert(propFilters, pf.new (PropType.DataId))
//-- table.insert(propFilters, pf.new (PropType.InstanceId))

//for filter in propFilters do
//    filter.valueText = ''
//    filter.valueRegex = nil
//end

//--ObjectType filtering
//local otypeProps = { }
//for index, value in ipairs(ObjectType.GetValues()) do table.insert(otypeProps, value) end


//---@class InventoryHud
//---@field Hud Hud -- The backing imgui hud
//---@field FilterText string|nil -- The current filter text
//---@field FilterObjectType ObjectType|nil -- Selected ObjectType filter
//---@field UseFilterType boolean -- Filter by ObjectType
//---@field FilterRegex Regex Regex from FilterText
//---@field FilterIntId IntId|nil -- Selected IntId filter
//---@field FilterIntText string|nil -- The current filter for Int properties
//---@field UseFilterInt boolean -- Filter by Int
//---@field FilterStringId StringId|nil --
//---@field FilterStringText string|nil --
//-- ---@field UsePropertyFilters boolean --
//---@field ShowBags boolean -- If true, shows bags on the sidebar.If false, everything is in one bag
//---@field ShowIcons boolean -- If true, draws an icon grid.If false, draws a table
//---@field ShowExtraFilters boolean -- If true, adds more filter options
//---@field IconSize Vector2 -- The size of icons to draw
//---@field SelectedBag number -- The id of the currently selected bag / container
//local InventoryHud = {
//    FilterText = "",
//    ShowBags = true,
//    ShowIcons = false,
//    FilterObjectType = 0,
//    UseFilterType = false,
//    FilterIntId = 0,
//    FilterIntText = '',
//    UseFilterInt = false,
//    FilterStringId = 0,
//    FilterStringText = '',
//    --UsePropertyFilters = true,
//    --UseFilterString = true,
//    ShowExtraFilters = false,
//    IconSize = Vector2.new(24, 24),
//    SelectedBag = game.CharacterId,
//    columnFlags = {
//        IM.ImGuiTableColumnFlags.WidthFixed,
//        IM.ImGuiTableColumnFlags.DefaultSort + IM.ImGuiTableColumnFlags.IsSorted,
//        IM.ImGuiTableColumnFlags.None,
//        IM.ImGuiTableColumnFlags.None
//    }
//}


//function Sort(isAscending, a, b)
//    if isAscending then
//        return a<b
//    else
//        return a> b
//    end
//end

//---Draw an item icon
//---@param s InventoryHud
//---@param index number
//---@param wo WorldObject
//function DrawItemIcon(s, index, wo)
//    local pad = 4
//    local availWidth = ImGui.GetContentRegionAvail().X - ((pad * 2))
//    local currentWidth = (s.DrawItemIndex * (s.IconSize.X + pad * 2)) + s.IconSize.X
//    if index ~= 1 and currentWidth + pad* 2 + s.IconSize.X<availWidth then
//        if true then ImGui.SameLine() end
//    else
//        s.DrawItemIndex = -1
//    end

//    local texture = GetOrCreateTexture(wo)
//    ImGui.TextureButton(tostring(wo.Id), texture, s.IconSize)

//    DrawItemTooltip(wo, texture, s)
//    DrawItemContextMenu(wo)

//    s.DrawItemIndex = s.DrawItemIndex + 1
//end

//---Draw a bag icon
//---@param s InventoryHud
//---@param wo WorldObject
//function DrawBagIcon(s, wo)
//    if ImGui.TextureButton(tostring(wo.Id), GetOrCreateTexture(wo), s.IconSize) then
//        s.SelectedBag = wo.Id
//    end
//    DrawBagItemTooltip(wo)
//    DrawBagContextMenu(wo)
//end

//---@param wo WorldObject
//function DrawBagContextMenu(wo)
//    if ImGui.BeginPopupContextItem() then
//        if ImGui.MenuItem("Drop") then wo.Drop() end
//        if ImGui.MenuItem("Give Selected") then
//            if game.World.Selected ~= nil then
//                wo.Give(game.World.Selected.Id)
//            else
//                print('Nothing selected')
//            end
//        end
//        if ImGui.MenuItem("Give Player") then
//            if game.World.Selected ~= nil and game.World.Selected.ObjectClass == ObjectClass.Player then
//                wo.Give(game.World.Selected.Id)
//            else
//                wo.Give(game.World.GetNearest(ObjectClass.Player).Id)
//            end
//        end
//        if ImGui.MenuItem("Give Vendor") then
//            if game.World.Selected ~= nil and game.World.Selected.ObjectClass == ObjectClass.Vendor then
//                wo.Give(game.World.Selected.Id)
//            else
//                wo.Give(game.World.GetNearest(ObjectClass.Vendor).Id)
//            end
//        end



//        ImGui.EndPopup()
//    end
//end



//---Table setup for bag
//---@param s InventoryHud
//---@param items { [number]: WorldObject }
//function BeginBagTable(s, items)
//    if true then return end

//    local flags = IM.ImGuiTableFlags.None + IM.ImGuiTableFlags.BordersInner + IM.ImGuiTableFlags.Resizable +
//        IM.ImGuiTableFlags.RowBg + IM.ImGuiTableFlags.Reorderable + IM.ImGuiTableFlags.Hideable +
//        IM.ImGuiTableFlags.ScrollY + IM.ImGuiTableFlags.Sortable
//    ImGui.BeginTable("items-table", 4, flags, ImGui.GetContentRegionAvail())
//    ImGui.TableSetupColumn("###Icon", s.columnFlags[1], 16)
//    ImGui.TableSetupColumn("Name", s.columnFlags[2])
//    ImGui.TableSetupColumn("Value", s.columnFlags[3])
//    ImGui.TableSetupColumn("ObjectClass", s.columnFlags[4])
//    ImGui.TableHeadersRow()

//    for colIndex = 0, ImGui.TableGetColumnCount(), 1 do
//        s.columnFlags[colIndex + 1] = ImGui.TableGetColumnFlags(colIndex)
//    end

//    local specs = ImGui.TableGetSortSpecs()
//    -- todo: needs tri sort support
//    if specs ~= nil and specs.SortDirection ~= IM.ImGuiSortDirection.None then
//        local asc = specs.SortDirection == IM.ImGuiSortDirection.Ascending
//        local cIndex = specs.ColumnIndex
//        table.sort(wos, function(a, b)
//            if cIndex == 0 then
//                return Sort(asc, a.Value(DataId.Icon), b.Value(DataId.Icon))
//            elseif cIndex == 1 then
//                return Sort(asc, a.Name, b.Name)
//            elseif cIndex == 2 then
//                return Sort(asc, a.Value(IntId.Value), b.Value(IntId.Value))
//            elseif cIndex == 3 then
//                return Sort(asc, tostring(a.ObjectClass), tostring(b.ObjectClass))
//            end
//        end)
//    end
//end

//function DrawBagTableItem()

//end

//function DrawBagItem()

//end

