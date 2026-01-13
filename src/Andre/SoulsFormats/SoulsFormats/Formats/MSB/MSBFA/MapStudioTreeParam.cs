using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace SoulsFormats
{
    public partial class MSBFA
    {
        /// <summary>
        /// A bounding-volume hierarchy used in calculations such as drawing and collision.
        /// </summary>
        public class MapStudioTreeParam : IMsbTreeParam
        {
            /// <summary>
            /// Unknown; probably some kind of version number.
            /// </summary>
            public int Version { get; set; }

            /// <summary>
            /// The Name or Type of the Param.
            /// </summary>
            private protected string Name { get; }

            /// <summary>
            /// Whether or not this is the last param.
            /// </summary>
            internal protected bool IsLastParam;

            /// <summary>
            /// The axis-aligned tree bounding volume hierarchy root node.<br/>
            /// Set to null when not present.
            /// </summary>
            public MapStudioTreeNode Root { get; set; }
            IMsbTreeNode IMsbTreeParam.Root { get => Root; set => Root = (MapStudioTreeNode)value; }

            /// <summary>
            /// Create a new <see cref="MapStudioTreeParam"/>.
            /// </summary>
            public MapStudioTreeParam()
            {
                Version = 10001002;
                Name = "MAPSTUDIO_TREE_ST";
            }

            internal MapStudioTreeNode Read(BinaryReaderEx br)
            {
                Version = br.ReadInt32();
                int nameOffset = br.ReadInt32();
                int offsetCount = br.ReadInt32();
                int rootNodeOffset;
                if (offsetCount - 1 > 0)
                {
                    rootNodeOffset = br.ReadInt32();
                    br.Skip(Math.Max(0, (offsetCount - 2) * 4)); // Entry Offsets
                }
                else
                {
                    rootNodeOffset = -1;
                }

                int nextParamOffset = br.ReadInt32();
                string name = br.GetASCII(nameOffset);
                if (name != Name)
                    throw new InvalidDataException($"Expected param \"{Name}\", got param \"{name}\"");

                if (offsetCount - 1 != 0)
                {
                    br.Position = rootNodeOffset;
                    Root = new MapStudioTreeNode(br);
                }

                IsLastParam = true;
                if (nextParamOffset > 0)
                {
                    IsLastParam = false;
                    br.Position = nextParamOffset;
                }

                return Root;
            }

            internal void Write(BinaryWriterEx bw)
            {
                bw.WriteInt32(Version);
                bw.ReserveInt32("ParamNameOffset");
                int count = Root?.GetNodeCount() ?? 0;
                bw.WriteInt32(count + 1);
                for (int i = 0; i < count; i++)
                {
                    bw.ReserveInt32($"OffsetTreeNode_{i}");
                }
                bw.ReserveInt32("NextParamOffset");

                bw.FillInt32("ParamNameOffset", (int)bw.Position);
                bw.WriteASCII(Name, true);
                bw.Pad(4);

                int index = 0;
                Root?.Write(bw, ref index);
            }
        }

        /// <summary>
        /// A tree hierarchy of axis-aligned bounding boxes used in various calculations such as drawing, culling, and collision detection.
        /// </summary>
        public class MapStudioTreeNode : IMsbTreeNode
        {
            /// <summary>
            /// The bounding box for this node.
            /// </summary>
            public MsbBoundingBox Bounds { get; set; }

            /// <summary>
            /// The first child of this node.<br/>
            /// Set to null when not present.
            /// </summary>
            public MapStudioTreeNode FirstChild { get; set; }
            IMsbTreeNode IMsbTreeNode.FirstChild { get => FirstChild; set => FirstChild = (MapStudioTreeNode)value; }

            /// <summary>
            /// The next sibling of this node.<br/>
            /// Set to null when not present.
            /// </summary>
            public MapStudioTreeNode NextSibling { get; set; }
            IMsbTreeNode IMsbTreeNode.NextSibling { get => NextSibling; set => NextSibling = (MapStudioTreeNode)value; }

            /// <summary>
            /// Indices to the parts this node contains.
            /// </summary>
            public List<short> PartIndices { get; set; }

            /// <summary>
            /// Create a new <see cref="MapStudioTreeNode"/>.
            /// </summary>
            public MapStudioTreeNode()
            {
                PartIndices = [];
                Bounds = new MsbBoundingBox();
            }

            /// <summary>
            /// Create a new <see cref="MapStudioTreeNode"/> with the given bounding information.
            /// </summary>
            /// <param name="min">The minimum extent of the bounding box.</param>
            /// <param name="max">The maximum extent of the bounding box.</param>
            public MapStudioTreeNode(Vector3 min, Vector3 max)
            {
                PartIndices = [];
                Bounds = new MsbBoundingBox(min, max);
            }

            internal MapStudioTreeNode(BinaryReaderEx br)
            {
                long start = br.Position;
                Vector3 minimum = br.ReadVector3();
                int firstChildOffset = br.ReadInt32();
                Vector3 maximum = br.ReadVector3();
                br.AssertInt32(0); // Unknown
                Vector3 origin = br.ReadVector3();
                int siblingOffset = br.ReadInt32();
                float radius = br.ReadSingle();
                Bounds = new MsbBoundingBox(minimum, maximum, origin, radius);

                int partIndexCount = br.ReadInt32();
                PartIndices = new List<short>(partIndexCount);
                for (int i = 0; i < partIndexCount; i++)
                {
                    PartIndices.Add(br.ReadInt16());
                }

                if (firstChildOffset > 0)
                {
                    br.StepIn(start + firstChildOffset);
                    FirstChild = new MapStudioTreeNode(br);
                    br.StepOut();
                }

                if (siblingOffset > 0)
                {
                    br.StepIn(start + siblingOffset);
                    NextSibling = new MapStudioTreeNode(br);
                    br.StepOut();
                }
            }

            internal void Write(BinaryWriterEx bw, ref int index)
            {
                long start = bw.Position;
                bw.FillInt32($"OffsetTreeNode_{index}", (int)start);
                string fillStr1 = $"TreeNodeFirstChild_{index}";
                string fillStr2 = $"TreeNodeNextSibling_{index}";

                bw.WriteVector3(Bounds.Min);
                bw.ReserveInt32(fillStr1);
                bw.WriteVector3(Bounds.Max);
                bw.WriteInt32(0); // Unknown
                bw.WriteVector3(Bounds.Origin);
                bw.ReserveInt32(fillStr2);
                bw.WriteSingle(Bounds.Radius);
                bw.WriteInt32(PartIndices.Count);
                for (int i = 0; i < PartIndices.Count; i++)
                {
                    bw.WriteInt16(PartIndices[i]);
                }

                bw.Pad(0x10);
                index += 1;

                if (FirstChild != null)
                {
                    bw.FillInt32(fillStr1, (int)(bw.Position - start));
                    FirstChild.Write(bw, ref index);
                }
                else
                {
                    bw.FillInt32(fillStr1, 0);
                }

                if (NextSibling != null)
                {
                    bw.FillInt32(fillStr2, (int)(bw.Position - start));
                    NextSibling.Write(bw, ref index);
                }
                else
                {
                    bw.FillInt32(fillStr2, 0);
                }
            }

            public int GetNodeCount()
            {
                int count = 1;
                if (FirstChild != null)
                {
                    count += FirstChild.GetNodeCount();
                }

                if (NextSibling != null)
                {
                    count += NextSibling.GetNodeCount();
                }

                return count;
            }
        }
    }
}