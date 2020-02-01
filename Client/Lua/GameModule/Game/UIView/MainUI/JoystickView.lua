JoystickView = JoystickView or BaseClass(BaseView)
local JoystickView = JoystickView

function JoystickView:__init()
	self.base_file = "mainui"
	self.layout_file = "JoystickView"
	self.close_mode = CloseMode.CloseDestroy
	self.destroy_imm = true
	
	self.arrow_show_state = false
	self.max_radius = 115				--拖拽的最大偏移距离
	self.img_radius = 80           
	self.dragging_dir = Vector2.zero
	self.dragging_length = 0

	self.root_wnd_pos = Vector2(0, 0)
	self.joy_stick_arrow_pos = Vector2(241,320) --旋转或放大缩小的物体中心点必须是(0.5, 0.5)
	self.real_dir = Vector2.zero

	self.load_callback = function()
		self:loadCallback()
		self:InitEvent()
	end

	self.close_callback = function()
		--self:OnTouchEnd()
	end

	self.destroy_callback = function ()
		--self:Remove()
	end
end

function JoystickView:__delete()
end

function JoystickView:Remove()
	if self.reset_id then 
		GlobalEventSystem:UnBind(self.reset_id)
		self.reset_id = nil 
	end

	if self.reset_id2 then 
		GlobalEventSystem:UnBind(self.reset_id2)
		self.reset_id2 = nil 
	end

	if self.orientation_change_id then
    	GlobalEventSystem:UnBind(self.orientation_change_id)
    	self.orientation_change_id = nil
    end
end

function JoystickView:loadCallback()	
 	self.touch = self:GetChild("JoystickCon/touch").gameObject
	self.btn = self:GetChild("JoystickCon/button")
	self.arrow = self:GetChild("JoystickCon/BgCon/Axis/arrow")

	self.default_btn_pos = Vector2.zero
	self.btn_pos = Vector2.zero
	self.center_pos = Vector2.zero
end

function JoystickView:InitEvent()
	local function touch_begin(target,pos_x, pos_y)
		self:OnTouchBegin(pos_x, pos_y)
	end
	local function draging(target,pos_x, pos_y)
		self:OnDragging(pos_x, pos_y)
	end
	local function touch_end(target,pos_x, pos_y)
		self:OnTouchEnd(pos_x, pos_y)
	end
	AddDownEvent(self.touch,touch_begin)
	AddDragEvent(self.touch,draging)
	AddUpEvent(self.touch,touch_end)
end

function JoystickView:OnTouchBegin(pos_x, pos_y)
	self.startTime = Time.time
	self.is_dragging = true
	self:OnDragging(pos_x,pos_y)
end

function JoystickView:OnDragging(pos_x, pos_y)
	if not self.is_dragging then 
		return 
	end

	pos_x,pos_y = ScreenToViewportPoint(pos_x,pos_y)
	local delta_x = pos_x - self.center_pos.x
	local delta_y = pos_y - self.center_pos.y
	self.dragging_dir.x = pos_x - self.center_pos.x
	self.dragging_dir.y = delta_y
	self.dragging_length = self.dragging_dir:normalise()

	--移动半径
	local img_x, img_y
	local ratio = GameMath_GetDistance(pos_x, pos_y, self.touch_img_pos.x, self.touch_img_pos.y,true)
	if ratio > self.img_radius then
		--超过移动半径，用等比公式计算半径范围内的位置
		img_x = delta_x*self.img_radius/ratio
		img_y = delta_y*self.img_radius/ratio
	else
		img_x = delta_x
		img_y = delta_y
	end
	--self.touch_img.anchoredPosition = Vector2(self.touch_img_pos.x+img_x, self.touch_img_pos.y+img_y)
	SetLocalPosition(self.touch_img,self.touch_img_pos.x+img_x,self.touch_img_pos.y+img_y,0)
	self.touch_img_drag_time = Status.NowTime

	--当拖动距离够大时发动摇杆，创建定时器，每帧刷新方向
	if self.dragging_length > 20 and self.update_handler == nil then
		if not self.joy_stick_arrow_show_state then
			self.joy_stick_arrow.gameObject:SetActive(true)
			self.joy_stick_arrow_show_state = true
		end
		self:Update()
		self.update_handler = true
		Runner.Instance:AddRunObj(self, 3)
	end
end


function JoystickView:OnTouchEnd(pos_x, pos_y)
	
	if true then
		return
	end

	if self.update_handler then
		self.update_handler = nil
		Runner.Instance:RemoveRunObj(self)
	end
	--触发事件
	if self.is_dragging then
		GlobalEventSystem:Fire(JoystickView.EVENT_TOUCH_END)
	end

	--self:ZoomOut()

	self.joy_stick_arrow.gameObject:SetActive(false)
	self.arrow_show_state = false
	self.dragging_length = 0
	self.scene.last_dir_move_end_time = Status.NowTime
	self.button.anchoredPosition = self.defaultTouchPos
	self.is_dragging = false
end

function JoystickView:Update(now_time, elapsed_time)
	now_time = now_time
	local pastTime = now_time - self.startTime
	local dir = self.dragging_dir
	self.last_fire_time = Status.NowTime

	local angle = SceneManager.Instance:GetAngle(dir.x, dir.y) - 90 --90是因为箭头默认角度是向上 
	self.joy_stick_arrow.localRotation = Quaternion.Euler(0,0,angle)
end