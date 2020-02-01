EventSystem = EventSystem or BaseClass()
local EventSystem = EventSystem
local table_insert = table.insert
local table_remove = table.remove
EventSystem.all_event_count = 88888888

function EventSystem:__init()
	self.all_event_dic = {}
	self.bind_id_to_event_id_dic = {}
	self.calling_event_dic = {}
	self.need_fire_events = false
	self.fire_callback_queue = false
end

function EventSystem:Bind(event_id, event_func)
	if event_id == nil then
		LogError("Try to bind to a nil event_id")
		--故意报错输出调用堆栈
		print(event_id .. "")
		PrintCallStack()
		return
	end
	
	if event_func == nil then
		LogError("Try to bind to a nil event_func")
		--故意报错输出调用堆栈
		print(event_func .. "")
		PrintCallStack()
		return
	end
	local event_list = self.all_event_dic[event_id]
	if event_list == nil then
		event_list = {}
		self.all_event_dic[event_id] = event_list
	end
	EventSystem.all_event_count = EventSystem.all_event_count + 1
	self.bind_id_to_event_id_dic[EventSystem.all_event_count] = event_id
	event_list[EventSystem.all_event_count] = event_func
	return EventSystem.all_event_count
end

function EventSystem:UnBind(bind_id)
	if bind_id == nil then
		return
	end
	local event_id = self.bind_id_to_event_id_dic[bind_id]
	if event_id then
		local calling_event = self.calling_event_dic[event_id]
		if calling_event ~= nil then
			if calling_event == false then
				calling_event = {}
				self.calling_event_dic[event_id] = calling_event
			end
			calling_event[bind_id] = true
			local event_list = EventSystem.getEvent(self, event_id)
			if event_list then
				event_list[bind_id] = false
			end
			return
		end
		self.bind_id_to_event_id_dic[bind_id] = nil
		local event_list = EventSystem.getEvent(self, event_id)
		if event_list then
			event_list[bind_id] = nil
		end
	end
end

function EventSystem:UnBindAll(is_delete)
	Runner:getInstance():RemoveLateRunObj(self)
	Runner:getInstance():RemoveRunObj(self)

	if is_delete then
		self.all_event_dic = nil
		self.bind_id_to_event_id_dic = nil
		self.calling_event_dic = nil	
	else
		self.all_event_dic = {}
		self.bind_id_to_event_id_dic = {}
		self.calling_event_dic = {}
	end

	self.need_fire_events = false
	self.fire_callback_queue = false
end

--调用已经处于派发队列中的Event
function EventSystem:Update()
	--timer quest
	--依次执行所有需要触发的事件
	if self.need_fire_events then
		while not List.Empty(self.need_fire_events) do
			local fire_info = List.PopFront(self.need_fire_events)
			if fire_info.event_list then
				for i, event_call_back in pairs(fire_info.event_list) do
					if event_call_back then
						event_call_back(fire_info.arg_list)

						if OpenBindEventLog then
							self:PrintEventFuncLog(i)
						end
					end
				end
			end
			-- fire_info.event(fire_info.arg_list)
		end
		self.need_fire_events = false
		Runner:getInstance():RemoveRunObj(self)
	end
end

function EventSystem:LateUpdate(now_time, elapse_time)
	if self.fire_callback_queue then 
		if self.fire_callback_queue:GetSize() > 0 then
			local call_back = self.fire_callback_queue:PopFront()
			if call_back then
				call_back()
			end
		else
			Runner:getInstance():RemoveLateRunObj(self)
		end
	end
end
--每帧触发一个callback
function EventSystem:DelayFire(event_id, ...)
	if event_id == nil then
		LogError("Try to call EventSystem:Fire() with a nil event_id")
		--故意抛出堆栈
		print(error.msg)
		return
	end

	local event_list = EventSystem.getEvent(self, event_id)
	if event_list ~= nil then
		local args = { ... }
		local paramCount = select('#', ...)
		self.fire_callback_queue = self.fire_callback_queue or Array.New()
		for i, event_call_back in pairs(event_list) do
			local function delay_callback()
				if event_list[i] and event_call_back then
					event_call_back(unpack(args, 1, paramCount))

					if OpenBindEventLog then
						self:PrintEventFuncLog(i)
					end
				end
			end
			self.fire_callback_queue:PushBack(delay_callback)
		end
		Runner:getInstance():AddLateRunObj(self, 1)
	end
end

function EventSystem:ClearDelayFireQueue()
	if self.fire_callback_queue and not self.fire_callback_queue:IsEmpty() then
		self.fire_callback_queue:Clear()
	end
end

--立即触发
function EventSystem:Fire(event_id, ...)
	if event_id == nil then
		LogError("Try to call EventSystem:Fire() with a nil event_id")
		--故意抛出堆栈
		print(error.msg)
		return
	end

	local event_list = EventSystem.getEvent(self, event_id)
	if event_list then
		self.calling_event_dic[event_id] = false
		for bind_id, event_call_back in pairs(event_list) do
			if event_call_back then
				event_call_back(...)

				if OpenBindEventLog then
					self:PrintEventFuncLog(bind_id)
				end
			end
		end
		local calling_event = self.calling_event_dic[event_id]
		self.calling_event_dic[event_id] = nil
		if calling_event then
			for bind_id, _ in pairs(calling_event) do
				EventSystem.UnBind(self, bind_id)
			end
		end
	end
end

--下一帧触发
function EventSystem:FireNextFrame(event_id, ...)
	if event_id == nil then
		LogError("Try to call EventSystem:FireNextFrame() with a nil event_id")
		--故意抛出堆栈
		print(error.msg)
		return
	end

	local event_list = EventSystem.getEvent(self, event_id)
	if event_list ~= nil then
		local fire_info = {}
		fire_info.event_id = event_id
		fire_info.event_list = event_list
		fire_info.arg_list = {...}
		if not self.need_fire_events then
			Runner:getInstance():AddRunObj(GlobalEventSystem, 3)
		end
		self.need_fire_events = self.need_fire_events or List.New()
		List.PushBack(self.need_fire_events, fire_info)
	end
end

function EventSystem:getEvent(event_id)
	return self.all_event_dic[event_id]
end

function EventSystem:__delete()
	EventSystem.UnBindAll(self, true)
end

function EventSystem:PrintEventFuncLog( bind_id )
	local event_id = self.bind_id_to_event_id_dic[bind_id]

	--正常绑定函数不输出
	local ignore_event = {
		[1001] = true,
		["ActivityIconManager.UPDATE_ALL_ICON"] = true,
		["vo_one_var_chg_curr_god_dot"] = true,
	}
	
	if not ignore_event[event_id] then
		print("--------EventFuncCall Event ID--------",event_id)
	end
end