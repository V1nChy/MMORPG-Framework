--需要处理 z值深度的ui对象
UIZDepth = UIZDepth or BaseClass()
local UIZDepth = UIZDepth

--每一步的深度值
UIZDepth.STEP_VALUE = 1000

--界面模型的深度值
UIZDepth.MODEL_Z_VALUE = -UIZDepth.STEP_VALUE + 300

--每个layer的起始值
UIZDepth.Start_ZDepth_list = {
	["Scene"] = 0,
	["Main"] = 0,
	["UI"] = 1,
	["Activity"] = 12,
	["Top"] = 25,
}

--每个layer的当前层数
UIZDepth.curr_ZDepth_list = {
	["Scene"] = UIZDepth.Start_ZDepth_list.Scene,
	["Main"] = UIZDepth.Start_ZDepth_list.Main,
	["UI"] = UIZDepth.Start_ZDepth_list.UI,
	["Activity"] = UIZDepth.Start_ZDepth_list.Activity,
	["Top"] = UIZDepth.Start_ZDepth_list.Top,
}

function UIZDepth:__init()
	self.layer_name = "Main"
	self.zdepth_counter = 0
end

function UIZDepth:__delete()
	self:ResetUIZDepth()
end

function UIZDepth:DeleteCurrLayerZDepth(layer_name,count)
	if UIZDepth.curr_ZDepth_list[layer_name] then
		UIZDepth.curr_ZDepth_list[layer_name] = UIZDepth.curr_ZDepth_list[layer_name] - count
	end
	self:SetUIZDepth()
end

function UIZDepth:GetCurrLayerZDepth(layer_name)
	return UIZDepth.curr_ZDepth_list[layer_name]
end

--打开界面的时候 加UI z深度
 function UIZDepth:AddUIZDepth()
 	if self.layer_name and self.layer_name ~= "Main" then
 		self.zdepth_counter = self.zdepth_counter + 1
 		if UIZDepth.curr_ZDepth_list[self.layer_name] then
			UIZDepth.curr_ZDepth_list[self.layer_name] = UIZDepth.curr_ZDepth_list[self.layer_name] + 1
		end
		self:SetUIZDepth()
 	end
 end

--真正设置ui的深度
 function UIZDepth:SetUIZDepth()
	local curr_count = self:GetCurrLayerZDepth(self.layer_name)
	if self.transform and curr_count > 0 then
		local camare_depth = 500 -curr_count * UIZDepth.STEP_VALUE * 0.01
		SetGlobalPositionZ(self.transform, camare_depth)
	end
 end

--ui控件要在模型上面显示
 function UIZDepth:CoverMode(ui_transform)
 	if ui_transform then
 		SetLocalPositionZ(ui_transform, -UIZDepth.STEP_VALUE + 1)
 	end
 end

--关闭界面的时候重置UI z深度
 function UIZDepth:ResetUIZDepth(count)
 	if self.layer_name and self.layer_name ~= "Main" then
 		if count then
 			if count > 0 then
	 			self:DeleteCurrLayerZDepth(self.layer_name,count)
	 			self.zdepth_counter = self.zdepth_counter - count
	 		end
 		else
	 		if self.zdepth_counter > 0 then
	 			self:DeleteCurrLayerZDepth(self.layer_name,self.zdepth_counter )
	 			self.zdepth_counter = 0
	 		end
 		end
 	end
 end
