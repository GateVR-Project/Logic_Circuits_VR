using System;
using System.Collections.Generic;

namespace GateVR.Application
{
    /// <summary>
    /// Coloca interruptores, compuertas y LED dentro del rectángulo de la placa.
    /// Las distancias están en metros del mismo espacio local. No usa UnityEngine.
    /// </summary>
    public static class StationLayout
    {
        public struct Area
        {
            public float MinX;
            public float MaxX;
            public float MinZ;
            public float MaxZ;

            public float Width
            {
                get { return MaxX - MinX; }
            }

            public float Depth
            {
                get { return MaxZ - MinZ; }
            }

            public static Area FromCenter(float centerX, float centerZ, float halfX, float halfZ)
            {
                return new Area
                {
                    MinX = centerX - halfX,
                    MaxX = centerX + halfX,
                    MinZ = centerZ - halfZ,
                    MaxZ = centerZ + halfZ
                };
            }
        }

        /// <summary>
        /// Distancias del pivote al borde del cuerpo y de sus pines.
        /// </summary>
        public struct Span
        {
            public float Left;
            public float Right;
            public float Near;
            public float Far;

            public static Span Union(Span a, Span b)
            {
                return new Span
                {
                    Left = a.Left > b.Left ? a.Left : b.Left,
                    Right = a.Right > b.Right ? a.Right : b.Right,
                    Near = a.Near > b.Near ? a.Near : b.Near,
                    Far = a.Far > b.Far ? a.Far : b.Far
                };
            }
        }

        public sealed class Plan
        {
            public bool Fits;
            public float GateScale;
            public float Gap;
            public float LaneSpacing;
            public Area Board;
            public float[] GateX = new float[0];
            public float[] GateZ = new float[0];
            public float LedX;
            public float LedZ;
            public float SwitchAX;
            public float SwitchAZ;
            public float SwitchBX;
            public float SwitchBZ;
        }

        public static void AssignColumns(CircuitStructure structure, int count, int[] columns, float[] lanes)
        {
            for (int i = 0; i < count; i++)
            {
                columns[i] = i;
                lanes[i] = 0f;
            }

            if (structure == null || !UsesParallelLanes(structure))
                return;

            var columnOf = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < structure.Modules.Count; i++)
                columnOf[structure.Modules[i].Id] = 0;

            for (int pass = 0; pass < structure.Modules.Count; pass++)
            {
                for (int linkIndex = 0; linkIndex < structure.Links.Count; linkIndex++)
                {
                    CircuitLinkSpec link = structure.Links[linkIndex];
                    if (CircuitStructure.IsSwitch(link.FromId) || !columnOf.ContainsKey(link.FromId) || !columnOf.ContainsKey(link.ToId))
                        continue;

                    int next = columnOf[link.FromId] + 1;
                    if (next > columnOf[link.ToId])
                        columnOf[link.ToId] = next;
                }
            }

            var perColumn = new Dictionary<int, int>();
            for (int i = 0; i < count && i < structure.Modules.Count; i++)
            {
                int column = columnOf[structure.Modules[i].Id];
                columns[i] = column;
                int seen;
                perColumn.TryGetValue(column, out seen);
                lanes[i] = seen;
                perColumn[column] = seen + 1;
            }

            var totals = new Dictionary<int, int>(perColumn);
            for (int i = 0; i < count; i++)
            {
                int total;
                if (!totals.TryGetValue(columns[i], out total) || total < 1)
                    total = 1;
                lanes[i] = lanes[i] - (total - 1) * 0.5f;
            }
        }

        public static Plan Arrange(
            Area board,
            Area table,
            Span gateUnit,
            Span led,
            Span switchSpan,
            float switchAX,
            float switchAZ,
            float switchBX,
            float switchBZ,
            float homeGateX,
            float homeGateZ,
            float homeLedX,
            float homeLedZ,
            float homeGateScale,
            int count,
            int[] columns,
            float[] lanes,
            float preferredScale,
            float minimumScale,
            float preferredGap,
            float minimumGap,
            float preferredLaneSpacing,
            float edgeMargin)
        {
            if (count < 1)
                count = 1;
            if (preferredScale < minimumScale)
                preferredScale = minimumScale;
            if (preferredGap < minimumGap)
                preferredGap = minimumGap;
            if (edgeMargin < 0f)
                edgeMargin = 0f;

            if (count <= 1)
            {
                return new Plan
                {
                    Fits = Inside(board, switchAX, switchAZ, switchSpan, 1f, edgeMargin)
                        && Inside(board, switchBX, switchBZ, switchSpan, 1f, edgeMargin)
                        && Inside(board, homeGateX, homeGateZ, gateUnit, homeGateScale, edgeMargin)
                        && Inside(board, homeLedX, homeLedZ, led, 1f, edgeMargin),
                    GateScale = homeGateScale,
                    Gap = preferredGap,
                    LaneSpacing = 0f,
                    Board = board,
                    GateX = new[] { homeGateX },
                    GateZ = new[] { homeGateZ },
                    LedX = homeLedX,
                    LedZ = homeLedZ,
                    SwitchAX = switchAX,
                    SwitchAZ = switchAZ,
                    SwitchBX = switchBX,
                    SwitchBZ = switchBZ
                };
            }

            Plan fitted = Search(board, board, gateUnit, led, switchSpan, switchAZ, switchBZ, homeGateZ, count, columns, lanes, preferredScale, minimumScale, preferredGap, minimumGap, preferredLaneSpacing, edgeMargin);
            if (fitted != null)
                return fitted;

            Plan grown = Search(board, table, gateUnit, led, switchSpan, switchAZ, switchBZ, homeGateZ, count, columns, lanes, preferredScale, minimumScale, preferredGap, minimumGap, preferredLaneSpacing, edgeMargin);
            if (grown != null)
                return grown;

            Plan fallback = Place(
                LimitToTable(board, table),
                gateUnit,
                led,
                switchSpan,
                switchAZ,
                switchBZ,
                homeGateZ,
                count,
                columns,
                lanes,
                minimumScale,
                minimumGap,
                preferredLaneSpacing,
                edgeMargin,
                true);
            fallback.Fits = false;
            return fallback;
        }

        public static bool Inside(Area board, float x, float z, Span span, float scale, float margin)
        {
            if (scale < 0f)
                scale = 0f;
            float left = x - span.Left * scale;
            float right = x + span.Right * scale;
            float near = z - span.Near * scale;
            float far = z + span.Far * scale;
            return left >= board.MinX + margin - 0.0001f
                && right <= board.MaxX - margin + 0.0001f
                && near >= board.MinZ + margin - 0.0001f
                && far <= board.MaxZ - margin + 0.0001f;
        }

        public static bool Separated(float ax, float az, Span a, float aScale, float bx, float bz, Span b, float bScale, float gap)
        {
            float aLeft = ax - a.Left * aScale;
            float aRight = ax + a.Right * aScale;
            float bLeft = bx - b.Left * bScale;
            float bRight = bx + b.Right * bScale;
            float aNear = az - a.Near * aScale;
            float aFar = az + a.Far * aScale;
            float bNear = bz - b.Near * bScale;
            float bFar = bz + b.Far * bScale;
            bool clearX = aRight + gap <= bLeft || bRight + gap <= aLeft;
            bool clearZ = aFar + gap <= bNear || bFar + gap <= aNear;
            return clearX || clearZ;
        }

        private static Plan Search(
            Area currentBoard,
            Area limit,
            Span gateUnit,
            Span led,
            Span switchSpan,
            float switchAZ,
            float switchBZ,
            float homeGateZ,
            int count,
            int[] columns,
            float[] lanes,
            float preferredScale,
            float minimumScale,
            float preferredGap,
            float minimumGap,
            float preferredLaneSpacing,
            float edgeMargin)
        {
            for (int scaleStep = 0; scaleStep <= 20; scaleStep++)
            {
                float scale = preferredScale - scaleStep * 0.01f;
                if (scale < minimumScale - 0.0001f)
                    break;

                for (int gapStep = 0; gapStep <= 12; gapStep++)
                {
                    float gap = preferredGap - gapStep * 0.005f;
                    if (gap < minimumGap - 0.0001f)
                        break;

                    Area board = BoardFor(currentBoard, limit, gateUnit, led, switchSpan, count, columns, lanes, scale, gap, preferredLaneSpacing, edgeMargin);
                    Plan plan = Place(board, gateUnit, led, switchSpan, switchAZ, switchBZ, homeGateZ, count, columns, lanes, scale, gap, preferredLaneSpacing, edgeMargin, false);
                    if (plan != null && plan.Fits)
                        return plan;
                }
            }

            return null;
        }

        private static Area BoardFor(
            Area currentBoard,
            Area limit,
            Span gateUnit,
            Span led,
            Span switchSpan,
            int count,
            int[] columns,
            float[] lanes,
            float scale,
            float gap,
            float preferredLaneSpacing,
            float edgeMargin)
        {
            int columnCount = ColumnCount(count, columns);
            float gateSpanX = (gateUnit.Left + gateUnit.Right) * scale;
            float chain = switchSpan.Left + switchSpan.Right + led.Left + led.Right + columnCount * gateSpanX + (columnCount + 1) * gap;
            float halfX = chain * 0.5f + edgeMargin;
            float currentHalfX = (currentBoard.MaxX - currentBoard.MinX) * 0.5f;
            if (halfX < currentHalfX)
                halfX = currentHalfX;

            float lane = LaneSpacing(currentBoard, gateUnit, count, lanes, scale, gap, preferredLaneSpacing, edgeMargin);
            float maxLane = MaxAbsLane(count, lanes);
            float gateHalfZ = Math.Max(gateUnit.Near, gateUnit.Far) * scale;
            float halfZ = maxLane * lane + gateHalfZ + edgeMargin;
            float currentHalfZ = (currentBoard.MaxZ - currentBoard.MinZ) * 0.5f;
            if (halfZ < currentHalfZ)
                halfZ = currentHalfZ;

            float centerX = (currentBoard.MinX + currentBoard.MaxX) * 0.5f;
            float centerZ = (currentBoard.MinZ + currentBoard.MaxZ) * 0.5f;
            float tableHalfX = Math.Min(Math.Abs(limit.MinX - centerX), Math.Abs(limit.MaxX - centerX));
            float tableHalfZ = Math.Min(Math.Abs(limit.MinZ - centerZ), Math.Abs(limit.MaxZ - centerZ));
            if (halfX > tableHalfX)
                halfX = tableHalfX;
            if (halfZ > tableHalfZ)
                halfZ = tableHalfZ;
            return Area.FromCenter(centerX, centerZ, halfX, halfZ);
        }

        private static Area LimitToTable(Area board, Area table)
        {
            return BoardFor(board, table, new Span(), new Span(), new Span(), 1, new[] { 0 }, new[] { 0f }, 0f, 0f, 0f, 0f);
        }

        private static Plan Place(
            Area board,
            Span gateUnit,
            Span led,
            Span switchSpan,
            float switchAZ,
            float switchBZ,
            float homeGateZ,
            int count,
            int[] columns,
            float[] lanes,
            float scale,
            float gap,
            float preferredLaneSpacing,
            float edgeMargin,
            bool force)
        {
            int columnCount = ColumnCount(count, columns);
            float gateSpanX = (gateUnit.Left + gateUnit.Right) * scale;
            float chain = switchSpan.Left + switchSpan.Right + led.Left + led.Right + columnCount * gateSpanX + (columnCount + 1) * gap;
            float inner = board.Width - edgeMargin * 2f;
            if (!force && chain > inner + 0.0001f)
                return null;

            float slack = inner - chain;
            if (slack < 0f)
                slack = 0f;
            float left = board.MinX + edgeMargin + slack * 0.5f;
            float switchX = left + switchSpan.Left;
            float cursor = switchX + switchSpan.Right + gap;
            var columnX = new float[columnCount];
            for (int column = 0; column < columnCount; column++)
            {
                columnX[column] = cursor + gateUnit.Left * scale;
                cursor = columnX[column] + gateUnit.Right * scale + gap;
            }

            float ledX = cursor + led.Left;
            float lane = LaneSpacing(board, gateUnit, count, lanes, scale, gap, preferredLaneSpacing, edgeMargin);
            var gateX = new float[count];
            var gateZ = new float[count];
            int lastColumn = columnCount - 1;
            float ledZSum = 0f;
            int ledZCount = 0;
            for (int i = 0; i < count; i++)
            {
                int column = columns != null && i < columns.Length ? columns[i] : i;
                if (column < 0)
                    column = 0;
                if (column >= columnCount)
                    column = columnCount - 1;
                float laneIndex = lanes != null && i < lanes.Length ? lanes[i] : 0f;
                gateX[i] = columnX[column];
                gateZ[i] = homeGateZ + laneIndex * lane;
                if (column == lastColumn)
                {
                    ledZSum += gateZ[i];
                    ledZCount++;
                }
            }

            float ledZ = ledZCount > 0 ? ledZSum / ledZCount : homeGateZ;
            bool fits = Inside(board, switchX, switchAZ, switchSpan, 1f, edgeMargin)
                && Inside(board, switchX, switchBZ, switchSpan, 1f, edgeMargin)
                && Inside(board, ledX, ledZ, led, 1f, edgeMargin);
            for (int i = 0; i < count && fits; i++)
                fits = Inside(board, gateX[i], gateZ[i], gateUnit, scale, edgeMargin);

            if (fits)
            {
                fits = Separated(switchX, switchAZ, switchSpan, 1f, switchX, switchBZ, switchSpan, 1f, 0.01f);
                for (int i = 0; i < count && fits; i++)
                {
                    fits = Separated(switchX, switchAZ, switchSpan, 1f, gateX[i], gateZ[i], gateUnit, scale, gap)
                        && Separated(switchX, switchBZ, switchSpan, 1f, gateX[i], gateZ[i], gateUnit, scale, gap)
                        && Separated(gateX[i], gateZ[i], gateUnit, scale, ledX, ledZ, led, 1f, gap);
                    for (int other = i + 1; other < count && fits; other++)
                        fits = Separated(gateX[i], gateZ[i], gateUnit, scale, gateX[other], gateZ[other], gateUnit, scale, gap);
                }
            }

            if (!fits && !force)
                return null;

            return new Plan
            {
                Fits = fits,
                GateScale = scale,
                Gap = gap,
                LaneSpacing = lane,
                Board = board,
                GateX = gateX,
                GateZ = gateZ,
                LedX = ledX,
                LedZ = ledZ,
                SwitchAX = switchX,
                SwitchAZ = switchAZ,
                SwitchBX = switchX,
                SwitchBZ = switchBZ
            };
        }

        private static float LaneSpacing(Area board, Span gateUnit, int count, float[] lanes, float scale, float gap, float preferred, float edgeMargin)
        {
            float gateDepth = (gateUnit.Near + gateUnit.Far) * scale;
            float minimum = gateDepth + gap;
            float spacing = preferred < minimum ? minimum : preferred;
            float maxLane = MaxAbsLane(count, lanes);
            if (maxLane <= 0.0001f)
                return 0f;

            float room = board.Depth * 0.5f - edgeMargin - Math.Max(gateUnit.Near, gateUnit.Far) * scale;
            float limit = room / maxLane;
            if (spacing > limit)
                spacing = limit;
            if (spacing < minimum)
                spacing = minimum;
            return spacing;
        }

        private static int ColumnCount(int count, int[] columns)
        {
            int columnCount = 1;
            if (columns == null)
                return count > 0 ? count : 1;

            for (int i = 0; i < count && i < columns.Length; i++)
            {
                if (columns[i] + 1 > columnCount)
                    columnCount = columns[i] + 1;
            }

            return columnCount;
        }

        private static float MaxAbsLane(int count, float[] lanes)
        {
            float max = 0f;
            if (lanes == null)
                return max;

            for (int i = 0; i < count && i < lanes.Length; i++)
            {
                float abs = lanes[i] < 0f ? -lanes[i] : lanes[i];
                if (abs > max)
                    max = abs;
            }

            return max;
        }

        private static bool UsesParallelLanes(CircuitStructure structure)
        {
            int roots = 0;
            for (int i = 0; i < structure.Modules.Count; i++)
            {
                bool fedByModule = false;
                for (int linkIndex = 0; linkIndex < structure.Links.Count; linkIndex++)
                {
                    CircuitLinkSpec link = structure.Links[linkIndex];
                    if (link.ToId == structure.Modules[i].Id && !CircuitStructure.IsSwitch(link.FromId))
                        fedByModule = true;
                }

                if (!fedByModule)
                    roots++;
            }

            return roots >= 2;
        }
    }
}
