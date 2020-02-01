--每创建一个新类 就递增1  保存每个类创建的顺序id
_in_ctype_count = _in_ctype_count or 0
--通过类的创建顺序id来保存每一个类
_in_ctype_map = _in_ctype_map or {}
--每创建一个新的对象 就递增1 在New中调用  保存每个对象创建的顺序id
_in_obj_ins_id = _in_obj_ins_id or 0
--保存每个类对应实例化的n个对象
_in_obj_ins_map = _in_obj_ins_map or {}
--保存每个类实例化对象的总个数
_in_obj_count_map = _in_obj_count_map or {}
--保存每个类作为不一样类的父类的个数
_be_super_count_map = _be_super_count_map or {}

local _in_ctype_count = _in_ctype_count
local _in_ctype_map = _in_ctype_map
local _in_obj_ins_map = _in_obj_ins_map
local _in_obj_count_map = _in_obj_count_map
local _be_super_count_map = _be_super_count_map

local setmetatable = setmetatable
local debug_getinfo = debug.getinfo

--先调用基类的init函数，依次往上层次调用派生类
local function createFunc(class, obj, ...)
	if class.super then
		createFunc(class.super, obj, ...)
	end
	if class.__init then
		class.__init(obj, ...)
	end
end

--先调用本身的deleteme函数，再依次往下调用基类的
local function deleteMeFunc(self)
	if self._use_delete_method then
		return
	end
	self._use_delete_method = true

	local now_super = self._class_type 
	while now_super ~= nil do	
		if now_super.__delete then
			now_super.__delete(self)
		end
		now_super = now_super.super
	end

	--清理该类所有的资源引用计数
	lua_resM:clearReference(self)
end

local function getInstance(class)
	if class.Instance == nil then
		class.Instance = class.New()
	end
	return class.Instance
end

function BaseClass(super, use_class_type)
	local class_type = 
	{
		__init   = false,
		__delete = false,
		New      = false,
		_source  = false,
		__index  = false,
		super    = false,
	}

	_in_ctype_count = _in_ctype_count + 1
	_in_ctype_map[_in_ctype_count] = class_type

	local cls_obj_ins_map = {}
	_in_obj_ins_map[class_type] = cls_obj_ins_map
	setmetatable(cls_obj_ins_map, {__mode = "v"})

	_in_obj_count_map[class_type] = 0

	local info = debug_getinfo(2, "Sl")
	class_type._source = info.source
	class_type.super = super

	if _in_ctype_count == 1 then  --设置为弱引用  只需设置一次
		setmetatable(_in_ctype_map, {__mode = "v"})
		setmetatable(_in_obj_ins_map, {__mode = "k"})
		setmetatable(_in_obj_count_map, {__mode = "k"})
		setmetatable(_be_super_count_map, {__mode = "k"})
	end

	if super then  --如果有引用父类 则该对象递增1
		if _be_super_count_map[super] == nil then
			_be_super_count_map[super] = 0
		end
		_be_super_count_map[super] = _be_super_count_map[super] + 1
	end

	if use_class_type then
		class_type.GetInstance = getInstance
	end

	class_type.New = function(...)
		local obj = nil
		if not use_class_type then
			_in_obj_ins_id = _in_obj_ins_id + 1

			if class_type.__defineVar then  --一次性生成该对象所要的属性  减少消耗
				obj = class_type:__defineVar()
			else
				obj = 
				{
					_class_type = class_type,
					_iid = _in_obj_ins_id,
					DeleteMe = nil,
					_use_delete_method = false
				}
			end

			local function newFunc(t, k)
				local ret = class_type[k]
				obj[k] = ret
				return ret
			end
			setmetatable(obj, {__index = newFunc})
		else
			obj = class_type
			obj._class_type = class_type
			obj.Instance = obj
		end

		cls_obj_ins_map[_in_obj_ins_id] = obj --save here for mem debug
		_in_obj_count_map[class_type] = _in_obj_count_map[class_type] + 1

		createFunc(class_type, obj, ...)
		obj.DeleteMe = deleteMeFunc

		if OpenCreateObjOrClass then
			print("create new class ",info.source)
		end
		return obj
	end

 	--如果该类中没有的方法 则通过元表来调用父类的该方法
	if super then
		local function superFunc(t, k)
			local ret = super[k]
			class_type[k] = ret
			return ret
		end
		setmetatable(class_type, {__index = superFunc })
	end
 
	return class_type
end