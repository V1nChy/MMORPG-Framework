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
    public interface IHeapItem<T> : IComparable<T>
    {
        int HeapIndex { get; set; }
    }

    public class Heap<T> where T : IHeapItem<T>
    {
        protected T[] m_data;

        protected int m_size;

        protected int m_max_size;

        public Heap()
        {
            this.m_size = 0;
            this.m_max_size = 0;
            this.Resize(128);
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// 放入元素，并以二分法筛选出最小值置顶
        /// </summary>
        public void Push(T val)
        {
            if (this.m_size == this.m_max_size)
            {
                this.Resize(this.m_max_size * 2);
            }
            int s = this.m_size;
            this.m_size++;
            while (s > 0)
            {
                int f = (s - 1) / 2;
                if (val.CompareTo(this.m_data[f]) >= 0)
                {
                    break;
                }
                this.m_data[s] = this.m_data[f];
                s = f;
            }
            this.m_data[s] = val;
        }

        /// <summary>
        /// 移除栈顶元素，并以二分法排序
        /// </summary>
        public void PopFront()
        {
            if (this.m_size != 0)
            {
                this.m_size--;
                if (this.m_size != 0)
                {
                    T tmp = this.m_data[this.m_size];
                    int f = 0;
                    int half_size = this.m_size / 2;
                    while (f < half_size)
                    {
                        int s = f * 2 + 1;
                        if (s + 1 < this.m_size && this.m_data[s + 1].CompareTo(this.m_data[s]) < 0)
                        {
                            s++;
                        }
                        if (this.m_data[s].CompareTo(tmp) >= 0)
                        {
                            break;
                        }
                        this.m_data[f] = this.m_data[s];
                        f = s;
                    }
                    this.m_data[f] = tmp;
                }
            }
        }

        public T Front()
        {
            return this.m_data[0];
        }

        public bool Front(ref T v)
        {
            if (this.m_size != 0)
            {
                v = this.m_data[0];
                return true;
            }
            return false;
        }

        public int Size()
        {
            return this.m_size;
        }

        public void Clear()
        {
            this.m_size = 0;
        }

        protected void Resize(int size)
        {
            if (size > this.m_max_size)
            {
                T[] new_data = new T[size];
                if (this.m_data != null)
                {
                    Array.Copy(this.m_data, new_data, this.m_data.Length);
                }
                this.m_data = new_data;
                this.m_max_size = size;
            }
        }
    }

    public class Point
    {
        public int x;
        public int y;

        public Point(Point v)
        {
            this.x = v.x;
            this.y = v.y;
        }

        public Point()
        {
            this.x = 0;
            this.y = 0;
        }

        public Point(int _x, int _y)
        {
            this.x = _x;
            this.y = _y;
        }
    }

    public class AStarFindPath
    {
        private class OpenItem : IHeapItem<OpenItem>
        {
            public int x;
            public int y;
            public int f;
            private int heapIndex;
            public int HeapIndex
            {
                get
                {
                    return this.heapIndex;
                }
                set
                {
                    this.heapIndex = value;
                }
            }

            public OpenItem()
            {
                this.x = 0;
                this.y = 0;
                this.f = 0;
            }

            public OpenItem(int _x, int _y, int _f)
            {
                this.x = _x;
                this.y = _y;
                this.f = _f;
            }

            public int CompareTo(AStarFindPath.OpenItem other)
            {
                if (this.f < other.f)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }

        private class PointInfo
        {
            public int x = 0;
            public int y = 0;
            public bool close;
            /// <summary>
            /// 当前点与起始点的代价
            /// </summary>
            public int g;
            /// <summary>
            /// 当前点与目标点的代价
            /// </summary>
            public int h;
            public PointInfo parant;
            public int dir;

            public void Reset()
            {
                this.x = 0;
                this.y = 0;
                this.close = false;
                this.g = 0;
                this.h = 0;
                this.dir = 0;
                this.parant = null;
            }
        }

        private int[,] POINT_DIR = new int[,]
        {
            {1,2,3},
            {4,0,6},
            {7,8,9}
        };

        public const int ZONE_TYPE_NONE = 0;
        public const int ZONE_TYPE_BLOCK = 1;
        public const int ZONE_TYPE_WAY = 2;
        public const int ZONE_TYPE_SAFE = 4;
        public const int ZONE_TYPE_SHIELD = 8;

        private Map m_map_obj = null;
        private int m_mask_width;
        private int m_mask_height;
        private char[][] m_v;//格子标识
        private ushort[][] m_h;//格子高度
        private AStarFindPath.PointInfo[][] m_map;
        private Heap<OpenItem> m_open_list = new Heap<OpenItem>();//开启列表

        private List<Point> m_path = new List<Point>();
        private Point m_start_point = new Point();
        private Point m_end_point = new Point();

        private bool m_is_init;
        private bool m_is_last_straight;

        public AStarFindPath(Map map_obj)
        {
            this.m_mask_width = 0;
            this.m_mask_height = 0;
            this.m_map = null;
            this.m_v = null;
            this.m_is_init = false;
            this.m_is_last_straight = false;
            this.m_map_obj = map_obj;
        }

        public void Dispose()
        {
            this.m_map_obj = null;
            this.Clear();
        }

        public void Clear()
        {
            this.m_mask_width = 0;
            this.m_mask_height = 0;
        }

        public bool Init(ref BinaryReader br, int mask_width, int mask_height)
        {
            this.Clear();
            this.m_mask_width = mask_width;
            this.m_mask_height = mask_height;
            this.m_v = new char[mask_width][];
            this.m_h = new ushort[mask_width][];
            this.m_map = new AStarFindPath.PointInfo[mask_width][];
            for (int i = 0; i < mask_width; i++)
            {
                this.m_v[i] = new char[mask_height];
                this.m_map[i] = new AStarFindPath.PointInfo[mask_height];
                this.m_h[i] = new ushort[mask_height];
            }
            for (int x = 0; x < mask_width; x++)
            {
                for (int y = 0; y < mask_height; y++)
                {
                    this.m_v[x][y] = br.ReadChar();
                    this.m_h[x][y] = Convert.ToUInt16(br.ReadUInt16());
                    this.m_map[x][y] = new AStarFindPath.PointInfo();
                }
            }
            this.m_is_init = true;
            return true;
        }

        public bool FindWay(Point start_point, Point end_point)
        {
            if (m_is_init)
            {
                this.m_is_last_straight = false;
                if (!(start_point.x < 0 || start_point.x >= this.m_mask_width || start_point.y < 0 || start_point.y >= this.m_mask_height)
                    && !(end_point.x < 0 || end_point.x >= this.m_mask_width || end_point.y < 0 || end_point.y >= this.m_mask_height))
                {
                    this.m_path.Clear();
                    this.Reset();
                    this.m_start_point = start_point;
                    this.m_end_point = end_point;
                    if (this.NoWalk(start_point.x, start_point.y))
                    {
                        this.m_end_point = start_point;
                        return false;
                    }
                    else
                    {
                        return RealFindWay(start_point, end_point);
                    }

                }
            }
            return false;
        }
        private bool RealFindWay(Point start_point, Point end_point)
        {
            Point cur_point = new Point(start_point);
            while(true)
            {
                //当前点设为闭点
                this.m_map[cur_point.x][cur_point.y].close = true;
                this.m_map[cur_point.x][cur_point.y].x = cur_point.x;
                this.m_map[cur_point.x][cur_point.y].y = cur_point.y;

                //设置搜索范围
                #region
                int x_begin = cur_point.x - 1;
                if (x_begin < 0)
                {
                    x_begin = 0;
                }

                int x_end = cur_point.x + 1;
                if (x_end >= this.m_mask_width)
                {
                    x_end = this.m_mask_width - 1;
                }

                int y_begin = cur_point.y - 1;
                if (y_begin < 0)
                {
                    y_begin = 0;
                }
                int y_end = cur_point.y + 1;
                if (y_end >= this.m_mask_height)
                {
                    y_end = this.m_mask_height - 1;
                }
                #endregion

                //遍历四周8个点
                for (int i = x_begin; i <= x_end; i++)
                {
                    for (int j = y_begin; j <= y_end; j++)
                    {
                        if (!this.m_map[i][j].close && !this.NoWalk(i, j))
                        {
                            if (i == end_point.x && j == end_point.y)
                            {
                                bool slash_blocked = false;//斜角是否不可达
                                //斜角处理
                                if (i != cur_point.x && j != cur_point.y && (this.NoWalk(i, cur_point.y) || this.NoWalk(cur_point.x, j)))
                                {
                                    slash_blocked = true;
                                }
                                if (!slash_blocked)
                                {
                                    this.m_map[i][j].parant = this.m_map[cur_point.x][cur_point.y];
                                    this.m_map[i][j].x = i;
                                    this.m_map[i][j].y = j;
                                    return true;
                                }
                            }
                            else
                            {
                                int h = i - cur_point.x;
                                int c = j - cur_point.y;
                                int next_dir = this.POINT_DIR[h + 1, c + 1];
                                bool is_slash = h * c != 0;
                                this.CalcWeight(cur_point.x, cur_point.y, i, j, is_slash, next_dir);
                            }
                        }
                    }
                }

                //在开集合中寻找最小F的有效点
                bool cant_find = true;
                AStarFindPath.OpenItem next_open = new AStarFindPath.OpenItem();
                while (this.m_open_list.Front(ref next_open))
                {
                    this.m_open_list.PopFront();
                    if (!this.m_map[next_open.x][next_open.y].close)
                    {
                        cur_point.x = next_open.x;
                        cur_point.y = next_open.y;
                        cant_find = false;
                        break;
                    }
                }
                if (cant_find)
                {
                    this.m_end_point = cur_point;
                    return false;
                }
            }
        }

        /// <summary>
        /// F=G+H;
        /// </summary>
        private void CalcWeight(int cur_x, int cur_y, int next_x, int next_y, bool is_slash, int next_dir)
        {
            //检查斜线可达
            if (is_slash)
            {
                if (this.NoWalk(next_x, cur_y) || this.NoWalk(cur_x, next_y))
                {
                    return;
                }
            }
            AStarFindPath.PointInfo next_p = this.m_map[next_x][next_y];
            AStarFindPath.PointInfo cur_p = this.m_map[cur_x][cur_y];

            //计算G
            int g = cur_p.g + (is_slash ? 14142 : 10000);
            if (cur_p.dir != next_dir)
            {
                g += 8200;
            }

            //计算H
            if (next_p.g == 0 || next_p.g > g)
            {
                next_p.g = g;
                next_p.parant = cur_p;
                next_p.dir = next_dir;
                if (next_p.h == 0)
                {
                    next_p.h = 10000 * this.CalH(next_x, next_y);
                }
                int f = next_p.h + next_p.g;
                this.m_open_list.Push(new AStarFindPath.OpenItem(next_x, next_y, f));
            }
        }
        //f(n) = g(n) + h(n)
        private int CalH(int pos_x, int pos_y)
        {
            int x_dis = pos_x - this.m_end_point.x;
            int y_dis = pos_y - this.m_end_point.y;
            if (x_dis < 0)
            {
                x_dis = -x_dis;
            }
            if (y_dis < 0)
            {
                y_dis = -y_dis;
            }
            return x_dis + y_dis;
        }
        private bool NoWalk(int x, int y)
        {
            if (((int)this.m_v[x][y] & ZONE_TYPE_BLOCK) != 0)
            {
                return true;
            }
            else
            {
                return this.m_map_obj != null && this.m_map_obj.DynamicBlock(x, y);
            }
        }

        public char GetZoneInfo(int logic_pos_x, int logic_pos_y)
        {
            if (logic_pos_x < 0 || logic_pos_x >= this.m_mask_width || logic_pos_y < 0 || logic_pos_y >= this.m_mask_height)
            {
                return (char)ZONE_TYPE_BLOCK;
            }
            else
            {
                if (this.m_is_init)
                {
                    return this.m_v[logic_pos_x][logic_pos_y];
                }
                else
                {
                    return (char)ZONE_TYPE_BLOCK;
                }
            }
        }

        public ushort GetZoneHeight(int logic_pos_x, int logic_pos_y)
        {
            if (logic_pos_x < 0 || logic_pos_x >= this.m_mask_width || logic_pos_y < 0 || logic_pos_y >= this.m_mask_height)
            {
                return 0;
            }
            else
            {
                if (this.m_is_init)
                {
                    return this.m_h[logic_pos_x][logic_pos_y];
                }
                else
                {
                    return 0;
                }
            }
        }

        public void GenerateInflexPoint(ref ArrayList inflex_points)
        {
            this.GenerateInflexPoint(ref inflex_points, 0f);
        }

        /// <summary>
        /// 生成路径
        /// </summary>
        /// <param name="inflex_points">路径数组</param>
        /// <param name="range">与终点的距离</param>
        public void GenerateInflexPoint(ref ArrayList inflex_points, float range)
        {
            AStarFindPath.PointInfo cur_p = this.m_map[this.m_end_point.x][this.m_end_point.y];
            if (cur_p == null)
            {
                inflex_points.Add(new Vector2((float)this.m_start_point.x, (float)this.m_start_point.y));
                inflex_points.Add(new Vector2((float)this.m_start_point.x, (float)this.m_start_point.y));
            }
            else
            {
                if (range > 0.001f)
                {
                    Vector2 start_pos = new Vector2((float)this.m_start_point.x, (float)this.m_start_point.y);
                    Vector2 end_pos = new Vector2((float)this.m_end_point.x, (float)this.m_end_point.y);
                    Vector2 dir = end_pos - start_pos;
                    if (dir.sqrMagnitude <= range * range)
                    {
                        inflex_points.Add(new Vector2((float)this.m_start_point.x, (float)this.m_start_point.y));
                        inflex_points.Add(new Vector2((float)this.m_start_point.x, (float)this.m_start_point.y));
                        return;
                    }
                    if (this.m_is_last_straight)
                    {
                        this.normalise(ref dir);
                        float real_range = range;
                        Vector2 cur_end_pos = default(Vector2);
                        cur_end_pos.x = end_pos.x;
                        cur_end_pos.y = end_pos.y;
                        for (; ; )
                        {
                            dir *= real_range;
                            cur_end_pos = end_pos - dir;
                            cur_end_pos.x = (float)((int)cur_end_pos.x);
                            cur_end_pos.y = (float)((int)cur_end_pos.y);
                            if (Vector2.Distance(cur_end_pos, end_pos) <= range * range)
                            {
                                break;
                            }
                            real_range -= 0.3f;
                            if (real_range < 0f)
                            {
                                cur_end_pos = end_pos;
                                inflex_points.Add(new Vector2(cur_end_pos.x, cur_end_pos.y));
                                inflex_points.Add(new Vector2((float)this.m_start_point.x, (float)this.m_start_point.y));
                                return;
                            }
                        }
                        inflex_points.Add(new Vector2(cur_end_pos.x, cur_end_pos.y));
                        inflex_points.Add(new Vector2((float)this.m_start_point.x, (float)this.m_start_point.y));
                        return;
                    }

                    AStarFindPath.PointInfo last_p = cur_p;
                    while (cur_p != null)
                    {
                        float tmp_x = (float)cur_p.x + 0.5f - (float)this.m_end_point.x;
                        float tmp_y = (float)cur_p.y + 0.5f - (float)this.m_end_point.y;
                        if (range * range < tmp_x * tmp_x + tmp_y * tmp_y)
                        {
                            break;
                        }
                        last_p = cur_p;
                        cur_p = cur_p.parant;
                    }
                    cur_p = last_p;
                }
                int cur_dir = -1;
                while (cur_p != null)
                {
                    if (cur_p.dir != cur_dir || (cur_p.x == this.m_start_point.x && cur_p.y == this.m_start_point.y))
                    {
                        inflex_points.Add(new Vector2((float)cur_p.x + 0.5f, (float)cur_p.y + 0.5f));
                        cur_dir = cur_p.dir;
                    }
                    cur_p = cur_p.parant;
                }
            }
        }

        public float normalise(ref Vector2 pos)
        {
            float fLength = pos.magnitude;
            if (fLength > 0f)
            {
                float fInvLength = 1f / fLength;
                pos.x *= fInvLength;
                pos.y *= fInvLength;
            }
            return fLength;
        }

        private void Reset()
        {
            for (int i = 0; i < this.m_mask_width; i++)
            {
                for (int j = 0; j < this.m_mask_height; j++)
                {
                    this.m_map[i][j].Reset();
                }
            }
            this.m_open_list.Clear();
        }
    }
}
