BaseController = BaseController or BaseClass()
local BaseController = BaseController

function BaseController:__init()

end

--[[@
功能:	绑定事件
参数:	绑定的id，绑定的函数
返回值: 一个事件的handler
其它:	无
作者:	raowei
]]
function BaseController:Bind(event_id, event_func,class_name)

	return GlobalEventSystem:Bind(event_id,event_func,class_name)
end
--[[@
功能:	解除绑定事件
参数:	事件的handler
返回值: 无
其它:	无
作者:	raowei
]]
function BaseController:UnBind( obj )
	GlobalEventSystem:UnBind( obj )
end
--[[@
功能:	立即触发事件
参数:	绑定的id，绑定的函数
返回值: 绑定的id，传递的参数
其它:	无
作者:	raowei
]]
function BaseController:Fire(event_id,...)
	GlobalEventSystem:Fire( event_id ,...)
end
--[[@
功能:	下一帧触发事件
参数:	绑定的id，绑定的函数
返回值: 绑定的id，传递的参数
其它:	无
作者:	raowei
]]
function BaseController:FireNextFrame(event_id,...)
	GlobalEventSystem:FireNextFrame( event_id ,...)
end