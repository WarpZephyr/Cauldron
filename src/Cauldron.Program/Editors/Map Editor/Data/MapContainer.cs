using Andre.Formats;
using SoulsFormats;
using StudioCore.Application;
using StudioCore.Editors.Common;
using StudioCore.Renderer;
using StudioCore.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml.Serialization;

namespace StudioCore.Editors.MapEditor;

/// <summary>
///     High level class that stores a single map (msb) and can serialize/
///     deserialize it. This is the logical portion of the map and does not
///     handle tasks like rendering or loading associated assets with it.
/// </summary>
public class MapContainer : ObjectContainer
{

    [XmlIgnore]
    private MapEditorScreen Editor;

    // This keeps all models that exist when loading a map, so that saves
    // can be byte perfect
    private readonly Dictionary<string, IMsbModel> LoadedModels;

    public MapContentLoadState LoadState = MapContentLoadState.Unloaded;

    [XmlIgnore]
    public List<Entity> BTLParents;

    [XmlIgnore]
    public Entity AutoInvadeParent = null;

    [XmlIgnore]
    public List<Entity> LightAtlasParents;

    [XmlIgnore]
    public List<Entity> LightProbeParents;

    [XmlIgnore]
    public Entity NavmeshParent = null;

    public Entity MapOffsetNode { get; set; }

    public List<Entity> Parts;
    public List<Entity> Events;
    public List<Entity> Regions;

    public List<Entity> Models;
    public List<Entity> Layers;
    public List<Entity> Routes;

    [XmlIgnore]
    public LightAtlasResolver LightAtlasResolver;

    public MapContainer(MapEditorScreen editor, string mapid)
    {
        Editor = editor;
        Name = mapid;

        LightAtlasResolver = new LightAtlasResolver(Editor, Editor.Project, this);

        LoadedModels = new();

        BTLParents = new();
        LightAtlasParents = new();
        LightProbeParents = new();

        Parts = new();
        Events = new();
        Regions = new();

        Models = new();
        Layers = new();
        Routes = new();

        var t = new MapTransformNode(mapid);
        RootObject = new MsbEntity(Editor, this, t, MsbEntityType.MapRoot);
        MapOffsetNode = new MsbEntity(Editor, this, new MapTransformNode(mapid));

        RootObject.AddChild(MapOffsetNode);
    }


    /// <summary>
    ///     The map offset used to transform BTL lights, DS2 Generators, and Navmesh.
    ///     Only DS2, Bloodborne, DS3, and Sekiro define map offsets.
    /// </summary>
    public Transform MapOffset
    {
        get => MapOffsetNode.GetLocalTransform();
        set
        {
            var node = (MapTransformNode)MapOffsetNode.WrappedObject;
            node.Position = value.Position;
            var x = Utils.RadiansToDeg(value.EulerRotation.X);
            var y = Utils.RadiansToDeg(value.EulerRotation.Y);
            var z = Utils.RadiansToDeg(value.EulerRotation.Z);
            node.Rotation = new Vector3(x, y, z);
        }
    }

    public void Unload()
    {
        foreach (Entity obj in Objects)
        {
            if (obj != null)
            {
                obj.Dispose();
            }
        }
    }

    public void LoadMSB(IMsb msb)
    {
        foreach (IMsbModel m in msb.Models.GetEntries())
        {
            var n = new MsbEntity(Editor, this, m, MsbEntityType.Model);
            Models.Add(n);
            Objects.Add(n);
            RootObject.AddChild(n);
        }

        foreach (IMsbPart p in msb.Parts.GetEntries())
        {
            var n = new MsbEntity(Editor, this, p, MsbEntityType.Part);
            Parts.Add(n);
            Objects.Add(n);
            RootObject.AddChild(n);
        }

        foreach (IMsbRegion p in msb.Regions.GetEntries())
        {
            var n = new MsbEntity(Editor, this, p, MsbEntityType.Region);
            Regions.Add(n);
            Objects.Add(n);
            RootObject.AddChild(n);
        }

        foreach (IMsbEvent p in msb.Events.GetEntries())
        {
            var n = new MsbEntity(Editor, this, p, MsbEntityType.Event);
            Events.Add(n);

            if (p is MSB2.Event.MapOffset mo1)
            {
                var t = Transform.Default;
                t.Position = mo1.Translation;
                MapOffset = t;
            }
            else if (p is MSBB.Event.MapOffset mo2)
            {
                var t = Transform.Default;
                t.Position = mo2.Position;
                t.EulerRotation = new Vector3(0f, Utils.DegToRadians(mo2.RotationY), 0f);
                MapOffset = t;
            }
            else if (p is MSB3.Event.MapOffset mo3)
            {
                var t = Transform.Default;
                t.Position = mo3.Position;
                t.EulerRotation = new Vector3(0f, Utils.DegToRadians(mo3.RotationY), 0f);
                MapOffset = t;
            }
            else if (p is MSBS.Event.MapOffset mo4)
            {
                var t = Transform.Default;
                t.Position = mo4.Position;
                t.EulerRotation = new Vector3(0f, Utils.DegToRadians(mo4.RotationY), 0f);
                MapOffset = t;
            }

            Objects.Add(n);
            RootObject.AddChild(n);
        }

        if (msb is IMsbRouted msbRouted)
        {
            foreach (IMsbRoute p in msbRouted.Routes.GetEntries())
            {
                var n = new MsbEntity(Editor, this, p, MsbEntityType.Route);
                Routes.Add(n);
                Objects.Add(n);
                RootObject.AddChild(n);
            }
        }

        if (msb is IMsbLayered msbLayered)
        {
            foreach (IMsbLayer p in msbLayered.Layers.GetEntries())
            {
                var n = new MsbEntity(Editor, this, p, MsbEntityType.Layer);
                Layers.Add(n);
                Objects.Add(n);
                RootObject.AddChild(n);
            }
        }

        foreach (Entity m in Objects)
        {
            m.BuildReferenceMap();
        }

        // Add map-level references after all others
        RootObject.BuildReferenceMap();
    }

    public void LoadBTL(FileDictionaryEntry curEntry, BTL btl)
    {
        var btlParent = new MsbEntity(Editor, this, curEntry.Filename, MsbEntityType.Editor);
        MapOffsetNode.AddChild(btlParent);
        foreach (BTL.Light l in btl.Lights)
        {
            var n = new MsbEntity(Editor, this, l, MsbEntityType.Light);
            Objects.Add(n);
            btlParent.AddChild(n);
        }

        BTLParents.Add(btlParent);
    }

    public void LoadAIP(string mapName, AIP aip)
    {
        var autoInvadeParent = new MsbEntity(Editor, this, mapName, MsbEntityType.Editor);

        MapOffsetNode.AddChild(autoInvadeParent);

        foreach (var point in aip.Points)
        {
            var newEntity = new MsbEntity(Editor, this, point, MsbEntityType.AutoInvadePoint);

            newEntity.SupportsName = false;

            Objects.Add(newEntity);
            autoInvadeParent.AddChild(newEntity);
        }

        AutoInvadeParent = autoInvadeParent;
    }

    public void LoadBTAB(string fileName, BTAB btab)
    {
        var lightAtlasParent = new MsbEntity(Editor, this, fileName, MsbEntityType.Editor);

        MapOffsetNode.AddChild(lightAtlasParent);

        foreach (var entry in btab.Entries)
        {
            var newEntity = new MsbEntity(Editor, this, entry, MsbEntityType.LightAtlas);
            newEntity.SupportsName = false;

            Objects.Add(newEntity);
            lightAtlasParent.AddChild(newEntity);
        }

        LightAtlasParents.Add(lightAtlasParent);
    }

    public void LoadBTPB(string fileName, BTPB btpb)
    {
        var lightProbeParent = new MsbEntity(Editor, this, fileName, MsbEntityType.Editor);

        MapOffsetNode.AddChild(lightProbeParent);

        foreach (var volume in btpb.Groups)
        {
            var newEntity = new MsbEntity(Editor, this, volume, MsbEntityType.LightProbeVolume);
            newEntity.SupportsName = false;

            Objects.Add(newEntity);
            lightProbeParent.AddChild(newEntity);
        }

        LightProbeParents.Add(lightProbeParent);
    }

    public void LoadHavokNVA(string mapName, NVA nva)
    {
        var nvaParent = new MsbEntity(Editor, this, mapName, MsbEntityType.Editor);

        MapOffsetNode.AddChild(nvaParent);

        // Navmesh Info
        foreach (var curNavmesh in nva.NavmeshInfoEntries)
        {
            var newEntity = new MsbEntity(Editor, this, curNavmesh, MsbEntityType.Navmesh);

            newEntity.SupportsName = false;

            var navid = $@"n{curNavmesh.ModelID:D6}";
            var navname = "n" + ModelLocator.MapModelNameToAssetName(Editor.Project, mapName, navid).Substring(1);

            ResourceDescriptor nasset = ModelLocator.GetHavokNavmeshModel(Editor.Project, mapName, navname);

            var mesh = MeshRenderableProxy.MeshRenderableFromHavokNavmeshResource(
                Editor.Universe.RenderScene, nasset.AssetVirtualPath, ModelMarkerType.Other);

            mesh.World = newEntity.GetWorldMatrix();
            mesh.SetSelectable(newEntity);
            mesh.DrawFilter = RenderFilter.Navmesh;
            newEntity.RenderSceneMesh = mesh;

            Objects.Add(newEntity);
            nvaParent.AddChild(newEntity);
        }

        // Face Data
        foreach (var curEntry in nva.FaceDataEntries)
        {
            var newEntity = new MsbEntity(Editor, this, curEntry, MsbEntityType.Navmesh);

            newEntity.SupportsName = false;

            Objects.Add(newEntity);
            nvaParent.AddChild(newEntity);
        }

        // Node Bank Data
        foreach (var curEntry in nva.NodeBankEntries)
        {
            var newEntity = new MsbEntity(Editor, this, curEntry, MsbEntityType.Navmesh);

            newEntity.SupportsName = false;

            Objects.Add(newEntity);
            nvaParent.AddChild(newEntity);
        }

        // Section 3 - Always empty
        //foreach (var curEntry in nva.Section3Entries)
        //{
        //    var newEntity = new MsbEntity(Editor, this, curEntry, MsbEntityType.Navmesh);

        //    newEntity.SupportsName = false;

        //    Objects.Add(newEntity);
        //    nvaParent.AddChild(newEntity);
        //}

        // Connectors
        foreach (var curEntry in nva.ConnectorEntries)
        {
            var newEntity = new MsbEntity(Editor, this, curEntry, MsbEntityType.Navmesh);

            newEntity.SupportsName = false;

            Objects.Add(newEntity);
            nvaParent.AddChild(newEntity);
        }

        // Level Connectors
        foreach (var curEntry in nva.LevelConnectorEntries)
        {
            var newEntity = new MsbEntity(Editor, this, curEntry, MsbEntityType.Navmesh);

            newEntity.SupportsName = false;

            var mesh = RenderableHelper.GetLevelConnectorSphereProxy(Editor.MapViewportView.RenderScene);

            mesh.World = newEntity.GetWorldMatrix();
            mesh.SetSelectable(newEntity);
            mesh.DrawFilter = RenderFilter.Navmesh;
            newEntity.RenderSceneMesh = mesh;

            Objects.Add(newEntity);
            nvaParent.AddChild(newEntity);
        }

        if (nva.Version == NVA.NVAVersion.EldenRing)
        {
            // Section 9 - Always empty
            //foreach (var curEntry in nva.Section9Entries)
            //{
            //    var newEntity = new MsbEntity(Editor, this, curEntry, MsbEntityType.Navmesh);

            //    newEntity.SupportsName = false;

            //    Objects.Add(newEntity);
            //    nvaParent.AddChild(newEntity);
            //}

            // Section 10
            foreach (var curEntry in nva.Section10Entries)
            {
                var newEntity = new MsbEntity(Editor, this, curEntry, MsbEntityType.Navmesh);

                newEntity.SupportsName = false;

                Objects.Add(newEntity);
                nvaParent.AddChild(newEntity);
            }

            // Section 11
            foreach (var curEntry in nva.Section11Entries)
            {
                var newEntity = new MsbEntity(Editor, this, curEntry, MsbEntityType.Navmesh);

                newEntity.SupportsName = false;

                Objects.Add(newEntity);
                nvaParent.AddChild(newEntity);
            }

            // Section 12
            foreach (var curEntry in nva.Section12Entries)
            {
                var newEntity = new MsbEntity(Editor, this, curEntry, MsbEntityType.Navmesh);

                newEntity.SupportsName = false;

                Objects.Add(newEntity);
                nvaParent.AddChild(newEntity);
            }

            // Section 13
            foreach (var curEntry in nva.Section13Entries)
            {
                var newEntity = new MsbEntity(Editor, this, curEntry, MsbEntityType.Navmesh);

                newEntity.SupportsName = false;

                Objects.Add(newEntity);
                nvaParent.AddChild(newEntity);
            }
        }

        NavmeshParent = nvaParent;
    }

    private void AddModel(IMsb m, string name)
    {
        IMsbModel model = CreateMsbModel(name);
        if (model != null)
        {
            m.Models.Add(model);
        }
    }

    public MsbEntity CreateModel(string name)
    {
        IMsbModel model = CreateMsbModel(name);
        if (model != null)
        {
            var n = new MsbEntity(Editor, this, model, MsbEntityType.Model);
            Objects.Add(n);
            RootObject.AddChild(n);
            n.BuildReferenceMap();

            // Add map-level references after all others
            RootObject.BuildReferenceMap();
            Editor.EntityTypeCache.InvalidateCache();
            return n;
        }

        return null;
    }

    private IMsbModel CreateMsbModel(string name)
    {
        IMsbModel model = null;
        if (Editor.Project.ProjectType is ProjectType.ACFA)
        {
            model = CreateModelACFA(name);
        }
        else if (Editor.Project.ProjectType is ProjectType.ACV)
        {
            model = CreateModelACV(name);
        }
        else if (Editor.Project.ProjectType is ProjectType.ACVD)
        {
            model = CreateModelACVD(name);
        }

        return model;
    }

    private static MSBFA.Model CreateModelACFA(string name)
    {
        MSBFA.Model model;

        if (name.StartsWith("m", StringComparison.CurrentCultureIgnoreCase))
        {
            model = new MSBFA.Model.MapPiece
            {
                Name = name,
                ResourcePath = $@"N:\AC45\data\model\map\{name}\model_sib\{name}.sib"
            };
        }
        else if (name.StartsWith("o", StringComparison.CurrentCultureIgnoreCase))
        {
            model = new MSBFA.Model.Object
            {
                Name = name,
                ResourcePath = $@"N:\AC45\data\model\obj\{name}\model_sib\{name}.sib"
            };
        }
        else if (name.StartsWith("e", StringComparison.CurrentCultureIgnoreCase))
        {
            model = new MSBFA.Model.Enemy
            {
                Name = name,
                ResourcePath = $@"N:\AC45\data\model\ene\{name}\model_sib\{name}.sib"
            };
        }
        else if (name.StartsWith("a", StringComparison.CurrentCultureIgnoreCase))
        {
            model = new MSBFA.Model.Dummy
            {
                Name = name,
                ResourcePath = $@"N:\AC45\data\model\dummy\dummy_ac\{name}.ap2"
            };
        }
        else
        {
            model = null;
        }

        return model;
    }

    private static MSBV.Model CreateModelACV(string name)
    {
        MSBV.Model model;

        if (name.StartsWith("m", StringComparison.CurrentCultureIgnoreCase))
        {
            model = new MSBV.Model.MapPiece
            {
                Name = name,
                ResourcePath = $@"N:\ACV\data\model\map\{name}\model_sib\{name}.sib"
            };
        }
        else if (name.StartsWith("o", StringComparison.CurrentCultureIgnoreCase))
        {
            model = new MSBV.Model.Object
            {
                Name = name,
                ResourcePath = $@"N:\ACV\data\model\obj\{name}\model_sib\{name}.sib"
            };
        }
        else if (name.StartsWith("e", StringComparison.CurrentCultureIgnoreCase))
        {
            model = new MSBV.Model.Enemy
            {
                Name = name,
                ResourcePath = $@"N:\ACV\data\model\ene\{name}\model_sib\{name}.sib"
            };
        }
        else if (name.StartsWith("a", StringComparison.CurrentCultureIgnoreCase))
        {
            model = new MSBV.Model.Dummy
            {
                Name = name,
                ResourcePath = $@"N:\ACV\data\model\dummy\dummy_ac\{name}.ap2"
            };
        }
        else
        {
            model = null;
        }

        return model;
    }

    private static MSBVD.Model CreateModelACVD(string name)
    {
        MSBVD.Model model;

        if (name.StartsWith("m", StringComparison.CurrentCultureIgnoreCase))
        {
            model = new MSBVD.Model.MapPiece
            {
                Name = name,
                ResourcePath = $@"N:\ACV2\data\model\map\{name}\model_sib\{name}.sib"
            };
        }
        else if (name.StartsWith("o", StringComparison.CurrentCultureIgnoreCase))
        {
            model = new MSBVD.Model.Object
            {
                Name = name,
                ResourcePath = $@"N:\ACV2\data\model\obj\{name}\model_sib\{name}.sib"
            };
        }
        else if (name.StartsWith("e", StringComparison.CurrentCultureIgnoreCase))
        {
            model = new MSBVD.Model.Enemy
            {
                Name = name,
                ResourcePath = $@"N:\ACV2\data\model\ene\{name}\model_sib\{name}.sib"
            };
        }
        else if (name.StartsWith("a", StringComparison.CurrentCultureIgnoreCase))
        {
            model = new MSBVD.Model.Dummy
            {
                Name = name,
                ResourcePath = $@"N:\ACV2\data\model\dummy\dummy_ac\{name}.ap2"
            };
        }
        else
        {
            model = null;
        }

        return model;
    }

    public void SerializeToMSB(IMsb msb, ProjectType game)
    {
        foreach (Entity m in Objects)
        {
            if (m.WrappedObject != null && m.WrappedObject is IMsbModel mo)
            {
                msb.Models.Add(mo);
            }
            else if (m.WrappedObject != null && m.WrappedObject is IMsbPart p)
            {
                msb.Parts.Add(p);
                if (m is MsbEntity me && !me.HasModel())
                {
                    AddModel(msb, Name);
                }
            }
            else if (m.WrappedObject != null && m.WrappedObject is IMsbRegion r)
            {
                msb.Regions.Add(r);
            }
            else if (m.WrappedObject != null && m.WrappedObject is IMsbEvent e)
            {
                msb.Events.Add(e);
            }
            else if (msb is IMsbRouted msbRouted && m.WrappedObject != null && m.WrappedObject is IMsbRoute ro)
            {
                msbRouted.Routes.Add(ro);
            }
            else if (msb is IMsbLayered msbLayered && m.WrappedObject != null && m.WrappedObject is IMsbLayer l)
            {
                msbLayered.Layers.Add(l);
            }
        }
    }

    /// <summary>
    ///     Gets all BTL.Light with matching ParentBtlNames.
    /// </summary>
    public List<BTL.Light> SerializeBtlLights(string btlName)
    {
        List<BTL.Light> lights = new();
        foreach (Entity p in BTLParents)
        {
            var name = (string)p.WrappedObject;
            if (name == btlName)
            {
                foreach (Entity e in p.Children)
                {
                    if (e.WrappedObject != null && e.WrappedObject is BTL.Light light)
                    {
                        lights.Add(light);
                    }
                    else
                    {
                        throw new Exception($"WrappedObject \"{e.WrappedObject}\" is not a BTL Light.");
                    }
                }
            }
        }

        return lights;
    }

    public void SerializeToXML(XmlSerializer serializer, TextWriter writer, ProjectType game)
    {
        serializer.Serialize(writer, this);
    }

    public bool SerializeDS2Generators(Param locations, Param generators)
    {
        HashSet<long> ids = new();
        foreach (Entity o in Objects)
        {
            if (o is MsbEntity m && m.Type == MsbEntityType.DS2Generator &&
                m.WrappedObject is MergedParamRow mp)
            {
                if (!ids.Contains(mp.ID))
                {
                    ids.Add(mp.ID);
                }
                else
                {
                    PlatformUtils.Instance.MessageBox(
                        $@"{mp.Name} has an ID that's already used. Please change it to something unique and save again.",
                        "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                Param.Row loc = mp.GetRow("generator-loc");
                if (loc != null)
                {
                    // Set param positions
                    var newloc = new Param.Row(loc, locations);
                    newloc.GetCellHandleOrThrow("PositionX").SetValue(
                        (float)loc.GetCellHandleOrThrow("PositionX").Value);
                    newloc.GetCellHandleOrThrow("PositionY").SetValue(
                        (float)loc.GetCellHandleOrThrow("PositionY").Value);
                    newloc.GetCellHandleOrThrow("PositionZ").SetValue(
                        (float)loc.GetCellHandleOrThrow("PositionZ").Value);
                    locations.AddRow(newloc);
                }

                Param.Row gen = mp.GetRow("generator");
                if (gen != null)
                {
                    generators.AddRow(new Param.Row(gen, generators));
                }
            }
        }

        return true;
    }

    public bool SerializeDS2Regist(Param regist)
    {
        HashSet<long> ids = new();
        foreach (Entity o in Objects)
        {
            if (o is MsbEntity m && m.Type == MsbEntityType.DS2GeneratorRegist &&
                m.WrappedObject is Param.Row mp)
            {
                if (!ids.Contains(mp.ID))
                {
                    ids.Add(mp.ID);
                }
                else
                {
                    PlatformUtils.Instance.MessageBox(
                        $@"{mp.Name} has an ID that's already used. Please change it to something unique and save again.",
                        "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                regist.AddRow(new Param.Row(mp, regist));
            }
        }

        return true;
    }

    public bool SerializeDS2Events(Param evs)
    {
        HashSet<long> ids = new();
        foreach (Entity o in Objects)
        {
            if (o is MsbEntity m && m.Type == MsbEntityType.DS2Event && m.WrappedObject is Param.Row mp)
            {
                if (!ids.Contains(mp.ID))
                {
                    ids.Add(mp.ID);
                }
                else
                {
                    PlatformUtils.Instance.MessageBox(
                        $@"{mp.Name} has an ID that's already used. Please change it to something unique and save again.",
                        "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                var newloc = new Param.Row(mp, evs);
                evs.AddRow(newloc);
            }
        }

        return true;
    }

    public bool SerializeDS2EventLocations(Param locs)
    {
        HashSet<long> ids = new();
        foreach (Entity o in Objects)
        {
            if (o is MsbEntity m && m.Type == MsbEntityType.DS2EventLocation &&
                m.WrappedObject is Param.Row mp)
            {
                if (!ids.Contains(mp.ID))
                {
                    ids.Add(mp.ID);
                }
                else
                {
                    PlatformUtils.Instance.MessageBox(
                        $@"{mp.Name} has an ID that's already used. Please change it to something unique and save again.",
                        "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Set param location positions
                var newloc = new Param.Row(mp, locs);
                newloc.GetCellHandleOrThrow("PositionX").SetValue(
                    (float)mp.GetCellHandleOrThrow("PositionX").Value);
                newloc.GetCellHandleOrThrow("PositionY").SetValue(
                    (float)mp.GetCellHandleOrThrow("PositionY").Value);
                newloc.GetCellHandleOrThrow("PositionZ").SetValue(
                    (float)mp.GetCellHandleOrThrow("PositionZ").Value);
                locs.AddRow(newloc);
            }
        }

        return true;
    }

    public bool SerializeDS2ObjInstances(Param objs)
    {
        HashSet<long> ids = new();
        foreach (Entity o in Objects)
        {
            if (o is MsbEntity m && m.Type == MsbEntityType.DS2ObjectInstance &&
                m.WrappedObject is Param.Row mp)
            {
                if (!ids.Contains(mp.ID))
                {
                    ids.Add(mp.ID);
                }
                else
                {
                    PlatformUtils.Instance.MessageBox(
                        $@"{mp.Name} has an ID that's already used. Please change it to something unique and save again.",
                        "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                var newobj = new Param.Row(mp, objs);
                objs.AddRow(newobj);
            }
        }

        return true;
    }
}
