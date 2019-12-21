BaseView = BaseView or BaseClass(UIPartical)
local BaseView = BaseView

local LuaViewManager = LuaViewManager
local lua_viewM = lua_viewM
local GlobalTimerQuest = GlobalTimerQuest
local SetLocalScale = SetLocalScale
local destroy = destroy
local GameObject = GameObject
local UiFactory = UiFactory
local UIZDepth = UIZDepth
local GlobalEventSystem = GlobalEventSystem
local AddClickEvent = AddClickEvent
local EventSystem = EventSystem
local GetChildTransforms = GetChildTransforms
local GetChildGameObjects = GetChildGameObjects
local GetChildImages = GetChildImages
local GetChildTexts = GetChildTexts

BaseView.Class_Type = "BaseView"
BaseView.OpenEvent = "BaseView.OpenEvent"
BaseView.CloseEvent = "BaseView.CloseEvent"
BaseView.DestroyEvent = "BaseView.DestroyEvent"
BaseView.CreateView = "BaseView.CreateView"

OpenMode = {
	OpenOnly = 1,
	OpenToggle = 2,

	OpenDefault = 1,
}

CloseMode = {
	CloseVisible = 1,
	CloseDestroy = 2,
	CloseDefault = 2,
}

function BaseView:__init()
	GlobalEventSystem:FireNextFrame(BaseView.CreateView,self)
end

function BaseView:__defineVar()
	return {
		_class_type = self,						-- 界面类型，通过判断_class_type.Class_Type识别baseview baseitem
		_iid = _in_obj_ins_id,					-- 界面对象标识id
		_use_delete_method = false,				-- 是否调用过delete函数
		base_file = ""	,						-- 模块资源名称
		layout_file = "",						-- 界面预设名字
		preLoad_list = false,					-- 打开界面之前需要预加载的资源,除了本模块UI资源外的
		hide_maincancas = true,					-- 是否隐藏主界面, 设置ture时,use_background也必须设置true
		use_background = false,					-- 窗口背景,用灰色半透明背景
		background_alpha = 0.8,					-- 窗口背景的透明度
		click_bg_toClose = false,				-- 点击背景是否关闭该窗口
		background_wnd = false,					-- 背景蒙版窗口
		destroy_imm = true,						-- 销毁窗口时采用立即销毁模式
		delay_delete_time = 3,  				-- 非立即销毁的界面，关闭后延迟销毁的等待时间
		change_scene_close = false,				-- 切换场景是否关闭
		reenter_scene_close = false,			-- 重新进入场景是否关闭
		change_scene_hide = false, 				-- 切换场景是否隐藏(不与change_scene_close、reenter_scene_close同时使用)
		preparing_res = false,					-- 异步加载进行中
		is_delete = false	,					-- 是否已经删除
		is_set_order_sorting = true, 			-- 是否需要设置深度,自动管理 canvas.sortingOrder
		is_set_zdepth = false, 					-- 是否设置z深度，当需要盖住下层的UI界面上的模型时，要设true
		open_mode = OpenMode.OpenDefault,		-- 打开模式
		close_mode = CloseMode.CloseDefault,	-- 关闭模式
		hide_item_use_view = false, 			-- 打开的时候是否关闭itemuseview
		isPop = false,							-- 是否可见
		wait_for_hide = false,					-- 等待加载完隐藏
		layer_name = "Main",					-- 窗口层次
		append_to_ctl_queue = false,    		-- 添加进入控制队列，打开下层界面时自动关闭上层，关闭下层自动打开上层
		is_delay_callback = true,   			-- 是否延迟调用load_callback和open_callback
		load_callback = false,					-- 加载的回调函数
		open_callback = false,					-- 窗口打开时回调
		close_callback = false,					-- 窗口关闭时回调
		destroy_callback = false,				-- 销毁的回调函数
		show_loading = false,					-- 是否显示加载界面
		is_loaded = false,						-- 是否已经加载完
		cache_findChild = false,				-- 缓存FindChild的接口
		gameObject = false,						-- ugui窗口对象
		transform = false,						-- 窗口transform
		use_local_view = false,   				-- 使用本地界面模式，将预设挂接在编辑器对应的canva层次上才能开启
		is_guide_module = false,				--是否是新手引导模块
		is_hide_skill_black_bg = false,			--是否隐藏羁绊技能的黑色遮罩
		ReOpen_callback = nil, --重新打开界面,刷新事件
		close_fog = nil, --开启界面的时候是否关闭场景FOG效果（如果出现场景中模型or特效和UI上效果显示不一样，开启）使用时候需要 use_background == true 
	}
end

--打开界面之前需要预加载的资源
function BaseView:AddPreLoadList(abName, assetNameList, type)
	if not self.preLoad_list then
		self.preLoad_list = {}
	end
	table.insert(self.preLoad_list,{type, abName, assetNameList})
end

--[[
获取子对象的transform
特别注意此方法是从根节点开始搜寻子对象 所以要写全路径 比如："input/Text"
]]
function BaseView:GetChild(name)
	return self.transform:Find(name)
	--[[if self.cache_findChild and self.transform then
		return self.cache_findChild(self.transform,name)
	end--]]
end

function BaseView:GetChildComponent(name,com)
	return self:GetChild(name):GetComponent(com)
end

--获取多个孩子transform
function BaseView:GetChildTransforms(names)
	return GetChildTransforms(self.transform, names)
end

--获取多个孩子go
function BaseView:GetChildGameObjects(names)
	return GetChildGameObjects(self.transform, names)
end

--获取多个孩子image
function BaseView:GetChildImages(names)
	return GetChildImages(self.transform, names)
end

--获取多个孩子text
function BaseView:GetChildTexts(names)
	return GetChildTexts(self.transform, names)
end

--是否真正打开  判断依据是调用了open以及资源加载已经完成
function BaseView:HasOpen()
	return self.isPop and self.is_loaded and not self.wait_for_hide
end

function BaseView:Open()
	self.isPop = true
	self.wait_for_hide = false
	self._use_delete_method = false
	self:RemoveDestroyTimer()

	if self.open_mode == OpenMode.OpenOnly then
		BaseView.OpenOnly(self)
	elseif self.open_mode == OpenMode.OpenToggle then
		BaseView.OpenToggle(self)
	end
end

function BaseView:OpenOnly()
	self.is_delete = false
	if self.preparing_res then
		return
	end

	-- 打开的时候界面还在缓存中
	if self.gameObject then
		BaseView.AfterOpen(self)
		if not self.destroy_imm then --如果是非立即销毁的界面，要重新设置父节点
			self.transform:SetParent(panelMgr:GetParent(self.layer_name))
		end
		return
	end

	self:AsnycLoadLayout()
end

function BaseView:AsnycLoadLayout()
	local preLoad_index = 0
	local preLoad_list_len = 0
	local function preLoadCallback()
		preLoad_index = preLoad_index + 1
		if preLoad_list_len == 0 or preLoad_index == preLoad_list_len then
			local res_load_finish = function(obj)
				self:DoCreateWindowIndeed(obj)
			end

			if self.base_file == "" then
				-- 没有预设体的界面，直接创建空对象
				local go = UiFactory.createChild(panelMgr:GetParent(self.layer_name), UIType.EmptyObject, LuaMemManager.GetLastStr(self._class_type._source,"/"))
				res_load_finish(go)
			else
				LuaViewManager.LoadView(lua_viewM, self,self.base_file, self.layout_file, self.layer_name, res_load_finish)
			end
		end
	end

	-- 先加载额外的依赖资源包
	self.preparing_res = true
	self.is_loaded = false
	if self.load_time_out_id then
		TimerQuest.CancelQuest(GlobalTimerQuest, self.load_time_out_id)
		self.load_time_out_id = nil
	end

	local function onCheckTimeOut()
		if self.layer_name == "UI" then
			self.preparing_res = false
			self:Close()
		end
	end
	-- 15秒都打不开界面则自动触发关闭流程，避免影响后续操作
	self.load_time_out_id = TimerQuest.AddDelayQuest(GlobalTimerQuest, onCheckTimeOut, 15)

	if self.preLoad_list then
		local vo = nil
		preLoad_list_len = #self.preLoad_list
		for i = 1, preLoad_list_len do
			vo = self.preLoad_list[i]
 			lua_resM:LoadRes(self, vo[1], vo[2], vo[3], preLoadCallback)
		end
		self.preLoad_list = nil
	else
		preLoadCallback()
	end

end

function BaseView:DoCreateWindowIndeed(obj)
	--iphone低端机强制设立即销毁
	if SystemMemoryLevel.Cur == SystemMemoryLevel.Low and Application.platform == RuntimePlatform.IPhonePlayer then
		self.destroy_imm = true
	end

	if self.load_time_out_id then
		TimerQuest.CancelQuest(GlobalTimerQuest, self.load_time_out_id)
		self.load_time_out_id = false
	end

	--资源下载或加载失败
	if obj == nil then
		self.preparing_res = false
		self:Close()
		return
	end

	self.gameObject = obj
	if self.isPop == false then --如果加载回来时,外部已经调用了关闭流程,则直接销毁界面
		self:Destroy()
		return
	end

	self.transform = LuaTransform(obj.transform)

	if self.show_loading then
		LuaViewManager.RemoveLoadingView(lua_viewM, self._iid)
	end

	if self.use_background then
		self.background_wnd = UiFactory.createChild(self.transform,UIType.Background,"activity_bg")
		self.background_wnd:GetComponent("Image").alpha = self.background_alpha
		self.background_wnd.transform.sizeDelta = Vector2(SrcScreenWidth * 1.2,ScreenHeight * 1.2)
		self.background_wnd.transform:SetAsFirstSibling()
	end

	self.base_view_closeBtn = self.transform:Find("Window/windowCloseBtn") or self.transform:Find("Window2/windowCloseBtn")
	if self.base_view_closeBtn then
		self.base_view_closeBtn:GetComponent("Image"):SetNativeSize()
		local function onCloseHandler()
			self:Close()
		end
		AddClickEvent(self.base_view_closeBtn.gameObject,onCloseHandler,LuaSoundManager.SOUND_UI.OPEN)
	end

	self.base_view_bg = self.transform:Find("Window") or self.transform:Find("Window2")
	if self.base_view_bg then
		local img = self.base_view_bg:GetComponent("Image")
		if img then
			img.raycastTarget = true
		end
	end

	if self.background_wnd and self.click_bg_toClose then
		local function onClickHandler()
			self:Close()
		end
		AddClickEvent(self.background_wnd,onClickHandler,LuaSoundManager.SOUND_UI.OPEN)
	end

	self.base_view_titleText = self.transform:Find("Window/windowTitleCon/windowTitleText") or self.transform:Find("Window2/windowTitleCon/windowTitleText")

	BaseView.CreateMainWindow(self)
	self.preparing_res = false
end

function BaseView:CreateMainWindow()

	if self.is_delay_callback and self.use_background and self.hide_maincancas then --大界面才需要延迟callback
		local function onLoad()
			self.is_loaded = true
			if self.load_callback and not self.is_delete and self.gameObject then
				self.load_callback()
			end
		end

		if self.delay_load_id then
			LuaViewManager.RemoveDelayQueue(lua_viewM, self.delay_load_id)
			self.delay_load_id = false
		end
		self.delay_load_id = LuaViewManager.AddDelayQueue(lua_viewM, onLoad)
	else
		self.is_loaded = true
		if self.load_callback then
			self.load_callback()
		end
	end

	BaseView.AfterOpen(self)
end

function BaseView:AfterOpen()
	if not self.gameObject then
		return
	end

	if self.isPop then
		local function onOpen()
			if self.open_callback then
				self.open_callback()
			end
			if self.use_background and self.close_fog == true then
				Scene.Instance:ChangeFogEnable(false)
			end
			if self.is_guide_module then
				if self.AddToStageHandler then
					self:AddToStageHandler()
				end
			end
			if self.hide_item_use_view then
				LuaViewManager.Fire(lua_viewM, LuaViewManager.HIDE_ITEM_USE_VIEW, true)
			end
			if self.is_hide_skill_black_bg then
				SkillManager:getInstance():AddHideBlackViews(self.layout_file)
				HideBlackGround()
			end
			if self.change_scene_hide and not Scene:getInstance():IsSceneProloadFinish() then
				self:Hide()
			end
		end
		if self.wait_for_hide then
			self.gameObject:SetActive(false)
			if self.is_set_zdepth then
				UIZDepth.ResetUIZDepth(self)
			end
			if self.use_background and self.hide_maincancas then
				LuaViewManager.Fire(lua_viewM, LuaViewManager.CHANGE_MAIN_CANVAS_VISIBLE, self, true)
			end
		elseif self.wait_for_hide == false then
			if self.use_background and self.hide_maincancas then
				LuaViewManager.Fire(lua_viewM, LuaViewManager.CHANGE_MAIN_CANVAS_VISIBLE, self, false)
			end
			if not self.gameObject.activeSelf then
				self.gameObject:SetActive(true)
			end
		end
		self.wait_for_hide = false
		if self.is_set_order_sorting then 
			self:SetUIDepth(self.gameObject)
		end		
		local curr_zdepth_count = nil
		if self.is_set_zdepth then 
			curr_zdepth_count = self:AddUIZDepth()
		end
		if self.layer_name == "Main" and not lua_viewM.main_cancas_last_visible then
			LuaViewManager.PuskMainCanvasCon(lua_viewM, self.transform)
		end

		if self.is_delay_callback and self.use_background and self.hide_maincancas then --大界面才需要延迟callback
			local function onDelayOpen()
				if not self.is_delete and self.isPop and self.gameObject then
					onOpen()
					if curr_zdepth_count then
						UIZDepth.SetUIZDepth(self, curr_zdepth_count)
					end
				end
			end

			if self.delay_open_id then
				LuaViewManager.RemoveDelayQueue(lua_viewM, self.delay_open_id)
				self.delay_open_id = false
			end
			self.delay_open_id = LuaViewManager.AddDelayQueue(lua_viewM, onDelayOpen)
		else
			onOpen()
		end

		LuaViewManager.OpenView(lua_viewM, self)
	else
		self:Close()
	end
end

function BaseView:AfterClose()
	--下面这句话要放在clse_callback前面 因为有些是在close_callback 关掉界面 会导致重新调用cancelhide 导致主界面被隐藏
	LuaViewManager.CloseView(lua_viewM, self)
	UIPartical.ClearAllEffect(self)
	if self.close_callback and self.is_loaded then
		self.close_callback()
	end
end

function BaseView:OpenToggle()
	if self.gameObject and self.isPop then
		self.Close()
	else
		self:OpenOnly()
	end
end

function BaseView:Close(called_by_mgr)
	self.isPop = false

	if self.close_mode == CloseMode.CloseVisible then
		BaseView.CloseVisible(self)
	elseif self.close_mode == CloseMode.CloseDestroy then
		BaseView.CloseDestroy(self)
	end
end

--隐藏
function BaseView:Hide()
	if self.gameObject then
		if self.use_background and self.hide_maincancas then
			LuaViewManager.Fire(lua_viewM, LuaViewManager.CHANGE_MAIN_CANVAS_VISIBLE, self, true)
		end
		self.gameObject:SetActive(false)
		self.wait_for_hide = nil
	else
		self.wait_for_hide = true
	end
end

--取消隐藏
function BaseView:CancelHide()
	self.isPop = true
	self.wait_for_hide = false
	BaseView.RemoveDestroyTimer(self)
	if self.use_background and self.hide_maincancas then
		LuaViewManager.Fire(lua_viewM, LuaViewManager.CHANGE_MAIN_CANVAS_VISIBLE, self, false)
	end
	if self.gameObject then
		self.gameObject:SetActive(true)
		if self.ReOpen_callback then
			self:ReOpen_callback()
		end
	end
end

function BaseView:CloseVisible()
	if self.gameObject then
		if self.close_mode == CloseMode.CloseDestroy and self.destroy_imm and not self.use_local_view then
			self.transform:SetParent(UIManager:GetUILayer("Scene"))
		else
			self.gameObject:SetActive(false)
		end
	end

	if self.all_close_event_id then
		EventSystem.UnBind(GlobalEventSystem,self.all_close_event_id)
		self.all_close_event_id = nil
	end

	if self.reenter_close_event_id then
		EventSystem.UnBind(GlobalEventSystem,self.reenter_close_event_id)
		self.reenter_close_event_id = nil
	end

	if self.delay_open_id then
		LuaViewManager.RemoveDelayQueue(lua_viewM, self.delay_open_id)
		self.delay_open_id = nil
	end

	if self.load_time_out_id then
		TimerQuest.CancelQuest(GlobalTimerQuest, self.load_time_out_id)
		self.load_time_out_id = nil
	end

	if self.is_hide_skill_black_bg then
		SkillManager:getInstance():RemoveHideBlackViews(self.layout_file)
	end

	if self.use_background and self.hide_maincancas then
		LuaViewManager.Fire(lua_viewM,LuaViewManager.CHANGE_MAIN_CANVAS_VISIBLE, self, true)
	end

	if self.hide_item_use_view then
		LuaViewManager.Fire(lua_viewM, LuaViewManager.HIDE_ITEM_USE_VIEW, false)
	end

	--隐藏时自动删掉挂接的模型,需要在对应界面的opencallback后重新创建
	lua_resM:clearRoleMode(self)
	if self.is_set_zdepth then
		UIZDepth.ResetUIZDepth(self)
	end
	BaseView.AfterClose(self)
end

function BaseView:CloseDestroy()
	self:CloseVisible()

	if self.destroy_imm then
		BaseView.Destroy(self)
	else
		BaseView.AddDestroyTimer(self)
	end
end

function BaseView:RemoveDestroyTimer()
	if self.destroy_timer then
		TimerQuest.CancelQuest(GlobalTimerQuest, self.destroy_timer)
		self.destroy_timer = false
	end
end

function BaseView:AddDestroyTimer()
	BaseView.RemoveDestroyTimer(self)

	local destroy_callback = function()
		self.destroy_timer = false
		self:Destroy()
	end
	self.destroy_timer = TimerQuest.AddDelayQuest(GlobalTimerQuest, destroy_callback, self.delay_delete_time)
end

function BaseView:Destroy()
	if self.is_delete or not self.gameObject then return end

	if self.delay_load_id then
		LuaViewManager.RemoveDelayQueue(lua_viewM, self.delay_load_id)
		self.delay_load_id = false
	end

	if self.trigger_guide_id then
		EventSystem.UnBind(GlobalEventSystem, self.trigger_guide_id)
		self.trigger_guide_id = false
	end

	self.is_delete = true
	BaseView.RemoveDestroyTimer(self)
	if self.destroy_callback and self.is_loaded then
		self.destroy_callback()
	end

	if self.show_loading then
		LuaViewManager.RemoveLoadingView(lua_viewM, self._iid)
	end
	self.preparing_res = false
	self.is_loaded = false

	self.popChild = nil
	self.parent_wnd = nil
	self.transform = nil
	self.cache_findChild = nil
	self.preLoad_list = nil

	self:DeleteMe()
	if not self.use_local_view or Application.platform ~= RuntimePlatform.WindowsEditor then
		LuaViewManager.DestroyView(lua_viewM,self.gameObject)
	end

	self.gameObject = nil
	EventSystem.Fire(GlobalEventSystem,BaseView.DestroyEvent,self)
end

function BaseView:__delete()
	if not self.is_delete then
		self:CloseDestroy()
	end
end

-- 显示子窗口
function BaseView:PopUpChild(win)
	if win then
	 	if self.popChild ~= nil and self.popChild ~= win then
	 		self.popChild:SetVisible(false)
	 	end
	 	self.popChild = win
	 	self.popChild:SetVisible(true)
	 end
 end

function BaseView:GetBaseFile()
	return self.base_file
end
