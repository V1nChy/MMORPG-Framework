--[[
数组索引由下标0到下标n-1
--]]

Array = Array or BaseClass()
local Array = Array
local table = table
local table_insert = table.insert
local table_remove = table.remove
Array.UPPER = 1 --升序
Array.LOWER = 2 --降序

function Array:__init()
	self.size = 0
	self.items = {}
end

function Array:IsEmpty()
	return self.size <= 0
end

--[[
在数组尾部插入一项
@para1 val in 插入数组的项
@author deadline
@create 5/16/2012
--]]
function Array:PushBack( val )
	self.size = self.size + 1
	self.items[self.size] = val
end


--[[
从数组尾部移除一项并返回
@return 返回移除的项
@author deadline
@create 5/16/2012
--]]
function Array:PopBack()
	if self.size > 0 then
		local val =self.items[self.size]
		self.items[self.size] = nil
		self.size = self.size - 1
		return val
	end
	return nil
end

--[[
从数组头部添加一项
@para1 val in 一个数组项
@author deadline
@create 5/18/2012
--]]
function Array:PushFront( val )
	table_insert(self.items, 1, val)
	self.size = self.size + 1
end


--[[
从数组头部移除一项并返回
@return 返回移除的项
@author deadline
@create 5/18/2012
--]]
function Array:PopFront()
	if self.size > 0 then
		local val = self.items[1]
		table_remove(self.items, 1)
		self.size = self.size - 1
		return val
	end
	return nil
end

--[[
逐项对数组每一项执行某操作
@para1 func in 每项执行的函数,类型为 func(item)
@author deadline
@create 5/16/2012
--]]
function Array:ForEach(func)
	for i = 1, self.size do
		func(self.items[i])
	end
end

--[[
按索引获取数组中的某项
@para1 index in 数组索引
@return 返回对应索引的项的项
@author deadline
@create 5/18/2012
--]]
function Array:Get(index)
	if index < self.size then
		return self.items[index + 1]
	else
		print("Array:Get() out of index!  " .. tostring(index))
	end
end

function Array:Contains(val)
	for i = 1, self.size do
		local item = self.items[i]
		if item == val then
			return true
		end
	end
	return false
end

function Array:IndexOf(val)
	for i = 1, self.size do
		local item = self.items[i]
		if item == val then
			return i
		end
	end
	return -1
end


function Array:GetList()
	return self.items
end

--[[
按索引设置数组中的某项
@para1 index in 数组索引
@para2 val in 设置的值
@author deadline
@create 5/18/2012
--]]
function Array:Set(index, val)
	if index < self.size then
		self.items[index + 1] = val
	else
		print("Array:Set() out of index!")
	end
end

--[[
逐项对数组每一项执行某操作
@return 数组大小
@author deadline
@create 5/16/2012
--]]
function Array:GetSize()
	return self.size
end

--[[
对数组执行升序排序操作
@para1 key_name in 用来排序的数组项Key(可选参数)
@author deadline
@create 5/18/2012
--]]
function Array:LowerSort(key_name)
	local sort_func
	if key_name then
		sort_func = SortTools.KeyLowerSorter(key_name)
	else
		sort_func = SortTools.ItemLowerSorter()
	end
	table.sort(self.items, sort_func)
end

--[[
对数组执行降序排序操作
@para1 key_name in 用来排序的数组项Key(可选参数)
@author deadline
@create 5/18/2012
--]]
function Array:UpperSort(key_name)
	local sort_func
	if key_name then
		sort_func = SortTools.KeyUpperSorter(key_name)
	else
		sort_func = SortTools.ItemUpperSorter()
	end
	table.sort(self.items, sort_func)
end

--[[
依据项中的Key查找数组中的一项
@para1 item_key in 用来搜索的数组项Key
@para2 val in 要查找的item_key的值
@para3 offset in 开始检索的位置
@return 返回检索到的项,检索到的项的Index
@author deadline
@create 5/18/2012
--]]
function Array:FindByKey(item_key, val, offset)
	offset = offset or 0
	for i = offset + 1, self.size do
		local item = self.items[i]
		if item[item_key] == val then
			return item, i-1
		end
	end
	return nil, -1
end

--[[
依据项中的Key查找数组中的一项
@para1 equal_func in 用来检测数组项是否符合要求的函数
@para2 offset in 开始检索的位置
@return 返回检索到的项,检索到的项的Index
@author deadline
@create 5/18/2012
--]]
function Array:FindByFunc(equal_func, offset)
	offset = offset or 0
	for i = offset + 1, self.size do
		local item = self.items[i]
		if equal_func(item) then
			return item, i-1
		end
	end
	return nil, -1
end

--[[
移除对应索引的项
@para1 index in 待删除的索引
@author deadline
@create 5/18/2012
--]]
function Array:Erase(index)
	index = index or self.size - 1
	if index >= 0 and index < self.size then
		table_remove(self.items, index + 1)
		self.size = self.size -1
	end
end

--[[
清空数组元素
]]
function Array:Clear()
	self.items = {}
	self.size = 0
end

--[[
在对应位置插入一项
@para1 key_name in 用来排序的数组项Key(可选参数)
@author deadline
@create 5/18/2012
--]]
function Array:Insert(item, index)
	index = index or 0
	table_insert(self.items, index + 1)
	self.size = self.size + 1
end

--[[
返回开始位置到结束位置之间的数组列表
@para1 begin_pos 开始位置
@para2 end_pos 结束位置
@author jkz
@create 8/8/2013
@return array
--]]
function Array:GetSubArray(begin_pos,end_pos)
	local ret = Array.New()
	local index = 0
	local len = self:GetSize()

	for i=begin_pos,end_pos-1 do

		if i >= len then
			break
		end
		ret:PushBack(self:Get(i))
	end
	return ret
end
--[[
返回数组中满足函数equal_func的子列表
@para1 equal_func 用来检测数组项是否符合要求的函数
@author jkz
@create 8/8/2013
@return array
--]]
function Array:Filter(equal_func)
	local ret = Array.New()
	for i = 1, self.size do
		local item = self.items[i]
		if equal_func(item) then
			ret:PushBack(item)
		end
	end
	return ret
end

--[[
二分查找方式插入一项, 用于快速构建一个排序数组
@para1 item in 插入到数组中的项
@para2 comp_func in 比较函数
@author deadline
@create 5/18/2012
--]]
--[[
function Array:BinaryInsert(item, comp_func)
	SortTools.BinaryInsert(self.items, item, comp_func)
end
--]]

function Array.GetArray(array,begin_pos,end_pos)

	if array == nil then
		return {}
	end

	local ret = {}
	for i=begin_pos,end_pos do

		if i > #(array) then
			break
		end

		table_insert(ret,array[i])
	end

	return ret
end


function Array.GetArrayByScale(array,scale,length)
	if array == nil then
		return {}
	end

	local ret = {}
	local begin_pos = math.floor(#(array)*scale) + 1
	local end_pos = begin_pos+length-1
	return Array.GetArray(array,begin_pos,end_pos)
end

--[[
对数组执行排序操作
@param arg 需要比较的表项中的多个key，
	   跟key对应的排序方式（升序:Array.UPPER还是降序:Array.LOWER）
@传参格式：arg = {{key1,key2,...},{Array.UPPER,Array.LOWER,...}}
		   不填写排序方式默认为升序Array.UPPER
@author jkz
@create 12/18/2013
--]]
function Array:sortOn( ... )
	local arg = {...}

	local sort_func = nil
	if arg == nil or arg[1] == nil then
		self:UpperSort()
		return
	elseif type(arg[1]) ~= "table" or #(arg[1]) <= 1 then 
		if arg[2] == nil or arg[2] == Array.UPPER then
		 self:UpperSort(arg[1])
		else
		 self:LowerSort(arg[1])
		end
		return
	end
	SortTools.MoreKeysSorter(self.items, arg[1],arg[2])
end

