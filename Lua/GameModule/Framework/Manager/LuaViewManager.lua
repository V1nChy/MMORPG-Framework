--[[@------------------------------------------------------------------
说明: lua端的单例界面管理器
作者: VinChy
----------------------------------------------------------------------]]

LuaViewManager = LuaViewManager or BaseClass(nil, true)
local LuaViewManager = LuaViewManager

local rawget = rawget
local Time = Time
local IsTableEmpty = IsTableEmpty
LuaViewManager.CHANGE_MAIN_CANVAS_VISIBLE = "LuaViewManager.CHANGE_MAIN_CANVAS_VISIBLE"
LuaViewManager.SHORT_CHANGE_MAIN_CANVAS_VISIBLE = "LuaViewManager.SHORT_CHANGE_MAIN_CANVAS_VISIBLE"
LuaViewManager.HIDE_ITEM_USE_VIEW = "LuaViewManager.HIDE_ITEM_USE_VIEW"
LuaViewManager.PUSH_VIEWS_OUT_SCREEN = "LuaViewManager.PUSH_VIEWS_OUT_SCREEN"

LuaViewManager.BOCAST_MAIN_CANVAS_LAST_STATE = "LuaViewManager.BOCAST_MAIN_CANVAS_LAST_STATE"

function LuaViewManager:__init()
	self.view_id = 0
	self.view_ctl_queue = {}

	self.update_main_canvas_time = 0

	self.maskIdPool = {
		maxID = 1,
		list = {},
		Pop = function(self)
			local length = #self.list
			if length > 0 then
				local result = self.list[length]
				table.remove(self.list, length)
				return result
			else
				if self.maxID == 255 then
					logWarn("MaskID分配已到达上限!!")
					return nil
				else
					self.maxID = self.maxID + 2
					return self.maxID - 2
				end
			end
		end,
		Push = function(self, mid)
			table.insert(self.list, mid)
		end,
	}

	self.show_loading_view_list = {} --显示loading的界面列表
	self.show_loading_step = 0

	self.delay_ui_list = Array.New() --延迟加载ui队列
	--Runner.Instance:AddRunObj(self, 1)
end

function LuaViewManager:Update(now_time, elapse_time)
	if self.update_visible and now_time - self.update_main_canvas_time > 0 then
		self.update_visible = false
		if self:NoDependHideView() then
			if not self.main_cancas_last_visible then
				self.main_cancas_last_visible = true
				-- local len = self.main_cancas_con.childCount - 1
				-- local list = {}
				-- for i = 0, len do
				-- 	table.insert(list, self.main_cancas_con:GetChild(i))
				-- end
				-- for i, child in ipairs(list) do
				-- 	child:SetParent(self.main_cancas)
				-- end

				self.main_cancas_con_go:SetActive(true)
				if SupportDymanicMainLayer() and self.Dynamic_Main_con_canvas then
					self.Dynamic_Main_con_canvas.enabled = true
				end
				--主聊天界面单独处理  先显示后隐藏，不然有bug
				GlobalEventSystem:Fire(EventName.HIDE_MAIN_CHAT_VIEW, true, MainUIModel and MainUIModel.OTHER_MODE2)
				GlobalEventSystem:Fire(EventName.HIDE_MAIN_CHAT_VIEW, false, MainUIModel and MainUIModel.OTHER_MODE2)

				-- if not self.is_lock_screen then
				-- end
				self:ChangeMainCanvasVisible()
				-- panelMgr:GetParent("Main").gameObject:SetActive(true)
			end
		else
			if self.main_cancas_last_visible then
				self.main_cancas_last_visible = false
				local len = self.main_cancas.childCount - 1
				local list = {}
				for i = 0, len do
					if self.main_cancas:GetChild(i) == self.main_cancas_con then
						-- Message.show("==============")
					else
						table.insert(list, self.main_cancas:GetChild(i))
					end
				end
				for i, child in ipairs(list) do
					child:SetParent(self.main_cancas_con)
				end

				self.main_cancas_con_go:SetActive(false)
				if SupportDymanicMainLayer() and self.Dynamic_Main_con_canvas then
					self.Dynamic_Main_con_canvas.enabled = false
				end
				-- GlobalEventSystem:Fire(EventName.HIDE_MAIN_CHAT_VIEW, true, MainUIModel and MainUIModel.OTHER_MODE2)
				-- if not self.is_lock_screen then
				-- end
				-- panelMgr:GetParent("Main").gameObject:SetActive(false)
				self:ChangeMainCanvasVisible()
			end
		end
	end

	local size = self.delay_ui_list:GetSize()
	if size > 0 then
		local call_back
		if size <= 10 then
			call_back = self.delay_ui_list:PopFront()
			if call_back then
				call_back()
			end
		else
			size = math.ceil(size * 0.1)
			for i = 1, size do
				call_back = self.delay_ui_list:PopFront()
				if call_back then
					call_back()
				end
			end
		end
	end

	-- if self.show_loading_step % 13 == 0 then
	-- 	self:CheckLoadingState(now_time)
	-- end
	-- self.show_loading_step = self.show_loading_step + 1
end

function LuaViewManager:LoadView(ref_tar,abName,pfName,layerName,callBack)
	lua_resM:addRefCount(ref_tar,abName)
	UIManager:CreateView(abName,pfName,layerName,callBack)
end

function LuaViewManager:OpenView(ref_tar)
	self.cur_layout = ref_tar.layout_file  --用于引导
	self.cur_use_background = ref_tar.use_background
	self.cur_layer_name = ref_tar.layer_name
	if ref_tar.append_to_ctl_queue and self.view_ctl_queue[ref_tar] == nil then
		for view, _ in pairs(self.view_ctl_queue) do
			view:Hide()
		end
		self.view_id = self.view_id + 1
		self.view_ctl_queue[ref_tar] = self.view_id
	end
end

function LuaViewManager:CloseView(ref_tar)
	self.last_use_background = ref_tar.use_background
	if ref_tar.append_to_ctl_queue then
		self.view_ctl_queue[ref_tar] = nil

		local max_id = 0
		local max_view = nil
		for view, id in pairs(self.view_ctl_queue) do
			if id > max_id then
				max_id = id
				max_view = view
			end
		end
		if max_view then
			max_view:CancelHide()
		end
	end
	if Scene and Scene.Instance and TableSize(self.view_ctl_queue) == 0 then
		Scene.Instance:ChangeFogEnable(true)
	end
end

function LuaViewManager:DestroyView(gameObject)
	-- panelMgr:CloseView(gameObject)
	destroy(gameObject, true)
end

function LuaViewManager:LoadItem(ref_tar,abName,pfName,callBack)
	lua_resM:addRefCount(ref_tar,abName)
	if ref_tar.use_local_view and ApplicationPlatform == RuntimePlatform.WindowsEditor then
		local view_path = "root/Canvas/" .. pfName
		local go = GameObject.Find(view_path)
		if go then
			local item = newObject(go)
			item.layer = LayerMask.NameToLayer("UI")
	        item.gameObject:SetActive(true)
	        callBack(item)
	     else
	     	Message.show("找不到本地界面:" .. view_path)
	     end
	else
		panelMgr:LoadItem(abName,pfName,callBack)
	end
	-- local function onLoadItemCallback(objs)
	-- 	if not ref_tar._use_delete_method then
	-- 		local go = nil
	-- 		if objs and objs[0] then
	-- 			go = newObject(objs[0])
 --                --go.name = assetName;

 --                go.layer = UIPartical.RenderingOther_List.UI
	-- 			--go.transform:SetParent(panelMgr:GetParent(layerName))
 --                -- go.transform.localScale = Vector3.one
 --                -- go.transform.localPosition = Vector3.zero
	-- 		end
	-- 		if callBack then
	-- 			callBack(go)
	-- 		end
	-- 		-- lua_resM:ImmeUnLoadAB(abName)
	-- 	end
	-- end
	-- lua_resM:loadPrefab(ref_tar,abName,pfName,onLoadItemCallback, nil, ASSETS_LEVEL.HIGHT)
end

function LuaViewManager:ClearItem(gameObject)
	destroy(gameObject,true)
	-- panelMgr:ClearItem(gameObject)
end

function LuaViewManager:ClearTimer()
	if self.hide_hook_role_timer_id then
		GlobalTimerQuest:CancelQuest(self.hide_hook_role_timer_id)
		self.hide_hook_role_timer_id = nil
	end

	if self.show_hook_role_timer_id then
		GlobalTimerQuest:CancelQuest(self.show_hook_role_timer_id)
		self.show_hook_role_timer_id = nil
	end
end

function LuaViewManager:ChangeLockScreenState()
	if Scene then
		local main_role = Scene:getInstance():GetMainRole()
		if self.is_lock_screen then
			if main_role then
				if main_role.sprite and not main_role.sprite:HasModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2) then
					main_role.sprite:SetModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2, true)
				end
				if main_role.pet and not main_role.pet:HasModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2) then
					main_role.pet:SetModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2, true)
				end
				if main_role.baby and not main_role.baby:HasModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2) then
					main_role.baby:SetModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2, true)
				end
				if main_role.follow_partner and not main_role.follow_partner:HasModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2) then
					main_role.follow_partner:SetModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2, true)
				end

				if main_role.star_soul and not main_role.star_soul:HasModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2) then
					main_role.star_soul:SetModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2, true)
				end

				local assist_star_list = Scene.Instance.assist_star_soul_list

				for _, list in pairs(assist_star_list) do
					for _, assist_star in pairs(list) do
						if not assist_star:HasModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2) then
			           		assist_star:SetModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2, true)
			            end
					end
				end

				local partner_list = Scene.Instance.partner_list
				for _,partner in pairs(partner_list) do
					partner:SetModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2, true)
				end

				main_role:RemoveLivenessImage()
			end
			MainCamera.Instance:SetScreenEffectCamereState(false)
			GlobalEventSystem:Fire(SceneEventType.HIDE_ALL_MONSTER)
			local hook_role_list = Scene.Instance.hook_role_list
			if TableSize(hook_role_list) > 0 then
				local list = {}
				for _,hook_role in pairs(hook_role_list) do
					table.insert(list,hook_role)
				end

				if self.hide_hook_role_timer_id then
					GlobalTimerQuest:CancelQuest(self.hide_hook_role_timer_id)
					self.hide_hook_role_timer_id = nil
				end

				local index = 0
				local max_index = #list
				local function onTimer()
					index = index + 1
					local hook_role = list[index]
					if hook_role then
						hook_role:SetModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2, true)
						hook_role:DoStand()
					end

					if index >= max_index then
						if self.hide_hook_role_timer_id then
							GlobalTimerQuest:CancelQuest(self.hide_hook_role_timer_id)
							self.hide_hook_role_timer_id = nil
						end
					end
				end
				self.hide_hook_role_timer_id = GlobalTimerQuest:AddPeriodQuest(onTimer,0.1)
			end
		else
			if main_role then
				if main_role.sprite and main_role.sprite:HasModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2) then
					main_role.sprite:SetModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2, false)
				end
				if main_role.pet and main_role.pet:HasModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2) then
					main_role.pet:SetModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2, false)
				end
				if main_role.baby and main_role.baby:HasModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2) then
					main_role.baby:SetModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2, false)
				end

				if main_role.follow_partner and main_role.follow_partner:HasModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2) then
					main_role.follow_partner:SetModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2, false)
				end

				if main_role.star_soul and main_role.star_soul:HasModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2) then
					main_role.star_soul:SetModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2, false)
				end

				local assist_star_list = Scene.Instance.assist_star_soul_list
				for _, list in pairs(assist_star_list) do
					for _, assist_star in pairs(list) do
						if assist_star:HasModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2) then
			           		assist_star:SetModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2, false)
			           end
					end
				end

				local partner_list = Scene.Instance.partner_list
				for _,partner in pairs(partner_list) do
					partner:SetModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2, false)
				end


				main_role:ShowLivenessImage()
			end
			MainCamera.Instance:SetScreenEffectCamereState(true)
			GlobalEventSystem:Fire(SceneEventType.SHOW_ALL_MONSTER)

			local hook_role_list = Scene.Instance.hook_role_list
			if TableSize(hook_role_list) > 0 then
				local list = {}
				for _,hook_role in pairs(hook_role_list) do
					table.insert(list,hook_role)
				end

				if self.show_hook_role_timer_id then
					GlobalTimerQuest:CancelQuest(self.show_hook_role_timer_id)
					self.show_hook_role_timer_id = nil
				end

				local index = 0
				local max_index = #list
				local function onTimer()
					index = index + 1
					local hook_role = list[index]
					if hook_role then
						hook_role:SetModelHideFlag(SceneObj.ModelHideFlag.IndependentHide2, false)
					end
					if index >= max_index then
						if self.show_hook_role_timer_id then
							GlobalTimerQuest:CancelQuest(self.show_hook_role_timer_id)
							self.show_hook_role_timer_id = nil
						end
					end
				end
				self.show_hook_role_timer_id = GlobalTimerQuest:AddPeriodQuest(onTimer,0.1)
			end
		end
		GlobalEventSystem:Fire(SceneEventType.UPDATE_ROLE_LIMIT)
	end
end

function LuaViewManager:ChangeMainCanvasVisible()
	--if Scene then
		-- local main_role = Scene:getInstance():GetMainRole()
		-- if main_role then
		-- 	if self.main_cancas_last_visible then
		-- 		if main_role:HasModelHideFlag(SceneObj.ModelHideFlag.PerformanceHide) then
		-- 			main_role:SetModelHideFlag(SceneObj.ModelHideFlag.PerformanceHide,false)
		-- 			main_role:ReplayAction()
		-- 			main_role:SetRealPos(RoleManager.Instance.mainRoleInfo.pos_x, RoleManager.Instance.mainRoleInfo.pos_y)
		-- 			if main_role.ShowLivenessImage then
		-- 				main_role:ShowLivenessImage()
		-- 			end
		-- 		end
		-- 	else
		-- 		if not main_role:HasModelHideFlag(SceneObj.ModelHideFlag.PerformanceHide) then
		-- 		 	main_role:SetModelHideFlag(SceneObj.ModelHideFlag.PerformanceHide,true)
		-- 		end
		-- 	end
		-- end
		self:Fire(LuaViewManager.BOCAST_MAIN_CANVAS_LAST_STATE,self.main_cancas_last_visible)
	-- end
end

--放入main层的隐藏父类里
function LuaViewManager:PuskMainCanvasCon(transform)
	if transform and self.main_cancas_con then
		transform:SetParent(self.main_cancas_con)
	end
end

function LuaViewManager:GetFrameUpdateCount()
	if self.main_cancas_last_visible then
		return 1
	else
		return 3
	end
end

--添加进入延迟队列
function LuaViewManager:AddDelayQueue(call_back)
	self.delay_ui_list:PushBack(call_back)
	return call_back
end

--移除
function LuaViewManager:RemoveDelayQueue(call_back)
	local index = self.delay_ui_list:IndexOf(call_back)
	if index and index > 0 then
		self.delay_ui_list:Erase(index - 1)
	end
end

function LuaViewManager:PushViewsOutScreen()
	local len = self.main_cancas.childCount - 1
	local list = {}
	for i = 0, len do
		if self.main_cancas:GetChild(i) == self.main_cancas_con then
			-- Message.show("==============")
		else
			table.insert(list, self.main_cancas:GetChild(i))
		end
	end
	for i, child in ipairs(list) do
		child:SetParent(self.main_cancas_con)
	end
	
	--为了解决聊天框表情错乱问题。是由于重设了父对象，没更新网格
	ChatModel:getInstance():Fire(ChatModel.HIDE_MIAN_UI_CHAT_VIEW,false)
	SetAnchoredPosition(self.main_cancas_con,50000,0)
	self:Fire(LuaViewManager.PUSH_VIEWS_OUT_SCREEN,true)
end

function LuaViewManager:PopViewsInScreen()
	SetAnchoredPosition(self.main_cancas_con,0,0)
	self:Fire(LuaViewManager.PUSH_VIEWS_OUT_SCREEN,false)
end

function LuaViewManager:NoDependHideView()
	if IsTableEmpty(self.main_canvas_hideView_list) then
		return true
	end

	for view,state in pairs(self.main_canvas_hideView_list) do
		if view.gameObject and not IsNull(view.gameObject) then
			if view.gameObject.activeInHierarchy then
				return false
			end
		else
			if view == "login_view" then
				return false
			else
				print("------------NoDependHideView: view gameObject is null: ------------",view._source or "empty name")
				return true
			end
		end
	end

	return true
end

--检测loading状态
function LuaViewManager:CheckLoadingState(now_time)
	local delete_list = nil
	local show_loading = false
	now_time = now_time or Status.NowTime
	for id, info in pairs(self.show_loading_view_list) do
		if now_time >= info.hide_time then
			delete_list = delete_list or {}
			table.insert(delete_list, id)
		elseif not show_loading and now_time >= info.start_show_time and now_time <= info.hide_time then
			show_loading = true
		end
	end

	if delete_list then
		for i, id in ipairs(delete_list) do
			self.show_loading_view_list[id] = nil
		end
	end
	if show_loading then
		self:ShowLoading()
	else
		self:CloseLoading()
	end
end
--显示loading
function LuaViewManager:ShowLoading()
	if not self.loadingView then 
		self.loadingView = LoadingView.New()
	end
	if not self.loadingView:HasOpen() then
		self.loadingView:Open()	
	end
end

--隐藏loading
function LuaViewManager:CloseLoading()
	if self.loadingView and self.loadingView:HasOpen() == true then
		self.loadingView:Close()
	end
end

--添加需要显示loading进度的界面对象
function LuaViewManager:AddLoadingView(id)
	local info = {start_show_time = Status.NowTime + 0.6, hide_time = Status.NowTime + 8}
	self.show_loading_view_list[id] = info
end

--移除loading界面对象
function LuaViewManager:RemoveLoadingView(id)
	self.show_loading_view_list[id] = nil
	self:CheckLoadingState()
end

function LuaViewManager:GetMaskID()
	return self.maskIdPool:Pop()
end

function LuaViewManager:ReleaseMaskID(mid)
	return self.maskIdPool:Push(mid)
end

function LuaViewManager:GetViewNum(  )
	return TableSize(self.view_ctl_queue)
end
