--[[@------------------------------------------------------------------
说明: lua端的单例日志输出管理器
作者: VinChy
----------------------------------------------------------------------]]

LuaLogManager = LuaLogManager or BaseClass(nil,true)

LuaLogManager.EnableRoleLog = false
function LuaLogManager:__init()
	local old_print_func = _G.print
	_G.print = function (...)
		if RuntimePlatform and (ApplicationPlatform == RuntimePlatform.Android or ApplicationPlatform == RuntimePlatform.IPhonePlayer) then
			return
		end
    	self:Log(...)
	end
end

function LuaLogManager:SetLogEnable(value)
	if LogManager then
		LogManager.EnableLog = value
	end
end

function LuaLogManager:PackageContent(...)
	local arg = {...}
	local printResult = ""
    for i,v in pairs(arg) do
       printResult = printResult..tostring(v).."\t"
    end
    printResult = self:GetCallFunction()..printResult
    return printResult
end

function LuaLogManager:Log( ... )
    if LogManager then
		if RuntimePlatform and (ApplicationPlatform == RuntimePlatform.Android or ApplicationPlatform == RuntimePlatform.IPhonePlayer) then
			return
		end
		local printResult = self:PackageContent(...)
		LogManager.Log(printResult)

		self:MainRoleLog(printResult)
    end
end

--警告日志--
function LuaLogManager:LogWarn( ... ) 
	if LogManager then
		if RuntimePlatform and (ApplicationPlatform == RuntimePlatform.Android or ApplicationPlatform == RuntimePlatform.IPhonePlayer) then
			return
		end
		local printResult = self:PackageContent(...)
    	LogManager.LogWarning(printResult)

    	self:MainRoleLog(printResult)
    end
end

--错误日志--
function LuaLogManager:LogError( ... ) 
	if LogManager then
		local printResult = self:PackageContent(...)
    	LogManager.LogError(printResult)
    end
end

function LuaLogManager:GetCallFunction()
	local info = debug.getinfo(5, "Sln")
	local str = ""
	if not info then
		str ""
	end
	if info.what == "C" then
		str = str.."C function"
	else
		str = str..string.format("[%s:%d]::%s() ",info.short_src, info.currentline, info.name)
	end
	return str
end

function LuaLogManager:MainRoleLog(content)
	if not LuaLogManager.EnableRoleLog then
		return
	end

	if RoleManager and RoleManager.Instance and RoleManager.Instance.mainRoleInfo and RoleManager.Instance.mainRoleInfo.role_id > 1 then
		local fileName = AppConst.AppDataPath.."/StreamingAssets/log/"..RoleManager.Instance.mainRoleInfo.role_id..".txt"
		self.log_map = self.log_map or {}
		local arg = 'w'
		if self.log_map[fileName] then
			arg = 'a'
		else
			self.log_map[fileName] = true
			content = RoleManager.Instance.mainRoleInfo.name.."的日志\n"..content
		end

		local f = assert(io.open(fileName,arg))
		f:write(content.."\n")
		f:close()
	end
end
