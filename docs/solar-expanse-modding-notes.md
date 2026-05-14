# Solar Expanse Modding Notes

These notes document what was discovered while converting the original
LifeSupportTracker mod into FleetTracker. They are meant as working notes for
future BepInEx/Harmony mods against the current Solar Expanse assembly.

## Runtime And Build Context

- The game is a Unity/Mono game and can be modded with BepInEx plus Harmony.
- This mod targets `net472`.
- The project references game assemblies from `MAC_SOLAR_EXPANSE_ROOT`:
  - `$MAC_SOLAR_EXPANSE_ROOT/BepInEx/core/BepInEx.dll`
  - `$MAC_SOLAR_EXPANSE_ROOT/BepInEx/core/0Harmony.dll`
  - `$MAC_SOLAR_EXPANSE_ROOT/Solar Expanse_Data/Managed/Assembly-CSharp.dll`
  - Unity assemblies from `$MAC_SOLAR_EXPANSE_ROOT/Solar Expanse_Data/Managed/`
- On this machine the installed CrossOver Steam copy had the game assembly at:
  - `/Users/niklas/Library/Application Support/CrossOver/Bottles/Steam/drive_c/Program Files (x86)/Steam/steamapps/common/Solar Expanse/Solar Expanse_Data/Managed/Assembly-CSharp.dll`
- The repo-level `.mise.toml` defines the default macOS CrossOver path for `MAC_SOLAR_EXPANSE_ROOT`.
- Useful metadata tools were not installed on PATH during the original conversion work:
  - `dotnet`
  - `monodis`
  - `ilspycmd`
  - `mono`
- The available fallback was direct .NET metadata inspection plus `strings`.

## Safe Coexistence Between Mods

To make two mods usable together, do not reuse identity or UI object names:

- Give each mod its own `BepInPlugin` GUID.
- Give each mod its own Harmony ID.
- Give each built DLL a distinct assembly name.
- Give injected Unity objects distinct names.
- Avoid anchoring panels/buttons to the exact same default screen position.

FleetTracker uses:

- Plugin GUID: `com.mod.solarexpanse.fleettracker`
- Harmony ID: `com.mod.solarexpanse.fleettracker`
- Assembly name: `FleetTracker`
- UI objects:
  - `modFleetTrackerButton`
  - `modFleetTrackerPanel`

The old LifeSupportTracker used different IDs and `modLifeSupport...` UI names,
so both can exist in the same BepInEx plugin directory.

## Reliable UI Injection Point

The current mod injects UI from:

- `Manager.NotificationManager.Awake`

The game notification UI is a useful anchor because it is present in normal
gameplay and already has the correct Canvas/font/style references.

Useful private fields on `NotificationManager`:

- `showNotificationHistory`: the notification history toggle `Button`
- `notificationHistory`: a `GameObject` that can be cloned as a styled panel

Practical pattern:

1. Harmony postfix `NotificationManager.Awake`.
2. Reflect `showNotificationHistory` and `notificationHistory`.
3. Get the parent `Canvas` from the notification button.
4. Clone `notificationHistory`.
5. Remove its original children.
6. Rebuild your own panel content under it.
7. Copy an existing scrollbar style or use a simple fallback.
8. Create a separate draggable button in the same Canvas.

Useful UI opening calls:

- `UIManager.Instance.Open(EWindowType.ObjectInfo, objectInfo)`
- `UIManager.Instance.Open(EWindowType.SpaceCraftInfo, spacecraft)`

## Core Managers

### `Manager.GameManager`

Useful singleton:

```csharp
MonoBehaviourSingleton<GameManager>.Instance
```

Useful members:

- `Player`: current player `Game.Company`
- `Companies`: list of companies
- `Economic`: economy helper, used by the old life-support tracker

### `Manager.ObjectInfoManager`

Useful singleton:

```csharp
MonoBehaviourSingleton<ObjectInfoManager>.Instance
```

Useful members:

- `allObjectInfos`: all known `Game.Info.ObjectInfo` objects
- `GetByID(int)`
- `GetByName(string)`

`ObjectInfo` has important object/body data:

- `ObjectName`
- `ImagePlanetUI`
- `ID`
- `GetObjectInfoData(company)`

### `Manager.ShipManager`

Useful singleton:

```csharp
MonoBehaviourSingleton<ShipManager>.Instance
```

Useful member:

- `ListAllSpaceShip`: all live `CustomUpdate.Spacecraft` objects

### `Manager.MissionInfoManager`

Useful singleton:

```csharp
MonoBehaviourSingleton<MissionInfoManager>.Instance
```

Useful member:

- `ListMissionInfo`: active/planned mission info objects

Useful methods:

- `CreateMissionInfo(...)`
- `AddMissionInfo(MissionInfo)`
- `RemoveMissionInfo(MissionInfo)`
- `GetNewMissionID()`

### `TimeController`

Useful singleton:

```csharp
MonoBehaviourSingleton<TimeController>.Instance
```

Useful member:

- `CurrentTime`: in-game `DateTime`

## Companies

Type:

- `Game.Company`

Useful members:

- `IsPlayer`
- `ID`
- `mainObjectInfo`
- `HullList`
- `SpaceCraftConstructDataListUnlock`
- `IsUnlockSpacecraftType(...)`
- `SaveRocketProject(...)`
- `AddRocketProject(...)`
- `MakeCompanyLVsAndSpacecraftsDataSave(...)`
- `LoadMission(...)`
- `LoadCyclicalMission(...)`
- `GetListSpacecraftAndConstructedPlanned(...)`

For player-only filtering, prefer:

```csharp
company != null && (company.IsPlayer || ReferenceEquals(company, player))
```

## Spacecraft

Type:

- `CustomUpdate.Spacecraft`

Useful fields/properties:

- `spacecraftName`
- `spacecraftType`
- `CurrentPhase`
- `CargoAll`
- `MissionStart`
- `MissionTarget`
- `CurrentlyOnThisObject`
- `TrajectoryObject`
- `FacilityQuantityIndex`
- `HaveLandingTime`
- `DateFinishRepair`
- `CycleMissionsData`
- `CraftCyclicalMissionController`

Useful methods:

- `GetSpacecraftName()`
- `GetCompany()`
- `GetMissionInfo()`
- `GetMissionInfo(PMMissionParameter)`
- `GetLifeSupportCurrentWhenFly(...)`
- `PlanMission(...)`
- `CancelMission(MissionInfo)`
- `IsPlanMission()`
- `Launch()`
- `SetCurrentlyOnThisObject(...)`
- `SetTabCargo(CargoAll)`

Known/observed `EPhase` values include:

- `Idle`
- `Fly`
- `Launch`
- `Landing`

For fleet tracking, `Fly`, `Launch`, and `Landing` are treated as transit
phases. Other phases are treated as parked/at-body unless the game exposes a
more specific state.

Spacecraft type:

- `Data.ScriptableObject.SpacecraftType`

Useful members:

- `Name`
- `NameRocketType`
- `SpriteId`
- `TimeToBuildInDays`
- `CargoCapacity`
- `FuelCapacity`
- `MAXLifeSupport`
- `BuildOnlyLowOrbit`
- `CanByBuildByUser`
- `AfterBuildTeleportLO`
- `OrbitSC`
- `LowOrbitContainer`
- `AsteroidPullingShip`
- `IsInterstellarShip`

## Missions

Type:

- `Game.Info.MissionInfo`

Useful fields/properties:

- `DateArrive`
- `DateLaunch`
- `start`
- `target`
- `cargoAll`
- `complete`
- `cancel`
- `company`
- `missionName`
- `id`
- `listSpacecraftInfo2`
- `spacecraftInfo`
- `spacecraftInfo2`
- `ListSpacecraftInfo2`
- `ListLaunchVehicleInfo2`
- `costFuel`
- `optimalCostFuel`
- `deltaV`

Useful methods:

- `Cancel()`
- `CancelAllDependsMissionInfo()`
- `UpdateDate()`
- `GetChainMissionInfoAfterThisMI()`
- `GetChainMissionInfoGravityAssists()`

Mission classification pattern:

- If `cancel` or `complete` is true, ignore it for active fleet display.
- If `DateLaunch > CurrentTime`, it is a planned transit.
- If `DateLaunch <= CurrentTime <= DateArrive`, it is in transit.
- If `DateArrive < CurrentTime`, it has already arrived and can usually be
  omitted from live fleet lists.

Mission ownership:

- Prefer `MissionInfo.company` when present.
- If company is unavailable, inspect assigned spacecraft info objects and call
  `GetCompany()` on them.

Assigned spacecraft:

- `MissionInfo.ListSpacecraftInfo2`
- `MissionInfo.spacecraftInfo2`
- `MissionInfo.spacecraftInfo`

These can be `ISpacecraftInfo`, `SpacecraftInfo`, or sometimes wrappers/fakes,
so reflection is safer than relying on one concrete class.

## Planned Mission Parameters

Type:

- `Game.UI.Windows.Elements.PlanMissionElements.PMMissionParameter`

Useful members seen in metadata:

- `FlyCompany`
- `Start`
- `Target`
- `CargoAll`
- `SC`
- `SCList`
- `SCCount`
- `LV`
- `LVList`
- `DepartureTimeDate`
- `Arrival`
- `MissionName`
- `MissionID`
- `AllFuelNeed`
- `OptimalFuelNeed`
- `LifeSupportNeed`
- `SupplyNeed`
- `FlightCost`
- `MissionCreator`
- `CostType`
- `ForCyclicalMission`
- `CycleMissionsDataData`

This type is useful if a mod wants to create or validate a mission rather than
only read already-created `MissionInfo` objects.

## Cargo

Live cargo type:

- `Game.ObjectInfoDataScripts.CargoAll`

Save/data cargo type:

- `Game.ObjectInfoDataScripts.CargoAllData`

Both represent similar concepts but with different list element types.

Useful `CargoAll` members:

- `listCargo`
- `listCargoToOrbit`
- `listCargoGravityAssists`
- `cargoFuel`
- `CargoCurrent`
- `CargoCurrentFuel`
- `FreeSpace`
- `GetTotalFuelInCargo()`
- `GetSupplyFromCargo()`
- `GetLifeSupportFromCargoSupply()`

Useful `CargoAllData` members:

- `listCargoData`
- `listCargoDataToOrbit`
- `listCargoGravityAssists`
- `cargoFuel`
- `entireAsteroid`
- `toReset`

Cargo item fields/properties observed through metadata/strings:

- `cargoMass`
- `resourceType`
- `moduleData`
- `crewValue`
- `lifeSupportValue`
- `crew`

Cargo display logic that worked well:

1. Iterate all cargo lists.
2. For each item, read `resourceType` or `moduleData`.
3. Use the referenced object's `Name` property when present.
4. Read `cargoMass` for tonnage.
5. Read `crewValue` for crew/human counts.
6. Include `cargoFuel` separately.

## Bodies, Colonies, And Object Data

Type:

- `Game.ObjectInfoDataScripts.ObjectInfoData`

Get it from:

```csharp
ObjectInfoData data = objectInfo.GetObjectInfoData(company);
```

Useful members:

- `ObjectInfo`
- `ListRowResourcesData`
- `ProductionItem`
- `ListFacility`
- `ListSpaceCrafts`
- `ConstructionEquipmentCount`
- `CumulativeConstructionPower`
- `CumulativeVehicleAssemblyPower`
- `VehicleAssemblyCount`
- `VehicleAssemblyCountEnable`
- `CurrentCrew`
- `ProductionWasStalledDueToLackOfLifeSupport`

Useful methods:

- `GetListRocketConstruct()`
- `GetProductionItemSCLV()`
- `WhenBuild(ProductionItem)`
- `NeedConstructionEquipment()`
- `IsFacilityOrBuildingInProduction()`
- `GetListSpacecraftAndConstructed(...)`
- `GetListSpacecraftAndConstructedPlanned(...)`
- `GetListSpacecraftFacility(...)`
- `AddRocketToConstruct(Data.SpacecraftConstructData)`
- `RocketConstructDataOnFinishConstruction(ProductionItem)`
- `AddSpacecraft(Spacecraft)`
- `RemoveSpacecraft(Spacecraft)`
- `CheckResources(...)`
- `AddResources(...)`
- `RemoveResource(...)`

The old LifeSupportTracker used:

- `ObjectInfoManager.allObjectInfos`
- `ObjectInfo.GetObjectInfoData(player)`
- `ObjectInfoData.CurrentCrew`
- `ObjectInfoData.CheckResources(resourceDefinition)`
- `ObjectInfoData.GetSupplyDemandPerDay()`
- `ObjectInfoData.GetPopulationHabitats()`
- `ObjectInfoData.ListRowResourcesData`

## Construction Queues

There are two relevant concepts:

### Planned/construct data

Type:

- `Data.SpacecraftConstructData`

Useful members:

- `SpacecraftType`
- `SpaceCraftName`
- `SpacecraftBuild`
- `LaunchVehicleBuild`
- `TimeToBuildInDays()`
- `FindProductionItemType()`
- `GetSpacecraftName()`
- `GetObjectInfo()`
- `GetObjectInfoPlan()`
- `GetCompany()`
- `GetMissionInfo()`
- `HasPlannedMissions()`
- `HaveMissions()`
- `CancelBuild()`
- `SetBuildSpaceCraft(Spacecraft)`

### Active production item

Type:

- `Game.ObjectInfoDataScripts.ProductionItem`

Useful members:

- `BuildProgress`
- `FinishConstructionBool`
- `Company`
- `ID`
- `ProductionItemType`
- `WhenBuild`
- `StartBuild`
- `ObjectInfoData`
- `TimeToBuildInDays()`
- `CancelBuild()`

Useful construction pattern:

1. For each body, get `ObjectInfoData`.
2. Call `GetListRocketConstruct()` to get `Data.SpacecraftConstructData`.
3. Call `GetProductionItemSCLV()` to get `ProductionItem` objects for vehicle
   production.
4. Match construct data to production items by comparing:
   - `construct.FindProductionItemType()`
   - `productionItem.ProductionItemType`
5. If a matched `ProductionItem` exists:
   - `StartBuild` tells whether it has started.
   - `BuildProgress` provides progress.
   - `WhenBuild` or `ObjectInfoData.WhenBuild(productionItem)` can expose the
     finish date.
6. If no production item is matched:
   - It is likely queued/planned.
   - `TimeToBuildInDays()` can provide a duration, but not necessarily a true
     queue-adjusted finish date.

Important caveat:

- Queued construction can be difficult to estimate correctly because the finish
  date may depend on earlier queued items, construction/vehicle assembly power,
  resources, and whether construction has started. A mod should display unknown
  or planned status rather than pretending to know a precise date.

## Object Info Fleet Rows

Types:

- `Game.UI.Windows.Elements.ObjectInfoElements.RowRocketData`
- `Game.UI.Windows.Elements.ObjectInfoElements.StackedRowRocketData`

Useful members on `RowRocketData`:

- `spacecraftInfoFake`
- `spacecraft`
- `SpacecraftInfo`
- `rConstruct`
- `GetSpacecraftInfo()`
- `GetProductionItem()`

`ObjectInfoData.GetListSpacecraftAndConstructed(...)` and
`GetListSpacecraftAndConstructedPlanned(...)` return lists of these rows.

These rows are useful if a mod wants to mimic the game's own object-info
vehicle lists. For FleetTracker, direct manager access was simpler.

## Save Data Field Names

The neighboring save-analysis project confirmed these save fields:

- Company list: `companyDataSave`
- Company spacecraft list: `spacecrafts`
- Company one-way missions: `listMission`
- Company cyclical missions: `listMissionCyclical`
- Spacecraft save object:
  - `idObjectInfo`
  - `idObjectTruly`
  - `spacecraftType`
  - `ID`
  - `spacecraftName`
  - `cargoAllData`
- Mission save object:
  - `missionID`
  - `scID`
  - `sclistID`
  - `start`
  - `target`
  - `departureTimeDate`
  - `arrival`
  - `cargoAllData`
  - `allFuelNeed`
  - `optimalFuelNeed`
  - `cancel`
- Cyclical mission save object:
  - `scIDList`
  - `A`
  - `B`
  - `Pause`
  - `Ends`
  - `TransferType`
  - `CargoStart`
  - `CargoEnd`
  - `CountMission`
  - `CountMax`

These names are useful for offline save parsing and also as hints for runtime
field names.

## Practical Reflection Guidance

Some useful members are public, but several are private fields. Reflection is
pragmatic for UI mods because it avoids tight coupling to one access pattern.

Suggested helper behavior:

- Try properties first.
- Then try fields.
- Use `BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic`.
- Case-insensitive lookup is useful because game fields/properties mix casing.
- For methods, match by name and argument count.
- Wrap reflection reads in small `try/catch` blocks so UI refresh does not break
  the whole panel.

## Known Limitations And Risk Areas

- The exact game version matters. These notes were derived from the local
  `Assembly-CSharp.dll` available on May 14, 2026.
- Build verification could not run in this shell because `dotnet` was not on
  PATH.
- Some runtime types are wrappers or fake UI objects. For example, spacecraft
  may appear as `Spacecraft`, `SpacecraftInfo`, `ISpacecraftInfo`, or fake info
  objects.
- Construction finish dates are only reliable when the game exposes a matched
  active `ProductionItem` with a `WhenBuild` value.
- Cargo can be live (`CargoAll`) or save/data (`CargoAllData`), so cargo readers
  should support both list naming schemes.
- Display names often come from `Name` properties on scriptable objects. These
  may be localized or dynamically generated.
- Cloning existing game UI is effective, but mods should remove inherited
  children/components they do not use, especially old layout groups and scroll
  rects.
