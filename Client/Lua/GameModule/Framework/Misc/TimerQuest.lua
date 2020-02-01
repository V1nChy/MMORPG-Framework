TimerQuest = TimerQuest or BaseClass()
local TimerQuest = TimerQuest

local Time = Time
local Array = Array
local table_insert = table.insert
local table_sort = table.sort

function TimerQuest:__init()
	self.timer_list = {}
	self.update_delta_time = 0
	self.timer_list2 = {}
	self.update_delta_time2 = 0
	self.timer_list3 = {}
	self.update_delta_time3 = 0

	self.update_count = 0
	self.waitfor_call_back_list = false
	self.timer_list_index = 0
	self.timer_cache_queue = Array.New()
	LateUpdateBeat:Add(TimerQuest.Update,self)
end

function TimerQuest:AddPeriodQuest(onLeftTimeHandler,duration,loop)
	self.timer_list_index = self.timer_list_index + 1
	local timer = nil
	if Array.GetSize(self.timer_cache_queue) > 0 then
		timer = Array.PopFront(self.timer_cache_queue)
		timer.add_time 	= self.timer_list_index
		timer.duration 	= duration
		timer.loop		= loop or -1
		timer.func		= onLeftTimeHandler
		timer.time		= duration
		timer.count		= Time.frameCount + 1
	else
		timer = {
		add_time 	= self.timer_list_index,
		duration 	= duration,
		loop		= loop or -1,
		func		= onLeftTimeHandler,
		time		= duration,
		count		= Time.frameCount + 1}
	end

	if OpenTimeQuestLog then
		timer.source = PrintFunctionCallPos("AddPeriodQuest")
	end

	if duration <= 0.6 then
		self.timer_list[self.timer_list_index] = timer
	elseif duration <= 2 then
		self.timer_list2[self.timer_list_index] = timer
	else
		self.timer_list3[self.timer_list_index] = timer
	end
	return self.timer_list_index
end

function TimerQuest:AddDelayQuest(onLeftTimeHandler,duration)
	self.timer_list_index = self.timer_list_index + 1
	local timer = nil
	if Array.GetSize(self.timer_cache_queue) > 0 then
		timer = Array.PopFront(self.timer_cache_queue)
		timer.add_time 	= self.timer_list_index
		timer.duration 	= duration
		timer.loop		= 0
		timer.func		= onLeftTimeHandler
		timer.time		= duration
		timer.count		= Time.frameCount + 1
	else
		timer = {
		add_time 	= self.timer_list_index,
		duration 	= duration,
		loop		= 0,
		func		= onLeftTimeHandler,
		time		= duration,
		count		= Time.frameCount + 1}
	end
	if duration <= 0.6 then
		self.timer_list[self.timer_list_index] = timer
	elseif duration <= 2 then
		self.timer_list2[self.timer_list_index] = timer
	else
		self.timer_list3[self.timer_list_index] = timer
	end
	return self.timer_list_index
end

function TimerQuest:CancelQuest(id)
	if id then
		local delete_vo = self.timer_list[id]
		if not delete_vo then
			delete_vo = self.timer_list2[id]
		end		
		if not delete_vo then
			delete_vo = self.timer_list3[id]
		end
		if delete_vo then
			delete_vo.func = nil
			if Array.GetSize(self.timer_cache_queue) <= 20 then
				Array.PushBack(self.timer_cache_queue, delete_vo)
			end
			self.timer_list[id] = nil
			self.timer_list2[id] = nil
			self.timer_list3[id] = nil
		end
	end
end

function TimerQuest:HandlerTimer(vo, i, frame_count)
	if vo.time <= 0 and frame_count > vo.count then
		
		if OpenTimeQuestLog then
			print("TimerQuest:",vo.source)
		end
		vo.func()
		if vo.loop > 0 then
			vo.loop = vo.loop - 1
			vo.time = vo.time + vo.duration
		end
		
		if vo.loop == 0 then
			self:CancelQuest(i)
		elseif vo.loop < 0 then
			vo.time = vo.time + vo.duration
		end
	end
end

function TimerQuest:Update()
	local delta = Time.deltaTime
	local frame_count = Time.frameCount
	self.update_count = self.update_count + 1
	for i,vo in pairs(self.timer_list) do
		vo.time = vo.time - delta
		self:HandlerTimer(vo, i, frame_count)
	end

	self.update_delta_time2 = self.update_delta_time2 + delta
	if self.update_count % 9 == 0 then
		for i,vo in pairs(self.timer_list2) do
			vo.time = vo.time - self.update_delta_time2
			self:HandlerTimer(vo, i, frame_count)
		end
		self.update_delta_time2 = 0
	end

	self.update_delta_time3 = self.update_delta_time3 + delta
	if self.update_count % 29 == 0 then
		for i,vo in pairs(self.timer_list3) do
			vo.time = vo.time - self.update_delta_time3
			self:HandlerTimer(vo, i, frame_count)
		end
		self.update_delta_time3 = 0
	end
end
