LoginView = LoginView or BaseClass(BaseView)
local LoginView = LoginView

function LoginView:__init()
	self.base_file = "account"
    self.layout_file = "LoginView"
    self.layer_name = "Main"
    self.close_mode = CloseMode.CloseDestroy
    self.destroy_imm = true
    self.use_background = false

    self.load_callback = function ()
        self:LoadSuccess()
    end
    self.open_callback = function ()
        
    end
    self.close_callback = function ()
        
    end
    self.destroy_callback = function ()
        
    end
end

function LoginView:__delete()
	
end

function LoginView:LoadSuccess()
	self.startBtn = self.transform:Find("StartBtn").gameObject

	local function onClicked()
		print("click...")
        TerrainManager:GetInstance():LoadTerrain()
        self:Close()
	end
	AddClickEvent(self.startBtn,onClicked)
end