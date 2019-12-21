--[[
    ！规则
    #1.Cofig文件夹的配置文件不直接加载，进行缓存
    #2.普通类（界面类）不直接加载，进行缓存
--]]
LuaMemSystem = LuaMemSystem or BaseClass(nil, true)
local LuaMemSystem = LuaMemSystem

local rawset = rawset
local rawget = rawget
local Time = Time
local string = string
local package_loaded = package.loaded

local string_gsub = string.gsub
local string_sub = string.sub
local string_find = string.find
local string_len = string.len
local table_insert = table.insert

--重新登录不清除缓存的model
LuaMemSystem.KEEP_CACHE_MODEL = {
    ["LoginModel"] = true,
    ["ServerModel"] = true,
    ["UIModelClass"] = true,
    ["BaseModel"] = true,
    ["GameSettingModel"] = true,
}

LuaMemSystem.KEEP_CONFIG = {
    ["Goods"] = true,
    ["Mon"] = true,
    ["Equipattr"] = true,
    ["Giftreward"] = true,
    ["Goodscompose"] = true,
    ["Gangskill"] = true,
    ["Equipstrenlv"] = true,
    ["Skill"] = true,
}

function LuaMemSystem:__init()
    self:initConfigInfo()
    self:initGameClassInfo()
    collectgarbage("setpause", 110)
    collectgarbage("setstepmul", 200)

    local function moduleRequire(path, direct_require, auto_delete_time, cannot_force_delete)
        if not direct_require then
            local class_name = LuaMemSystem.GetLastStr(path,".")
            local ctype = 0
            if string_find(path,"Game.",1,true) then
                --1.Cofig文件夹的配置文件不直接加载，进行缓存
                if string_find(path,"Game.Config",1,true) then
                    ctype = 1
                else
                    ctype = 2
                end
            elseif string_find(path,"Framework.UI",1,true) then
                ctype = 2
            end

            if ctype == 1 then
                if not self.config_list[class_name] then
                    self.config_list[class_name] = {isLoad = false, path = path, last_used_Time = 0}
                    if auto_delete_time then
                        self.config_list[class_name].auto_delete_time = auto_delete_time
                    end              
                    if cannot_force_delete then
                        self.config_list[class_name].cannot_force_delete = cannot_force_delete
                    end
                end
                if auto_delete_time and cannot_force_delete then
                    if Config[class_name] then end
                end
            elseif ctype == 2 then
                if self.class_list[class_name] == nil then
                    self.class_list[class_name] = {isLoad = false, path = path, last_used_Time = 0}
                end
            end
            if ctype > 0 then
                return
            end
        end
        originalRequire(path)
    end  
     _G.originalRequire = _G.require  
     _G.require = moduleRequire 
end

function LuaMemSystem:initConfigInfo()
    self.check_config_time = 0
    Config = Config or {}
    self.config_list = {}
    self.config_data = {}

    setmetatable(Config,{
        __newindex = function(t,k,v) 
            rawset(self.config_data,k,v)
            if v == nil then
               local ct = self.config_list[k]
               if ct then
                   ct.isLoad = false
                   if package_loaded[ct.path] then
                       package_loaded[ct.path] = nil
                   end
               end
            end
        end
        ,
        __index = function (t,k)
            local ct = self.config_list[k]
            if ct and not ct.isLoad then
                ct.isLoad = true
                ct.last_used_Time = Time.time
                originalRequire(ct.path)
            elseif ct then
                ct.last_used_Time = Time.time
            end
            return rawget(self.config_data,k)
        end
    })
end

function LuaMemSystem:initGameClassInfo()
    self.check_class_time = 0
    self.class_list = {}
    --_G为所有全局变量的父节点
    setmetatable(_G,{
        __newindex = function(t,k,v) 
            if v == nil then
                local ct = self.class_list[k]
                if ct then
                    ct.isLoad = false
                    if package_loaded[ct.path] then
                        package_loaded[ct.path] = nil
                    end
                end 
            end
            rawset(_G ,k ,v)  
        end
        ,
        __index = function (t,k)
            local ct = self.class_list[k]
            if ct and not ct.isLoad then
                ct.isLoad = true
                ct.last_used_Time = Time.time
                originalRequire(ct.path)
            end
            return rawget(_G ,k)
        end
    })
end

function LuaMemSystem:InitInfo()
    self:SetReleaseMemTimer(true)
end

function LuaMemSystem:SetReleaseMemTimer(bool)
    if bool and not self.check_mem_timer then
        local function onUpdateFunc()
            self:checkToReleaseMem()
        end
        self.check_mem_timer = GlobalTimerQuest:AddPeriodQuest(onUpdateFunc, 95, -1)
    else
        if self.check_mem_timer then
            GlobalTimerQuest:CancelQuest(self.check_mem_timer)
            self.check_mem_timer = nil
        end
    end
end

function LuaMemSystem:ClearMemory()
    -- print("强制垃圾回收")
    -- collectgarbage("collect")
    collectgarbage()
end


function LuaMemSystem:getInstance()
    if LuaMemSystem.Instance == nil then
        LuaMemSystem.New()
    end
    return LuaMemSystem.Instance
end
--将 szFullString 对象拆分为一个子字符串表
function LuaMemSystem.Split(szFullString, szSeparator)
    local nFindStartIndex = 1
    local nSplitIndex = 1
    local nSplitArray = {}
    while true do
       local nFindLastIndex = string_find(szFullString, szSeparator, nFindStartIndex,true)
       if not nFindLastIndex then
        nSplitArray[nSplitIndex] = string_sub(szFullString, nFindStartIndex, string_len(szFullString))
        break
       end
       table_insert(nSplitArray, string_sub(szFullString, nFindStartIndex, nFindLastIndex - 1))
       nFindStartIndex = nFindLastIndex + string_len(szSeparator)
       nSplitIndex = nSplitIndex + 1
    end
    return nSplitArray
end

--获取最后一个字符串
function LuaMemSystem.GetLastStr(szFullString, szSeparator)
    local nFindStartIndex = 1
    local nSplitIndex = 1
    while true do
       local nFindLastIndex = string_find(szFullString, szSeparator, nFindStartIndex,true)
       if not nFindLastIndex then
            return string_sub(szFullString, nFindStartIndex, string_len(szFullString))
       end
       nFindStartIndex = nFindLastIndex + string_len(szSeparator)
       nSplitIndex = nSplitIndex + 1
    end
end

--把配置对象置空 
function LuaMemSystem:clearConfigPro(class_name)
    Config[class_name] = nil
end


--去掉配置对象在package的引用
function LuaMemSystem:resetPackageLoaded(class_name)
    local ct = self.config_list[class_name]
    if ct then
        -- print("销毁配置 ",class_name)
        ct.isLoad = false
        if package_loaded[ct.path] then
            package_loaded[ct.path] = nil
        end
    end
end

--清理类对象
function LuaMemSystem:clearGameClass(class_type)
    if class_type then
        local now_super = class_type
        local delete_list = nil
        local child_will_delete = true
        local t = nil
        local class_name = nil
        while now_super ~= nil do    
            -- print("now_super._source=",now_super._source)
            if now_super._source then
                class_name = LuaMemSystem.GetLastStr(now_super._source,"/")
                class_name = string_gsub(class_name,".lua","")
                if class_name and self.class_list[class_name] then
                    if child_will_delete then  --只有子类要被删除 才去判断父类是否需要去掉一个作为父类的引用
                         if _be_super_count_map[now_super] then
                             _be_super_count_map[now_super] = _be_super_count_map[now_super] - 1
                         end
                         --如果该类引用的实力对象的引用次数为0  而且 该类没有作为任何类的父类 才需要被删除
                         if (_in_obj_ins_map[now_super] == nil or _G.next(_in_obj_ins_map[now_super]) == nil) and (_be_super_count_map[now_super] == nil or _be_super_count_map[now_super] == 0) then
                            delete_list = delete_list or {}
                             table_insert(delete_list,class_name)
                             child_will_delete = true
                         else
                             child_will_delete = false
                         end
                    end
                end
            end
            now_super = now_super.super
        end
        if delete_list then
            for i = 1,#delete_list do
                -- print("销毁类 ",delete_list[i])
                self:resetGameClassPackageLoaded(delete_list[i])
                _G[delete_list[i]] = nil
            end
        end
    end
    return false
end

--清理类对象在package的引用
function LuaMemSystem:resetGameClassPackageLoaded(className)
   local ct = self.class_list[className]
    if ct then
        ct.isLoad = false
        if package_loaded[ct.path] then
            package_loaded[ct.path] = nil
        end
    end
end

function LuaMemSystem:checkToReleaseMem(forceToRelease, hot_update)
    --检测配置是否需要释放内存
    forceToRelease = forceToRelease or hot_update
    if forceToRelease or (self.check_config_time == 0 or Status.NowTime - self.check_config_time > 90) then  --检测是否有需要清理的配置对象
        self.check_config_time = Status.NowTime
        local can_force_release = forceToRelease
        for class_name,vo in pairs(self.config_list) do
            if vo.isLoad then
                if not hot_update and vo.cannot_force_delete then
                    can_force_release = false
                end
                if can_force_release then
                    if Status.NowTime - vo.last_used_Time < 0.5 then --0.5秒的最大限度加载时间
                        can_force_release = false
                    end
                end

                if not LuaMemSystem.KEEP_CONFIG[class_name] and 
                (can_force_release or (Status.NowTime - vo.last_used_Time > (vo.auto_delete_time or 90))) then --5秒没被引用就销毁
                    self:clearConfigPro(class_name)
                end
            end
        end     
    end   


    --检测类是否需要释放内存
    if forceToRelease or ((self.check_class_time == 0 or Status.NowTime - self.check_class_time >= 90)) then   --检测是否有需要清理的类对象 调试阶段先用1
        self.check_class_time = Status.NowTime
        local class_type = nil
        for class_name,vo in pairs(self.class_list) do
            if vo.isLoad then--当前已经被加载
                if Status.NowTime - vo.last_used_Time > 0.5 then --0.5秒的最大限度加载时间
                    class_type = _G[class_name]
                    if class_type  
                        and (_be_super_count_map[class_type] == nil or _be_super_count_map[class_type] == 0) --没有作为父类的引用
                        and (_in_obj_ins_map[class_type] == nil or _G.next(_in_obj_ins_map[class_type]) == nil)--没有实例化的对象
                        then 
                        self:clearGameClass(class_type)
                    end
                end
            end
        end 
    end   

    if forceToRelease then -- or self.clear_mem_start_time == 0 or Status.NowTime - self.clear_mem_start_time >= 3 then
       -- self.clear_mem_start_time = Status.NowTime
        --if forceToRelease or collectgarbage("count") > 20000 then
            self:ClearMemory()
       -- end
    end
end