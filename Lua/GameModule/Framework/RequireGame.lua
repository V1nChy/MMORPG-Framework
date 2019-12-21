local require = require
require("Game.UIView.Login.LoginController")
require("Game.UIView.MainUI.MainUIController")

require("Game.Gameplay.TerrainManager")

RequireGame = RequireGame or {}
function RequireGame.Init()
	LoginController.New()
	MainUIController.New()
end