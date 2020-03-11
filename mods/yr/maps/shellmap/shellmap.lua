allies_unit_types_1_1 = {"mtnk", "mtnk", "mtnk"} --[[Grizzy Battle Tank x 3, Mirage Tank x 2]]
allies_unit_types_1_2 = {"mgtk", "mgtk"}
allies_unit_types_2_1 = {"mtnk", "mtnk"} --[[Grizzy Battle Tank x 2, IFV x 2, Tank Destroyer x 1]]
allies_unit_types_2_2 = {"fv", "fv"}
allies_unit_types_2_3 = {"tnkd"}

sov_unit_type = {"schp"} --[[Soviets only send air force in this operation]]

BindActorTriggers = function(a)
	a.Attack(PsychicBeacon) --[[Allies and Soviets need to destroy the Psychic Beacon firstly , or they will be controlled by Yuri]]
end

SendAlliesUnits = function(entryCell, unitTypes, interval)
	local units = Reinforcements.Reinforce(allies, unitTypes, { entryCell }, interval)
	Utils.Do(units, function(unit)
		BindActorTriggers(unit)
	end)
	Trigger.OnAllKilled(units, function() SendAlliesUnits(entryCell, unitTypes, interval) end)
end

SendSovietUnits = function(entryCell, unitTypes, interval)
	local units = Reinforcements.Reinforce(soviets, unitTypes, { entryCell }, interval)
	Utils.Do(units, function(unit)
		BindActorTriggers(unit)
	end)
	Trigger.OnAllKilled(units, function() SendSovietUnits(entryCell, unitTypes, interval) end)
end

RepairBeacon = function()
	PsychicBeacon.StartBuildingRepairs(yuri) --[[Try to repair the beacon]]
end

Tick = function()
	if PsychicBeacon.IsDead then
		return
	end
	RepairBeacon()
	
	--[[Prevent Yuri from becoming poor and unable to repair the beacon]]
	yuri.Cash += 10
end

WorldLoaded = function()
	Camera.Position = PsychicBeacon.CenterPosition
	
	allies = Player.GetPlayer("Allies")
	soviets = Player.GetPlayer("Soviets")
	yuri = Player.GetPlayer("Yuri")
	
	SendAlliesUnits(allieswaypoint1.Location, allies_unit_types_1_1, 2000)
	SendAlliesUnits(allieswaypoint1.Location, allies_unit_types_1_2, 2000)
	SendAlliesUnits(allieswaypoint2.Location, allies_unit_types_2_1, 2000)
	SendAlliesUnits(allieswaypoint2.Location, allies_unit_types_2_2, 2000)
	SendAlliesUnits(allieswaypoint2.Location, allies_unit_types_2_3, 2000)
	SendSovietUnits(sovwaypoint1.Location, sov_unit_type, 6000)
	SendSovietUnits(sovwaypoint2.Location, sov_unit_type, 6000)
	SendSovietUnits(sovwaypoint3.Location, sov_unit_type, 6000)
end