local require = require
require("Game.UIView.MainUI.JoystickView")

MainUIController = MainUIController or BaseClass(BaseController, true)
local MainUIController = MainUIController

function MainUIController:__init()
	print("MainUIController:__init()")
end

function MainUIController:OpenView()
	if self.joystick == nil then
		self.joystick = JoystickView.New()
	end
	self.joystick:Open()
end