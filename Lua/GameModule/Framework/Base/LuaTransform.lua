--[[@------------------------------------------------------------------
说明: Transform组件的lua拓展
作者: VinChy
----------------------------------------------------------------------]]
LuaTransform =
{            
    _position = nil,
    _localPosition = nil,
    _eulerAngles = nil,
    _localEulerAngles = nil,
    _rotation = nil,
    _localRotation = nil,
    _localScale = nil
}
LuaTransform.__index = LuaTransform
setmetatable(LuaTransform,{__call = function(self,...)
    return LuaTransform.Extend(...)
end})
function LuaTransform.Extend(u_transform)        
    local peer = {}            
    setmetatable(peer, LuaTransform)
    peer:Init(u_transform)                  
    tolua.setpeer(u_transform, peer)                                
    return u_transform
end

function LuaTransform:Init(u_transform)
    self._position = u_transform.position
    self._localPosition = u_transform.localPosition
    self._eulerAngles = u_transform.eulerAngles
    self._localEulerAngles = u_transform.localEulerAngles
    self._rotation = u_transform.rotation
    self._localRotation = u_transform.localRotation
    self._localScale = u_transform.localScale
end

--重写同名函数
function LuaTransform:Find(...)            
    print('child Find')
    return self.base:Find(...)
end

local get = tolua.initget(LuaTransform)
local set = tolua.initset(LuaTransform)       
get.position = function(self)
    return self._position
end
set.position = function(self, v)
    if self._position ~= v then                                  
        self._position = v                
        self.base.position = v                                            
    end
end

get.localPosition = function(self)
    return self._localPosition
end
set.localPosition = function(self, v)
    if self._localPosition ~= v then                                  
        self._localPosition = v                
        self.base.localPosition = v                                            
    end
end

get.eulerAngles = function(self)
    return self._eulerAngles
end
set.eulerAngles = function(self, v)
    if self._eulerAngles ~= v then                                  
        self._eulerAngles = v                
        self.base.eulerAngles = v                                            
    end
end

get.localEulerAngles = function(self)
    return self._localEulerAngles
end
set.localEulerAngles = function(self, v)
    if self._localEulerAngles ~= v then                                  
        self._localEulerAngles = v                
        self.base.localEulerAngles = v                                            
    end
end

get.rotation = function(self)
    return self._rotation
end
set.rotation = function(self, v)
    if self._rotation ~= v then                                  
        self._rotation = v                
        self.base.rotation = v                                            
    end
end

get.localRotation = function(self)
    return self._localRotation
end
set.localRotation = function(self, v)
    if self._localRotation ~= v then                                  
        self._localRotation = v                
        self.base.localRotation = v                                            
    end
end

get.localScale = function(self)
    return self._localScale
end
set.localScale = function(self, v)
    if self._localScale ~= v then                                  
        self._localScale = v                
        self.base.localScale = v                                            
    end
end