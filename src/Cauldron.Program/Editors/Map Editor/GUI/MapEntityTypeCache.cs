using StudioCore.Application;
using StudioCore.Editors.Common;
using System;
using System.Collections.Generic;

namespace StudioCore.Editors.MapEditor;

public class MapEntityTypeCache
{
    private MapEditorScreen Editor;

    public Dictionary<string, Dictionary<MsbEntityType, Dictionary<Type, List<MsbEntity>>>> _cachedTypeView;

    public MapEntityTypeCache(MapEditorScreen editor)
    {
        Editor = editor;
    }

    public void InvalidateCache()
    {
        _cachedTypeView = null;
    }

    public void RemoveMapFromCache(MapContainer container)
    {
        if (_cachedTypeView != null &&
            container == null && 
            _cachedTypeView.ContainsKey(container.Name))
        {
            _cachedTypeView.Remove(container.Name);
        }
    }

    public void AddMapToCache(MapContainer map)
    {
        if (_cachedTypeView == null || !_cachedTypeView.ContainsKey(map.Name))
        {
            RebuildCache(map);
        }
    }

    public void RebuildCache(MapContainer map)
    {
        if (_cachedTypeView == null)
        {
            _cachedTypeView =
                new Dictionary<string, Dictionary<MsbEntityType, Dictionary<Type, List<MsbEntity>>>>();
        }

        // Build the groupings from each top type
        Dictionary<MsbEntityType, Dictionary<Type, List<MsbEntity>>> mapcache = new()
        {
            // Internal Types
            { MsbEntityType.Model, new Dictionary<Type, List<MsbEntity>>() },
            { MsbEntityType.Part, new Dictionary<Type, List<MsbEntity>>() },
            { MsbEntityType.Region, new Dictionary<Type, List<MsbEntity>>() },
            { MsbEntityType.Event, new Dictionary<Type, List<MsbEntity>>() }
        };

        // Routes
        if (Editor.Project.ProjectType is ProjectType.AC4
            or ProjectType.ACFA
            or ProjectType.ACV
            or ProjectType.ACVD)
        {
            mapcache.Add(MsbEntityType.Route, new Dictionary<Type, List<MsbEntity>>());
        }

        // Layers
        if (Editor.Project.ProjectType is ProjectType.AC4
            or ProjectType.ACFA
            or ProjectType.ACV
            or ProjectType.ACVD)
        {
            mapcache.Add(MsbEntityType.Layer, new Dictionary<Type, List<MsbEntity>>());
        }

        // MapStudioTree
        if (Editor.Project.ProjectType is ProjectType.AC4
             or ProjectType.ACFA
             or ProjectType.ACV
             or ProjectType.ACVD)
        {
            mapcache.Add(MsbEntityType.MapStudioTree, new Dictionary<Type, List<MsbEntity>>());
        }

        // External: BTL
        if (Editor.Project.ProjectType is ProjectType.BB
            or ProjectType.DS3
            or ProjectType.SDT
            or ProjectType.ER
            or ProjectType.NR
            or ProjectType.AC6)
        {
            mapcache.Add(MsbEntityType.Light, new Dictionary<Type, List<MsbEntity>>());
        }

        // External: AIP
        if (Editor.AutoInvadeBank.CanUse())
        {
            mapcache.Add(MsbEntityType.AutoInvadePoint, new Dictionary<Type, List<MsbEntity>>());
        }

        // External: BTAB
        if (Editor.LightAtlasBank.CanUse())
        {
            mapcache.Add(MsbEntityType.LightAtlas, new Dictionary<Type, List<MsbEntity>>());
        }

        // External: BTPB
        if (Editor.LightProbeBank.CanUse())
        {
            mapcache.Add(MsbEntityType.LightProbeVolume, new Dictionary<Type, List<MsbEntity>>());
        }

        // External: NVA
        if (Editor.HavokNavmeshBank.CanUse())
        {
            mapcache.Add(MsbEntityType.Navmesh, new Dictionary<Type, List<MsbEntity>>());
        }

        // External: DS2 PARAM
        else if (Editor.Project.ProjectType is ProjectType.DS2S
            or ProjectType.DS2)
        {
            mapcache.Add(MsbEntityType.Light, new Dictionary<Type, List<MsbEntity>>());

            mapcache.Add(MsbEntityType.DS2Event, new Dictionary<Type, List<MsbEntity>>());

            mapcache.Add(MsbEntityType.DS2EventLocation, new Dictionary<Type, List<MsbEntity>>());

            mapcache.Add(MsbEntityType.DS2Generator, new Dictionary<Type, List<MsbEntity>>());

            mapcache.Add(MsbEntityType.DS2GeneratorRegist, new Dictionary<Type, List<MsbEntity>>());
        }

        // Fill the map cache
        foreach (Entity obj in map.Objects)
        {
            if (obj is MsbEntity e && mapcache.ContainsKey(e.Type))
            {
                Type typ = e.WrappedObject.GetType();
                if (!mapcache[e.Type].ContainsKey(typ))
                {
                    mapcache[e.Type].Add(typ, new List<MsbEntity>());
                }

                mapcache[e.Type][typ].Add(e);
            }
        }

        // Fill the type cache for this map
        if (!_cachedTypeView.ContainsKey(map.Name))
        {
            _cachedTypeView.Add(map.Name, mapcache);
        }
        else
        {
            _cachedTypeView[map.Name] = mapcache;
        }
    }
}
