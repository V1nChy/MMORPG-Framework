using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GFW.ManagerSystem;

namespace CodeX
{
    public class MapManager : Manager
    {
        private const uint LogicWidth = 100u;
        private const uint LogicHeight = 100u;

        private Dictionary<uint, Map> mapCacher = new Dictionary<uint, Map>();
        public bool MapLoaded = false;
        public uint currentMapId;

        public void LoadMap(uint mapId, TextAsset txt)
        {
            this.currentMapId = mapId;
            this.LoadOtherMap(mapId, txt);
        }

        public void LoadOtherMap(uint mapId, TextAsset txt)
        {
            if (!this.mapCacher.ContainsKey(mapId))
            {
                Map map = new Map(LogicWidth, LogicHeight);
                map.LoadData(txt);
                this.mapCacher.Add(mapId, map);
            }
        }

        public Map CurMap()
        {
            return this.mapCacher[this.currentMapId];
        }

        public Map MapByScene(uint sceneId)
        {
            return this.mapCacher[sceneId];
        }

        public void ClearMap(uint mapId)
        {
            if (this.mapCacher.ContainsKey(mapId))
            {
                this.mapCacher.Remove(mapId);
            }
        }
    }
}
