﻿//using ACE.DatLoader.FileTypes;
//using ACE.Entity.Models;
using ImGuiNET;
using System;
using System.Collections.Generic;
using UtilityBelt.Scripting.Interop;
using UtilityBelt.Service;
using PropType = ACEditor.Props.PropType;
using System.Threading.Tasks;
using ACEditor.Table;
using WorldObject = UtilityBelt.Scripting.Interop.WorldObject;
using InventoryUI;

namespace ACEditor;
internal class PropertyEditor : IDisposable
{
    /// <summary>
    /// The UBService Hud
    /// </summary>
    readonly UtilityBelt.Service.Views.Hud hud;
    readonly Game game = new();
    readonly List<PropertyTable> propTables = new()
    {
        new (PropType.Int),
        new (PropType.Int64),
        new (PropType.Float),
        new (PropType.String),
        new (PropType.DataId),
        new (PropType.InstanceId),
    };

    /// <summary>
    /// Original clone of the WorldObject
    /// </summary>
    PropertyData Original = new();
    /// <summary>
    /// Current version of property data
    /// </summary>
    //PropertyData Current = new();

    public PropertyEditor()
    {
        // Create a new UBService Hud
        hud = UBService.Huds.CreateHud("ACEditor");

        hud.Visible = true;
        
        //hud.WindowSettings = ImGuiWindowFlags.AlwaysAutoResize;

        // set to show our icon in the UBService HudBar
        hud.ShowInBar = true;

        // subscribe to the hud render event so we can draw some controls
        hud.OnRender += Hud_OnRender;

        game.World.OnObjectSelected += OnSelected;
    }


    private Task OnSelected(object sender, UtilityBelt.Scripting.Events.ObjectSelectedEventArgs e)
    {
        return Task.CompletedTask;
        var wo = game.World.Get(e.ObjectId);

        if (wo is null)
            return Task.CompletedTask;

        SetTarget(wo);

        return Task.CompletedTask;
    }

    //Change the target being edited
    private void SetTarget(WorldObject wo)
    {
        //Clone WO
        Original = new PropertyData(wo);
        C.Chat($"Target now: {wo.Name}");
        //Current = new PropertyData(wo);

        foreach (var table in propTables)
        {
            table.SetTarget(Original);
        }
    }


    /// <summary>
    /// Called every time the ui is redrawing.
    /// </summary>
    private void Hud_OnRender(object sender, EventArgs e)
    {
        try
        {
            DrawMenu();

            //ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 300), ImGuiCond.FirstUseEver);
            ImGui.BeginChild("Editor");
            DrawTabBar();
            ImGui.EndChild();
        }
        catch (Exception ex)
        {
            PluginCore.Log(ex);
        }
    }

    private void DrawMenu()
    {
        //Draw each table as a tab
        if (ImGui.Button("Selected"))
        {
            if (game.World.Selected != null)
                SetTarget(game.World.Selected);
            else
                C.Chat("No WorldObject selected!");
        }
        ImGui.SameLine();
        if (ImGui.Button("Save"))
        {
            C.Chat("Todo!");
        }
        ImGui.Separator();

    }

    private void DrawTabBar()
    {
        if (ImGui.BeginTabBar("PropertyTab"))
        {
            //ImGui.Text($"Tabs: {propTables.Count}");
            foreach (var table in propTables)
            {
                if (ImGui.BeginTabItem($"{table.Name}"))
                {
                   // ImGui.Text($"Testing {table.Type}");

                    table.Render();

                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }
    }

    public void Dispose()
    {
        try
        {
            game.World.OnObjectSelected -= OnSelected;
            //hud.OnRender -= Hud_OnRender;
        }
        catch (Exception)
        {
            throw;
        }

        hud?.Dispose();
    }
}