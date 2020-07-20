
--[[Soviet Mission 06: Flying to Moon Script]]

SovietInitForce1 = { "htnk" } --[[Rhino Tank x 1, Soviet MCV x 1, Cosmonaut x 5]]
SovietInitForce2 = { "smcv" } 
SovietInitForce3 = { "lunr", "lunr", "lunr", "lunr", "lunr" } 

YuriInitForce1 = { "tele" } --[[Magnetron x 1, Master Mind x 1]]
YuriInitForce2 = { "mind" }
YuriProductionTypeInfantry = { "lunr" } --[[ 70% percent chance ]]
YuriProductionTypeVehicle = { "ytnk", "ltnk" } --[[ 30% percent chance ]]

YuriIncome = 0
YuriProductionInitDelay = 0

if Map.LobbyOption("difficulty") == "easy" then
	YuriIncome = 10
	YuriProductionDelay = DateTime.Second(15)
	
elseif Map.LobbyOption("difficulty") == "normal" then
	YuriIncome = 50
	YuriProductionDelay = DateTime.Second(10)

elseif Map.LobbyOption("difficulty") == "hard" then
	YuriIncome = 100
	YuriProductionDelay = DateTime.Second(5)

else
	YuriIncome = 5
	YuriProductionDelay = DateTime.Second(20)
end

LaunchSovietForce = function()
	--[[TODO: Add a movement path for these actors]]
	Utils.Do(SovietInitForce1, function(unitType)
		Actor.Create(unitType, true, { Location = SovietSpawnPoint1.Location, Facing = Facing.NorthEast, Owner = player })
	end)
	
	Utils.Do(SovietInitForce2, function(unitType)
		Actor.Create(unitType, true, { Location = SovietSpawnPoint2.Location, Facing = Facing.NorthEast, Owner = player })
	end)
	
	Utils.Do(SovietInitForce3, function(unitType)
		Actor.Create(unitType, true, { Location = SovietSpawnPoint3.Location, Facing = Facing.NorthEast, Owner = player })
	end)
end

MoveCameraTo = function(position)
	Camera.Position = position
end

MissionAccomplished = function()
	Media.PlaySoundNotification(player, "Cheer")
	Media.PlaySoundNotification(player, "Cheer")
	Media.PlaySoundNotification(player, "Cheer")
	Media.PlaySoundNotification(player, "Cheer")
	Media.PlaySpeechNotification(player, "MissionAccomplished")
end

MissionFailed = function()
	Media.PlaySpeechNotification(player, "MissionFailed")
end

CheckBaseHasBuilt = function()
	actors = player.GetActors()
	isFinishObjective1 = Utils.Any(actors, function(actor) return actor.Type == "nacnst" end)
	if isFinishObjective1 then
		destroyYuriMoonCommandCenter = player.AddPrimaryObjective("Find and destroy Yuri's Moon command center")
		destroyYuriForces = player.AddPrimaryObjective("Destroy Yuri forces")
		player.Cash = 80000
		yuri.Cash = 1000000
		player.MarkCompletedObjective(buildBaseObjective)
	end
end

CheckYuriCommandCenter = function()
	if YuriCommandCenter.IsDead then
		player.MarkCompletedObjective(destroyYuriMoonCommandCenter)
	end
end

CheckYuriHasDestroyed = function()
	actors = yuri.GetActors()
	local isAllDead = Utils.All(actors, function(actor) return actor.IsDead end)
	if isAllDead then
		player.MarkCompletedObjective(destroyYuriForces)
	end
end

YuriIncomeTick = function()
	yuri.cash = yuri.cash + YuriIncome
end

YuriProductionTick = function()
	
	if YuriProductionTypeInfantry then
		Trigger.AfterDelay(InfantryDelay, YuriInfantryProduction)
	end

	if YuriProductionTypeVehicle then
		Trigger.AfterDelay(VehicleDelay, YuriVehicleProduction)
	end
end

YuriInfantryProduction = function()
	local toBuild = { Utils.Random(YuriProductionTypeInfantry) }
	
	yuri.Build(toBuild, function(unit)
			var randomActor = GetPlayerRandomActor()
			unit.Attack(randomActor)
	end)
end

YuriVehicleProduction = function()
	local toBuild = { Utils.Random(YuriProductionTypeVehicle) }
	
	yuri.Build(toBuild, function(unit)
			var randomActor = GetPlayerRandomActor()
			unit.Attack(randomActor)
	end)
end

GetPlayerRandomActor = function()
	var actors = player.GetActors();
	var randomActor = Utils.Random(actors)
	return randomActor
end

Tick = function()
	if not isFinishObjective1 then
		CheckBaseHasBuilt()
	end
	CheckYuriCommandCenter()
	CheckYuriHasDestroyed()
	
	YuriProductionTick()
	YuriIncomeTick()
end

WorldLoaded = function()
	player = Player.GetPlayer("Soviet")
	yuri = Player.GetPlayer("Yuri")

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.PlaySpeechNotification(player, "NewMissionObjectiveReceived")
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective", player.Color)
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.PlaySpeechNotification(player, "PrimaryObjectiveAchieved")
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed", player.Color)
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed", player.Color)
	end)

	Trigger.OnPlayerLost(player, MissionFailed)
	Trigger.OnPlayerWon(player, MissionAccomplished)
	
	buildBaseObjective = player.AddPrimaryObjective("Find a suitable place and build base")
	
	isFinishObjective1 = false
	
	--[[TODO: Add Init Yuri uints to play a show with us like the original]]
	--[[      And add flash effect]]
	
	MoveCameraTo(CommandCenterPoint.CenterPosition)
	
	Trigger.AfterDelay(DateTime.Minutes(8), function()
		MoveCameraTo(SovietSpawnPoint2.CenterPosition)
	end)
	
	LaunchSovietForce()
end
