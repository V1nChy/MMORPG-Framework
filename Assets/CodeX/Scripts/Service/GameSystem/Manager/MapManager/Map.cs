using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CodeX
{
    public struct AreaInfo
    {
        public uint id;

        public uint type;

        public byte[,] data;
    }

    public class Map
    {
        private static uint logicX = 100u;
        private static uint logicY = 100u;
        private char version;
        private ushort resId;
        private ushort mapHeight;
        private ushort mapWidth;
        private uint gridRow;
        private uint gridCollumn;
        private char[][] Grid;
        private uint tileDataSize;
        private uint maskDataSize;

        private AStarFindPath findWayObj;
        private Dictionary<uint, AreaInfo> areaData = new Dictionary<uint, AreaInfo>();

        public Map(uint LogicWidth, uint LogicHeight)
        {
            logicX = LogicWidth;
            logicY = LogicHeight;
        }

        public void LoadData(TextAsset txt)
        {
            this.LoadData(txt.bytes);
        }

        public void LoadData(byte[] buff)
        {
            MemoryStream ms = new MemoryStream(buff);
            BinaryReader br = new BinaryReader(ms);
            this.version = br.ReadChar();
            this.resId = Convert.ToUInt16(br.ReadUInt16());
            this.mapWidth = Convert.ToUInt16(br.ReadUInt16());
            this.mapHeight = Convert.ToUInt16(br.ReadUInt16());
            this.gridCollumn = ((uint)this.mapWidth + logicX - 1u) / logicX;
            this.gridRow = ((uint)this.mapHeight + logicY - 1u) / logicY;
            this.findWayObj = new AStarFindPath(this);
            this.findWayObj.Init(ref br, (int)this.gridCollumn, (int)this.gridRow);
            if (Convert.ToUInt32(this.version.ToString()) >= 2u)
            {
                uint area_count = br.ReadUInt32();
                for (uint i = 0u; i < area_count; i += 1u)
                {
                    AreaInfo area = default(AreaInfo);
                    area.id = br.ReadUInt32();
                    area.type = br.ReadUInt32();
                    area.data = new byte[(int)this.gridCollumn, (int)this.gridRow];
                    uint pos_count = br.ReadUInt32();
                    for (uint j = 0u; j < pos_count; j += 1u)
                    {
                        uint x = br.ReadUInt32();
                        uint y = br.ReadUInt32();
                        area.data[(int)x, (int)y] = 1;
                    }
                    this.areaData[area.id] = area;
                }
            }
            br.Close();
            ms.Close();
        }

        /// <summary>
        /// 设置动态区域的类型
        /// </summary>
		public void SetDynamicArea(uint area_id, uint area_type)
        {
            AreaInfo area;
            if (areaData.TryGetValue(area_id, out area))
            {
                area.type = area_type;
                this.areaData[area_id] = area;
            }
        }

        public Vector2[] FindWay(Vector2 start, Vector2 end, float range)
        {
            Vector2[] findWayVec = new Vector2[1];
            Point start_point = new Point((int)start.x, (int)start.y);
            Point end_point = new Point((int)end.x, (int)end.y);
            Vector2[] result;
            if (start_point.x == end_point.x && start_point.y == end_point.y)
            {
                result = new Vector2[]
                {
                    new Vector2((float)start_point.x, (float)start_point.y),
                    new Vector2((float)end_point.x, (float)end_point.y)
                };
            }
            else
            {
                ArrayList inflex_vec = new ArrayList();
                if (this.FindWay(start_point, end_point, ref inflex_vec, range))
                {
                    findWayVec = new Vector2[inflex_vec.Count];
                    for (int i = 0; i < inflex_vec.Count; i++)
                    {
                        findWayVec[i] = (Vector2)inflex_vec[i];
                    }
                    result = findWayVec;
                }
                else
                {
                    result = findWayVec;
                }
            }
            return result;
        }

        public bool FindWay(Point start, Point end, ref ArrayList inflex_vec, float range)
        {
            bool find_way_rlt = findWayObj.FindWay(start, end);
            if (findWayObj.FindWay(start, end))
            {
                findWayObj.GenerateInflexPoint(ref inflex_vec, range);
            }
            return find_way_rlt;
        }

        public bool IsStraightLine(Vector2 start_point, Vector2 end_point)
        {
            Vector2 end_point_tmp = default(Vector2);
            Vector2 start_pos = new Vector2(start_point.x, start_point.y);
            Vector2 end_pos = new Vector2(end_point.x, end_point.y);
            Vector2 se_dir = end_pos - start_pos;
            float dis = this.findWayObj.normalise(ref se_dir);
            if (this.DetectLastUnblockPoint(start_point, se_dir, dis, ref end_point_tmp, false))
            {
                if ((int)end_point_tmp.x == (int)end_point.x && (int)end_point_tmp.y == (int)end_point.y)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HandleStraightLine(Vector2 start_point, Vector2 dir, float max_distance, ref Vector2 last_valid_point)
        {
            float cur_dis = 0f;
            Vector2 dir_normal = dir;
            this.findWayObj.normalise(ref dir_normal);
            Vector2 last_step = new Vector2(start_point.x, start_point.y);
            Vector2 next_step = new Vector2(start_point.x, start_point.y);
            while (cur_dis < max_distance)
            {
                last_step = next_step;
                cur_dis += 0.1f;
                if (cur_dis >= max_distance)
                {
                    cur_dis = max_distance;
                }
                next_step = dir_normal * cur_dis;
                next_step = start_point + next_step;
                if (!this.IsBlock(next_step))
                {
                    int x_d = (int)last_step.x - (int)next_step.x;
                    int y_d = (int)last_step.y - (int)next_step.y;
                    if (x_d < 0)
                    {
                        x_d = -x_d;
                    }
                    if (y_d < 0)
                    {
                        y_d = -y_d;
                    }
                    if (x_d + y_d == 2)
                    {
                        Vector2 p = new Vector2(last_step.x, next_step.y);
                        Vector2 p2 = new Vector2(next_step.x, last_step.y);
                        if (this.IsBlock(p) || this.IsBlock(p2))
                        {
                            last_valid_point.x = (float)((int)last_step.x);
                            last_valid_point.y = (float)((int)last_step.y);
                            return false;
                        }
                    }
                    continue;
                }
                last_valid_point.x = last_step.x;
                last_valid_point.y = last_step.y;
                return false;
            }
            last_valid_point.x = (float)((int)last_step.x);
            last_valid_point.y = (float)((int)last_step.y);
            return true;
        }

        public Vector3 DetectLastUnblockPoint(Vector2 start_point, Vector2 dir, float max_distance, bool jump_state)
        {
            Vector2 last_valid_point = new Vector2(0, 0);
            bool ret = this.DetectLastUnblockPoint(start_point, dir, max_distance, ref last_valid_point, jump_state);
            if (ret && !jump_state)
            {
                if (this.IsBlock(last_valid_point))
                {
                    last_valid_point.x = 0f;
                    last_valid_point.y = 0f;
                    ret = this.DetectLastUnblockPoint(start_point, dir, max_distance, ref last_valid_point, false);
                }
            }
            int real_ret = 0;
            if (ret)
            {
                real_ret = 1;
            }
            Vector3 result = new Vector3(last_valid_point.x, last_valid_point.y, (float)real_ret);
            return result;
        }

        public bool DetectLastUnblockPoint(Vector2 start_point, Vector2 dir, float max_distance, ref Vector2 last_valid_point, bool jump_state)
        {
            float cur_dis = 0f;
            Vector2 dir_normal = dir;
            this.findWayObj.normalise(ref dir_normal);
            Vector2 last_pt = start_point;
            Vector2 now_pt = start_point;
            bool result;
            if (dir_normal.x == 0f && dir_normal.y == 0f)
            {
                last_valid_point = last_pt;
                result = !this.IsBlock(last_pt, jump_state);
            }
            else
            {
                while (cur_dis < max_distance)
                {
                    float step_x = 999.9f;
                    float step_y = 999.9f;
                    if (dir_normal.x != 0f)
                    {
                        float dx;
                        if (dir_normal.x > 0f)
                        {
                            dx = 0.95f + (float)((int)now_pt.x) - now_pt.x;
                        }
                        else
                        {
                            dx = now_pt.x - (float)((int)now_pt.x) - 0.05f;
                        }
                        step_x = dx / dir_normal.x;
                    }
                    if (dir_normal.y != 0f)
                    {
                        float dy;
                        if (dir_normal.y > 0f)
                        {
                            dy = 0.95f + (float)((int)now_pt.y) - now_pt.y;
                        }
                        else
                        {
                            dy = now_pt.y - (float)((int)now_pt.y) - 0.05f;
                        }
                        step_y = dy / dir_normal.y;
                    }
                    float step;
                    if (step_x < 0.001f || step_y < 0.001f)
                    {
                        step = 0.08f;
                    }
                    else
                    {
                        step = Math.Min(step_x, step_y);
                    }
                    cur_dis += step;
                    if (cur_dis > max_distance)
                    {
                        cur_dis = max_distance;
                    }
                    now_pt = start_point + dir_normal * cur_dis;
                    if (this.IsBlock(now_pt, jump_state))
                    {
                        break;
                    }
                    bool is_slash = (int)last_pt.x != (int)now_pt.x && (int)last_pt.y != (int)now_pt.y;
                    if (is_slash)
                    {
                        Vector2 p = new Vector2(last_pt.x, now_pt.y);
                        Vector2 p2 = new Vector2(now_pt.x, last_pt.y);
                        if (this.IsBlock(p, jump_state) || this.IsBlock(p2, jump_state))
                        {
                            break;
                        }
                    }
                    last_pt = now_pt;
                }
                last_valid_point = last_pt;
                result = true;
            }
            return result;
        }

        public Vector3 DetectFirstUnblockPoint(Vector2 start_point, Vector2 dir, float max_distance, bool jump_state)
        {
            float cur_dis = 0f;
            Vector2 dir_normal = dir;
            this.findWayObj.normalise(ref dir_normal);
            Vector2 now_pt = start_point;
            Vector2 valid_point = default(Vector2);
            if (dir_normal.x == 0f && dir_normal.y == 0f)
            {
                valid_point = now_pt;
                int ret = 0;
                if (!this.IsBlock(now_pt))
                {
                    ret = 1;
                }
                return new Vector3(valid_point.x, valid_point.y, (float)ret);
            }
            else
            {
                for (; ; )
                {
                    if (!this.IsBlock(now_pt))
                    {
                        break;
                    }
                    float step_x = 999.9f;
                    float step_y = 999.9f;
                    if (dir_normal.x != 0f)
                    {
                        float dx;
                        if (dir_normal.x > 0f)
                        {
                            dx = 0.95f + (float)((int)now_pt.x) - now_pt.x;
                        }
                        else
                        {
                            dx = now_pt.x - (float)((int)now_pt.x) - 0.05f;
                        }
                        step_x = dx / dir_normal.x;
                    }
                    if (dir_normal.y != 0f)
                    {
                        float dy;
                        if (dir_normal.y > 0f)
                        {
                            dy = 0.95f + (float)((int)now_pt.y) - now_pt.y;
                        }
                        else
                        {
                            dy = now_pt.y - (float)((int)now_pt.y) - 0.05f;
                        }
                        step_y = dy / dir_normal.y;
                    }
                    float step;
                    if (step_x < 0.001f || step_y < 0.001f)
                    {
                        step = 0.08f;
                    }
                    else
                    {
                        step = Math.Min(step_x, step_y);
                    }
                    cur_dis += step;
                    if (cur_dis >= max_distance)
                    {
                        if (!this.IsBlock(start_point))
                        {
                            valid_point = now_pt;
                            return new Vector3(valid_point.x, valid_point.y, 1f);
                        }
                        else
                        {
                            return new Vector3(valid_point.x, valid_point.y, 0f);
                        }
                    }
                    now_pt = start_point + dir_normal * cur_dis;
                }
                valid_point = now_pt;
                return new Vector3(valid_point.x, valid_point.y, 1f);
            }
        }

        public Vector2 findStraightEnd(Vector2 player_logic_pos, Vector2 dir, float max_distance)
        {
            Vector2 result;
            if (dir.x == 0f && dir.y == 0f)
            {
                result = player_logic_pos;
            }
            else
            {
                Vector2 end_point = default(Vector2);
                this.DetectLastUnblockPoint(player_logic_pos, dir, max_distance, ref end_point, false);
                result = end_point;
            }
            return result;
        }

        public int GetAreaType(int x, int y)
        {
            char result = this.findWayObj.GetZoneInfo(x, y);
            if (((int)result & AStarFindPath.ZONE_TYPE_BLOCK) == 0)
            {
                foreach (KeyValuePair<uint, AreaInfo> kvp in this.areaData)
                {
                    int row = kvp.Value.data.GetLength(0);
                    int col = kvp.Value.data.GetLength(1);
                    bool flag2 = x < row && y < col && kvp.Value.data[x, y] == 1;
                    if (flag2)
                    {
                        return (int)kvp.Value.type;
                    }
                }
            }
            return (int)result;
        }

        public ushort GetZoneHeight(int logic_pos_x, int logic_pos_y)
        {
            return this.findWayObj.GetZoneHeight(logic_pos_x, logic_pos_y);
        }

        public bool IsBlock(Vector2 pos, bool jump_state)
        {
            char result = this.findWayObj.GetZoneInfo((int)pos.x, (int)pos.y);
            if (((int)result & AStarFindPath.ZONE_TYPE_BLOCK) != 0)
            {
                return true;
            }
            return this.DynamicBlock((int)pos.x, (int)pos.y);
        }

        public bool IsBlock(int x, int y, bool jump_state)
        {
            return this.IsBlock(new Vector2((float)x, (float)y), jump_state);
        }

        public bool IsBlock(Vector2 pos)
        {
            return this.IsBlock(pos, false);
        }

        public bool DynamicBlock(int x, int y)
        {
            foreach (KeyValuePair<uint, AreaInfo> kvp in this.areaData)
            {
                if (kvp.Value.type == (uint)AStarFindPath.ZONE_TYPE_BLOCK)
                {
                    int row = kvp.Value.data.GetLength(0);
                    int col = kvp.Value.data.GetLength(1);
                    if (x < row && y < col && kvp.Value.data[x, y] == 1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
