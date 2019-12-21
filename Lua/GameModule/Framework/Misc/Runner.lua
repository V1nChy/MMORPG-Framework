--[[@------------------------------------------------------------------
说明: 根据外部设定的优先级在每一帧中依次执行所有托管的RunObj
作者: deadline
----------------------------------------------------------------------]]

Runner = Runner or BaseClass(nil,true)
local Runner = Runner

function Runner:__init()
	local function generateRunner()
		local newRunner =  {
			all_run_obj_list = {},		--用于标记某个模块是否已经注册,避免重复性的注册
			id_count = 0,
			curr_frame_count = 0,      --当前的帧数

			--支持1 ~ 16帧的update方法调用频率
			priority_run_obj_list = {},
			elapse_time = {},   
		}
		for i=1,16 do
			newRunner.elapse_time[i] = 0
			table.insert(newRunner.priority_run_obj_list, {})
		end
		return newRunner
	end
	self.updateRunner = generateRunner()
	self.lateUpdateRunner = generateRunner()
end

--[[@
功能:	主Update中调用该方法,触发托管对象的Update
参数:	
		无
返回值:
		无
其它:	无
作者:	deadline
]]
function Runner:Update( now_time, elapse_time )
	local runner = self.updateRunner
	runner.curr_frame_count = runner.curr_frame_count + 1
	for i, priority_tbl in pairs(runner.priority_run_obj_list) do
		runner.elapse_time[i] = (runner.elapse_time[i] or 0) + elapse_time
		if runner.curr_frame_count % i == 0 then
			for _, v in pairs(priority_tbl) do
				v:Update(now_time, runner.elapse_time[i])
			end
			runner.elapse_time[i] = 0
		end
	end
end

function Runner:LateUpdate( now_time, elapse_time )
	local runner = self.lateUpdateRunner
	runner.curr_frame_count = runner.curr_frame_count + 1
	for i, priority_tbl in pairs(runner.priority_run_obj_list) do
		runner.elapse_time[i] = (runner.elapse_time[i] or 0) + elapse_time
		if runner.curr_frame_count % i == 0 then
			for _, v in pairs(priority_tbl) do
				v:LateUpdate(now_time, runner.elapse_time[i])
			end
			runner.elapse_time[i] = 0
		end
	end
end


function Runner:AddRunObj( run_obj , priority_level )
	Runner.RealAddRunObj(self, self.updateRunner, run_obj, priority_level)
	if run_obj["Update"] == nil then
		LogError("Runner:AddRunObj try to add a obj not have Update method!")
	end
end

function Runner:AddLateRunObj( run_obj , priority_level )
	Runner.RealAddRunObj(self, self.lateUpdateRunner, run_obj, priority_level)
	if run_obj["LateUpdate"] == nil then
		LogError("Runner:AddLateRunObj try to add a obj not have LateUpdate method!")
	end
end

function Runner:RealAddRunObj(runner, run_obj, priority_level)
	local obj = runner.all_run_obj_list[run_obj]
	if obj ~= nil then
		--已经存在该对象, 不重复添加
		return false
	else
		--对象不存在,正常添加
		runner.id_count = runner.id_count + 1
		priority_level = priority_level or 1
		runner.all_run_obj_list[run_obj] = {priority_level, runner.id_count}
		runner.priority_run_obj_list[priority_level][runner.id_count] = run_obj
	end
end

--[[@
功能:	从Runner中删除一个run_obj
参数:	
		run_obj
返回值:
		无
其它:	无
作者:	deadline
]]
function Runner:RemoveRunObj(run_obj )
	Runner.RealRemoveRunObj(self, self.updateRunner, run_obj)
end

function Runner:RemoveLateRunObj(run_obj)
	Runner.RealRemoveRunObj(self, self.lateUpdateRunner, run_obj)
end

function Runner:RealRemoveRunObj(runner, run_obj)
	local key_info = runner.all_run_obj_list[run_obj]
	if key_info ~= nil then
		runner.all_run_obj_list[run_obj] = nil
		runner.priority_run_obj_list[key_info[1]][key_info[2]] = nil
	end
end