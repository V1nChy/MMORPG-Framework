SystemMemoryLevel = SystemMemoryLevel or {}
SystemMemoryLevel.Low = 1
SystemMemoryLevel.Middle = 2
SystemMemoryLevel.Hight = 3
SystemMemoryLevel.Cur = nil

function SystemMemoryLevel.Init()
	local memory_size = math.floor(GetSystemMemorySize()/1024 + 0.5)
	local lowMem,mediumMem,highMem = 1,2,3
	if ApplicationPlatform == RuntimePlatform.Android then
		lowMem = 3
		mediumMem = 6
		highMem = 6
	elseif ApplicationPlatform == RuntimePlatform.IPhonePlayer then
		lowMem = 1
		mediumMem = 3
		highMem = 3
	end

	if memory_size < lowMem then
		SystemMemoryLevel.Cur = SystemMemoryLevel.Low    --iphone  <  1g  ( <= iphone4s ipadmini1)        android < 3g
	elseif memory_size < mediumMem then
		SystemMemoryLevel.Cur = SystemMemoryLevel.Middle --iphone  <  3g  ( >= iphone5 ipadmini2 )      3g <= android  < 6g 
	else
		SystemMemoryLevel.Cur = SystemMemoryLevel.Hight	 --iphone  >= 3g  ( >= iphone 7p)                android >= 6g 
	end
end