
--需要处理例子特效的ui对象 需要继承该类 比如baseview  baseitem
UIPartical = UIPartical or BaseClass(UIZDepth)
local UIPartical = UIPartical

--每个layer的起始值
UIPartical.Start_SortingOrder_list = {
	["Scene"] = 0,
	["Main"] = 100,
	["Dynamic_Main"] = 150,
	["UI"] = 200,
	["Activity"] = 300,
	["Top"] = 400,
}

--每个layer的当前层数
UIPartical.curr_SortingOrder_list = {
	["Scene"] = UIPartical.Start_SortingOrder_list.Scene,
	["Main"] = UIPartical.Start_SortingOrder_list.Main,
	["Dynamic_Main"] = UIPartical.Start_SortingOrder_list.Dynamic_Main,
	["UI"] = UIPartical.Start_SortingOrder_list.UI,
	["Activity"] = UIPartical.Start_SortingOrder_list.Activity,
	["Top"] = UIPartical.Start_SortingOrder_list.Top,
}

UIPartical.NextLayerCount = 
{
	["Scene"] = 100,
	["Main"] = 200,
	["UI"] = 300,
	["Activity"] = 400,
}

--设置渲染特效所在的layer层
UIPartical.RenderingOther_List = {
	DEFAULT = 0,
	UI = 5,
	Blackground = 10,
}

function UIPartical:__init()
	self.layer_name = "Main"
	self.depth_counter = 0

	self.partical_list = {}
	self.callback_list = {}
	self.lastTime_list = {}
	self.cache_partical_map = {}
	self.show_partical_map = {}
	self.wait_load_effect_trans = {}
	self.speed = 1
end

function UIPartical:__delete()
	self:ClearAllEffect()
	self:ClearCacheEffect()
end

function UIPartical:ClearCacheEffect()
	for abname,state in pairs(self.cache_partical_map) do
		lua_resM:ClearObjPool(abname)
	end
	self.cache_partical_map = {}
	self.wait_load_effect_trans = {}
	self.show_partical_map = {}
end

function UIPartical:DeleteCurrLayerDepth(layer_name,count)
	if UIPartical.curr_SortingOrder_list[layer_name] then
		UIPartical.curr_SortingOrder_list[layer_name] = UIPartical.curr_SortingOrder_list[layer_name]  - count
	end

	if UIPartical.Start_SortingOrder_list[layer_name] then
	 	if self:GetCurrLayerDepth(layer_name) < UIPartical.Start_SortingOrder_list[layer_name] then
			UIPartical.curr_SortingOrder_list[layer_name] = UIPartical.Start_SortingOrder_list[layer_name]
		end
	end
end

function UIPartical:AddCurrLayerDepth(layer_name,count)
	if UIPartical.curr_SortingOrder_list[layer_name] then
		UIPartical.curr_SortingOrder_list[layer_name] = UIPartical.curr_SortingOrder_list[layer_name] + (count or 1)
	end

	if UIPartical.NextLayerCount[layer_name] and UIPartical.curr_SortingOrder_list[layer_name] >= UIPartical.NextLayerCount[layer_name] then
		LogError( self._source .. " want to add " .. layer_name .. " layer count to " .. UIPartical.curr_SortingOrder_list[layer_name] .. ":" .. debug.traceback())
		UIPartical.curr_SortingOrder_list[layer_name] = UIPartical.Start_SortingOrder_list[layer_name] + 99
	end 
end

function UIPartical:GetCurrLayerDepth(layer_name)
	return UIPartical.curr_SortingOrder_list[layer_name]
end

--打开界面的时候 设置UI SortingOrder深度
 function UIPartical:SetUIDepth(gameObject)
 	if self.layer_name and self.layer_name ~= "Main" then
 		if self:GetCurrLayerDepth(self.layer_name) < UIPartical.Start_SortingOrder_list[self.layer_name] then
 			UIPartical.curr_SortingOrder_list[self.layer_name] = UIPartical.Start_SortingOrder_list[self.layer_name]
 		end
 		self.depth_counter = self.depth_counter + 1
 		self:AddCurrLayerDepth(self.layer_name)
 		-- print("层级",self.layer_name,"===",self:GetCurrLayerDepth(self.layer_name))
 		UIManager.SetUIDepth(gameObject,true,self:GetCurrLayerDepth(self.layer_name))
 	end
 end

 function UIPartical:SetGameObjectDepth(gameObject)
 	if gameObject == nil then return end

 	if self.layer_name and self.layer_name ~= "Main" then
 		self.depth_counter = self.depth_counter + 1
 		self:AddCurrLayerDepth(self.layer_name)
 		UIManager.SetUIDepth(gameObject,true,self:GetCurrLayerDepth(self.layer_name))
 		gameObject.layer = LayerMask.NameToLayer("UI")
 	end
 end

--关闭界面的时候重置UI深度
 function UIPartical:ResetUIDepth(count)
 	if self.layer_name and (self.layer_name ~= "Main" or self.need_to_reset_layer) then
 		if count then
 			if count > 0 then
	 			self:DeleteCurrLayerDepth(self.layer_name,count )
	 			self.depth_counter = self.depth_counter - count
	 		end
 		else
	 		if self.depth_counter > 0 then
	 			self:DeleteCurrLayerDepth(self.layer_name,self.depth_counter )
	 			self.depth_counter = 0
	 		end
 		end
 	end
 end

--这里分为UI层跟其它层，UI层的粒子层级加1，而其他层级，例如主界面，则固定为1
 function UIPartical:SetParticleDepth(gameObject, forceAdd)
 	if self.layer_name  then
 		if self.layer_name == "Main" and not forceAdd then
 			local depth = UIPartical.Start_SortingOrder_list[self.layer_name] + 1
	 		if depth ~= nil then
	 			UIManager.SetUIDepth(gameObject,false,depth)
	 		end
 		else
	 		self.depth_counter = self.depth_counter + 1
	 		self:AddCurrLayerDepth(self.layer_name)
	 		UIManager.SetUIDepth(gameObject,false,self:GetCurrLayerDepth(self.layer_name))
 		end
 	end
 end

--添加屏幕特效
function UIPartical:AddScreenEffect(resname, pos, scale, is_loop ,last_time)
	local parent_go = UiFactory.createChild(panelMgr:GetParent(self.layer_name), UIType.EmptyObject, resname)
	local function call_back_func()
		destroy(parent_go)
	end
	self:AddUIEffect(resname, parent_go.transform, self.layer_name, pos, scale, is_loop ,last_time, nil, call_back_func)
end

--[[
	功能：添加到UI上的特效 如果是baseview里面设置特效 需要在opencallback方法里设置才行  baseitem需要再baseview调用opencallback的时候重新设置
	uiTranform 只能挂接一个特效
	其他. lastTime为-1时播放结束不主动销毁
	save_by_batch: 合并批处理，这个和useMask是互斥关系，如两个特效的resname相同时，不能一个用useMask另一个用save_by_batch。因为共享材质只有一个。
]]

function UIPartical:AddUIEffectConfig(resname, uiTranform, layer_name, config)
	config = config or {}
	local pos = config.pos
	local scale = config.scale or 1
	local is_loop = config.is_loop
	local last_time = config.last_time
	local useMask = config.useMask
	local call_back_func = config.call_back_func
	local load_finish_func = config.load_finish_func
	local speed = config.speed
	local layer = config.layer
	local not_delete_old_ps = config.not_delete_old_ps
	local save_by_batch = config.save_by_batch
	local need_cache = config.need_cache

	self:AddUIEffect(resname, uiTranform, layer_name, pos, scale, is_loop ,last_time, useMask, call_back_func,load_finish_func, speed, layer, not_delete_old_ps, save_by_batch, need_cache)
end


function UIPartical:AddUIEffect(resname, uiTranform, layer_name, pos, scale, is_loop ,last_time, useMask, call_back_func,load_finish_func, speed, layer, not_delete_old_ps, save_by_batch, need_cache,rotate)
	if uiTranform then
		local instance_id = uiTranform:GetInstanceID()
		if self.wait_load_effect_trans[instance_id] then
			return
		end
		self.layer_name = layer_name or self.layer_name
		pos = pos or Vector3.zero
		scale = scale
		if is_loop == nil then
			is_loop = true
		end

		if need_cache == nil then
			need_cache = true
		end

		local function load_call_back(objs, is_gameObject)
			self.wait_load_effect_trans[instance_id] = false
			if self.transform then --没有删除根变换
				if self._use_delete_method then
					return
				end
				if IsNull(uiTranform) then return end

				--如果没特效，则返回
				if not objs or not objs[0] or tostring(objs[0]) == "null" then
					return
				end

				local curr_effect_sortingOrder = nil
				if self.layer_name and self.layer_name ~= "Main" and not useMask then
					self.depth_counter = self.depth_counter + 1
				 	self:AddCurrLayerDepth(self.layer_name)
					curr_effect_sortingOrder = self:GetCurrLayerDepth(self.layer_name)
				end

				local go = is_gameObject and objs[0] or newObject(objs[0])

				local shader_mask = UIParticleMaskShader.Res[resname]
				if shader_mask then
					local objs = go:GetComponentsInChildren(typeof(UnityEngine.Renderer))
					for i=1,objs.Length do
						local mats = objs[i-1].sharedMaterials
						for i=0, mats.Length - 1 do
							if mats[i] then
								local shader_config = UIParticleMaskShader.Shader[mats[i].shader.name]
								if shader_config then
									mats[i].shader = ShaderTools.GetShader(shader_config)
								end
							end
						end
					end
				end

				self.partical_list[go] = true

				local gameobject_id = go:GetInstanceID()
				self.show_partical_map[gameobject_id] = resname

				local transform = go.transform
				transform:SetParent(uiTranform)
				transform.localPosition = pos
				if rotate then
					transform.localRotation = rotate
				end
				if type(scale) == "number" then
					if scale ~= -1 then
						if not is_gameObject or (Scene and Scene.Instance:IsPreLoadPoolEffect(resname)) then

							local particleSystems = go:GetComponentsInChildren(typeof(UnityEngine.ParticleSystem))
							if particleSystems and scale ~= 1 then
								for i = 0, particleSystems.Length - 1 do
									particleSystems[i].main.scalingMode = UnityEngine.ParticleSystemScalingMode.IntToEnum(2)
								end
							end

							cs_particleM:SetScale(go,scale)
						end
						transform.localScale = 	Vector3.one * scale * 100
					end
				elseif type(scale) == "table" then
					local particleSystems = go:GetComponentsInChildren(typeof(UnityEngine.ParticleSystem))
					if scale.z == nil then scale.z = 1 end
					local orign_scale
					local symbol = {x = 1,y = 1,z = 1}
					for i = 0, particleSystems.Length - 1 do
						local ps = particleSystems[i]
						ps.main.scalingMode = UnityEngine.ParticleSystemScalingMode.IntToEnum(1)
						orign_scale = ps.transform.localScale
						symbol.x = orign_scale.x > 0 and 1 or -1
						symbol.y = orign_scale.y > 0 and 1 or -1
						symbol.z = orign_scale.z > 0 and 1 or -1
						ps.transform.localScale = Vector3(scale.x * symbol.x, scale.y * symbol.y, scale.z * symbol.z)
					end
					transform.localScale = Vector3(scale.x, scale.y, scale.z) * 100
				else
					transform.localScale = Vector3(72,72,72)
				end

				PrintParticleInfo(go)

				self:SetSpeed(go, speed)
				go:SetActive(false)
				go:SetActive(true)
				self:SetUILayer(uiTranform, layer)
			
				if useMask then
					self:SetEffectMask(go)
				else
					if curr_effect_sortingOrder then
			 			UIManager.SetUIDepth(go,false,curr_effect_sortingOrder)
					else
						self:SetParticleDepth(go, maskID)
					end
					if save_by_batch then
						--需要动态批处理，设置共享材质
						local renders = go:GetComponentsInChildren(typeof(UnityEngine.Renderer))
						for i = 0, renders.Length - 1 do
						    local mats = renders[i].sharedMaterials
							for j = 0, mats.Length - 1 do
								mats[j]:SetFloat("_Stencil", 0)
							end
						end
						local curTrans = go.transform.parent
						local maskImage
						while(curTrans and curTrans.name ~= "Canvas") do
							maskImage = curTrans:GetComponent("Mask")
							if maskImage then
								break
							else
								curTrans = curTrans.parent
							end
						end
						if maskImage then
							local canvas = curTrans:GetComponent("Canvas")
							if canvas then
								UIManager.SetUIDepth(go,false,canvas.sortingOrder + 1)
							end
						end
					else
						--由于使用遮罩的材质调用了共享材质，这里对不使用遮罩的特效使用实例化材质
						local renders = go:GetComponentsInChildren(typeof(UnityEngine.Renderer))
						for i = 0, renders.Length - 1 do
						    local mats = renders[i].materials
							for j = 0, mats.Length - 1 do
								mats[j]:SetFloat("_Stencil", 0)
							end
						end
					end
				end

				self.callback_list[go] = call_back_func
				if last_time and last_time > 0 and not self.lastTime_list[go] then
					local function onDelayFunc()
						self.show_partical_map[gameobject_id] = nil
						self:PlayEnd(go,need_cache,resname)
						if not need_cache and not self._use_delete_method then
							lua_resM:reduceRefCount(self, resname)
						end
					end
					self.lastTime_list[go] = GlobalTimerQuest:AddDelayQuest(onDelayFunc,last_time)
				end
				if is_loop then
					cs_particleM:SetLoop(go,is_loop)
				elseif last_time ~= -1 and not self.lastTime_list[go] then
					local function playEndCallback(go)
						self.show_partical_map[gameobject_id] = nil
						self:PlayEnd(go,need_cache,resname)
						if not need_cache and not self._use_delete_method then
							lua_resM:reduceRefCount(self, resname)
						end
					end
					ParticleManager:getInstance():AddUIPartical(go,playEndCallback)
				end

				if load_finish_func then
					load_finish_func(go)
				end

				ClearTrailRenderer(go)
			end
		end

		if not not_delete_old_ps then
			self:ClearUIEffect(uiTranform)
		end
		
		self.wait_load_effect_trans[instance_id] = true
		lua_resM:loadPrefab(self,resname,resname, load_call_back,false,ASSETS_LEVEL.HIGHT)
	end
end

function UIPartical:SetSpeed(go, speed)
	self.speed = speed or self.speed
	if self.partical_list[go] and self.speed then
		cs_particleM:SetSpeed(go,self.speed)
	end
end

--清除挂接在父容器的所有对象, 判断进入对象缓存池
function UIPartical:ClearUIEffect(uiTranform)
	if not IsNull(uiTranform) then
		for i = 0,uiTranform.childCount - 1 do
			local go = uiTranform:GetChild(0).gameObject
			local cache_res = self.show_partical_map[go:GetInstanceID()]
			self:PlayEnd(go,cache_res,cache_res)
		end
	else
		PrintCallStack()
	end
end

--删除所有特效
function UIPartical:ClearAllEffect()
	self:ResetUIDepth()
	for go,callback in pairs(self.callback_list) do
		callback()
		self.partical_list[go] = nil
	end

	for go,last_time_id in pairs(self.lastTime_list) do
		GlobalTimerQuest:CancelQuest(last_time_id)
		self.lastTime_list[go] = nil
	end
	for go,_ in pairs(self.partical_list) do
		ParticleManager:getInstance():RemoveUIPartical(go)

		local gomeobject_id = go:GetInstanceID()
		local cache_res = self.show_partical_map[gomeobject_id]
		if cache_res then
			self.show_partical_map[gomeobject_id] = nil
			if not IsNull(go) then
				go:SetActive(false)
				lua_resM:AddObjToPool(self, cache_res, cache_res, go)
				self.cache_partical_map[cache_res] = true
			end
		else
			destroy(go,true)
		end
		self.partical_list[go] = nil
	end
end


--单个特效播放结束
function UIPartical:PlayEnd(go,need_cache,resname)
	if go then
		local callback = self.callback_list[go]
		if callback and not self._use_delete_method then
			callback()
		end
		self.callback_list[go] = nil

		local last_time_id = self.lastTime_list[go]
		if last_time_id then
			GlobalTimerQuest:CancelQuest(last_time_id)
		end
		self.lastTime_list[go] = nil

		ParticleManager:getInstance():RemoveUIPartical(go)

		self.partical_list[go] = nil

		if need_cache and resname then
			self.cache_partical_map[resname] = true
			go:SetActive(false)
			lua_resM:AddObjToPool(self, resname, resname, go)
		else
			destroy(go,true)
		end

		if self.layer_name ~= "Main" and self.depth_counter > 0 and not self.dontCleardepth then
			self.depth_counter = self.depth_counter - 1
			self:DeleteCurrLayerDepth(self.layer_name, 1)
		end
	end
end

function UIPartical:SetUILayer(obj, layer)
	layer = layer or UIPartical.RenderingOther_List.UI
	if IsNull(obj) then
		return
	end
	for i = 0,obj.childCount - 1 do
		obj:GetChild(i).gameObject.layer = layer
		self:SetUILayer(obj:GetChild(i), layer)
	end
end

function UIPartical:SetEffectMask(obj)
	local renders = obj:GetComponentsInChildren(typeof(UnityEngine.Renderer))
	for i = 0, renders.Length - 1 do
	    --使用Renderer.material获取Material引用时,会把Render里Materials列表第一个预设的Material进行实例，这样每个object都有各自的材质对象，无法实现批处理draw call
		local mats = renders[i].sharedMaterials
		for j = 0, mats.Length - 1 do
			if mats[j] then
				mats[j]:SetFloat("_Stencil", 1)
			end
		end
	end
	local curTrans = obj.transform.parent
	local maskImage
	while(curTrans and curTrans.name ~= "Canvas") do
		maskImage = curTrans:GetComponent("Mask")
		if maskImage then
			break
		else
			curTrans = curTrans.parent
		end
	end
	if maskImage then
		local canvas = curTrans:GetComponent("Canvas")
		if canvas then
			UIManager.SetUIDepth(obj,false,canvas.sortingOrder + 1)
		end
	end
end

function UIPartical:RegisterMask(maskImage,bool)
	self.dontCleardepth = bool
	self:ApplyMaskID(maskImage, 1)
	self.need_to_reset_layer = true
	return 1
end

function UIPartical:ApplyMaskID(maskImage, maskID)
	local function callback(objs)
		if objs and objs[0] and not IsNull(maskImage) then
			maskImage.material = Material.New(objs[0])
			maskImage.material:SetFloat("_StencilID", maskID)
		end
	end

	ResourceManager:LoadMaterial("scene_material", "mat_mask_image" , callback, ASSETS_LEVEL.HIGHT)

	maskImage.gameObject.layer = UIPartical.RenderingOther_List.UI
	self:SetUILayer(maskImage.transform)
	self.depth_counter = self.depth_counter + 1
	self:AddCurrLayerDepth(self.layer_name)

	local oldMask = maskImage:GetComponent("Mask")
	local showMaskGraphic = oldMask.showMaskGraphic
	UnityEngine.Object.DestroyImmediate(oldMask)

	local newMask = maskImage.gameObject:AddComponent(typeof(EffectMask))
	newMask.showMaskGraphic = showMaskGraphic
	UIManager.SetUIDepth(maskImage.gameObject,true,self:GetCurrLayerDepth(self.layer_name))

	if self.stencilGo then
		return
	end

	self.stencilGo = UiFactory.createChild(maskImage.transform.parent,UIType.ImageExtend,"StencilImage")
	self.stencilGo:SetActive(true)

	local stencilTrans,img,rect = self.stencilGo.transform, self.stencilGo:GetComponent("Image"),maskImage.transform.rect
	stencilTrans.localPosition = Vector2(rect.center.x, rect.center.y, 0)
	stencilTrans:SetSiblingIndex(maskImage.transform:GetSiblingIndex() + 1)

	local sizeDelta = maskImage.transform.sizeDelta
	stencilTrans.sizeDelta = Vector2(rect.width, rect.height)

	img.alpha = 0
	img.raycastTarget = false
	img.material:SetFloat("_Stencil", 1)
	img.material:SetFloat("_StencilOp", 1)
	img.material:SetFloat("_StencilComp",8)

	local canvas = self.stencilGo:AddComponent(typeof(UnityEngine.Canvas))
	canvas.overrideSorting = true

	self:AddCurrLayerDepth(self.layer_name)
	self:AddCurrLayerDepth(self.layer_name)
	self.depth_counter = self.depth_counter + 2
	canvas.sortingOrder = self:GetCurrLayerDepth(self.layer_name)
end

function UIPartical:UnRegisterMask(maskID)
	self.stencilGo = nil
end
