LuaResManager = LuaResManager or BaseClass(nil, true)
local LuaResManager = LuaResManager

local soundMgr = soundMgr
local ResourceManager = ResourceManager
local Array_New = Array.New
local Time = Time
local IsIosSystem = (ApplicationPlatform == RuntimePlatform.IPhonePlayer)
local IsLowSystemState = (SystemMemoryLevel.Cur == SystemMemoryLevel.Low)

LuaResManager.RES_TYPE = {
	PREFAB = 1,
	SPRITE = 2,
	RENDERTEXTURE = 3,
	TEXTASSET = 4,
}

function LuaResManager:__init()
	--assetbundle包资源引用列表
	self.ref_count_list = {}

	--延迟unload的资源列表
	self.delay_unload_list = {}

	--界面显示模型对象列表
	self.role_mode_list = {}

	--每隔多长时间执行一次内存清理
	self.clear_memory_time = 10
	if ApplicationPlatform == RuntimePlatform.IPhonePlayer then
		self.clear_memory_time = 5
		if SystemMemoryLevel.Cur == SystemMemoryLevel.Low then
			self.clear_memory_time = 1
		elseif SystemMemoryLevel.Cur == SystemMemoryLevel.Middle then
			self.clear_memory_time = 3
		end
	elseif ApplicationPlatform == RuntimePlatform.Android then
		if SystemMemoryLevel.Cur == SystemMemoryLevel.Low then
			self.clear_memory_time = 15
		end
	end

	--定时清理资源函数
	local function onCheckToClearObjPool()
		self:CheckToClearObjPool()
	end
	--GlobalTimerQuest:AddPeriodQuest(onCheckToClearObjPool, self.clear_memory_time, -1)

	--资源对象池
	self.obj_pool_list = {}

	--对象池总ab包对象数量
	self.obj_pool_ab_count = 0

	--静态的缓存对象名字列表
	self.static_obj_name_list = {}

	--曾经加载过的AB包列表
	self.has_use_abname = {}

	--静态的缓存对象列表
	self.static_obj_pool_list = {}
	self.static_obj_pool_container = GameObject.New("static_obj_pool_container").transform
	self.static_obj_pool_container:SetParent(UIManager:GetTagRoot("SceneRoot"))
	self.static_obj_pool_container.gameObject:SetActive(false)

	--图片资源使用记录,处理请求未返回时触发的二次请求
	self.image_set_record_map = {}
	self.outimg_set_record_map = {}
end

----------------------------资源引用计数-------------------------------------------

--增加引用数
function LuaResManager:addRefCount(ref_tar,abName,count)
	count = count or 1
	if count > 0 and abName then
		local ref_info = self.ref_count_list[ref_tar] or {}
		local ref_count = ref_info[abName] and ref_info[abName] + count or count
		ref_info[abName] = ref_count
		self.ref_count_list[ref_tar] = ref_info

		--发现资源在待卸载列表，更新其使用时间
		local delay_info = self.delay_unload_list[abName]
		if delay_info then
			delay_info.use_time = Time.time
		end
	end
end

--减少引用数
function LuaResManager:reduceRefCount(ref_tar,abName)
	if self.ref_count_list[ref_tar] and self.ref_count_list[ref_tar][abName] and self.ref_count_list[ref_tar][abName] > 0 then
		self.ref_count_list[ref_tar][abName] = self.ref_count_list[ref_tar][abName] - 1
	end
end

--清除关联对象的所有资源关联信息
function LuaResManager:clearReference(ref_tar)
	self:clearRefCount(ref_tar)
	self:clearRoleMode(ref_tar)
	self:clearImageSet(ref_tar)
end

--清除该对象的所有资源引用
function LuaResManager:clearRefCount(ref_tar)
	local ref_count_obj = self.ref_count_list[ref_tar]
	if ref_count_obj then
		for abName,ref_count in pairs(ref_count_obj) do
			if ref_count and ref_count > 0 then
				local delay_info = self.delay_unload_list[abName]
				if delay_info == nil then
					delay_info = {
						use_time = Time.time,
						ref_count = ref_count,
					}
					self.delay_unload_list[abName] = delay_info
				else
					delay_info.use_time = Time.time
					delay_info.ref_count = delay_info.ref_count + ref_count
				end
			end
		end
		self.ref_count_list[ref_tar] = nil
	end
end

--清除该对象的所有角色模型的引用
function LuaResManager:clearRoleMode(ref_tar)
	local ref_count_obj = self.role_mode_list[ref_tar]
	if ref_count_obj then
		for p,uiModelClass in pairs(ref_count_obj) do
			uiModelClass:DeleteMe()
		end
		self.role_mode_list[ref_tar] = nil
	end
end

function LuaResManager:clearImageSet(ref_tar)
	self.image_set_record_map[ref_tar] = nil
	self.outimg_set_record_map[ref_tar] = nil
end

-------------------------------------------对象缓存池管理-----------------------------------------------

--添加游戏对象列表进入缓存池 通过loadPrefabs加载的资源对象列表
function LuaResManager:AddObjListToPool(ref_tar, abName, resName_list, gameObject_list)
	if gameObject_list then
		self:CreateABObjPool(abName)
		local resName = "resName_list" .. table.concat(resName_list, "_")
		local res_list = self.obj_pool_list[abName]["res_name_list"][resName]
		if res_list == nil then
			res_list = Array_New()
			self.obj_pool_list[abName]["res_name_list"][resName] = res_list
		end
		if res_list:GetSize() > self:GetMaxChildPoolObjCount() then
			for i, go in ipairs(gameObject_list) do
				destroy(go)
			end
		else
			self:reduceRefCount(ref_tar, abName)
			for i, go in ipairs(gameObject_list) do
				go.transform:SetParent(self.obj_pool_list[abName].container)
			end
			res_list:PushBack(gameObject_list)
			self.obj_pool_list[abName].last_change_time = Time.time
		end
	end
end

--添加游戏对象进入缓存池 通过loadPrefab加载的资源对象
function LuaResManager:AddObjToPool(ref_tar, abName, resName, gameObject, alpha, is_default_color)
	if gameObject then
		--非静态缓存对象
		if abName and (not self.static_obj_name_list[abName] or self.static_obj_pool_list[abName]) then 
			self:CreateABObjPool(abName)
			local res_list = self.obj_pool_list[abName]["res_name_list"][resName]
			if res_list == nil then
				res_list = Array_New()
				self.obj_pool_list[abName]["res_name_list"][resName] = res_list
			end
			if res_list:GetSize() > self:GetMaxChildPoolObjCount() then
				--大于限制的相同对象缓存数量，不放入缓存
				destroy(gameObject)
			else
				if not IsNull(gameObject) then
					--放入缓存后减少引用计数
					self:reduceRefCount(ref_tar, abName)
					gameObject.transform:SetParent(self.obj_pool_list[abName].container)
					res_list:PushBack(gameObject)
					self.obj_pool_list[abName].last_change_time = Time.time
				end
			end
		else
			--静态缓存对象
			if not abName or self.static_obj_pool_list[abName] then
				--只缓存一个相同的静态对象
				destroy(gameObject)
			else
				--重置shader
				if (alpha and alpha < 1) or is_default_color == false then  
					local skinned_mesh_renderer = gameObject:GetComponentInChildren(typeof(UnityEngine.SkinnedMeshRenderer))
					if skinned_mesh_renderer then
						local sd = SceneManager:getInstance():GetTextureShader()
						if sd then
							skinned_mesh_renderer.material.shader = sd
						end
					end
				end

				self:reduceRefCount(ref_tar, abName)
				self.static_obj_pool_list[abName] = gameObject
				gameObject.transform:SetParent(self.static_obj_pool_container)

				--设置引用计数10000，避免静态缓存对象异常情况引用归零被销毁
				self:addRefCount(self.static_obj_pool_list, abName, 10000)
			end
		end
	end
end

--添加缓存对象时初始化缓存信息
function LuaResManager:CreateABObjPool(abName)
	if not self.obj_pool_container then
		self.obj_pool_container = UiFactory.createChild(panelMgr:GetParent("SceneObjContainer"), UIType.EmptyObject, "obj_pool_container").transform
		self.obj_pool_container.gameObject:SetActive(false)
	end
	
	if self.obj_pool_list[abName] == nil then
		--缓存AB包超过最大数量时，强制清理最久未使用的那个资源
		if self.obj_pool_ab_count >= self:GetMaxPoolObjCount() then
			local min_change_time = 9999999
			local min_abName = nil
			for pool_abName, ab_info in pairs(self.obj_pool_list) do
				if ab_info.last_change_time < min_change_time and ab_info then
					min_change_time = ab_info.last_change_time
					min_abName = pool_abName
				end
			end
			if min_abName then
				self:ClearObjPool(min_abName)
			end
		end
		self.obj_pool_ab_count = self.obj_pool_ab_count + 1
		self.obj_pool_list[abName] = {last_change_time = Time.time, res_name_list = {}, container = UiFactory.createChild(self.obj_pool_container, UIType.EmptyObject, abName .. "_container").transform}
	end
end

--从缓存池中获取游戏对象列表
function LuaResManager:GetObjListFormPool(ref_tar, abName, resName_list)
	local resName = "resName_list" .. table.concat(resName_list, "_")
	if self.obj_pool_list[abName] and self.obj_pool_list[abName]["res_name_list"][resName] then
		local gameObject_list = self.obj_pool_list[abName]["res_name_list"][resName]:PopBack()
		if gameObject_list then
			self.obj_pool_list[abName].last_change_time = Time.time
			self:addRefCount(ref_tar, abName)
			return gameObject_list
		end
	end
end

--从缓存池中获取游戏对象
function LuaResManager:GetObjFormPool(ref_tar, abName, resName)
	if self.static_obj_name_list[abName] then
		local go = self.static_obj_pool_list[abName]
		if go then
			self.static_obj_pool_list[abName] = nil
			self:addRefCount(ref_tar, abName)
			return go
		end
	end

	if self.obj_pool_list[abName] and self.obj_pool_list[abName]["res_name_list"][resName] then
		local gameObject = self.obj_pool_list[abName]["res_name_list"][resName]:PopBack()
		if gameObject then
			self.obj_pool_list[abName].last_change_time = Time.time
			self:addRefCount(ref_tar, abName)
			return gameObject
		end
	end
end

--清除单个ab包的缓存游戏对象
function LuaResManager:ClearObjPool(abName, ab_info)
	ab_info = ab_info or self.obj_pool_list[abName]
	if ab_info then
		local all_ref_count = 0
		for resName, res_list in pairs(ab_info.res_name_list) do
			if res_list and res_list:GetSize() > 0 then
				all_ref_count = all_ref_count + res_list:GetSize()
			end
		end
		self:addRefCount(self.obj_pool_container, abName, all_ref_count)
		self:clearReference(self.obj_pool_container)
		destroy(ab_info.container.gameObject)
		self.obj_pool_list[abName] = nil
		self.obj_pool_ab_count = self.obj_pool_ab_count - 1
	end
end

--清除所有缓存池游戏对象，切换场景时调用
function LuaResManager:ClearAllObjPool()
	local obj = nil
	if self.obj_pool_container then
		local all_ref_count = 0
		for abName, ab_info in pairs(self.obj_pool_list) do
			all_ref_count = 0
			for resName, res_list in pairs(ab_info.res_name_list) do
				if res_list and res_list:GetSize() > 0 then
					all_ref_count = all_ref_count + res_list:GetSize()
				end
			end
			self:addRefCount(self.obj_pool_container, abName, all_ref_count)
		end
		self:clearReference(self.obj_pool_container)
		destroy(self.obj_pool_container.gameObject, true)
		self.obj_pool_container = nil
	end
	self.obj_pool_list = {}
	self.obj_pool_ab_count = 0
	self:CheckToClearObjPool(true)
end

--有10秒定时器检测，切换场景时自动调用一次
function LuaResManager:CheckToClearObjPool(force_unload)
	local unload_count = 0
	for abName,vo in pairs(self.delay_unload_list) do
		if force_unload or Time.time - vo.use_time >= self.clear_memory_time then --延迟一定时间unload ab包
			unload_count = unload_count + 1
			if not force_unload and unload_count > 10 and ApplicationPlatform ~= RuntimePlatform.IPhonePlayer then --自动unload的时候 避免一次性交互过多
				return
			end
			--print("-------UnloadAssetBundle---------",abName)
			ResourceManager:UnloadAssetBundle(abName,true,vo.ref_count)
			self.delay_unload_list[abName] = nil
		end
	end
end

--获得最大的缓存AB包数量
function LuaResManager:GetMaxPoolObjCount()
	if IsLowSystemState and IsIosSystem then
		return 10
	else
		if IsIosSystem then
			return 50
		else
			return 100
		end
	end
end

--获得最大的缓存子对象数量
function LuaResManager:GetMaxChildPoolObjCount()
	if IsLowSystemState and IsIosSystem then
		return 3
	else
		if IsIosSystem then
			return 10
		else
			return 18
		end
	end
end

---------------------------------静态对象缓存------------------------------------
--res_name 静态资源名字
--res_type  资源类型  0默认  永不删除的类型
function LuaResManager:AppendStaticObjName(res_name, res_type)
	if res_name and res_name ~= "" then
		self.static_obj_name_list[res_name] = res_type or 0
	end
end

--删除制定类型的静态资源
function LuaResManager:RemoveStaticObj(remove_res_type)
	for res_name, res_type in pairs(self.static_obj_name_list) do
		if remove_res_type == res_type then
			self.static_obj_name_list[res_name] = nil
			local go = self.static_obj_pool_list[res_name]
			if go then
				self.static_obj_pool_list[res_name] = nil
				destroy(go)
				local delay_info = self.delay_unload_list[res_name]
				if delay_info == nil then
					delay_info = {
						use_time = Time.time,
						ref_count = 10000,
					}
					self.delay_unload_list[res_name] = delay_info
				else
					delay_info.use_time = Time.time
					delay_info.ref_count = delay_info.ref_count + 10000
				end
			end
		end
	end
end

---------------------------------------------界面显示模型使用-----------------------------------------------

--新版：通过摄像机Z轴控制显示前后顺序

--ui挂机模型最新的接口  free_param 自由参数 通过type类型来确定其用途   ignore_param（为了保持旧接口的参数位置，暂用这个参数来确认是否为登录界面）
--[[
	ref_tar:必须是继承baseclass的对象
	parent:父容器
	career：职业
	sex:性别
	clothe_res_id：衣服模型id
	weapon_res_id：武器模型id
	weapon_clothe_id: 武器时装
	type：类型
	layer_name:层级
	rotate:旋转度数
	action_name_list:动作集
	can_rotate:是否支持左右旋转 
	scale:缩放 
	position:位置
	fashion_model_id:时装模型id
	texture_id:时装贴图id
	action_delta:动作间隔
	free_param,
	ignore_param,
	skill_id:技能id
	callBack:加载完成的回调，返回modelClass
	wing_id:翅膀id
	image_id,
	head_wear_id,
	head_clothe_id,
	footmark_id:足迹id
	layout_file:是否支持配置控制模型大小，位置，角度参数
--]]
--修改成table形式  只添需要的参数
function LuaResManager:NewSetRoleModel(ref_tar,parent,data)
	local ref_info = self.role_mode_list[ref_tar] or {}
	local curr_roleModel = ref_info[parent]
	if curr_roleModel then
		curr_roleModel:DeleteMe()
		ref_info[parent] = nil
	end
	local function lCallBack(modelClass)
		if not ref_tar._use_delete_method then
			if callBack then callBack(modelClass) end
		end
	end

	local career = data and data.career or nil 
	local sex = data and data.sex or nil 
	local clothe_res_id = data and data.clothe_res_id or nil
	local weapon_res_id = data and data.weapon_res_id or nil
	local weapon_clothe_id = data and data.weapon_clothe_id or nil 
	local type = data and data.type or nil 
	local layer_name = data and data.layer_name or nil
	--local rotate = data and data.rotate or nil 
	local action_name_list = data and data.action_name_list or nil 
	local can_rotate = data and data.can_rotate 
	local fashion_model_id = data and data.fashion_model_id  or nil
	local texture_id = data and data.texture_id or nil 
	local action_delta = data and data.action_delta or nil
	local free_param = data and data.free_param or nil
	local ignore_param = data and data.ignore_param or nil
	local skill_id = data and  data.skill_id or nil 
	local callBack = data and data.callBack  or nil
	local wing_id = data and data.wing_id or nil
	local image_id = data and data.image_id or nil
	local head_wear_id = data and data.head_wear_id or nil
	local head_clothe_id = data and data.head_clothe_id or nil
	local footmark_id = data and data.footmark_id or nil
	local hair_id = data and data.hair_id or nil
	local hallows_id = data and data.hallows_id or nil
	local layout_file = data and data.layout_file or nil
	local show_gray = data and data.show_gray or false
	local particle_scale = data and data.particle_scale or false

	local temp_scale
	local temp_position
	local temp_rotate
	local cfg = Config.UIModelParameter 
	
	--没有指定单独界面参数 和没有自主传参数就用 全界面通用配置参数
	if not layout_file then
		temp_scale 	  = data.scale  and  data.scale or cfg.Defaut.scale
		temp_position = data.position and  data.position or cfg.Defaut.position
		temp_rotate   = data.rotate   and  data.rotate or  cfg.Defaut.rotate
	else
		--指定资源id参数
		local specific_cfg = cfg[layout_file] and cfg[layout_file][clothe_res_id] and cfg[layout_file][clothe_res_id] or nil

		--可能是时装
		if not specific_cfg then
			specific_cfg = cfg[layout_file] and cfg[layout_file][fashion_model_id] and cfg[layout_file][fashion_model_id] or nil
		end
		--界面统一参数配置
		local part_cfg = cfg[layout_file] and  cfg[layout_file] or nil
		if specific_cfg then
			temp_scale    = specific_cfg.scale or data.scale
			temp_position = specific_cfg.position or data.position
			temp_rotate = specific_cfg.rotate or data.rotate
		else
			temp_scale    = part_cfg and part_cfg.defaut.scale or data.scale
			temp_position = part_cfg and part_cfg.defaut.position or data.position
			temp_rotate   = part_cfg and part_cfg.defaut.rotate or data.rotate
		end	
	end

	ref_info[parent] = NewUIModelClass.New(parent,career,sex,clothe_res_id,weapon_res_id,weapon_clothe_id or "",type, layer_name ,temp_rotate, action_name_list, can_rotate,temp_scale,temp_position, fashion_model_id, texture_id, action_delta,free_param,ignore_param, skill_id, callBack, wing_id,image_id,head_wear_id,head_clothe_id,footmark_id,show_gray,particle_scale,hair_id,hallows_id)
	self.role_mode_list[ref_tar] = ref_info
end

--替换单个部件 目前只适用于时装界面
function LuaResManager:NewSetRoleModelSingle(ref_tar,parent,data)
	local par_data = self:GetPartModel(ref_tar,parent)
	if par_data and data then
		local clothe_res_id = data.clothe_res_id or nil
		local fashion_model_id = data.fashion_model_id  or nil
		--身体改变需要重新创
		if (clothe_res_id == nil or clothe_res_id ~= par_data.clothe_res_id or (fashion_model_id == nil and par_data.fashion_model_id ~= nil) or fashion_model_id ~= par_data.fashion_model_id) or force then
			self:NewSetRoleModel(ref_tar,parent,data)
			return
		end

		local career = data.career or nil 
		local sex = data.sex or nil 
		
		local weapon_res_id = data.weapon_res_id or nil
		local weapon_clothe_id = data.weapon_clothe_id or nil 
		local type = data.type or nil 
		local layer_name = data.layer_name or nil
		local rotate = data and data.rotate or nil 
		local action_name_list = data.action_name_list or nil 
		local can_rotate = data.can_rotate 
		
		local texture_id = data.texture_id or nil 
		local action_delta = data.action_delta or nil
		local free_param = data.free_param or nil
		local ignore_param = data.ignore_param or nil
		local skill_id = data.skill_id or nil 
		local callBack = data.callBack  or nil
		local wing_id = data.wing_id or nil
		local image_id = data.image_id or nil
		local head_wear_id = data.head_wear_id or nil
		local head_clothe_id = data.head_clothe_id or nil
		local footmark_id = data.footmark_id or nil
		local hair_id = data.hair_id or nil
		local layout_file = data.layout_file or nil
		local show_gray = data.show_gray or false
		local particle_scale = data.particle_scale or false


		local temp_rotate
		local cfg = Config.UIModelParameter 
	
		--没有指定单独界面参数 和没有自主传参数就用 全界面通用配置参数
		if not layout_file then
			temp_rotate = data.rotate and data.rotate or cfg.Defaut.rotate
		else
			--指定资源id参数
			local specific_cfg = cfg[layout_file] and cfg[layout_file][clothe_res_id] and cfg[layout_file][clothe_res_id] or nil

			--可能是时装
			if not specific_cfg then
				specific_cfg = cfg[layout_file] and cfg[layout_file][fashion_model_id] and cfg[layout_file][fashion_model_id] or nil
			end
			--界面统一参数配置
			local part_cfg = cfg[layout_file] and  cfg[layout_file] or nil
			if specific_cfg then
				temp_rotate = specific_cfg.rotate or data.rotate
			else
				temp_rotate   = part_cfg and part_cfg.defaut.rotate or data.rotate
			end	
		end

		par_data:ChangeHeadWear(head_wear_id,head_clothe_id)
		par_data:ChangeHair(hair_id)
		par_data:ChangeWeapon(weapon_res_id,career,weapon_clothe_id)
		par_data:ChangeFootmark(footmark_id)
		par_data:ChangeWing(wing_id)
		par_data:ChangeAction(action_name_list,action_delta)
		par_data:ChangeRotate(temp_rotate)

		
	else
		self:NewSetRoleModel(ref_tar,parent,data)
	end
end


--[[
	res_id, texture_id, modelPartPos, type, raycastParent,texture_size,scale, layer_name,position, action_name_list, action_delta,layout_file,can_rotate,rotate
	参数修改成table形式 只传需要的参数

]]
function LuaResManager:NewSetPartModel(ref_tar, parent, data)
	local ref_info = self.role_mode_list[ref_tar] or {}
	local curr_roleModel = ref_info[parent]
	if curr_roleModel then
		curr_roleModel:DeleteMe()
		ref_info[parent] = nil
	end

	local res_id = data and data.res_id or nil 
	local texture_id = data and data.texture_id or nil
	local modelPartPos = data and data.modelPartPos or nil 
	local type = data and data.type or nil 
	local raycastParent = data and data.raycastParent
	local texture_size = data and data.texture_size or nil
	local layer_name = data and data.layer_name or nil
	local action_name_list = data and data.action_name_list or nil 
	local action_delta = data and data.action_delta or nil
	local layout_file = data and data.layout_file or nil
	local can_rotate = data and data.can_rotate 
	--local rotate = data and data.rotate or nil
    

	local cfg = Config.UIModelParameter 
	local temp_cfg = Config.UIPartModelConfig[res_id]
	local temp_scale = nil
	local temp_position = nil
	local temp_rotate = nil
	local size = nil
	
	if temp_cfg then
		size =  data.texture_size and data.texture_size or temp_cfg.size
		temp_rotate = data.rotate or temp_cfg.rotate
		can_rotate = data.can_rotate or  temp_cfg.can_rotate
		temp_position = data.position or temp_cfg.position
		temp_scale = data.scale or temp_cfg.scale
	else
		--没有指定单独界面参数 和没有自主传参数就用 全界面通用配置参数
		if not layout_file then
			temp_scale 	  = data.scale  and  data.scale or cfg.Defaut.scale
			temp_position = data.position and  data.position or cfg.Defaut.position
			size = data.texture_size and data.texture_size or cfg.Defaut.texture_size
			temp_rotate   = data.rotate   and  data.rotate or  cfg.Defaut.rotate
		else
			local specific_cfg = cfg[layout_file] and  cfg[layout_file][res_id] and cfg[layout_file][res_id] or nil
			local part_cfg = cfg[layout_file] and  cfg[layout_file] or nil
			if specific_cfg then
				temp_scale    = specific_cfg.scale or data.scale
				temp_position = specific_cfg.position or data.position
				temp_rotate = specific_cfg.rotate or data.rotate
				size = specific_cfg.texture_size or data.texture_size
			else
				temp_scale    = part_cfg and part_cfg.defaut.scale or data.scale
				temp_position = part_cfg and part_cfg.defaut.position or data.position
				temp_rotate   = part_cfg and part_cfg.defaut.rotate or data.rotate
				size = part_cfg and part_cfg.defaut.texture_size or data.texture_size
			end	
		end
	end


	ref_info[parent] = NewUIPartModelClass.New(parent, res_id, texture_id, modelPartPos, type, raycastParent,size,temp_scale, layer_name,temp_position, action_name_list, action_delta,can_rotate,temp_rotate)
	self.role_mode_list[ref_tar] = ref_info
end

function LuaResManager:GetPartModel(ref_tar, parent)
	local ref_info = self.role_mode_list[ref_tar] or {}
	local curr_roleModel = ref_info[parent]

	return curr_roleModel
end

--旧版: 使用设置摄像机的targetTexture模式
--[[
	ref_tar必须是继承baseclass的对象
	parent:父容器
	career：职业
	clothe_res_id：衣服模型id
	weapon_res_id：武器模型id
	weapon_clothe_id: 武器时装
	type：类型
	size：大小
	rotate：读书
	action_name_list：动作集
	can_rotate：是否支持左右旋转
	scale：缩放
	position：位置
	fashion_model_id：时装模型id
	texture_id:时装贴图id
	action_delta:
	renderSize:自定义RenderTexture尺寸
	partner_id：
	skill_id:
	callBack:加载完成的回调，返回modelClass
	wing_id:翅膀id
]]

function LuaResManager:setRoleModel(ref_tar ,parent, career, clothe_res_id, weapon_res_id,weapon_clothe_id, type, size, rotate, action_name_list, can_rotate, scale, position, fashion_model_id, texture_id, action_delta,renderSize,partner_id, skill_id, callBack, wing_id,image_id,head_wear_id,head_clothe_id,footmark_id, raycastParent)
	local function loadCameraCallBack(objs)
		if parent and not ref_tar._use_delete_method and objs and objs[0] then
			local go = newObject(objs[0])
			local ref_info = self.role_mode_list[ref_tar] or {}
			local curr_roleModel = ref_info[parent]
			if curr_roleModel then
				curr_roleModel:DeleteMe()
				ref_info[parent] = nil
			end
			ref_info[parent] = UIModelClass.New(go,parent,career,clothe_res_id,weapon_res_id,weapon_clothe_id or "",type,size,rotate, action_name_list, can_rotate,scale,position, fashion_model_id, texture_id, action_delta,renderSize,partner_id, skill_id, callBack, wing_id,image_id,head_wear_id,head_clothe_id,footmark_id,raycastParent)
			self.role_mode_list[ref_tar] = ref_info
		end
	end
	self:loadPrefab(ref_tar,"common","RoleMode", loadCameraCallBack)
end

--设置单一模型显示(不需要显示角色模型) 在ui上显示 比如只显示武器模型
--[[
	ref_tar必须是继承baseclass的对象
	parent:父容器
	res_id：模型id
	modelPartPos:模型部件位置
	type：类型 模型部件前缀类型  ModelPartName
	can_rotate：是否支持左右旋转
]]
function LuaResManager:setPartModel(ref_tar, parent, res_id, texture_id, modelPartPos, type, raycastParent,texture_size,model_scale, layer_name,position, action_name_list, action_delta)
	local function loadCameraCallBack(objs)
		if parent and not ref_tar._use_delete_method and objs and objs[0] then
			local go = newObject(objs[0])
			local ref_info = self.role_mode_list[ref_tar] or {}
			local curr_roleModel = ref_info[parent]
			if curr_roleModel then
				curr_roleModel:DeleteMe()
				ref_info[parent] = nil
			end
			ref_info[parent] = UIPartModelClass.New(go, parent, res_id, texture_id, modelPartPos, type, raycastParent,texture_size,model_scale)
			self.role_mode_list[ref_tar] = ref_info
		end
	end
	self:loadPrefab(ref_tar,"common","RoleMode", loadCameraCallBack)
end

--------------------------------------动态设置纹理使用------------------------------------------------------------

--设置Sprite的图片:图片资源在外部icon或iconjpg目录的情况
function LuaResManager:setOutsideSpriteRender(ref_tar, sp_render, respath,call_back)
	local function loadedCallBack(sp)
		if sp_render and not ref_tar._use_delete_method then
			if sp then
				sp_render.sprite = sp[0]
			end
			if call_back then
				call_back()
			end
		end
	end

	local abName, res_name = GameResPath.GetOutSideResAbName(respath)
	self:addRefCount(ref_tar,abName)
	ResourceManager:LoadSprites(abName, {res_name}, loadedCallBack, ASSETS_LEVEL.HIGHT)

	if OpenGameLuaLog then
		print("--------setOutsideSpriteRender-------",abName,res_name)
	end

	self.has_use_abname[abName] = true
end

--设置image的图片:图片资源在外部icon或iconjpg目录的情况
function LuaResManager:setOutsideImageSprite(ref_tar, image, respath, setNativeSize, call_back, force_sprite)
	self.outimg_set_record_map[ref_tar] = self.outimg_set_record_map[ref_tar] or {}
	local image_set_record = self.outimg_set_record_map[ref_tar]
	if image then
		image_set_record[image] = image_set_record[image] or {}
		local list = {ref_tar=ref_tar,image=image,respath=respath,setNativeSize=setNativeSize,call_back=call_back}
		table.insert(image_set_record[image], list)

		--资源已在请求中
		if #image_set_record[image] > 1 then return end
	end
	local function loadedCallBack(sp)
		if image then
			table.remove(image_set_record[image], 1)
		end
		if not image or #image_set_record[image]==0 then
			if image and not image:IsDestroyed() then
				if sp ~= nil and sp[0] then
					local sprite = sp[0]
					if force_sprite and sprite:GetType().FullName ~= "UnityEngine.Sprite" then
						if tonumber(AppConst.EnglineVer) >= 75 then
							sprite = Util.TextureToSprite(sprite)
						else
							return
						end
					end
					image.sprite = sprite
					if setNativeSize then
						image:SetNativeSize()
					end
				end
			end
			if not ref_tar._use_delete_method and call_back then
				call_back(sp)
			end
		else
			--已有新的加载请求
			local list = image and image_set_record[image][1]
			if list then
				local abName, res_name = GameResPath.GetOutSideResAbName(list.respath)
				self:addRefCount(ref_tar,abName)
				ResourceManager:LoadSprites(abName, {res_name}, loadedCallBack, ASSETS_LEVEL.HIGHT)
			end
		end
	end
	local abName, res_name = GameResPath.GetOutSideResAbName(respath)
	self:addRefCount(ref_tar,abName)
	ResourceManager:LoadSprites(abName, {res_name}, loadedCallBack, ASSETS_LEVEL.HIGHT)

	if OpenGameLuaLog then
		print("--------setOutsideImageSprite-------",abName,res_name)
	end

	self.has_use_abname[abName] = true
end

--设置rawimage的图片:图片资源在外部icon或iconjpg目录的情况
function LuaResManager:setOutsideRawImage(ref_tar, image, respath, setNativeSize, call_back, force_texture)
	local function loadedCallBack(texture)
		if image and not image:IsDestroyed() then
			if texture and texture[0] then
				local now_res = texture[0]
				if force_texture and now_res:GetType().FullName ~= "UnityEngine.Texture2D" then
					now_res = now_res.texture
				end
				image.texture = now_res
				if setNativeSize then
					image:SetNativeSize()
				end
			else
				logWarn("资源"..respath.."缺失！！")
			end
			if not ref_tar._use_delete_method and call_back then
				call_back()
			end
		end
	end

	local abName, res_name = GameResPath.GetOutSideResAbName(respath)
	self:addRefCount(ref_tar,abName)
	ResourceManager:LoadTexture(abName, {res_name}, loadedCallBack, ASSETS_LEVEL.HIGHT)

	if OpenGameLuaLog then
		print("--------setOutsideRawImage-------",abName,res_name)
	end

	self.has_use_abname[abName] = true
end

--设置image的图片:图片资源在UI预制中的情况
--ref_tar必须是继承baseclass的对象
--not_insert 不是外部设置新的图片，不用插入队列中
function LuaResManager:setImageSprite(ref_tar,image,abName,resName,setNativeSize, call_back, load_level, not_insert)
	self.image_set_record_map[ref_tar] = self.image_set_record_map[ref_tar] or {}
	local image_set_record = self.image_set_record_map[ref_tar]
	if not not_insert then
		if image then
			local list = {ref_tar=ref_tar,image=image,abName=abName,resName=resName,setNativeSize=setNativeSize,call_back=call_back,load_level=load_level}
			image_set_record[image] = image_set_record[image] or {}
			table.insert(image_set_record[image], list)
			if image_set_record[image] and #image_set_record[image] > 1 then return end
		end
	end

	load_level = load_level or ASSETS_LEVEL.HIGHT
	local function loadedCallBack(objs)
		if not ref_tar._use_delete_method and image and not image:IsDestroyed() then
			if objs and objs[0] then
				image.sprite = objs[0]
				if setNativeSize then
					image:SetNativeSize()
				end
			end
			if not ref_tar._use_delete_method and call_back then
				call_back(objs)
			end
		end

		local image_set_record = self.image_set_record_map[ref_tar]
		if image and image_set_record and image_set_record[image] then
			if #image_set_record[image] > 0 then
				table.remove(image_set_record[image], 1)
			end
			if #image_set_record[image] > 0 then
				local next_image = image_set_record[image][1]
				self:setImageSprite(next_image.ref_tar,next_image.image,next_image.abName,next_image.resName,next_image.setNativeSize, next_image.call_back, next_image.load_level, true)
			end
		end
	end
	self:loadSprite(ref_tar, abName, resName, loadedCallBack, load_level)
end

--设置rawimage的图片:图片资源在UI预制中的情况
--ref_tar必须是继承baseclass的对象
function LuaResManager:setRawImage(ref_tar,image,abName,resName,setNativeSize, call_back)
	local function loadedCallBack(objs)
		if not ref_tar._use_delete_method and image and not image:IsDestroyed() then
			if objs and objs[0] then
				image.texture = objs[0]
				if setNativeSize then
					image:SetNativeSize()
				end
			end
			if not ref_tar._use_delete_method and call_back then
				call_back()
			end
		end
	end
	self:loadTexture(ref_tar,abName,{resName},loadedCallBack, ASSETS_LEVEL.HIGHT)
end

-----------------------------------------对象加载----------------------------------------------------------

--目前只有baseview使用
function LuaResManager:LoadRes(ref_tar, res_type, abName, pfNameList, callBack)
	if res_type == nil or res_type == LuaResManager.RES_TYPE.PREFAB then
		self:loadPrefabs(ref_tar, abName, pfNameList, callBack, nil, ASSETS_LEVEL.HIGHT)
	elseif res_type == LuaResManager.RES_TYPE.SPRITE then
		self:loadSprites(ref_tar, abName, pfNameList, callBack, ASSETS_LEVEL.HIGHT)
	end
end

--加载一个预设
function LuaResManager:loadPrefab(ref_tar,abName,pfName,callBack, ignore_pool, load_level)
	load_level = load_level or ASSETS_LEVEL.NORMAL
	if not ref_tar or ref_tar._use_delete_method then return end
	local reload_prefab = true
	if not ignore_pool then
		local obj_pool = self:GetObjFormPool(ref_tar, abName, pfName)
		if obj_pool then
			reload_prefab = false
			if callBack then
				--print("--------LoadPool-------",abName)
				callBack({[0] = obj_pool}, true)
				-- print("---------loadPrefab from pool -----",abName,pfName)
			end
		end
	end
	if reload_prefab then
		self:addRefCount(ref_tar,abName)		
		ResourceManager:LoadPrefab(abName, {pfName}, callBack, load_level)

		if OpenGameLuaLog then
			print("--------LoadPrefab-------",abName,pfName)
		end
	end

	self.has_use_abname[abName] = true
end

--加载多个预设
function LuaResManager:loadPrefabs(ref_tar,abName,pfNameList,callBack, ignore_pool, load_level)
	load_level = load_level or ASSETS_LEVEL.NORMAL
	if not ref_tar or ref_tar._use_delete_method then return end
	if not ignore_pool then
		local obj_pool = self:GetObjListFormPool(ref_tar, abName, pfNameList)
		if obj_pool then
			if callBack then
				callBack(obj_pool, true)
			end
		else
			self:addRefCount(ref_tar,abName)
			ResourceManager:LoadPrefab(abName, pfNameList, callBack, load_level)

			if OpenGameLuaLog then
				print("--------LoadPrefabs-------",abName)
			end
		end
	else
		self:addRefCount(ref_tar,abName)
		ResourceManager:LoadPrefab(abName, pfNameList, callBack, load_level)

		if OpenGameLuaLog then
			print("--------LoadPrefabs-------",abName)
		end
	end

	self.has_use_abname[abName] = true
end

--加载一个对象
function LuaResManager:loadObject(ref_tar,abName,pfName,callBack, ignore_pool, load_level)
	load_level = load_level or ASSETS_LEVEL.NORMAL
	if not ref_tar or ref_tar._use_delete_method then return end
	local reload_prefab = true
	if not ignore_pool then
		local obj_pool = self:GetObjFormPool(ref_tar, abName, pfName)
		if obj_pool then
			reload_prefab = false
			if callBack then
				callBack({[0] = obj_pool}, true)
			end
		end
	end
	if reload_prefab then
		self:addRefCount(ref_tar,abName)
		ResourceManager:LoadObject(abName, pfName, callBack, load_level)

		if OpenGameLuaLog then
			print("--------LoadObject-------",abName,pfName)
		end
	end

	self.has_use_abname[abName] = true
end

--加载一个材质
function LuaResManager:loadMateaial(ref_tar,abName,pfName,callBack, load_level)
	load_level = load_level or ASSETS_LEVEL.NORMAL
	if not ref_tar or ref_tar._use_delete_method then return end
	self:addRefCount(ref_tar,abName)
	ResourceManager:LoadMaterial(abName, pfName, callBack, load_level)

	if OpenGameLuaLog then
		print("--------LoadMaterial-------",abName,pfName)
	end

	self.has_use_abname[abName] = true
end

--加载一个精灵
function LuaResManager:loadSprite(ref_tar,abName,pfName,callBack,load_level)
	load_level = load_level or ASSETS_LEVEL.HIGHT
	self:addRefCount(ref_tar,abName)
	ResourceManager:LoadSprite(abName, pfName, callBack, load_level)

	if OpenGameLuaLog then
		print("--------LoadSprite-------",abName,pfName)
	end

	self.has_use_abname[abName] = true
end

--加载多个精灵
function LuaResManager:loadSprites(ref_tar,abName,pfNameList,callBack,load_level)
	load_level = load_level or ASSETS_LEVEL.HIGHT
	self:addRefCount(ref_tar,abName)

	ResourceManager:LoadSprites(abName, pfNameList, callBack, load_level)

	if OpenGameLuaLog then
		print("--------LoadSprites-------",abName)
	end

	self.has_use_abname[abName] = true
end

--加载一张纹理
function LuaResManager:loadTexture(ref_tar,abName,pfNameList,callBack,load_level)
	load_level = load_level or ASSETS_LEVEL.HIGHT
	self:addRefCount(ref_tar,abName)
	ResourceManager:LoadTexture(abName, pfNameList, callBack, load_level)

	if OpenGameLuaLog then
		print("--------LoadTexture-------",abName)
	end

	self.has_use_abname[abName] = true
end

--加载二进制数据
function LuaResManager:loadTextAsset(ref_tar,abName,pfName,callBack,load_level)
	load_level= load_level or ASSETS_LEVEL.NORMAL
	self:addRefCount(ref_tar,abName)
	ResourceManager:LoadTextAsset(abName, pfName, callBack,load_level)

	if OpenGameLuaLog then
		print("--------loadTextAsset-------",abName,pfName)
	end
end

--加载并播放声音
function LuaResManager:loadSound(ref_tar, abName, resName, is_loop,vol_modulus, speed,load_from_pool)
	local custom_speed = speed or 1 
	if abName ~= LuaSoundManager.SOUND_PRE[LuaSoundManager.SOUND_TYPE.SKILL] then
		--92 版本之后才使用缓存接口，常用声音不加入管理
		if tonumber(AppConst.EnglineVer) >= 92 then
			if not Config.ConfigSound.CacheSound[resName] then 
				self:addRefCount(ref_tar, abName)
			end
		else
			self:addRefCount(ref_tar, abName)
		end
	end

	if OpenGameLuaLog then
		print("--------loadSound-------",abName,resName)
	end

	self.has_use_abname[abName] = true

	if tonumber(AppConst.EnglineVer) >= 92 then
		load_from_pool = load_from_pool or false
		return soundMgr:PlayEffect(abName, resName, is_loop,vol_modulus, custom_speed,load_from_pool)
	elseif tonumber(AppConst.EnglineVer) >= 89 then
		return soundMgr:PlayEffect(abName, resName, is_loop,vol_modulus, custom_speed)
	else
		return soundMgr:PlayEffect(abName, resName, is_loop,vol_modulus)
	end
end

--加载外部二进制文件，目前只有db加载使用，是常驻内存的,不需引用计数
function LuaResManager:loadOutsideTextAsset(ref_tar, respath, callBack)
	outsideResourceManager:LoadTextAsset(respath, callBack,OutSideFileType.BYTE)
end

--停止一个声音
function LuaResManager:stopSound(ref_tar, abName, effect_id)
	if abName ~= LuaSoundManager.SOUND_PRE[LuaSoundManager.SOUND_TYPE.SKILL] then
	--	self:reduceRefCount(ref_tar, abName)
	end

	if effect_id then
		local res_id = tonumber(effect_id)
		if res_id then
			soundMgr:StopEffect(effect_id)
		else
			logWarn("stopSound error abName = " .. abName)
		end
	end
end

function LuaResManager:ResHasUseState( ab_name )
	return self.has_use_abname[ ab_name ]
end