using AcClient;
//using ACE.DatLoader.FileTypes;
//using ACE.Entity.Models;
using CommandLine;
using Decal.Adapter;
using ImGuiNET;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Numerics;
using System.Reflection.Emit;
using System.Reflection;
using System.Security.Claims;
using System.Text.RegularExpressions;
using UtilityBelt.Common.Enums;
using UtilityBelt.Scripting.Interop;
using UtilityBelt.Scripting.Lib;
using UtilityBelt.Service;
using UtilityBelt.Service.Views;
using static AcClient.UIQueueManager;
using static System.Net.Mime.MediaTypeNames;
using static UtilityBelt.Common.Messages.Types.Fellowship;
using static UtilityBelt.Common.Messages.Types.PlayerModule.OptionProperty.Window;
using ACEditor.Props;
using PropType = ACEditor.Props.PropType;
using UtilityBelt.Scripting;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Linq.Expressions;
using ACEditor.Table;
using Decal.Adapter.Wrappers;
using WorldObject = UtilityBelt.Scripting.Interop.WorldObject;

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
        new (PropType.PropertyInt),
        new (PropType.PropertyInt64),
        new (PropType.PropertyFloat),
        new (PropType.PropertyString),
        new (PropType.PropertyDataId),
        new (PropType.PropertyInstanceId),
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