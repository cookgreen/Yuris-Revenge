AlliedForce = {allied_grandcannon01, allied_grandcannon02, allied_destroyer01, allied_destroyer02, allied_destroyer03, allied_destroyer04, allied_carrier01, allied_carrier02, allied_aiegs01 }

Tick = function()
	CheckAlliedForce()
	CheckPower()
end

SendSovietReforcement = function()
	--[[TODO: Send Soviet Troopers]]
end

CheckAlliedForce = function()
	local deadnum = 0
	local totalnum = 0
	Utils.Do(AlliedForce, function(alliedunit)
		totalnum = totalnum + 1
		if alliedunit.IsDead then
			deadnum = deadnum + 1
	end)
	if deadnum == totalnum then --[[All dead, Objective 1 was completed]]
		player.MarkCompletedObjective(DestroyAlliedForceObjective)
		SendSovietReforcement()
end

CheckPower = function()
end

MissionAccomplished = function()
	Media.PlaySpeechNotification(player, "MissionAccomplished")
end

MissionFailed = function()
	Media.PlaySpeechNotification(player, "MissionFailed")
end

InitObjectives = function()
	DestroyAlliedForceObjective = player.AddPrimaryObjective("Destroy the Allied Navy and Grand Cannons.")
	CaptureTimeMachineObjective = player.AddPrimaryObjective("Destroy Pill Boxes around Time Machine to capture it.")
	CaptureFourPowerPlantsObjective = player.AddPrimaryObjective("Capture 4 Power Plants to Power Time Machine.")
	DestroyPsychicDominatorObjective = player.AddPrimaryObjective("Take Control of Soviet Base and use it to Destroy the Psychic Dominator.")
end

WorldLoaded = function()
	allies = Player.GetPlayer("Allies")
	player = Player.GetPlayer("Soviets")
	yuri = Player.GetPlayer("Yuri")
	
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)
	
	Trigger.OnPlayerLost(player, MissionFailed)
	Trigger.OnPlayerWon(player, MissionAccomplished)
	
	InitObjectives()
end