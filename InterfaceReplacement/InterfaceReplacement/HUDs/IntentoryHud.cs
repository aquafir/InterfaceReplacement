﻿namespace InventoryUI.HUDs;

public class InventoryHud : IDisposable
{
    #region State / Config
    ScriptHudManager sHud = new();
    readonly Hud hud;
    Game game = new();
    private float Index = 0;
    uint SelectedBag = 0;
    uint SelectedItem = 0;

    List<WorldObject> filteredItems = new();   //Filtered items to be drawn

    /// <summary>
    /// If true sets focus to the basic filter
    /// </summary>
    bool focusFilter = false;
    /// <summary>
    /// If true updates filters and items before rendering
    /// </summary>
    bool refreshHud = false;

    //Options
    bool showBags;
    bool showIcons;
    bool showExtraFilter;
    bool showGroupActions = true;
    bool showEquipment;

    #region Filter Setup
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

    Regex FilterRegex = new("", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    string customFilterText = "";
    Regex CustomFilterRegex = new("", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    const string valueReqPattern = @"^(>=?|<=?|!=\?{0,2}|==|\?{1,2}|!B|B)(.*)";
    Regex ValueReqRegex = new(valueReqPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    ValueRequirement valueRequirement;
    #endregion

    //Setup for icon textures
    readonly Vector2 IconSize = new(24, 24);
    const int ICON_PAD = 8;
    const int ICON_COL_WIDTH = 24 + ICON_PAD;
    Vector4 SELECTED_COLOR = new(200, 200, 0, 255);
    Vector4 UNSELECTED_COLOR = new(0, 0, 0, 0);

    //Setup for item table
    const ImGuiTableFlags TABLE_FLAGS =
    ImGuiTableFlags.BordersInner | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable;
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
    #endregion

    public InventoryHud(Hud hud)
    {
        SelectedBag = game.CharacterId;
        this.hud = hud;

        AddEvents();
    }

    #region Event Handling
    private void Hud_OnShow(object sender, EventArgs e)
    {
        refreshHud = true;
    }

    private void Incoming_Qualities_PrivateUpdateInstanceID(object sender, UtilityBelt.Common.Messages.Events.Qualities_PrivateUpdateInstanceID_S2C_EventArgs e)
    {
        refreshHud = true;
    }

    private void Incoming_Qualities_UpdateInstanceID(object sender, UtilityBelt.Common.Messages.Events.Qualities_UpdateInstanceID_S2C_EventArgs e)
    {
        refreshHud = true;
    }

    private void Game_OnRender2D(object sender, EventArgs e)
    {
        CheckHotkeys();
    }
    #endregion

    #region Hotkeys / Other Checks
    private void CheckRefresh()
    {
        if (refreshHud)
        {
            refreshHud = false;
            UpdateFilters();
        }
    }

    private void CheckHotkeys()
    {
        if (hud is null)
            return;

        if (ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
        {
            if (ImGui.IsKeyPressed(ImGuiKey.B))
                showBags = !showBags;
            if (ImGui.IsKeyPressed(ImGuiKey.E))
                showExtraFilter = !showExtraFilter;

            if (ImGui.IsKeyPressed(ImGuiKey.I))
            {
                //ShowIcons = !ShowIcons;
                hud.Visible = !hud.Visible;
            }
            if (ImGui.IsKeyPressed(ImGuiKey.F))
            {
                focusFilter = true;

                if (!hud.Visible)
                    hud.Visible = true;
            }
        }
    }
    #endregion

    #region Draw
    public void Draw()
    {
        try
        {
            //var watch = Stopwatch.StartNew();
            CheckRefresh();
            DrawOptions();
            DrawEquipment();
            DrawGroupActions();
            DrawFilters();
            DrawInventory();
            //watch.Stop();
            //C.Chat($"{watch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            PluginCore.Log(ex);
        }
    }

    #region Menu
    private void DrawOptions()
    {
        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("Options###InvOpt"))
            {
                ImGui.MenuItem("Show Bags", "", ref showBags);
                ImGui.MenuItem("Show Icons", "", ref showIcons);
                ImGui.MenuItem("Show Extra Filters", "", ref showExtraFilter);
                ImGui.MenuItem("Show Group Actions", "", ref showGroupActions);
                ImGui.MenuItem("Show Equipment", "", ref showEquipment);

                ImGui.EndMenu();
            }
            ImGui.EndMenuBar();
        }
    }

    void DrawItemIcon(WorldObject wo)
    {
        var texture = wo.GetOrCreateTexture();
        ImGui.TextureButton($"{wo.Id}", texture, IconSize);

        DrawItemTooltip(wo);
        DrawItemContextMenu(wo);

        Index++;
    }
    #endregion

    private void DrawEquipment()
    {
        if (!showEquipment)
            return;

        EquipmentHelper.DrawEquipment();
        //        function ToHex(num) return "0x"..string.format("%08x", num) end

        //for i, armor in ipairs(game.Character.Equipment) do
        //                local armorMask = EquipMask.FromValue(armor.Value(IntId.CurrentWieldedLocation))
        //  print(armor, ToHex(armorMask.ToNumber()), armorMask)
        //  for i, mask in ipairs(EquipMask.GetValues()) do
        //                if mask ~= EquipMask.None and armorMask + mask == armorMask then
        //      print("  - "..ToHex(mask.ToNumber())..":"..tostring(mask))
        //    end
        //  end
        //end

        //foreach(var equip in game.Character.Equipment)
        //{
        //    var locs = equip.CurrentWieldedLocation;
        //    //EquipMask
        //}

        //if (ImGui.TextureButton($"{Texture.EquipNecklace}", Texture.EquipNecklace.GetOrCreateTexture(), IconSize, 0, SelectedItem == wo.Id ? SELECTED_COLOR : UNSELECTED_COLOR))
        //{

        //}

    }

    private void DrawGroupActions()
    {
        if (!showGroupActions)
            return;

        if (ImGui.Button("Drop All"))
            FastDropAll();
        ImGui.SameLine();
        if (ImGui.Button("Give All"))
            FastGiveAll();
    }

    #region Filters
    private void DrawFilters()
    {
        //Basic name filter
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 45);
        if (focusFilter)
        {
            focusFilter = false;
            ImGui.SetKeyboardFocusHere();
        }

        if (ImGui.InputText("Filter", ref FilterText, 512, ImGuiInputTextFlags.AutoSelectAll))
        {
            try
            {
                FilterRegex = new(FilterText, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            catch (ArgumentException ex) { }
            SetFilteredItems();
        }

        //Extra filter section
        if (!showExtraFilter) return;

        bool changed = false;

        //Render PropType selector
        ImGui.SetNextItemWidth(80);
        if (ImGui.Combo("Prop", ref filterComboIndex, filterTypes, filterTypes.Length))
        {
            //Parse PropType from combo.  Do here to prevent PropFilter change refresh
            if (Enum.TryParse(filterTypes[filterComboIndex], out propType))
            {
                propFilter = new PropertyFilter(propType);
                SetFilteredItems();
                C.Chat($"Custom filter set to: {propType}");
            }

            changed = true;
        }

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
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
        {
            C.Chat("Updating filters");
            UpdateFilters();
        }
    }

    private void UpdateFilters()
    {
        //Rebuild basic filter regex
        FilterRegex = new(FilterText, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        //ExtraFilter stuff
        if (showExtraFilter)
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

    /// <summary>
    /// Returns true if an object is filtered given the current options and filters
    /// </summary>
    private bool IsFiltered(WorldObject wo)
    {
        if (!wo.HasAppraisalData)
            wo.Appraise();

        //Filter by regex
        if (!string.IsNullOrEmpty(FilterText))
        {
            if (!FilterRegex.IsMatch(wo.Name))
                return true;
        }

        //Filter by prop
        if (!showExtraFilter)
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
        }

        return false;
    }

    /// <summary>
    /// Creates a filtered list of WorldObjects for the current bag view
    /// </summary>
    private void SetFilteredItems()
    {
        //If a bag is selected and available use the items in it, otherwise use the inventory
        var bag = game.World.Get(SelectedBag);
        var items = !showBags || bag is null ? game.Character.Inventory : bag.Items;

        filteredItems = items.Where(x => !IsFiltered(x)).ToList();
        //C.Chat($"Rebuild filter {items.Count}->{filteredItems.Count}");
    }
    #endregion

    #region Draw Item Icons/Table
    private void DrawInventory()
    {
        Index = 0;

        if (showBags)
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
        if (showIcons)
            DrawItemsAsIcons();
        else
            DrawItemsAsTable();
    }

    private void DrawBagIcon(WorldObject wo)
    {
        if (ImGui.TextureButton($"{wo.Id}", wo.GetOrCreateTexture(), IconSize, 0, SelectedBag == wo.Id ? SELECTED_COLOR : UNSELECTED_COLOR))
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
            ImGui.TextureButton(wo.Id.ToString(), wo.GetOrCreateTexture(), IconSize);

            ImGui.TableSetColumnIndex((int)ItemColumn.Name);
            ImGui.Text(wo.Name);
            DrawItemContextMenu(wo);

            if (showExtraFilter && propFilter.EnumIndex != null)
            {
                ImGui.TableSetColumnIndex((int)ItemColumn.Value);
                ImGui.Text(propFilter.FindValue(wo) ?? "");
            }
            //if (ShowExtraFilter && propFilter.EnumIndex != null)
            //    ImGui.Text(propFilter.FindValue(wo) ?? "");
            //else
            //    ImGui.Text(wo.Value(IntId.Value).ToString());
        }

        ImGui.EndTable();
    }

    private void BeginBagTable()
    {
        ImGui.BeginTable("items-table", showExtraFilter ? COLUMN_FLAGS.Count : COLUMN_FLAGS.Count - 1, TABLE_FLAGS, ImGui.GetContentRegionAvail());

        ImGui.TableSetupColumn("Icon", COLUMN_FLAGS[ItemColumn.Icon], IconSize.X + ICON_PAD, (int)ItemColumn.Icon);
        ImGui.TableSetupColumn("Name", COLUMN_FLAGS[ItemColumn.Name], 0, (int)ItemColumn.Name);

        if (showExtraFilter && propFilter.EnumIndex != null)
            ImGui.TableSetupColumn(propFilter.Selection, COLUMN_FLAGS[ItemColumn.Value], 60, (int)ItemColumn.Value);
        //else
        //    ImGui.TableSetupColumn("Value", COLUMN_FLAGS[ItemColumn.Value], 60, (int)ItemColumn.Value);

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        //Sort if needed?
        //Checked to make sure after headers is fine
        SortItems();
    }
    #endregion

    #region Context Menus
    /// <summary>
    /// Draw context menu for a WorldObject container
    /// </summary>
    void DrawBagContextMenu(WorldObject wo)
    {
        if (ImGui.BeginPopupContextItem($"###{wo.Id}", ImGuiPopupFlags.MouseButtonRight))
        {
            if (ImGui.MenuItem("Drop"))
                wo.Drop();

            if (ImGui.MenuItem("Give Selected"))
            {
                if (game.World.Selected is not null)
                    wo.Give(game.World.Selected.Id);
            }

            if (ImGui.MenuItem("Give Player"))
            {
                if (game.World.Selected is not null)
                    wo.Give(game.World.Selected.Id);
                else if (TryGetNearest(out var player))
                    wo.Give(player.Id);
            }

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

            if (ImGui.MenuItem("Drop"))
                wo.Drop();

            if (ImGui.MenuItem("Use"))
                wo.Use();

            if (ImGui.MenuItem("Use Self"))
                wo.UseOn(game.CharacterId);

            //if(wo.ObjectType == ObjectType)
            //if (ImGui.MenuItem("Give Selected"))
            //{
            //    if (game.World.Selected is not null)
            //        wo.Give(game.World.Selected.Id);
            //}

            //Give selected or nearest
            if (ImGui.MenuItem("Give Player"))
            {
                if (game.World.Selected is not null)
                    wo.Give(game.World.Selected.Id);
                else if (TryGetNearest(out var player))
                    wo.Give(player.Id);
            }
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

    #region Tooltips
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
        var texture = wo.GetOrCreateTexture();

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
    #endregion
    #endregion

    private unsafe void FastDropAll()
    {
        foreach (var item in filteredItems)
        {
            item.Drop();
        }
        //foreach (var item in filteredItems)
        //{
        //    using (var stream = new MemoryStream())
        //    using (var writer = new BinaryWriter(stream))
        //    {
        //        writer.Write((uint)0xF7B1); // order header
        //        writer.Write((uint)0x0); // sequence.. ace doesnt verify this
        //        writer.Write((uint)0x001B); // drop item
        //        writer.Write((uint)item);
        //        var bytes = stream.ToArray();
        //        fixed (byte* bytesPtr = bytes)
        //        {
        //            Proto_UI.SendToControl((char*)bytesPtr, bytes.Length);
        //        }
        //    }
        //}
    }

    ActionOptions quickFail = new()
    {
        MaxRetryCount = 0,
        TimeoutMilliseconds = 100,
    };
    private unsafe void FastGiveAll()
    {
        if (game.World.Selected is null)
            return;


        if (game.World.Selected.ObjectType == ObjectType.Container)
        {
            C.Chat($"Moving items to {game.World.Selected.Name}");
            foreach (var item in filteredItems)
                //game.Actions.ObjectMove(item.Id, game.World.Selected.Id);
                item.Move(game.World.Selected.Id, 0, true, quickFail);

        }
        else
            foreach (var item in filteredItems)
                item.Give(game.World.Selected.Id);

        return;
        foreach (var item in filteredItems)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((uint)0xF7B1); // order header
                writer.Write((uint)0x0); // sequence.. ace doesnt verify this
                writer.Write((uint)0x00CD); // give item
                writer.Write(game.World.Selected.Id); //target
                writer.Write((uint)item); // item
                writer.Write(1); // amount
                var bytes = stream.ToArray();
                fixed (byte* bytesPtr = bytes)
                {
                    Proto_UI.SendToControl((char*)bytesPtr, bytes.Length);
                }
            }
        }
    }

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
                2 when !showExtraFilter => filteredItems.OrderBy(x => x.Value(IntId.Value)).ToList(),
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
                2 when !showExtraFilter => filteredItems.OrderByDescending(x => x.Value(IntId.Value)).ToList(),
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


    private bool TryGetNearest(out WorldObject wo)
    {
        wo = game.World.GetNearest(ObjectClass.Player);
        return wo is not null;
    }
    #endregion

    #region Disposal
    private void AddEvents()
    {
        try
        {
            game.OnRender2D += Game_OnRender2D;
            //game.World.OnChatInput += World_OnChatInput;

            hud.OnShow += Hud_OnShow;

            game.Messages.Incoming.Qualities_UpdateInstanceID += Incoming_Qualities_UpdateInstanceID;
            game.Messages.Incoming.Qualities_PrivateUpdateInstanceID += Incoming_Qualities_PrivateUpdateInstanceID;
        }
        catch (Exception ex)
        {
            PluginCore.Log(ex);
        }
    }

    private void RemoveEvents()
    {
        try
        {
            game.OnRender2D -= Game_OnRender2D;
            //game.World.OnChatInput -= World_OnChatInput;

            hud.OnShow -= Hud_OnShow;

            game.Messages.Incoming.Qualities_UpdateInstanceID -= Incoming_Qualities_UpdateInstanceID;
            game.Messages.Incoming.Qualities_PrivateUpdateInstanceID -= Incoming_Qualities_PrivateUpdateInstanceID;
        }
        catch (Exception ex)
        {
            PluginCore.Log(ex);
        }
    }

    public void Dispose()
    {
        try
        {
            RemoveEvents();
        }
        catch (Exception ex)
        {
            PluginCore.Log(ex);
        }
    }
    #endregion
}


