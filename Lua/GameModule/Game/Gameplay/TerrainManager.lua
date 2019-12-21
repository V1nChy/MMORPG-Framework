TerrainManager = TerrainManager or BaseClass(nil,true)
local TerrainManager = TerrainManager

function TerrainManager:__init()
	
end

function TerrainManager:LoadTerrain()

	local function onLoadSceneABCallback(objs)
		local async = SceneManager.LoadSceneAsync("1000")
		UIManager.ShowProgressView(async, function ()
			print("scene load finish")
			MainUIController.Instance:OpenView()
		end)
	end
	ResourceManager:LoadPrefab("terrain_scene_1000",{},onLoadSceneABCallback)
end