local require = require
require("Framework.RequireBase")

local Time = Time

GameModule = GameModule or {}
local GameModule = GameModule
function GameModule.Start()
	print("GameModule Start")

	LuaMemSystem:GetInstance()
	require("Framework.RequireCore")
	require("Framework.RequireUtil")
	require("Framework.RequireMisc")
	require("Framework.RequireManager")
	require("Framework.RequireUI")

	LuaLogManager:GetInstance():SetLogEnable(true)
	SystemMemoryLevel.Init()

	GlobalTimerQuest = TimerQuest.New()
	GlobalEventSystem = EventSystem.New()
	GameModule.runner = Runner:GetInstance()
	lua_resM = LuaResManager:GetInstance()
    lua_viewM = LuaViewManager:GetInstance()
	LuaMemSystem:GetInstance():InitInfo()

	UpdateBeat:Add(GameModule.Update)
	LateUpdateBeat:Add(GameModule.LateUpdate)
	GameModule.InitGame()
end

function GameModule.InitGame()
	print("GameModule InitGame")

	require("Framework.RequireGame")
	TerrainManager:GetInstance()
	RequireGame.Init()

	LoginController.Instance:OpenView()
end

function GameModule.Update()
	GameModule.runner:Update(Time.time, Time.deltaTime)
end

function GameModule.LateUpdate()
	GameModule.runner:LateUpdate(Time.time, Time.deltaTime)
end