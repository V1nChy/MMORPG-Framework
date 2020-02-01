local LuaHelper = LuaHelper


--输出日志--
function log(str)
    Util.Log(str);
end

--错误日志--
function logError(str) 
	Util.LogError(str);
end

--警告日志--
function logWarn(str) 
	Util.LogWarning(str);
end

--查找对象--
function find(str)
	return GameObject.Find(str);
end

function destroy(obj)
	GameObject.Destroy(obj);
end

function newObject(prefab)
	return GameObject.Instantiate(prefab);
end

--创建面板--
function createPanel(name)
	PanelManager:CreatePanel(name);
end

function child(str)
	return transform:FindChild(str);
end

function subGet(childNode, typeName)		
	return child(childNode):GetComponent(typeName);
end

function findPanel(str) 
	local obj = find(str);
	if obj == nil then
		error(str.." is null");
		return nil;
	end
	return obj:GetComponent("BaseLua");
end

--输出日志--
function Log(...)
    LuaLogManager.Instance:Log(...)
end

--错误日志--
function LogError(...)
    LuaLogManager.Instance:LogError(...)
end

--警告日志--
function LogWarn(...)
    LuaLogManager.Instance:LogWarn(...)
end


function GetSystemMemorySize()
    return LuaHelper.GetSystemMemorySize()
end

function PrintTable(tbl,level)
    if RuntimePlatform and (ApplicationPlatform == RuntimePlatform.Android or ApplicationPlatform == RuntimePlatform.IPhonePlayer) then
        return
    end

    if tbl == nil or type(tbl) ~= "table" then
        return
    end

    level = level or 1

    local indent_str = ""
    for i = 1, level do
        indent_str = indent_str.."  "
    end
    print(indent_str .. "{")
    for k,v in pairs(tbl) do

        local item_str = string.format("%s%s = %s", indent_str .. " ",tostring(k), tostring(v))
        print(item_str)
        if type(v) == "table" then
            PrintTable(v, level + 1)
        end
    end
    print(indent_str .. "}")
end

--添加点击事件
function AddClickEvent(target,call_back,use_sound)
    if target then
        -- local com = target:GetComponent("Button")
        -- if not com then
        --  target:AddComponent(typeof(UnityEngine.UI.Button))
        -- end
        use_sound = false
        if use_sound == nil then
            use_sound = 2
        end
        if use_sound  then
            local function call_back_2(target,...)
                GlobalEventSystem:Fire(EventName.PLAY_UI_EFFECT_SOUND,use_sound)
                call_back(target,...)
            end
            LuaClickListener.Get(target).onClick = call_back_2
        else
            LuaClickListener.Get(target).onClick = call_back
        end
    end
end

--添加按下事件
function AddDownEvent(target,call_back,use_sound)
    if target then
        if use_sound  then
            local function call_back_2(target,...)
                GlobalEventSystem:Fire(EventName.PLAY_UI_EFFECT_SOUND,use_sound)
                call_back(target,...)
            end
            LuaEventListener.Get(target).onDown = call_back_2
        else
            LuaEventListener.Get(target).onDown = call_back
        end
    end
end

--添加松开事件
function AddUpEvent(target,call_back)
    if target then
        LuaEventListener.Get(target).onUp = call_back
    end
end

--添加拖拽事件
function AddDragEvent(target,call_back)
    if target then
        LuaDragListener.Get(target).onDrag = call_back
    end
end