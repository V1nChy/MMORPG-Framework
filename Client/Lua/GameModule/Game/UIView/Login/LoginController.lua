local require = require
require("Game.UIView.Login.LoginView")

LoginController = LoginController or BaseClass(BaseController, true)
local LoginController = LoginController

function LoginController:__init()
	print("LoginController:__init()")
end

function LoginController:OpenView()
	if self.login_view == nil then
		self.login_view = LoginView.New()
	end
	self.login_view:Open()
end